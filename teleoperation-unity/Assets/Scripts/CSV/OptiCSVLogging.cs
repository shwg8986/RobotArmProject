using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
/* for writing/reading csv */
using System.IO;
using System.Text;
using UnityEngine.PlayerLoop;
using Debug = UnityEngine.Debug;

/// <summary>
/// データをCSV に書き出す
/// </summary>
public class OptiCSVLogging : CsvLogging {
    public enum RotationMode {
        Euler,
        Quaternion
    }
    [Header("GameObjects attached OptiTrack Client Rigid Body")]
    [SerializeField] protected GameObject[] targetObjects = default; // 保存するデータたち
    [SerializeField] protected float FPS = 100f;
    public RotationMode rotationMode = RotationMode.Quaternion;
    
    private Stopwatch stopwatch = new Stopwatch();
    private List<float> timeStamps;

    // ステート管理のためのフラグたち
    // これ以外の方法があったはずだけど...
    private bool isMeasuring = false;


    private Dictionary<string, QuatData> quatDatas;
    private Dictionary<string, EulerData> eulerDatas;
    private List<string> csvHeaders;
    // data の数を数える
    private int dataNum = 0;

    void initializeDataDictionary() {
        foreach (var tpar in targetObjects) {
            quatDatas.Add(tpar.name, new QuatData());
            eulerDatas.Add(tpar.name, new EulerData());
        }
    }

    void updateDataDictionary() {
        if (rotationMode == RotationMode.Quaternion) {
            foreach (var tpar in targetObjects) {
                var pos = tpar.transform.position;
                quatDatas[tpar.name].px.Add(pos.x);
                quatDatas[tpar.name].py.Add(pos.y);
                quatDatas[tpar.name].pz.Add(pos.z);
                var rot = tpar.transform.rotation;
                quatDatas[tpar.name].qx.Add(rot.x);
                quatDatas[tpar.name].qy.Add(rot.y);
                quatDatas[tpar.name].qz.Add(rot.z);
                quatDatas[tpar.name].qw.Add(rot.w);
            }
        } else if (rotationMode == RotationMode.Euler) {
            foreach (var tpar in targetObjects) {
                var pos = tpar.transform.position;
                eulerDatas[tpar.name].px.Add(pos.x);
                eulerDatas[tpar.name].py.Add(pos.y);
                eulerDatas[tpar.name].pz.Add(pos.z);
                var angle = tpar.transform.eulerAngles;
                eulerDatas[tpar.name].rx.Add(angle.x);
                eulerDatas[tpar.name].ry.Add(angle.y);
                eulerDatas[tpar.name].rz.Add(angle.z);
                }
        }
    }

    override protected void Awake() {
        base.Awake();


        if (targetObjects.Length <= 0) {
            Debug.LogError("Objects for csv log were not set");
            Application.Quit();
        }

        quatDatas  = new Dictionary<string, QuatData>();
        eulerDatas = new Dictionary<string, EulerData>();
        initializeDataDictionary();
        timeStamps = new List<float>();
        // fixedUpdate の FPS を 100Hz に
        Time.fixedDeltaTime = 1.0f / FPS;
        // CSV ファイルのヘッダー
        csvHeaders = new List<string>();

        // 回転情報の保存の仕方によって変数の個数が変化するので分岐
        if (rotationMode == RotationMode.Quaternion) {
            foreach (var key in quatDatas.Keys) {
                foreach (var suffix in QuatData.suffixes) {
                    csvHeaders.Add(key + suffix);
                }
            }
        } else if (rotationMode == RotationMode.Euler) {
            foreach (var key in eulerDatas.Keys) {
                foreach (var suffix in EulerData.suffixes) {
                    csvHeaders.Add(key + suffix);
                }
            }
        }
    }

    IEnumerator UpdateCoroutine() {
        while (isMeasuring) {
            timeStamps.Add(stopwatch.ElapsedMilliseconds / 1000f);
            updateDataDictionary();
            dataNum++;
            yield return new WaitForSeconds(1f / FPS);
        }
    }

