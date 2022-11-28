using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace Teleoperation {
public class CustomUdpClient : MonoBehaviour {

    [SerializeField] public string address = "127.0.0.1";
    [SerializeField] public int port = 3333;
    [SerializeField] public float dataTransmissionInterval = 0f;
    [SerializeField] public int maxQueueSize = 100;
    
    Udp _udp = new Udp(); // 自作した Udp クラス
    Teleoperation.Thread _thread = new Teleoperation.Thread();

    private Queue<object> _messages = new Queue<object>();
    private object _lockObject = new object();
    
    private string _address = "";
    private int _port;
    
    // 恐らくイベント系のデリゲート
    // public ClientStartEvent onClientStarted = new ClientStartEvent();
    // public ClientStopEvent onClientStopped = new ClientStopEvent();
    void OnEnable() {
        StartClient();
    }

    private void OnDisable() {
        StopClient();
    }


    public void StartClient() {
        _udp.StartClient(address, port);
        _thread.Start(UpdateSend);
        _address = address;
        _port = port;
        // OnClientStarted.Invoke(address, port); // client 起動時に実行する関数 だと思われる
    }

    void DebugPrintArray<T>(ref T[] a) {
        Debug.Log("Type: " + a.ToString());
        Debug.Log("Sending data Length: " + a.Length);
        Debug.Log("Sending bytes...: ");
        foreach (var v in a) {
            Debug.Log(v);
        }
    }
    
    void UpdateSend() {
        while (_messages.Count > 0) {
            var sw = Stopwatch.StartNew();

            object message;
            lock (_lockObject) {
                message = _messages.Dequeue();
            }

            // uOSC 特有の処理．シンプルに利用する場合は必要ないか？
            // 全ての型をbyte[] に変換するために，MemoryStream を使っている？
            // とりあえず，int だけ送ることができれば良いので，割愛
            // --
            // using (var stream = new MemoryStream()) {
            //     if (message is float) {
            //         
            //     }
            // }
            byte[] _bytes = new byte[]{0};
            if (message is int) {
                _bytes = BitConverter.GetBytes((int) message);
                // DebugPrintArray<byte>(ref _bytes);
            } else if (message is byte[]) {
                _bytes = (byte[]) message;
                // DebugPrintArray(ref _bytes);
            } else if (message is float[]) {
                float[] val = (float[]) message;
                _bytes = new byte[val.Length * 4]; // float は 4byte なので 4倍のサイズの配列を用意する
                Buffer.BlockCopy(val, 0, _bytes, 0, _bytes.Length); // 配列のコピーをする (dstArrayのサイズ分)
                // DebugPrintArray(ref _bytes);
            } else {
                Debug.LogWarning("message is not int, byte[], float[]");
                continue;
            }
            _udp.Send(_bytes, _bytes.Length);

            if (dataTransmissionInterval > 0f) {
                var ticks = (long) Mathf.Round(dataTransmissionInterval / 1000f * Stopwatch.Frequency);
                while (sw.ElapsedTicks < ticks) ;
            }
            
        }
    }


    public void StopClient() {
        _thread.Stop();
        _udp.Stop();
        // OnClientStopped.Invoke(address, port); // client 停止時に実行する関数 だと思われる
    }

    void Update() {
        UpdateChangePortAndAddress();
    }
    
    void UpdateChangePortAndAddress() {
        // 恐らく動的なポートやIPアドレスの変化に対応するために実装している
        if (_port == port && _address == address) return;
        StopClient();
        StartClient();
    }
    
    void Add(object data) {
        lock (_lockObject) {
            _messages.Enqueue(data);

            while (_messages.Count > maxQueueSize) {
                _messages.Dequeue();
            }
        }
    }

    public void Send(object value) {
        Add(value);
    }
    
}
}
