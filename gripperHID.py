#!/usr/bin/python3
import time
import numpy as np
import math
import hid
h = hid.device()
h.close()
while True:
    try:
        h.open(0x2886, 0x802f)
    except OSError as e:
        print("not open. try to open in 1sec")
        time.sleep(1)
    else:
        print("success to open xaio")
        break

actAng = [0, 0, 0, 0] #indexJ3, indexJ2, IndexJ1, Thumb; degree * 10
last_actAng = [0, 0, 0, 0]
TargAng =  [10, -10, 5, -450] #indexJ3, indexJ2, IndexJ1, Thumb (4 motors); degree * 10

Lx = 8
Ly = 29
L = (Lx**2 + Ly**2)**0.5
#grip Thumb -45deg to 0 and [-15, 58, 12] to [x=38, y=36, Phi=-92]
#roll:  Thumb -5deg to 5 ;[25, 36, -111] to [50, 31, -72]
def kinematic(angJ3, angJ2, angJ1):
    Phi = angJ1 + angJ2+ angJ3
    x = -L * (math.sin(math.radians(angJ1)) + math.sin(math.radians(angJ1) + math.radians(angJ2)))
    y = L * (math.cos(math.radians(angJ1)) + math.cos(math.radians(angJ1) + math.radians(angJ2)))
    return x, y, Phi
def invkinematic(x, y, Phi):
    AB = x*x + y*y
    Theta = math.acos((AB - 2*L*L) / (-2*L*L))
    indexJ2 = math.degrees(Theta) -180 # degree
    alpha1 = math.acos((x*x + y*y)/ (2*L*(x*x + y*y)**0.5))
    alpha = math.atan2(y, x)
    indexJ1 = math.degrees(alpha1+alpha) - 90
    indexJ3 = Phi - indexJ2 - indexJ1
    return indexJ3, indexJ2, indexJ1

gripFactor = 0#[0,1]ジョイスティックの係数
f = 1
gripForce = 10 #大きくなるほど強く握る
rollFactor = -0.9 #[-1,1]ジョイスティックの係数
while 1:
    try:
        time.sleep(0.2)

        # gripFactor += f*0.1
        # if gripFactor > 1:
        #    f = -1
        #    gripFactor = 1
        # elif gripFactor < 0:
        #    f = 1
        #    gripFactor = 0

        print(gripFactor)

        # rollFactor += f*0.1
        # if rollFactor > 1:
        #    f = -1
        #    rollFactor = 1
        # elif rollFactor < -1:
        #    f = 1
        #    rollFactor = -1

        TargAng[3] = (int)(10 * (-45 + 45 * gripFactor + 5 * rollFactor)) #thumb
        x = -15 + (15+38) * gripFactor + 12 * rollFactor
        y = 50 + (-50+25) * gripFactor - 3 * rollFactor - gripForce * gripFactor
        Phi = 12 + (-12-92)  * gripFactor + 20 * rollFactor
        inv = invkinematic(x, y, Phi)
        # print(inv)
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
        h.write(dataBytes)#send target to 4 motors
        getBytes = h.read(8)#get actual angle values of 4 motors
        actAng[0] =  np.array((getBytes[0] << 8) + getBytes[1], dtype='int16')
        actAng[1] =  np.array((getBytes[2] << 8) + getBytes[3], dtype='int16')
        actAng[2] =  np.array((getBytes[4] << 8) + getBytes[5], dtype='int16')
        actAng[3] =  np.array((getBytes[6] << 8) + getBytes[7], dtype='int16')
        #print("actual angle: " , actAng)
        angJ1 = actAng[2] / 10 #degree
        angJ2 = actAng[1] / 10
        angJ3 = actAng[0] / 10
        kine =  kinematic(angJ3, angJ2, angJ1)
        # print("kinematic: ", kine)
        #inv = invkinematic(kine[0], kine[1], kine[2])
        #print(inv)
    except KeyboardInterrupt:
        h.close()
        break
    except Exception as e:
        print("hid unknown error")
        h.close()
        while True:
            try:
                h.close()
                h.open(0x2886, 0x802f)
            except OSError as e:
                print("not open. try to open in 1sec")
                time.sleep(1)
            else:
                break
        pass
