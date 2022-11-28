#! /usr/bin/env python3

###
# KINOVA (R) KORTEX (TM)
#
# Copyright (c) 2018 Kinova inc. All rights reserved.
#
# This software may be modified and distributed
# under the terms of the BSD 3-Clause license.
#
# Refer to the LICENSE file for details.
#
###
import math
import sys
import os
import time
import threading
import numpy as np

from kortex_api.autogen.client_stubs.BaseClientRpc import BaseClient
from kortex_api.autogen.client_stubs.BaseCyclicClientRpc import BaseCyclicClient

from kortex_api.autogen.messages import Base_pb2, BaseCyclic_pb2, Common_pb2



# Maximum allowed waiting time during actions (in seconds)
TIMEOUT_DURATION = 20

# Create closure to set an event after an END or an ABORT
def check_for_end_or_abort(e):
    """Return a closure checking for END or ABORT notifications

    Arguments:
    e -- event to signal when the action is completed
        (will be set when an END or ABORT occurs)
    """
    def check(notification, e = e):
        print("EVENT : " + \
              Base_pb2.ActionEvent.Name(notification.action_event))
        if notification.action_event == Base_pb2.ACTION_END \
        or notification.action_event == Base_pb2.ACTION_ABORT:
            e.set()
    return check

def example_cartesian_action_movement(base, base_cyclic, px, py, pz, rx, ry, rz):
    
    print("Starting Cartesian action movement ...")
    action = Base_pb2.Action()
    action.name = "Example Cartesian action movement"
    action.application_data = ""

    feedback = base_cyclic.RefreshFeedback()

    cartesian_pose = action.reach_pose.target_pose
    cartesian_pose.x = px # (meters)
    cartesian_pose.y = py # (meters)
    cartesian_pose.z = pz    # (meters)
    cartesian_pose.theta_x = rx # (degrees)
    cartesian_pose.theta_y = ry # (degrees)
    cartesian_pose.theta_z = rz # (degrees)

    e = threading.Event()
    notification_handle = base.OnNotificationActionTopic(
        check_for_end_or_abort(e),
        Base_pb2.NotificationOptions()
    )

    print("Executing action")
    base.ExecuteAction(action)

    print("Waiting for movement to finish ...")
    finished = e.wait(TIMEOUT_DURATION)
    base.Unsubscribe(notification_handle)

    if finished:
        print("Cartesian movement completed")
    else:
        print("Timeout on action notification wait")
    return finished

 
def example_move(base, name):
    # Make sure the arm is in Single Level Servoing mode
    base_servo_mode = Base_pb2.ServoingModeInformation()
    base_servo_mode.servoing_mode = Base_pb2.SINGLE_LEVEL_SERVOING
    base.SetServoingMode(base_servo_mode)
    
    # Move arm to ready position
    print("Moving the arm to a safe position")
    action_type = Base_pb2.RequestedActionType()
    action_type.action_type = Base_pb2.REACH_JOINT_ANGLES
    action_list = base.ReadAllActions(action_type)
    action_handle = None
    # print(action_list)
    for action in action_list.action_list:
        if action.name == name:
            action_handle = action.handle

    if action_handle == None:
        print("Can't reach safe position. Exiting")
        return False

    e = threading.Event()
    notification_handle = base.OnNotificationActionTopic(
        check_for_end_or_abort(e),
        Base_pb2.NotificationOptions()
    )

    base.ExecuteActionFromReference(action_handle)
    finished = e.wait(TIMEOUT_DURATION)
    base.Unsubscribe(notification_handle)

    if finished:
        print("Safe position reached")
    else:
        print("Timeout on action notification wait")
    return finished

stop_observe_feedback = False
def observe_feedback(base_cyclic):
    global stop_observe_feedback
    stop_observe_feedfack = False
    while not stop_observe_feedback:
        feedback = base_cyclic.RefreshFeedback()
        print('World Positoin(m) x {:.2f} y {:.2f} z {:.2f}'.format(feedback.base.tool_pose_x, feedback.base.tool_pose_y, feedback.base.tool_pose_z))
        print('World Eular Rotation(deg) x {:.2f} y {:.2f} z {:.2f}'.format(feedback.base.tool_pose_theta_x, feedback.base.tool_pose_theta_y, feedback.base.tool_pose_theta_z), end='\n\n')
        #print('Positoin x(m)', feedback.base.tool_pose_x , 'y(m)', feedback.base.tool_pose_y, 'z(m)', feedback.base.tool_pose_z, '\nEular x(deg)', feedback.base.tool_pose_theta_x, 'y(deg)', feedback.base.tool_pose_theta_y, 'z(deg)', feedback.base.tool_pose_theta_z, end='\n\n')
        time.sleep(0.1)

