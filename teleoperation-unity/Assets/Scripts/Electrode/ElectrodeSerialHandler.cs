using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System.Threading;

namespace Teleoperation {

    public class ElectrodeSerialHandler : MonoBehaviour {
        public delegate void ByteReceivedEventHandler(byte message);

        public event ByteReceivedEventHandler OnDataReceived;
        public string portName = "COM3";
        public int baudRate = 115200;

        private SerialPort m_SerialPort;

        private System.Threading.Thread m_Thread;
        private bool m_IsRunning = false;

        private string m_Message;
        private byte m_Rcv;
        private bool m_IsNewMessageReceived = false;

        void Awake() {
            Open();
        }

        void Update() {
            if (m_IsNewMessageReceived) {
                try {
                    if (OnDataReceived != null) {
                        OnDataReceived(m_Rcv); // OnDataReceived にメソッドを登録してないとNullException になる
                    }
                } catch (System.Exception e) {
                    Debug.LogError(e.Message);
                }
            }

            m_IsNewMessageReceived = false;
        }

        private void OnDestroy() {
            Close();
        }

        private void OnApplicationQuit() {
            // Close();
        }

        void Open() {
            m_SerialPort             = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            m_SerialPort.ReadTimeout = 20;
            // m_SerialPort.NewLine     = "\n";
            // m_SerialPort.WriteTimeout = 20;
            m_SerialPort.Open(); // 上手くいかない場合はこの実行タイミングを検討する
            m_SerialPort.DiscardInBuffer();
            m_SerialPort.DiscardOutBuffer();

            // Read Thread
            m_IsRunning = true;
            m_Thread    = new System.Threading.Thread(Read);
            m_Thread.Start();
            Debug.Log($"{this.GetType()} - Open() Serial open and Thread start");
        }


        void Read() {
            // Debug.Log($"{this.GetType()} - Read() first line");
            while (m_IsRunning && m_SerialPort != null && m_SerialPort.IsOpen) {
                // Debug.Log($"{this.GetType()} - Read() in While()");
                try {
                    if (m_SerialPort.BytesToRead > 0) {
                        m_Message = m_SerialPort.ReadLine();
                        Debug.Log($"{this.GetType()} - Read() after ReadLine()");
                        m_IsNewMessageReceived = true;
                    }
                } catch (System.Exception e) {
                    Debug.LogError(e.Message);
                }
            }
        }

        int ReadByte() {
            int rcv = 0;
            try {
                if (m_SerialPort.BytesToRead > 0) {
                    rcv = m_SerialPort.ReadByte();
                }
            } catch (System.Exception e) {
                Debug.LogError(e.Message);
            }

            return rcv;
        }

        public void ClearReadBuffer() {
            if (m_SerialPort != null && m_SerialPort.IsOpen) {
                m_SerialPort.DiscardInBuffer();
            }
        }

        public void ClearWriteBuffer() {
            if (m_SerialPort != null && m_SerialPort.IsOpen) {
                m_SerialPort.DiscardOutBuffer();
            }
        }

        public bool IsWriting() {
            return m_SerialPort.BytesToWrite > 0;
        }
        
        public void Close() {
            m_IsNewMessageReceived = false;
            m_IsRunning            = false;

            if (m_Thread != null && m_Thread.IsAlive) {
                m_Thread.Join();
            }

            if (m_SerialPort != null && m_SerialPort.IsOpen) {
                m_SerialPort.Close();
                m_SerialPort.Dispose();
            }
        }

        public void Write(string message) {
            Debug.Log($"{this.GetType()} - Write(): {message}");
            try {
                m_SerialPort.Write(message);
            } catch (System.Exception e) {
                Debug.LogError(e.Message);
            }
        }

        public void Write(byte[] data) {
            try {
                m_SerialPort.Write(data, 0, data.Length);
                // Debug.Log($"{this.GetType()} - Write(byte[] data)");
            } catch (System.Exception e) {
                Debug.LogError(e.Message);
            }
        }

        public void Write(byte data) {
            byte[] d = { data };
            try {
                m_SerialPort.Write(d, 0, d.Length);
            } catch (System.Exception e) {
                Debug.LogError(e.Message);
            }
        }

        public bool IsOpen() {
            return m_SerialPort.IsOpen;
        }
    }
}