using System.Collections;
using System.Collections.Generic;
using Teleoperation;
using TMPro;
using UnityEngine;

public class ElectrodeForceIndicator : MonoBehaviour {
    public HV513 hv513 = default;
    public ElectrodeIndicater2D electrodeIndicator2D = default;
    public ForceSensorIndicator2D forceSensorIndicator2D = default;
    // public TextMeshProUGUI stimulationParamsText = default;
    public GameObject stimulationParamsPanel = default;
    private TextMeshProUGUI stimulationParamsText = default;
    public GameObject stimParams2dPrefab;
    private GameObject stimParams2dIndicater;
    void Start() {
        if (electrodeIndicator2D is null) {
            electrodeIndicator2D = GetComponentInChildren<ElectrodeIndicater2D>();
        }
        if (forceSensorIndicator2D is null) {
            forceSensorIndicator2D = GetComponentInChildren<ForceSensorIndicator2D>();
        }

        if (stimulationParamsPanel is null) {
            stimulationParamsPanel = GameObject.Find("StimParamsPanel");
        }

        stimulationParamsText = stimulationParamsPanel.GetComponentInChildren<TextMeshProUGUI>();
        
        // パルス幅やパルス高さを表示するためのバーをinstantiateする
        Vector2 paramOffset = new Vector2(0f, 0f);
        InstantiateParameters(paramOffset);
    }
    
    // 刺激パラメータ用のTextオブジェクトのテキストを更新する
    // デバッグ用に電気刺激強度を表示するために、テキストを利用している
    public void UpdateParameterText(int vol, int width, int polarity, string mode) {
        string _text = $"volume: {vol}\nwidth: {width}\npolarity: {polarity}\nmode: {mode}";
        stimulationParamsText.text = _text;
    }
    
    void InstantiateParameters(Vector2 offset) {
        stimParams2dIndicater = Instantiate(stimParams2dPrefab, stimulationParamsPanel.transform);
        var rectTransform = stimParams2dIndicater.GetComponent<RectTransform>(); 
        // rectTransform.anchoredPosition
        var   panelRectTransform = stimulationParamsPanel.GetComponent<RectTransform>();
        float width              = panelRectTransform.rect.width;
        float height             = panelRectTransform.rect.height;
        Debug.Log($"panel width: {width}, height: {height}");
        var pos = new Vector2(0f + offset.x, -height + offset.y);
        rectTransform.anchoredPosition = pos;
        stimParams2dIndicater.GetComponent<UnityEngine.UI.Image>().color     = Color.green;
    }
    
    // パルス高さと幅の表示を更新する
    public void UpdateStimParams(int amp, int width) {
        if (stimParams2dIndicater is null) return;
        var   panelRectTransform = stimulationParamsPanel.GetComponent<RectTransform>();
        var   rectTransform      = stimParams2dIndicater.GetComponent<RectTransform>();
        float _amp               = amp / 10f;
        rectTransform.sizeDelta = new Vector2(width/2f,  _amp/2f);
        var   pos  = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = new Vector2(pos.x,-panelRectTransform.rect.height / 2f + (_amp/2f)/2f);
    }
    void Update() {
        electrodeIndicator2D.UpdateIndicator();
        UpdateStimParams((int)hv513.volume, (int)hv513.width);
    }
}
