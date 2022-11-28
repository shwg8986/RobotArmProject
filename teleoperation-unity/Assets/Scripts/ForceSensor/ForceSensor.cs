using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using Teleoperation;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// 受信した圧力センサのデータを処理するためのクラス
/// 電気刺激との間をかけ持つためにも利用される
/// </summary>
public class ForceSensor : MonoBehaviour {
    public HV513 hv513 = default;
    public ForceSensorIndicator2D forceSensorIndicator2D = default;
    public TMP_Text fpsText;
    public ForceSensorSerialHandler forceSensorSerialHandler;
    public int magnifyScale = 5;
    

    //software definitions
    public const int PC_ESP32_MEASURE_REQUEST = 0xFE; // before: PC_MBED_MEASURE_REQUEST
    const int ESP32_PC_MEASURE_RESULT = 0xFF;  // before: MBED_PC_MEASURE_RESULT

//graphical attributes

    public const int SENSOR_X_NUM = 10;
    public const int SENSOR_Y_NUM = 10;
    public const int THERMAL_NUM = 2;
    public const int FINGER_NUM = 3;
    public const int SENSOR_NUM = SENSOR_X_NUM * SENSOR_Y_NUM * FINGER_NUM;
    public const int DATA_LENGTH = (int)(FINGER_NUM * (SENSOR_X_NUM * SENSOR_Y_NUM + THERMAL_NUM) * 3f / 2f) + 2;

    public const int MAX_12BIT_VALUE = 4096; // 12bitの最大値
    // bit shift に利用される．ただESP32のプログラムを見ると，シフトされずに12bitデータが送られてくるので0にする
    public const int PressureRange  = 0; 


    public const float THERMAL_OFFSET = 28.0f; // degree-C, Temperature of the room.
    public const float THERMAL_MAX = 35.0f;    // degree-C, Temperature of the room.
    public const float THERMAL_AMP = 30.0f;
    
    int[,,] PressureDistribution = new int[FINGER_NUM, SENSOR_X_NUM, SENSOR_Y_NUM];
    private int[,,] OffsetPressure = new int[FINGER_NUM, SENSOR_X_NUM, SENSOR_Y_NUM];
    int[,] iThermalDist = new int[FINGER_NUM, THERMAL_NUM];
    float[,] ThermalDistribution = new float[FINGER_NUM, THERMAL_NUM];

    int u_timer = 0, prev_t = 0;
    bool SerialDataSendRequest = false, SaveDataFlag = false;

    //public static bool touched; //島田追加
    public static int pv_sum; //島田追加
    public static float[] pv_sum_raw; //島田追加


    void Awake() {
        // シリアル通信で、データが送られてきた際に呼び出すメソッドを登録する (Awakeなど早めに登録をする必要がある)
        forceSensorSerialHandler.OnDataReceived += new ForceSensorSerialHandler.BytesReceivedEventHandler(OnReceivedProcess);
        // Application.targetFrameRate = 9999; // Frame Rate を制御するために，target自体は大きな値にしておく
    }

    private void Start() {
        // send initial request
        forceSensorSerialHandler.WriteByte(PC_ESP32_MEASURE_REQUEST);
        StartCoroutine(OffsetProcess());
    }

    void Update() {
        // 一応デバッグ用にサンプリングレートを表示しておく (正しいかは微妙なところ。大体200Hzぐらいが限度)
        float samplingRate = forceSensorSerialHandler.GetSamplingRate();
        fpsText.text = $"fps:{samplingRate:f2}";
        //Debug.Log($"touched:{touched}");
    }
    

