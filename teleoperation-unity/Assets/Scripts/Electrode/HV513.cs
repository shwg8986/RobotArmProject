using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using TMPro;
using UnityEditorInternal;
using UnityEngine;


namespace Teleoperation {
    
    /// <summary>
    /// 電気刺激装置を扱うためのクラス
    /// 電極と圧力センサのマッピングも行う
    /// </summary>
public class HV513 : MonoBehaviour {

    //電気刺激のモード
    // バーが上下か左右に動くような刺激をする (デモ用)
    public enum Mode {
        VerticalBar,
        HorizontalBar,
        Pressure,
        Vibration, // 電気刺激ではないが、電気刺激基板を使って振動提示をするのでここに組み込む
        PressureAndVibration,
        None
    }

    public ForceSensor forceSensor;
    public ElectrodeSerialHandler electrodeSerialHandler;
    // public ElectrodeIndicater electrodeIndicater;
    public ElectrodeIndicater2D electrodeIndicater2D;
    public ElectrodeForceIndicator electrodeForceIndicator;

    private const int PC_ESP32_MEASURE_REQUEST = 0xFE;
    private const int PC_ESP32_STIM_PATTERN = 0xFF;
    private const int PC_ESP32_POLARITY_CHANGE = 0xFD;
    private const int PC_ESP32_VIBRATION = 0xFC;
    
    private const int ESP32_PC_MEASURE_RESULT = 0xFF;
    private const int ESP32_INIT_POLARITY = 1;
    public const int ELECTRODE_NUM = 128;
    
    private byte[] m_SwitchState = new byte[ELECTRODE_NUM];
    public byte[] stimPattern { get; private set; } = new byte[ELECTRODE_NUM];
    // private int[] m_Voltage = new int[ELECTRODE_NUM];
    private int m_Polarity = ESP32_INIT_POLARITY; // 1: positive, -1: negative
    //public int volume { get; private set; } = 0;
    public int volume = 0;
    //public int width { get; private set; } = 50;
    public int width = 50;
    private bool m_IsStimulation = true;
    public Mode mode { get; private set; } = Mode.VerticalBar;
    // public Mode mode { get; private set; }
    
    private bool m_IsRunning = false;
    private System.Threading.Thread m_StimThread = default;

    // This is the mapping from HV513 shift resistors' output to 7 by 9 electrodes.
    // This is the locations of each electrode.
    public static readonly int[] ElectrodePosX = { // -11 to 11
            -11, -7, -9, -11, -9, -11, 
            -7, -9, -7, -9, -11, -7,
            -11, -7, -9, -11, -9, -11,
            -7, -9, -7, -9, -11, -7,
            -11, -7, -9, -11, -9, -9, 
            -7, -7, -3, -1, -5, -3,
            -5, -3, -1, -5, -1, -5, -3, -1, -3, -1, -5, -3, -5,
            -3, -1, -5, -1, -5, -3, -1, -3, -1, -5, -3, -5, -3, -1, -5,
            1, 5, 3, 1, 3, 1, 5, 3, 5, 3, 1, 5, 1, 5, 3,
            1, 3, 1, 5, 3, 5, 3, 1, 5, 1, 5, 3, 1, 3, 3, 5, 5, 9, 11, 7, 9, 7, 9, 11, 7, 11, 7, 9, 11, 9, 11, 7, 9,
            7, 9, 11, 7, 11, 7, 9, 11, 9, 11, 7, 9, 7, 9, 11, 7
    };

    public static readonly int[] ElectrodePosY = { //-10 to 10
            -10, -10, -10, -8, -8, -6, -8, -6, -6, -4, -4, -4, -2, -2, -2, 0, 0, 2, 0, 2, 2, 4, 4, 4, 6, 6, 6, 8, 8,
            10, 8, 10, 10, 8, 10, 8, 8, 6, 6, 6, 4, 4, 4, 2, 2, 0, 2, 0, 0, -2,
            -2, -2, -4, -4, -4, -6, -6, -8, -6, -8, -8, -10, -10, -10, -10, -10, -10, -8, -8, -6, -8, -6, -6, -4,
            -4, -4, -2, -2, -2,
            0, 0, 2, 0, 2, 2, 4, 4, 4, 6, 6, 6, 8, 8, 10, 8, 10, 10, 8, 10, 8, 8, 6, 6, 6, 4, 4, 4, 2, 2, 0, 2, 0,
            0, -2, -2, -2, -4, -4, -4, -6, -6, -8,
            -6, -8, -8, -10, -10, -10
    };

