using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CsvReader: MonoBehaviour {
    protected string filePath; // Start で [Unity Project Path]/Assets/data/ になる
    // protected string fileName; // Controller から 指定される．
    protected string folderName;
    protected StreamReader sr;

    void Awake() {
        string dataPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
        folderName = "experiment_data"; // debug
        filePath   = Path.Combine(dataPath, folderName);
                // Debug.Log(filePath);
        // DirectoryUtils.SafeCreateDirectory(filePath);
    }
    void Start() {
        // ReadConditions("sample_condition.csv"); // debug
    }

    public ExperimentCondition[] ReadConditions(string fileName) {
        // csv serializer ? https://qiita.com/Kirikabu_ueda/items/23b4827abf5b8b6251bc
        sr = new StreamReader(Path.Combine(filePath, fileName));
        bool skipHeader = true; // header をスキップする
        if (skipHeader) {
            sr.ReadLine(); 
        }

        var conditions = new List<ExperimentCondition>() ;
        try {
            while (sr.Peek() >= 0) {
                string   text       = sr.ReadLine();
                if (text is null) break;
                string[]            subs                = text.Split(',');
                ExperimentCondition experimentCondition = new ExperimentCondition();
                experimentCondition.subjectName = subs[0];
                experimentCondition.stimulation = subs[1];
                experimentCondition.position    = subs[2];
                conditions.Add(experimentCondition);
                Debug.Log($"{this.GetType()}: conditions {text}");
            }
        } catch (IOException e) {
            Debug.LogError(e);
        }

        return conditions.ToArray();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