    /// <summary>
    /// Rigidbody のtransformの更新に合わせる
    /// targetFPSを120f としてUpdateで更新するので、Updateの後に記録するようにする
    /// </summary>
    void FixedUpdate() {
        // if (isMeasuring) {
        //     timeStamps.Add(stopwatch.ElapsedMilliseconds / 1000f);
        //     updateDataDictionary();
        //     dataNum++;
        // }
    }

    override public void startLogging(string subjectName, string _filePrefix) {
        fileName = Path.Combine(subjectName,  _filePrefix + "-" + System.DateTime.Now.ToString("yyMMdd-HHmmss"));
        // ディレクトリ確認を先にしておく
        // stream writer を先に開いてしまうと記録中に active window を変更した時エラーになってしまうので
        // 書き出す時にまとめる
        DirectoryUtils.SafeCreateDirectory(Path.Combine(filePath, subjectName));
        isMeasuring = true;
        
        stopwatch.Reset();
        stopwatch.Start();

        StartCoroutine(UpdateCoroutine());
    }

    override public void endLogging() {
        if (!isMeasuring) return; // measure が開始されていないと書き出せないようにする
        isMeasuring = false;
        writeCSV();
    }

    /// <summary>
    /// CSVに書き出すデータを整形する
    /// </summary>
    protected void formattingData(ref float[][] writeData, ref float[,] formattedData) {
        // メモリ管理気を付ける
        int index = 0;
        if (rotationMode == RotationMode.Quaternion) {
            foreach (var paq in quatDatas.Values) {
                writeData[index++] = paq.px.ToArray();
                writeData[index++] = paq.py.ToArray();
                writeData[index++] = paq.pz.ToArray();
                writeData[index++] = paq.qx.ToArray();
                writeData[index++] = paq.qy.ToArray();
                writeData[index++] = paq.qz.ToArray();
                writeData[index++] = paq.qw.ToArray();
            }
        } else if (rotationMode == RotationMode.Euler) {
            Debug.Log($"{this.GetType()}: formattingData - EulerMode!");
            foreach (var paq in eulerDatas.Values) {
                writeData[index++] = paq.px.ToArray();
                writeData[index++] = paq.py.ToArray();
                writeData[index++] = paq.pz.ToArray();
                writeData[index++] = paq.rx.ToArray();
                writeData[index++] = paq.ry.ToArray();
                writeData[index++] = paq.rz.ToArray();
            }
        }

        // 転置する作業
        for (int i = 0; i < formattedData.GetLength(0); i++) {
            for (int j = 0; j < formattedData.GetLength(1); j++) {
                formattedData[i, j] = writeData[j][i];
            }
        }
    }

    override protected void writeCSV() {


        // それぞれのデータを連結して転置するという処理をしたい
        // 列ごとに並んでる方が見やすいので...
        Debug.Log($"{GetType()}: csvHeaders: {csvHeaders}");
        float[][] writeData = new float[csvHeaders.Count][];
        float[,] formattedData = new float[dataNum, csvHeaders.Count];
        formattingData(ref writeData, ref formattedData);

        /*
        foreach (var d in formattedData) {
            Debug.Log(d);
        }
        */
        string path = Path.Combine(filePath, fileName + ".csv");
        sw = new StreamWriter(path, false, Encoding.UTF8);
        sw.WriteLine("timestamp, " + string.Join(",", csvHeaders));  // header の書き出し
        for (int i = 0; i < formattedData.GetLength(0); i++) {
            string line = timeStamps[i] + ",";
            for (int j = 0; j < formattedData.GetLength(1); j++) {
                //TODO: data の種類に合わせてデータの指定子(?)を変えられるように
                line += string.Format("{0:f10},", formattedData[i, j]);
            }
            sw.WriteLine(line);
        }

        sw.Flush();
        sw.Close();

        // データの初期化
        // ガベージコレクション頼んだ～
        quatDatas.Clear();
        eulerDatas.Clear();
        initializeDataDictionary();
        timeStamps.Clear();
        dataNum = 0;
        sw = null;
        Debug.Log("End write CSV");
    }

    void OnDestroy() {
        if (sw != null) {
            sw.Flush();
            sw.Close();
        }
    }
}
