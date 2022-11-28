using System.Collections;
using System.Collections.Generic;

using System.IO; // StreamWriter
using UnityEngine;

/// <summary>
/// CSV log の Base クラス
/// 基本は StringBuilder で記録して最後に書き出す方式
/// 継承して書き出すデータを設定するだけで使えるようにする
/// </summary>
public abstract class CsvLogging : MonoBehaviour {
    protected string filePath; // Start で [Unity Project Path]/Assets/data/ になる
    protected string fileName; // Controller から 指定される．
    protected string filePrefix; // Controller から指定される
    protected StreamWriter sw;
    public string folderName = "OptiData";
    virtual protected void Awake() {
        string dataPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
        filePath = Path.Combine(dataPath, folderName);
        // Debug.Log(filePath);
        DirectoryUtils.SafeCreateDirectory(filePath);
    }

    abstract public void startLogging(string subjectName, string _filePrefix);
    abstract public void endLogging();
    abstract protected void writeCSV();
}