    // 配列的にアクセスするために、各電極の座標を計算して二次元配列にしたもの7
    // 上のposition配列では奇数や偶数になっているが、それを 0 ~ [各軸の電極数] に変換した値
    public static (int x, int y)[] convertedElectrodePos = new (int x, int y)[ELECTRODE_NUM]; // 電極にアクセスしやすいように、indexから座標に変換するための配列
    public float[] electrodeIntensities = new float[ELECTRODE_NUM]; // 各電極の刺激強度を決めるための値。圧力センサ値をベースに計算する
    
    void Awake() {
        // serialHandler.OnDataReceived += SerialEvent;
        
        for (var i = 0; i < ELECTRODE_NUM; i++) { 
            m_SwitchState[i] = 0;
        }

        // Position の変換. Start() などであらかじめやっておいてもいい
        
        for (var _k = 0; _k < ELECTRODE_NUM; _k++) {
            int xPos = ElectrodePosX[_k];
            if (xPos > 0) {
                convertedElectrodePos[_k].x = (xPos + 1) / 2;
            } else {
                convertedElectrodePos[_k].x = (xPos - 1) / 2;
            }
     
            int yPos = ElectrodePosY[_k];
            convertedElectrodePos[_k].y = yPos / 2 + 5;
        }
        
        

    }

    private void Start() {
        // 刺激はCoroutineで回すことによって一定周期で刺激できるようにしている
        // TODO: これは別スレッドでループを回したほうが安定化するか
        // StartCoroutine(Stimulation());
        m_IsRunning  = true;
        m_StimThread = new System.Threading.Thread(Stimulation);
        m_StimThread.Start();
    }

    // 圧力分布を基に電気刺激強度を決定する
    // パルス高さをどう決定するかは用検討
    // pressureDistribution を渡して、electrodeIntensities を更新する
    public void CalcIntensityWithForce(ref int[,,] pressureDistribution) {
        // int electrodeIndex = 0;
        int ELECTRODE_COL  = 6;
        int ELECTRODE_ROW  = 11;
        int finger         = 0;
        // 電極ベースで圧力センサのデータにアクセスして、刺激強度を決定する
        for (var index = 0; index < ELECTRODE_NUM; index++) {
            // 電極は一次元的に配列でアクセスする。一つ当たり 64点だとして、64~からはもう一つの電極
            // 現状は 64点電極を2つ使うことを想定している
            if (index < 64) finger = 0;
            else finger            = 1;
            
            // 一次元配列のインデックスから二次元的な座標を計算する.
            // ch: は...
            // 電極を上から見たときに左下を原点として、横軸をx, 縦軸をyとしたときの離散的な座標
            // TODO?: ch はもしかしたら、index と同じ値なのかもしれない
            (int ch, int xPos, int yPos) = ConvertToCh(index);
            if (ch < 0) continue;
            
            // 計算的には、電極の座標から対応する圧力センサの座標を計算して、その値から刺激強度を決める
            // というイメージ
            // 圧力センサは横軸が y, 縦軸が x になっているので，xとyと逆にする
            // さらに，x 軸を反転させる
            // ここの座標の対応付けはセットアップやプロトコルによって変化する
            int iy1 = 9 - Mathf.Clamp(Mathf.FloorToInt((xPos / (float)(ELECTRODE_COL-1)) * (float)(ForceSensor.SENSOR_X_NUM-1)), 0, 10-1);
            // int ixCeil  = Mathf.Clamp(Mathf.CeilToInt((xPos / (float)(ELECTRODE_COL-1)) * (ForceSensor.SENSOR_X_NUM-1)), 0, 10-1);
            int iy2 = Mathf.Clamp(iy1 + 1, 0, 10 - 1);
            int ix1 = Mathf.Clamp(Mathf.FloorToInt((yPos / (float)(ELECTRODE_ROW-1)) * (float)(ForceSensor.SENSOR_Y_NUM-1)), 0, 10-1);
            // int iyCeil  = Mathf.Clamp(Mathf.CeilToInt((yPos / (float)(ELECTRODE_ROW-1)) * (ForceSensor.SENSOR_Y_NUM-1)), 0, 10-1);
            ix1 = 9 - ix1; // x軸を反転させる (圧力センサの縦の方向)
            int ix2 = Mathf.Clamp(ix1 + 1, 0, 10 - 1);
            // Debug.Log($"[{this.GetType()}] index: {index} ixf: {ix1}, iyf: {iy1}");
            
            // ForceSensor.PressureRange での調整 (これであってる？)
            // 圧縮されて送られてくるなら，ビットシフトをする方向逆では？
            // 四か所のローパスを利用する
            int sum = pressureDistribution[finger, ix1, iy1]
                      + pressureDistribution[finger, ix1, iy2]
                      + pressureDistribution[finger, ix2, iy1]
                      + pressureDistribution[finger, ix2, iy2];
            electrodeIntensities[ch] = sum / 4f;
            // if (electrodeIntensities[ch]/ForceSensor.MAX_12BIT_VALUE >= 0.1f) Debug.Log($"[{GetType()}]ch: {ch} electrodeIntensity: {electrodeIntensities[ch]/ForceSensor.MAX_12BIT_VALUE:f5}");
        }
        
            
    }
    