    // ForceSensorSerialHandler の方の OnDataReceived (Delegate) に登録される関数 
    // - 送られてきたデータを圧力値に変換し、
    // - ディスプレイに表示する (ForceSensorIndicator2D)、
    // 電気刺激に変換する (HV513)、
    // という処理を行う
    void OnReceivedProcess(byte[] data) {
        // Debug.Log($"{this.GetType()}: OnDataReceived!");
        //
        // ここでデータが全て送られているか確認すべきかも
        //
        int rcv      = 0, x, y, finger, t, packing;
        int bufIndex = 0;

        //send next request. This request is issued "before" reading serial buffer, to save time of ESP32.
        // WriteByte(m_SerialPort, PC_ESP32_MEASURE_REQUEST);

        //data reading.This is the data assoc  iated with previous request.
        // Received Data:     AAAAAAAA AAAABBBB BBBBBBBB
        // Restructured Data: AAAAAAAAAAAA BBBBBBBBBBBB

        // byte[] data が受信したデータに対応する
        // bufIndex によって順番にアクセスしていくことで、データを読み取っていく
        
        // mod 2 で利用して処理を二つに分岐するために利用
        // これによって、分割されたデータ列 (8 x 3) を 12bit に変換する
        packing = 0; 
        
        // ----------- データの読み込み ---------
        // データの数は センサの個数 x x軸の個数 x y軸の個数になる。
        // 全データにアクセスする
        for (finger = 0; finger < FINGER_NUM; finger++) {
            for (x = 0; x < SENSOR_X_NUM; x++) {
                for (y = 0; y < SENSOR_Y_NUM; y++) {
                    if (packing == 0) {
                        // rcv                                =  m_SerialPort.ReadByte();
                        rcv                                =  data[bufIndex++];
                        PressureDistribution[finger, x, y] =  rcv << 4; //AAAAAAAA
                        // rcv                                =  m_SerialPort.ReadByte();
                        rcv                                =  data[bufIndex++];
                        PressureDistribution[finger, x, y] |= rcv >> 4; //AAAA
                        
                    } else if (packing == 1) {
                        PressureDistribution[finger, x, y] =  (rcv & 0x0F) << 8; //BBBB
                        // rcv                                =  m_SerialPort.ReadByte();
                        rcv                                =  data[bufIndex++];
                        PressureDistribution[finger, x, y] |= rcv; //BBBBBBBB
                    }
                    // offset 処理
                    PressureDistribution[finger, x, y] =
                            PressureDistribution[finger, x, y] - OffsetPressure[finger, x, y];
                    // [CAUTION] 小さな変化で大きく変化するように、倍率をかえる. 正しい値ではなくなるので、注意
                    // 観察すると、だいたいMaxで2000ぐらいまでしかいかないので、maxを2000に設定した
                    PressureDistribution[finger, x, y] = (int) Mathf.Clamp(PressureDistribution[finger, x, y] * magnifyScale, 0, 2000) ;

                    packing = (packing + 1) % 2; //0,1,0,1,0,1,...
                }
            }

            // 温度センサのデータも送られてくる
            // 受信を行う
            for (x = 0; x < THERMAL_NUM; x++) {
                if (packing == 0) {
                    // rcv                     =  m_SerialPort.ReadByte();
                    rcv                     =  data[bufIndex++];
                    iThermalDist[finger, x] =  rcv << 4; //AAAAAAAA
                    // rcv                     =  m_SerialPort.ReadByte();
                    rcv                     =  data[bufIndex++];
                    iThermalDist[finger, x] |= rcv >> 4; //AAAA
                } else if (packing == 1) {
                    iThermalDist[finger, x] =  (rcv & 0x0F) << 8; //BBBB
                    // rcv                     =  m_SerialPort.ReadByte();
                    rcv                     =  data[bufIndex++];
                    iThermalDist[finger, x] |= rcv; //BBBBBBBB
                }

                packing = (packing + 1) % 2; //0,1,0,1,0,1,...
                ThermalDistribution[finger, x] =
                        0.0155f * (float)iThermalDist[finger, x] - 16.557f; //Calculation by theoretical formula.
            }
        }

        //timer
        // 牛山ここの処理の意味をあまり理解できていない。
        // timestampで整合性をとろうとしているのはなんとなくわかる
        t = data[bufIndex++]; // timestamp //time (0-255) in milliseconds (mbed)
        if (t != -1) {          //t==-1 means insufficient number of data in buffer.
            if (t >= prev_t) {
                u_timer = u_timer + t - prev_t;
            } else { //overflow management
                u_timer = u_timer + t + 255 - prev_t;
            }

            prev_t = t;
            //remove the terminating character
            // if (m_SerialPort.ReadByte() != ESP32_PC_MEASURE_RESULT) {
            //     m_SerialPort.DiscardInBuffer();
            // }
            if (data[bufIndex] != ESP32_PC_MEASURE_RESULT) {
                forceSensorSerialHandler.ClearReadBuffer();
            }
        }
        // m_SerialPort.DiscardInBuffer();
        // forceSensorSerialHandler.ClearReadBuffer();
        // DebugLogPressure(PressureDistribution); // 圧力データを確認するためのデバッグメソッド
        // -------- 圧力センサの値を使った処理 --------
        forceSensorIndicator2D.UpdateDisplay(ref PressureDistribution); // Unityの画面上でデータを視覚化するための処理
        hv513.CalcIntensityWithForce(ref PressureDistribution); // 圧力センサのデータを元に、電気刺激の強度を設定する
        forceSensorSerialHandler.WriteByte(PC_ESP32_MEASURE_REQUEST);
        //touched = forceSensorIndicator2D.TouchedSensor(ref PressureDistribution); //島田追加
        pv_sum = forceSensorIndicator2D.TouchedSensor(ref PressureDistribution); //島田追加
        pv_sum_raw = forceSensorIndicator2D.SensorTouched_Raw(ref PressureDistribution); //島田追加
        //Debug.Log($"pv_sum_raw : {pv_sum_raw[0]} ,  {pv_sum_raw[1]}");
    }

    IEnumerator OffsetProcess() {
        yield return new WaitForSeconds(3); // 最初に数秒待って、その時の値をオフセットとして記録する
        SaveDistributionAsOffset();
    }
    
    public void SaveDistributionAsOffset() {
        Debug.Log($"{this.GetType()}: SavePressureDistributionAsOffset");
        Array.Copy(PressureDistribution, OffsetPressure, PressureDistribution.Length);
        Debug.Log(OffsetPressure.ToString());
    }

    public float AveragePressureDistribution(int finger_num) {
        float average = 0f;
        for (int x = 0; x < SENSOR_X_NUM; x++) {
            for (int y = 0; y < SENSOR_Y_NUM; y++) {
                average += PressureDistribution[finger_num, x, y] / (float) (SENSOR_X_NUM * SENSOR_Y_NUM);
            }
        }
        return average;
    }

    // 圧力値をデバッグしたいときに利用
    void DebugLogPressure(int[,,] a) {
        string debug = "";
        for (var i = 0; i < SENSOR_Y_NUM; i++) {
            for (var j = 0; j < SENSOR_X_NUM; j++) {
                debug += a[0, j, i] + ", ";
            }
            debug += "\n";
        }
        Debug.Log(debug);
    }
}