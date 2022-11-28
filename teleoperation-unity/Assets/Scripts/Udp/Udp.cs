using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using UnityEngine;

namespace Teleoperation {
/// <summary>
/// https://github.com/hecomi/uOSC/blob/master/Assets/uOSC/Runtime/Core/DotNet/Udp.cs
/// uOSC を参考に Udp Client/Server の実装を理解する 
/// </summary>
public class Udp {

    enum State {
        Stop,
        Server,
        Client,
    }

    private State _state = State.Stop;

    // 何に使う変数？
    private Queue<byte[]> _messageQueue = new Queue<byte[]>();
    // 何に使う？
    private object _lockObject = new object();
    
    private UdpClient _udpClient;
    private IPEndPoint _endPoint;
    // thread は何に使ってる？
    private Thread _thread = new Teleoperation.Thread(); // 何故ここで new している？
    

    public void StartClient(string address, int port) {
        Stop();
        _state = State.Client;
        
        // ここは何をしている？
        var ip = IPAddress.Parse(address);
        _endPoint = new IPEndPoint(ip, port);
        _udpClient = new UdpClient(_endPoint.AddressFamily);
    }

    public void Stop() {
        if (_state == State.Stop) return;
        
        _thread.Stop();
        _udpClient.Close();
        _state = State.Stop;
    }

    public void Send(byte[] data, int size) {
        try {
            _udpClient.Send(data, size, _endPoint);
        } catch (System.Exception e) {
            UnityEngine.Debug.LogError(e.ToString());
        }
    }
}
}