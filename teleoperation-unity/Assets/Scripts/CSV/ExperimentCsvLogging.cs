using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text;

public class ExperimentCsvLogging : CsvLogging {
    protected string m_OutputPath = default;
    public override void startLogging(string subjectName, string _filePrefix) {
        fileName = Path.Combine(subjectName,  _filePrefix + "-" + System.DateTime.Now.ToString("yyMMdd-HHmmss"));
        // ディレクトリ確認を先にしておく
        // stream writer を先に開いてしまうと記録中に active window を変更した時エラーになってしまうので
        // 書き出す時にまとめる
        m_OutputPath = Path.Combine(filePath, subjectName);
        DirectoryUtils.SafeCreateDirectory(m_OutputPath);
    }

    public override void endLogging() {
        throw new System.NotImplementedException();
    }

    public void AddLineToCsv(string line) {
        using (sw = new StreamWriter(Path.Combine(filePath, fileName+".csv"), true, Encoding.UTF8)) {
            sw.WriteLine(line);
        }
    }
    public void AddDataLineToCsv(string[] elements) {
        string line = "";
        foreach (var e in elements) {
            line += e + ",";
        }

        using (sw = new StreamWriter(Path.Combine(filePath, fileName), true, Encoding.UTF8)) {
            sw.WriteLine(line);
        }
    }

    public void OutputCsv() {
        
    }

    protected override void writeCSV() {
        throw new System.NotImplementedException();
    }
}