    // 電極に左下から右上に順番にアクセスできるように配列の番号を返す
    public static (int ch, int x, int y) ConvertToCh(int index) {
        // index から channel の探索
        // index は 0 ~ 127 の値が渡される．電極数分
        // 左下から右上に向かって順番に電極にアクセスできるようにすることを想定
        // index に対応する ch (stimPattern などに利用している) を探索する
        int ch = -1;
        int ix, iy, x, y;
        
        // TODO: 座標の変換のしかた。少し特殊なので、別途解説資料を作成
        // ElectrodPosX, ElectrodePosY がベースになっている
        if (index < 64) {
            // 配列にアクセスするようのx, y
            // ConvertedElectrodePos の要素に対応する
            ix              = -1 * (index % 6 + 1);
            iy              = (index / 6);
            // 左下からカウントした際の (x, y) 座標
            x = index % 6;
            y = index / 6;
            if (index >= 60) {
                ix -= 1;
                x  += 1;
            }
            
        } else {
            // 配列にアクセスするようのx, y
            int _i          = index - 64;
            ix           = 7 - (_i % 6 + 1);
            iy           = _i / 6;
            // 左下からカウントした際の (x, y) 座標
            x = _i % 6;
            y = _i / 6;
            if (_i >= 60) {
                ix -= 1;
                x  += 1;
            }
        }

        // x と y の「位置」と一致する 「ch」を探す
        // convertedElectrodePosの並んでいる順番に基づいて、計算されたix, iyがどのindexのデータと対応するか、を探索している
        // マッチした index を ch としてreturn している。引数として渡されている index と同じ値になる？
        var extract = convertedElectrodePos
                      .Select(((tuple, i) => new { Content = tuple, Index = i }))
                      .Where(data => data.Content.x == ix && data.Content.y == iy)
                      .Select(data => data.Index);
        // Debug.Log($"[{extract.ToList().Count}]");
        ch = extract.ToList()[0]; // 必ず一つ抽出できるはずなので，0で固定
        // cPos.Select()
        return (ch, x, y);
    }

    // HorizontalBar や VerticalBar などの刺激をするために、電気刺激の刺激を決定する (stimPattern) を作るメソッド
    // Processing のコードをベースにしている
    void SetTestStimulation(int localTimer, ref byte[] stimPattern) {
        for (var ch = 0; ch < ELECTRODE_NUM; ch++) {
            float x = ElectrodePosX[ch];
            float y = ElectrodePosY[ch];
            if (mode is Mode.VerticalBar) {
                if ((x > (localTimer / 7f - 11f)) && (x < (localTimer / 7f - 9f))) {
                    // stimPattern[ch] = 1;
                    stimPattern[ch] = (byte)((1 << 2) | (3 & 0x03));
                } else {
                    // stimPattern[ch] = 0;
                    stimPattern[ch] = (byte)(0 | (3 & 0x03));
                }
            } else if (mode is Mode.HorizontalBar) {
                if ((y >= (localTimer / 4f - 10f)) && (y < (localTimer / 4f - 8f))) {
                    // stimPattern[ch] = 1;
                    stimPattern[ch] = (byte)((1 << 2) | (3 & 0x03));
                } else {
                    // stimPattern[ch] = 0;
                    stimPattern[ch] = (byte)(0 | (3 & 0x03));
                }
            }
        }
    }