def example_twist_command(base, dx, dy, dz, angular_y):

    command = Base_pb2.TwistCommand()

    command.reference_frame = Base_pb2.CARTESIAN_REFERENCE_FRAME_MIXED
    command.duration = 0

    twist = command.twist
    twist.linear_x = dx # (m/s)
    twist.linear_y = dy # (m/s)
    twist.linear_z = dz # (m/s)
    twist.angular_x = 0 # (deg/s)
    twist.angular_y = angular_y # (deg/s)
    twist.angular_z = 0 # (deg/s)

    base.SendTwistCommand(command)

    return True


class GripperCommandExample:
    def __init__(self, base, proportional_gain = 2.0):

        self.proportional_gain = proportional_gain
        # self.router = router

        # Create base client using TCP router
        self.base = base
        
    def ExampleSendGripperCommands(self, position):

        # Create the GripperCommand we will send
        gripper_command = Base_pb2.GripperCommand()
        finger = gripper_command.gripper.finger.add()

        # Close the gripper with position increments
        print("Performing gripper test in position...")
        gripper_command.mode = Base_pb2.GRIPPER_POSITION
        finger.finger_identifier = 1
        
        finger.value = position
        print("Going to position {:0.2f}...".format(finger.value))
        self.base.SendGripperCommand(gripper_command)

