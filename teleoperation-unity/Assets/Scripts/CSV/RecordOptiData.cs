using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;

public class RecordOptiData : MonoBehaviour {
    [SerializeField] CsvLogging csvLogging = default;
    [SerializeField] private TMP_Text text;
    private Stopwatch m_SW = new Stopwatch();
    private long m_RecordingDuration = 10 * 1000L; // 10 sec in millisec

    private string m_DefaultText = "waiting...";
    private string m_RecordingText = "recording";
    
    private string m_FilePrefix = "teleperation";

    private void Start() {
        text.text = m_DefaultText;
    }

    void OnRecord() {
        text.text  = m_RecordingText;
        text.color = Color.red;
    }

    void OnEnd() {
        text.text = m_DefaultText;
        text.color = Color.white;
    }
    
    void Update() {
        if (Input.GetKeyDown(KeyCode.R)) {
            csvLogging.startLogging("test", m_FilePrefix);
            m_SW.Reset();
            m_SW.Start();
            OnRecord();
        }

        if (Input.GetKeyDown(KeyCode.E)) {
            m_SW.Stop();
            m_SW.Reset();
            OnEnd();
            csvLogging.endLogging();
        }

        // if (m_SW.ElapsedMilliseconds > m_RecordingDuration) {
        //     m_SW.Stop();
        //     m_SW.Reset();
        //     OnEnd();
        //     csvLogging.endLogging();
        // }
    }
}