    /// <summary>
    /// 牛山作成. 圧力センサの値 (electrodeIntensities) をベースに刺激位置と強度 (stimPattern) を決定する
    /// 圧力センサ値を利用して、刺激の強度 (現状は刺激するかどうかの確率を4段階で設けている) を決める
    /// TODO: うまく知覚のダイナミックレンジを提示できていない。
    /// </summary>
    /// <param name="stimPattern"></param>
    void SetStimulationWithForce(ref byte[] stimPattern) {

        // float[] intensityRange = {0.125f, 0.25f,0.5f,1.0f};
        // 確率的に刺激強度設定する。圧力センサの値が大きいほど、大きい刺激強度に設定される
        // 4段階で設定される
        // TODO: ここの強度設定の処理はかなり、牛山のさじ加減で適当に実装されたもの
        // もうすこし根拠に基づいて実装するように修正。どうするのが適切なのか
        
        // float[] intensityRange = {0.03125f, 0.0625f, 0.125f,0.25f};
        float[] intensityRange = {0.04f, 0.09f, 0.16f, 0.25f}; // TODO: 牛山が勘で決めたもの。要再検討
        for (var ch = 0; ch < ELECTRODE_NUM; ch++) {
            var   intensity      = electrodeIntensities[ch] / ForceSensor.MAX_12BIT_VALUE; // 0.0 ~ 1.0 になるはず;
            // この intensity との兼ね合いで intensityRange を決める。TODO: 一度どのぐらいのレンジでデータが送られてくるか確認する
            // Debug.Log($"[{GetType()}] intensity: {intensity:f5}");
            int   intensityIndex = 0;
            for (var i = 0; i < intensityRange.Length; i++) {
                if (intensity >= intensityRange[i]) {
                    intensityIndex = i;
                }
            }
            // まず、刺激するかどうか？の閾値を設定し、これを越えなければ刺激をしない

            // if(intensityIndex != 0) Debug.Log($"[{GetType()}] intensityIndex: {intensityIndex}");
            if (intensity >= intensityRange[0]) {
                // 刺激をする場合
                // xxxx 4bit で刺激について決定して、上位2bitが刺激をするかしないか、下位2bitで刺激の強度について設定
                // 中山君の手法を真似している
                // ESP32のコードを参照 (別途確率の閾値が決められていて、送信するのはあくまで "index" である)
                stimPattern[ch] = (byte)((1 << 2) | (intensityIndex & 0x03));
            } else {
                stimPattern[ch] = (byte)(0 | (intensityIndex & 0x03));
            }
        }
    }

    void ElectricalStimulation(int localTimer) {
        byte[] stimPattern = new byte[ELECTRODE_NUM];
        // Debug.Log($"{localTimer}");

        // electrodeSerialHandler.
        
        if (mode == Mode.Pressure || mode == Mode.PressureAndVibration) {
            SetStimulationWithForce(ref stimPattern);
            // electrodeIndicater2D.UpdateIndicatorWithIntensity(ref electrodeIntensities);
        } else if (mode == Mode.HorizontalBar || mode == Mode.VerticalBar) {
            SetTestStimulation(localTimer, ref stimPattern);
            // electrodeIndicater2D.UpdateIndicater(ref stimPattern); // Unity側の描画を更新するための処理 [only main thread]
        }

        // 必要だろうか？
        electrodeSerialHandler.ClearWriteBuffer(); // バッファにデータが残ってると、ずれてしまう？
            
        // debugStimPattern(ref stimPattern);
        electrodeSerialHandler.Write((byte)PC_ESP32_STIM_PATTERN);
        byte volL = (byte)(volume & 0x3F);
        byte volH = (byte) ((volume >> 6) & 0x3F);
        // Debug.Log($"vol_l {volL} vol_h {volH}");
        electrodeSerialHandler.Write(volL);
        electrodeSerialHandler.Write(volH);
        // electrodeSerialHandler.Write((byte)volume);
        electrodeSerialHandler.Write((byte)(width/10));
        electrodeSerialHandler.Write(stimPattern);
        this.stimPattern = stimPattern;
            
            
        // electrodeIndicater2D.UpdateStimParams(m_Volume, m_Width); // Unity画面の描画を更新するための処理
        // electrodeForceIndicator.UpdateStimParams(m_Volume, m_Width); // [only main thread]
    }