class YemGripper:
    Lx = 8; Ly = 29; L = (Lx**2 + Ly**2)**0.5
    def __init__(self):
        self.last_actAng = [0, 0, 0, 0]
    def open(self):
        self.rollInput = self.RollInput()
        import hid
        self.h = hid.device()
        self.h.close()
        while True:
            try:
                time.sleep(1)
                self.h.open(0x2886, 0x802f)
            except OSError as e:
                print("not open. try to open in 1sec")
            else:
                return

    def kinematic(self, angJ3, angJ2, angJ1):
        Phi = angJ1 + angJ2+ angJ3
        L = self.L
        x = -L * (math.sin(math.radians(angJ1)) + math.sin(math.radians(angJ1) + math.radians(angJ2)))
        y = L * (math.cos(math.radians(angJ1)) + math.cos(math.radians(angJ1) + math.radians(angJ2)))
        return x, y, Phi

    def invkinematic(self, x, y, Phi):
        AB = x*x + y*y
        L = self.L
        Theta = math.acos((AB - 2*L*L) / (-2*L*L))
        indexJ2 = math.degrees(Theta) -180 # degree
        alpha1 = math.acos((x*x + y*y)/ (2*L*(x*x + y*y)**0.5))
        alpha = math.atan2(y, x)
        indexJ1 = math.degrees(alpha1+alpha) - 90
        indexJ3 = Phi - indexJ2 - indexJ1
        return indexJ3, indexJ2, indexJ1
    
    def send(self, gripFactor, gripForce = 5):
        try:
            rollFactor = self.rollInput.value
            TargAng =  [10, -10, 5, -450] #indexJ3, indexJ2, IndexJ1, Thumb (4 motors); degree * 10
            TargAng[3] = (int)(10 * (-30 + 40 * gripFactor + 25 * rollFactor)) #thumb
            x =  -0+ (0+25) * gripFactor + 5 * rollFactor
            y = 45 + (-45+25) * gripFactor + 3 * rollFactor - gripForce * gripFactor
            Phi = 20 + (-20-135)  * gripFactor + 25 * rollFactor
            inv = self.invkinematic(x, y, Phi)
            TargAng[0] = (int)(10*inv[0])#J3
            TargAng[1] = (int)(10*inv[1])#J2
            TargAng[2] = (int)(10*inv[2])#J1
            dataBytes = [0, 0, 0, 0, 0, 0, 0, 0, 0]#header, TargAng
            dataBytes[1] = (TargAng[0] >> 8) & 0xff
            dataBytes[2] = TargAng[0] & 0xff
            dataBytes[3] = (TargAng[1] >> 8) & 0xff
            dataBytes[4] = TargAng[1] & 0xff
            dataBytes[5] = (TargAng[2] >> 8) & 0xff
            dataBytes[6] = TargAng[2] & 0xff
            dataBytes[7] = (TargAng[3] >> 8) & 0xff
            dataBytes[8] = TargAng[3] & 0xff
            self.h.write(dataBytes)#send target to 4 motors
            getBytes = self.h.read(8)#get actual angle values of 4 motors
            actAng = [0, 0, 0, 0] #indexJ3, indexJ2, IndexJ1, Thumb; degree * 10
            actAng[0] =  np.array((getBytes[0] << 8) + getBytes[1], dtype='int16')
            actAng[1] =  np.array((getBytes[2] << 8) + getBytes[3], dtype='int16')
            actAng[2] =  np.array((getBytes[4] << 8) + getBytes[5], dtype='int16')
            actAng[3] =  np.array((getBytes[6] << 8) + getBytes[7], dtype='int16')
            #print("actual angle: " , actAng)
            angJ1 = actAng[2] / 10 #degree
            angJ2 = actAng[1] / 10
            angJ3 = actAng[0] / 10
            kine =  self.kinematic(angJ3, angJ2, angJ1)
            # print("kinematic: ", kine)
            #inv = invkinematic(kine[0], kine[1], kine[2])
            #print(inv)
        except KeyboardInterrupt:
            self.h.close()
            return
        except Exception as e:
            print("hid unknown error")
            self.h.close()
            try:
                self.h.open(0x2886, 0x802f)
            except OSError as e:
                print("not open. try to open")
            else:
                return
    def close(self):
        self.h.close()
        self.rollInput.close()

    class RollInput:
        value = 0
        def __init__(self):
            from serial import Serial
            from serial.serialutil import SerialException
            while 1:
                try:
                    time.sleep(1)
                    self.s = Serial('COM128', 9600, timeout=1)
                except SerialException as e:
                    print(e)
                else:
                    break
            self.s.reset_input_buffer()
            self.run = True
            self.readloop_thread = threading.Thread(target=self.readloop)
            self.readloop_thread.start()
        def readloop(self):
            from serial.serialutil import SerialException
            while self.run:
                try:
                    self.s.reset_input_buffer()
                    recvData = self.s.read(5)
                except SerialException as e:
                    print(e)
                    while 1:
                        try:
                            self.s.close()
                            self.s.open()
                        except SerialException as e:
                            print(e)
                            time.sleep(1)
                        else:
                            break
                    continue
                try:
                    uint8array = np.array([recvData[0], recvData[1], recvData[2], recvData[3]], dtype='uint8')
                except IndexError as e:
                    print(e)
                    continue
                x = uint8array[0] << 24
                x += uint8array[1] << 16
                x += uint8array[2] << 8
                x += uint8array[3]
                self.value =  2.0*(x/1023.0 - 0.5)
        def close(self):
            self.run = False
            time.sleep(3)
            self.readloop_thread.join()
            self.s.close()


[vx, vy, vz, grip, angular_speed_y, recv_time, yemGripper] = [0, 0, 0, 1, 0, 0, None]


def convert(client, server, message):
    global vx, vy, vz, grip, angular_speed_y, recv_time

    recv_time = time.time()
    read_array = np.array(message, dtype='int8')

    vx = read_array[2] / 100
    vy = -read_array[0] / 100
    vz = read_array[1] / 120

    grip = read_array[3] / 100

    if len(read_array) > 4:
        angular_speed_y = read_array[4]
key_value = ''
key_status = False

base = None

MyHomePosition = {'px': 0.44, 'py': 0.19, 'pz': 0.45, 'rx': 90, 'ry':0, 'rz': 150}

