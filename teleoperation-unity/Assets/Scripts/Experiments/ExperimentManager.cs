using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Teleoperation;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ExperimentManager : MonoBehaviour {
    // 担当すること
    // 1. 条件の管理
    // 2. タスク遂行時間の計測
    // 3. 
    public enum State {
        DEBUG,
        EXPERIMENT
    }
    public State m_State = State.DEBUG;
    
    public string subjectName;
    public string subjectOld;
    public string dominantHand;
    public HV513 hv513 = default;
    public ForceSensor forceSensor = default;
    public TextMeshProUGUI conditionText = default;
    public CsvReader csvReader = default;
    public ExperimentCsvLogging experimentCsvLogging = default;
    public ExperimentCondition[] experimentConditions;

    private int m_FailNum = 0;
    private int m_CurrentTrial = 0;
    private bool m_IsStimulation = false;
    private string ElectricalStimulation = "e";
    private string Vibration = "v";
    private string NoStimulation = "n";
    private string m_CurrentStimulationCondition = null;
    private string m_CurrentPositionCondition = null;

    private Stopwatch m_Timer = new Stopwatch();
    public GameObject recordingNotifier = default;


    void Start() {
        string conditionFile = $"{subjectName}_condition.csv";
        experimentConditions = csvReader.ReadConditions(conditionFile);
        SetCondition();
        if (m_State == State.EXPERIMENT) {
            experimentCsvLogging.startLogging(subjectName, "task_time");
        }
    }

    void SetCondition() {
        string s = experimentConditions[m_CurrentTrial].stimulation;
        m_CurrentStimulationCondition = s;
        if (s == ElectricalStimulation) {
            /* set electrical stimulation */
            hv513.SetStimulationMode(HV513.Mode.Pressure);
        } else if (s == Vibration) {
            /* set */
            hv513.SetStimulationMode(HV513.Mode.Vibration);
        } else if (s == NoStimulation) {
            /* set */
            hv513.SetStimulationMode(HV513.Mode.None);
        }
        
        string p = experimentConditions[m_CurrentTrial].position;
        m_CurrentPositionCondition = p;
        /*
         * Notify position condition to an experimenter.
         */
        conditionText.text = $"stimulation type: {s}\nstart position: {p}";
    }
    
    void NextCondition() {
        m_CurrentTrial = Mathf.Clamp(m_CurrentTrial + 1, 0, experimentConditions.Length - 1);
        SetCondition();
        m_FailNum = 0;
    }

    void BackCondition() {
        m_CurrentTrial =  Mathf.Clamp(m_CurrentTrial - 1, 0, experimentConditions.Length - 1);
        SetCondition();
        m_FailNum = 0;
    }

    void StartTimer() {
        m_Timer.Reset();
        m_Timer.Start();
        recordingNotifier.SetActive(true);
        Debug.Log($"{this.GetType()}: Start Timer");
    }

    void EndTimer() {
        m_Timer.Stop();
        long   elapsedMilliSec = m_Timer.ElapsedMilliseconds;
        string result      = $"{subjectName},{subjectOld},{dominantHand},{m_FailNum},{hv513.volume},{hv513.width},{m_CurrentStimulationCondition},{m_CurrentPositionCondition},{elapsedMilliSec}";
        if (m_State == State.EXPERIMENT) {
            experimentCsvLogging.AddLineToCsv(result);
            Debug.Log($"{this.GetType()}: Added Result to Line");
        }
        recordingNotifier.SetActive(false);
        Debug.Log($"{this.GetType()}: End Timer");
    }
    
    void Update() {
        if (Input.GetKeyDown(KeyCode.N)) {
            NextCondition();
        } else if (Input.GetKeyDown(KeyCode.B)) {
            BackCondition();
        } else if (Input.GetKeyDown(KeyCode.T)) {
            if (m_Timer.IsRunning) EndTimer();
            else StartTimer();
        } else if (Input.GetKeyDown(KeyCode.O)) {
            forceSensor.SaveDistributionAsOffset();
        } else if (Input.GetKeyDown(KeyCode.F)) {
            m_FailNum += 1;
            Debug.Log($"{this.GetType()}: Number of Trial Fail = {m_FailNum}");
        } else if (Input.GetKeyDown(KeyCode.E)) {
            Debug.Log($"{this.GetType()}: Stimulation toggled");
            hv513.ToggleStimulation();
            hv513.StopStimulation();
        }
    }
}
