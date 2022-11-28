using System.Collections;
using System.Collections.Generic;
using Teleoperation;
using UnityEngine;

public class StimulationController : MonoBehaviour {
    public HV513 hv513;
    public ElectrodeIndicater2D electrodeIndicater2D;
    public ForceSensor forceSensor;
    public ForceSensorIndicator2D forceSensorIndicator2D;
    void Start() {
        
    }
    
    // 圧力分布の取得や電気刺激の高周波数との兼ね合いに注意する
    void Update() {
        // electrodeIndicater2D.UpdateIndicatorWithIntensity(ref electrodeIntensities);
        // electrodeIndicater2D.UpdateIndicator();
        
    }
}