def main():
    global base, vx, vy, vz, grip, recv_time, yemGripper, key_status, key_value, MyHomePosition
    # yemGripper = YemGripper()
    # yemGripper.open()
    # for cout in range(0,2):
    #     for gripFactor in range(10, -1, -1):
    #         yemGripper.send(gripFactor/10, 0)
    #         print(gripFactor)
    #         time.sleep(0.2)
    #     for gripFactor in range(0, 11, 1):
    #         yemGripper.send(gripFactor/10, 0)
    #         print(gripFactor)
    #         time.sleep(0.2)
    
    # Import the utilities helper module
    sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
    import utilities

    # Parse arguments
    args = utilities.parseConnectionArguments()
    
    # Create connection to the device and get the router

    while True:
        try:
            with utilities.DeviceConnection.createTcpConnection(args) as router:
                print("Success connecting to Kinova!")
                # Create required services
                base = BaseClient(router)
                base.ClearFaults()
                time.sleep(1)	
                base_cyclic = BaseCyclicClient(router)

                # grip = 0
                # d = 0.01
                # example = GripperCommandExample(base)
                # while 1:
                #     example.ExampleSendGripperCommands(grip)
                #     grip += d
                #     if grip > 1:
                #         d = -0.01
                #     elif grip < 0:
                #         d = 0.01
                #     time.sleep(0.001)

                # Example core
                success = True

                success &= example_move(base, 'Home')
                feedback = base_cyclic.RefreshFeedback()
                print('World Positoin(m) x {:.2f} y {:.2f} z {:.2f}'.format(feedback.base.tool_pose_x, feedback.base.tool_pose_y, feedback.base.tool_pose_z))
                print('World Eular Rotation(deg) x {:.2f} y {:.2f} z {:.2f}'.format(feedback.base.tool_pose_theta_x, feedback.base.tool_pose_theta_y, feedback.base.tool_pose_theta_z), end='\n\n')
                eux = 0.44
                euy = 0.19
                euz = 0

                example = GripperCommandExample(base)

                grip_count = 0

                while 1:
                    if key_status:
                        key_status = False
                        if key_value == 'h':
                            # success &= example_move(base, 'Home')
                            success &= example_cartesian_action_movement(base, base_cyclic, MyHomePosition['px'], MyHomePosition['py'], MyHomePosition['pz'], MyHomePosition['rx'], MyHomePosition['ry'], MyHomePosition['rz'])
                        elif key_value == 'r':
                            success &= example_move(base, 'Retract')
                        elif key_value == 'p':
                            success &= example_move(base, 'Packaging')
                        elif key_value == 'm':
                            feedback = base_cyclic.RefreshFeedback()
                            MyHomePosition['px'] = feedback.base.tool_pose_x; MyHomePosition['py'] = feedback.base.tool_pose_y; MyHomePosition['pz'] = feedback.base.tool_pose_z
                            MyHomePosition['rx'] = feedback.base.tool_pose_theta_x; MyHomePosition['ry'] = feedback.base.tool_pose_theta_y; MyHomePosition['rz'] = feedback.base.tool_pose_theta_z
                        elif key_value == '\x03':
                            base.Stop()
                            return 0
                        continue
                    if time.time() - recv_time < 0.2:
                        stop_count = 0
                        success &= example_twist_command(base, vx, vy, vz, angular_speed_y)
                        # print(vx, vy, vz, grip)
                        time.sleep(0.1)
                        # grip
                        example = GripperCommandExample(base)
                        example.ExampleSendGripperCommands(grip/2 + 0.5)
                        # yemGripper.send(grip)

                    else:
                        success &= example_twist_command(base, 0, 0, 0, 0)
                return 0 if success else 1
        except ConnectionRefusedError as e:
            print("try connecting to Kinova in 3 sec...")
            time.sleep(3)


def wsserver_run():
    from websocket_server import WebsocketServer
    # Called for every client connecting (after handshake)
    def new_client(client, server):
        print("New client connected and was given id %d" % client['id'])
        server.send_message_to_all("Hey all, a new client has joined us")
    # Called for every client disconnecting
    def client_left(client, server):
        print("Client(%d) disconnected" % client['id'])
    wsserver = WebsocketServer(port = 9001)
    wsserver.set_fn_new_client(new_client)
    wsserver.set_fn_client_left(client_left)
    wsserver.set_fn_message_received(convert)
    wsserver.run_forever(threaded=True)

run = True
def key_loop():
    global base, key_status, key_value, run
    from readchar import readchar
    while run:
        key_value = readchar()
        key_status = True
        if key_value == '\x03':
            base.Stop()


if __name__ == "__main__":
    wsserver_run()
    th_key_loop = threading.Thread(target=key_loop)
    th_key_loop.start()
    try:
        exit(main())
    except:
        print('exit main')
        run = False
    else:
        run = False

