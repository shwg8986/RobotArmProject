using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class ForceSensorSerialHandler : MonoBehaviour {
    // Delegate for event handler
    public delegate void BytesReceivedEventHandler(byte[] data);
    public event BytesReceivedEventHandler OnDataReceived;

    public string portName = "COM4"; // Change this!
    public int baudRate = 921600;
    private SerialPort m_SerialPort;
    private bool m_IsDataReceived = false;
    private bool m_IsRunning = false;
    private byte[] m_Rcv = new byte[(int)(ForceSensor.FINGER_NUM * (ForceSensor.SENSOR_X_NUM * ForceSensor.SENSOR_Y_NUM + ForceSensor.THERMAL_NUM) * 3f / 2f) + 2];
    private Thread m_Thread;

    private float samplingRate = 0f;
    private object lockObj = new object(); // データの受信は別スレッドで行うので、処理と受信とがかぶらないようにlockする
    
    void Awake() {
        Open();
        m_IsRunning = true;
        m_Thread    = new Thread(Read);
        m_Thread.Start();
        
        
        Debug.Log($"{this.GetType()} - Open() Serial open and Thread start");
    }

    void Open() {

        m_SerialPort             = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        // ReadTimeout を設定することで、UnityのPlayモード終了時にフリーズすることを防ぐ
        m_SerialPort.ReadTimeout = 20;
        // m_SerialPort.RtsEnable   = true;
        // m_SerialPort.DtrEnable   = true;
        m_SerialPort.Open();
        m_SerialPort.DiscardInBuffer();
        m_SerialPort.DiscardOutBuffer();
    }
    public void Close() {

        if (m_SerialPort != null && m_SerialPort.IsOpen) {
            m_SerialPort.DiscardInBuffer();
            m_SerialPort.DiscardOutBuffer();
            m_SerialPort.Close();
            m_SerialPort.Dispose();
        }
    }
    private void OnDestroy() {
        Close();
    }

    public void ClearReadBuffer() {
        m_SerialPort.DiscardInBuffer();
    }

    public float GetSamplingRate() {
        return samplingRate;
    }
    private void Start() {
        
    }

    // Update is called once per frame
    void Update() {

        if (m_IsDataReceived) {
            try {
                if (OnDataReceived != null) {
                    // 圧力センサのデータに対する処理 (電気刺激強度の決定など)のためにlockしておく
                    lock (lockObj) {
                        // Debug.Log($"{this.GetType()}: call OnDataReceived");
                        OnDataReceived(m_Rcv); // OnDataReceived にメソッドを登録してないとNullException になる
                        m_IsDataReceived = false;
                    }
                }
            } catch (System.Exception e) {
                Debug.LogError(e.Message);
            }
        }
    }
    
    // 電気刺激装置にデータを送信するのに利用している  
    public void WriteByte(byte data) {
        byte[] d = { data }; // SerialPort.Write() は byte[] にする必要がある
        try {
            m_SerialPort.Write(d, 0, d.Length);
        } catch (System.Exception e) {
            Debug.LogError(e.Message);
        }
    }
    // ESP32 (圧力センサ) から送信されるデータを受信するメソッド
    // 別スレッドで立ち上げて、動かし続けることで常に受信できるようにしている
    // シリアルポートが開いてるとき & thread が立ち上がっているときにはループし続ける
    void Read() {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Reset();
        sw.Start();
        var prevTime = sw.ElapsedMilliseconds;
        // Debug.Log($"{this.GetType()} - Read() first line");
        while (m_IsRunning && m_SerialPort != null && m_SerialPort.IsOpen) {
            // int dataLength = 0;
            // Debug.Log($"{this.GetType()}: while loop in Read()");
            try {
// 特定のデータ量が送られてきたら、読み込み処理をする
                if (m_SerialPort.BytesToRead >= ForceSensor.DATA_LENGTH) {
                    // データの受信 -> 圧力センサデータの更新のためにlockする
                    lock (lockObj) {
                        // どのぐらいのフレームレートでデータを受信できているのか確認する用
                        samplingRate = 1000f / (sw.ElapsedMilliseconds - prevTime);
                        // m_Rcv は byte[] で送られてきたbyteを一度にReadする
                        m_SerialPort.Read(m_Rcv, 0, ForceSensor.DATA_LENGTH);


                        // m_SerialPort.DiscardInBuffer(); // もしかしたら一回バッファをクリアした方がいいかも？とりあえず今は使わない
                        /*
                        // [注意] このスレッドの中では UnityEngineを使った処理はできない
                        // FIXME: null になる危険性あり
                         if (OnDataReceived != null) {
                             OnDataReceived(m_Rcv);
                        }
                        */
                    
                        // Debug.Log($"{this.GetType()}: data received in Read()"); // Read() が呼ばれているか確認するためのDebug.Log

                        // データを受信したことを伝えるためのフラグ
                        // Update() 側でデータに対する処理を呼びだしている
                        m_IsDataReceived = true;
                        prevTime         = sw.ElapsedMilliseconds;
                    }
                } else if (m_SerialPort.BytesToRead <= 0) {
                    // [CAUTION]
                    // 本来なら、(いわゆる) SerialEvent() の方でデータを受け取って処理をしたあとに REQUEST を投げて、またデータを受信する
                    // という流れにしたい (元となったProcessingのコードはそうなっている) が、Unity (C#) の場合は、別スレッドで動かしているからか、
                    // 仕様の違いかのため、REQUESTをしていてもデータが止まってしまうときがある。
                    // そのため、データが止まった時には再度REQUESTを投げるようにしている
                    // Debug.Log($"Data Stopping.... Re-Request Measure");
                    WriteByte(ForceSensor.PC_ESP32_MEASURE_REQUEST);
                    // m_SerialPort.DiscardInBuffer();
                }
                // Thread.Sleep(5); // millisec
            } catch (Exception e) {
                // Console.WriteLine(e);
                Debug.LogError($"{this.GetType()}: error: {e}");
                break;
            }
            
        }
        sw.Stop();
    }
}