    // 強度を計算し、使いやすいように0~1に正規化する
    float CalcVibrationIntensity(int finger) {
        float average =  forceSensor.AveragePressureDistribution(finger);
        // 圧力センサ数が多い (100点) ので、平均値は使い方によって大きく変化する
        // debug してみる。指で押したときに、500 ~ 800 ぐらいになる
        // Debug.Log($"{this.GetType()}: {finger} - Ave {average}");
        float intensity = Mathf.Clamp(average / 400f, 0f, 1f);
        return intensity;
    }
    void VibratoryStimulation() {
        byte mapVal          = 255;
        byte thumb_intensity = (byte) (CalcVibrationIntensity(0) * mapVal);
        byte index_intensity = (byte) (CalcVibrationIntensity(1) * mapVal);
        
        electrodeSerialHandler.Write(PC_ESP32_VIBRATION);
        electrodeSerialHandler.Write(thumb_intensity);
        electrodeSerialHandler.Write(index_intensity);
        electrodeSerialHandler.Write(stimPattern); // dummy data
        
    }
    
    // 刺激するためのCoroutine
    // 一定周期で実行される
    // TODO: 電気刺激に関しては、より安定化させるために別スレッドを立てたほうが良いかも
    void Stimulation() {
        int localTimer = 0;
        while (electrodeSerialHandler.IsOpen() && m_IsRunning) { /* ! 無限ループさせているので注意 ! */
            if (!m_IsStimulation || mode == Mode.None) {
                try {
                    // 10ms (100Hz) ぐらいが限界. これ以上早くすると、うまく刺激できなくなる (原因がESP32側か PC側にあるかは、まだ不明)
                    System.Threading.Thread.Sleep(10); // 1 ms -> 1 kHz
                } catch (Exception e) {
                    Debug.LogError($"{this.GetType()}: Sleep is interrupted");
                }
                continue;
            }
            if (mode == Mode.Vibration) { 
                VibratoryStimulation();
            }
             else if (mode == Mode.Pressure || mode == Mode.HorizontalBar || mode == Mode.VerticalBar) {
                ElectricalStimulation(localTimer);
                localTimer = (localTimer + 1) % 160; // why 160?
            }
            else if (mode == Mode.PressureAndVibration)
            {
                VibratoryStimulation();
                ElectricalStimulation(localTimer);
                localTimer = (localTimer + 1) % 160; // why 160?
            }
            

            // Debug.Log($"{this.GetType()} - Stimulation()");
            // yield return null;
            // yield return new WaitForSeconds(0.005f); // 0.01周期 -> 100 Hz で刺激する
            try {
                // 10ms (100Hz) ぐらいが限界. これ以上早くすると、うまく刺激できなくなる (原因がESP32側か PC側にあるかは、まだ不明)
                System.Threading.Thread.Sleep(10); // 1 ms -> 1 kHz
            } catch (Exception e) {
                Debug.LogError($"{this.GetType()}: Sleep is interrupted");
                break;
            }
            
        }
        Debug.LogError($"{this.GetType()}: Stimulation Loop Exited!");
        // Application.Quit(); // 危険なので、ストップしてしまう
    }

    void debugStimPattern(ref byte[] stimPattern) {
        string str = "";
        foreach (var p in stimPattern) {
            str += $"{p}, ";
        }
        Debug.Log(str);
    }

    public void ToggleStimulation() {
        m_IsStimulation = !m_IsStimulation;
    }
    public void StopStimulation() {
        electrodeSerialHandler.ClearWriteBuffer();

        if (mode == Mode.Pressure || mode == Mode.HorizontalBar || mode == Mode.VerticalBar) {
            electrodeSerialHandler.Write((byte)PC_ESP32_STIM_PATTERN);
            electrodeSerialHandler.Write(0); // vol_l
            electrodeSerialHandler.Write(0); // vol_h
            electrodeSerialHandler.Write(0); // width
        } else if (mode == Mode.Vibration) {
            electrodeSerialHandler.Write(PC_ESP32_VIBRATION);
            electrodeSerialHandler.Write(0); // thumb
            electrodeSerialHandler.Write(0); // index
        }
        
        byte[] _stimPattern = new byte[ELECTRODE_NUM];
        electrodeSerialHandler.Write(_stimPattern);
    }

    private void OnApplicationQuit() {
        Debug.Log($"{this.GetType()}: OnApplicationQuit");
        m_IsRunning = false;
        m_StimThread.Interrupt();
        
        StopStimulation();
        // electrodeSerialHandler.Write((byte)PC_ESP32_STIM_PATTERN);
        // electrodeSerialHandler.Write(0); // vol_l
        // electrodeSerialHandler.Write(0); // vol_h
        // electrodeSerialHandler.Write(0); // width
        // byte[] _stimPattern = new byte[ELECTRODE_NUM];
        // electrodeSerialHandler.Write(_stimPattern);
        if (m_Polarity == -1) {
            electrodeSerialHandler.Write((byte)PC_ESP32_POLARITY_CHANGE);
        }
        

        // electrodeSerialHandler.Write(PC_ESP32_VIBRATION);
        // electrodeSerialHandler.Write(0);
        // electrodeSerialHandler.Write(0);
        electrodeSerialHandler.Close();
    }
    

    private void ChangePolarity() {
        // Polarity Change を送るだけでは、反応しないので、形式的に使われないデータを送る
        electrodeSerialHandler.Write((byte)PC_ESP32_POLARITY_CHANGE);
        // これらがなくても機能する？
        // electrodeSerialHandler.Write(0); // 1 byte
        // electrodeSerialHandler.Write(0); // 1 byte
        // electrodeSerialHandler.Write(m_StimPattern); // electrodeNum byte
        
    }

    public void SetStimulationMode(Mode nextMode) {
        mode = nextMode;
    }
    
    // Update内で、Volumeやパルス幅などのパラメータを受け付ける
    void Update() {
        int volumeMax = 4000;
        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            volume = Mathf.Clamp(volume + 50, 0, volumeMax);
        } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
            volume = Mathf.Clamp(volume - 50, 0, volumeMax);
        } else if (Input.GetKeyDown(KeyCode.H)) {
            volume = Mathf.Clamp(volume + 10, 0, volumeMax);
        } else if (Input.GetKeyDown(KeyCode.J)) {
            volume = Mathf.Clamp(volume - 10, 0, volumeMax);
        } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
            width = Mathf.Clamp(width + 10, 0, 200);
        } else if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            width = Mathf.Clamp(width - 10, 0, 200);
        } else if (Input.GetKeyDown(KeyCode.O)) {
            volume = 0;
        } else if (Input.GetKeyDown(KeyCode.P)) {
            Debug.LogWarning($"[{this.GetType()}] Polarity Changed!!");
            // electrodeSerialHandler.Write(PC_ESP32_POLARITY_CHANGE);
            ChangePolarity();
            m_Polarity *= -1;
        } else if (Input.GetKeyDown(KeyCode.M)) {
            //if (mode == Mode.HorizontalBar) {
            //mode = Mode.VerticalBar;
            //} 
            if (mode == Mode.PressureAndVibration){//島田追加
                mode = Mode.VerticalBar;
            }
            else if (mode == Mode.VerticalBar) {
                mode = Mode.Pressure;
            } else if (mode == Mode.Pressure) {
                mode = Mode.Vibration;
            } else if (mode == Mode.Vibration) {
                mode = Mode.HorizontalBar;
            } else if (mode == Mode.HorizontalBar){
                mode = Mode.PressureAndVibration;
            }
            if (mode == Mode.None) {
                mode = Mode.VerticalBar;
            }
        }
        // UpdateParameterText(m_Volume,  m_Width, m_Polarity); // updateで毎回呼び出している。負荷が大きくなる場合はコメントアウトする
        electrodeForceIndicator.UpdateParameterText(volume, width, m_Polarity, mode.ToString());
    }
}
}
