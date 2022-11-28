using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 圧力分布を描画するためのクラス
/// Imageオブジェクトを使って表示している
/// </summary>
public class ForceSensorIndicator2D : MonoBehaviour {
    public GameObject sensor2dPrefab;
    public GameObject textPrefab;
    private  GameObject[] m_SensorsObjects = new GameObject[ForceSensor.SENSOR_NUM];
    private GameObject[] m_IndexTexts = new GameObject[ForceSensor.SENSOR_NUM];
    private GameObject[] m_SensorValTexts = new GameObject[ForceSensor.SENSOR_NUM];
    
    const int WINDOW_SIZE_X = 1500;
    const int WINDOW_SIZE_Y = 1000;
    const int PIXEL_SIZE_X =  WINDOW_SIZE_X / ForceSensor.FINGER_NUM / (ForceSensor.SENSOR_X_NUM + 1);
    const int PIXEL_SIZE_Y = WINDOW_SIZE_Y / 2 / (ForceSensor.SENSOR_Y_NUM);
    float[] THERMAL_XCOORD = { 5.0f, 6.0f };
    float[] THERMAL_YCOORD = { 3.0f, 7.0f };
    
    public ForceSensor forceSensor;

    void Awake() {
        Vector2 sensorOffset = new Vector2(0f, 100f);
        InstantiateSensors(sensorOffset);
    }

    void InstantiateSensors(Vector2 offset) {
        float   x, y, finger, xoffset;
        float rectSize = 25f;
        //ask mbed to send serial data if there is no data
        //  receiveData();
        int index = 0;
        for (finger=0; finger<ForceSensor.FINGER_NUM; finger++) {
            xoffset = -1 * (ForceSensor.SENSOR_X_NUM+1) * rectSize * finger; // [catuion] 電極の対応関係に合わせるために -1 をかけてる
            if (finger >= 2) break; // third sensor will not be drawn
            for (y=0; y<ForceSensor.SENSOR_X_NUM; y++) {
                for (x=0; x<ForceSensor.SENSOR_Y_NUM; x++) { // [caution] 電極の方向に合わせるために、x軸から for-loop をまわす
                    // var _x = (ForceSensor.SENSOR_X_NUM - x) * rectSize + xoffset + offset.x;
                    // var _y = y * rectSize + offset.y;
                    var _x = (ForceSensor.SENSOR_X_NUM - x) * rectSize + xoffset + offset.x;
                    var _y = rectSize * ForceSensor.SENSOR_Y_NUM - y * rectSize + offset.y; // [caution] 電極の方向に合わせるために、上から配置していく
                    m_SensorsObjects[index] = Instantiate(sensor2dPrefab, transform);
                    // index text
                    // m_IndexTexts[index]     = Instantiate(indexTextPrefab, m_SensorsObjects[index].transform);
                    // TextMeshProUGUI m_TMP = m_IndexTexts[index].GetComponent<TextMeshProUGUI>();
                    // m_TMP.text     = index.ToString();
                    // m_TMP.fontSize = rectSize / 2f;
                    // ---
                    // sensor values text
                    m_SensorValTexts[index] = Instantiate(textPrefab, m_SensorsObjects[index].transform);
                    TextMeshProUGUI m_TMP = m_SensorValTexts[index].GetComponent<TextMeshProUGUI>();
                    m_TMP.text     = "0000";
                    m_TMP.fontSize = rectSize / 3f;
                    // ---
                    var rt = m_SensorsObjects[index].GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(_x, _y);
                    // rt.anchoredPosition = new Vector2(-1 * _y, _x); // invert x and y to match direction to electrodes
                    rt.sizeDelta        = new Vector2(rectSize, rectSize);
                    index++;
                    // rect((ForceSensor.SENSOR_X_NUM-x)*PIXEL_SIZE_X + xoffset, y*PIXEL_SIZE_Y, PIXEL_SIZE_X, PIXEL_SIZE_Y);
                }
            }

#region not_used
            // for (x=0; x<ForceSensor.THERMAL_NUM; x++) {
            //     fill((int)((ThermalDistribution[finger][x]-ForceSensor.THERMAL_OFFSET)*ForceSensor.THERMAL_AMP), 20, (ForceSensor.THERMAL_MAX - ThermalDistribution[finger][x])*ForceSensor.THERMAL_AMP);
            //     rect((int)(THERMAL_XCOORD[x]*(float)PIXEL_SIZE_X) + xoffset, WINDOW_SIZE_Y/2 + (int)(THERMAL_YCOORD[x]*(float)PIXEL_SIZE_Y), PIXEL_SIZE_X, PIXEL_SIZE_Y);
            //     fill(0, 100, 150);
            //     text(ThermalDistribution[finger][x], (int)(THERMAL_XCOORD[x]*(float)PIXEL_SIZE_X) + xoffset, WINDOW_SIZE_Y/2 + (int)(THERMAL_YCOORD[x]*(float)PIXEL_SIZE_Y));
            // }
            

#endregion
            
        }
    }
    
    // 圧力センサのデータをディスプレイに表示するためのメソッド
    // image オブジェクトの色を変えることで表示するようにしている。
    
    public void UpdateDisplay(ref int[,,] pressureDistribution) {
        int index = 0;
        for (var finger = 0; finger < ForceSensor.FINGER_NUM; finger++) {
            for (var x = 0; x < ForceSensor.SENSOR_X_NUM; x++) {
                for (var y = 0; y < ForceSensor.SENSOR_Y_NUM; y++) {
                    if (m_SensorsObjects[index] == null)
                    {
                        continue;
                    }
                    var img = m_SensorsObjects[index].GetComponent<Image>();
                    int pv  = pressureDistribution[finger, x, y];
                    m_SensorValTexts[index].GetComponent<TextMeshProUGUI>().text = pv.ToString();
                    float maxVal = 2000f;
                    float g      = Mathf.Clamp(pv / maxVal, 0f, 1f);
                    //Debug.Log($"{this.GetType()}: {pv}");
                    var c   = new Color(20f/255f, 
                            g, 
                            20f/255f);
                    //Debug.Log($"{this.GetType()}: {c}");
                    img.color = c;
                    index++;
                    //Debug.Log($"pressureDistribution : {pv}");
                }
            }
        }
    }
    public int TouchedSensor(ref int[,,] pressureDistribution)
    {
        int index = 0;
        //var TouchedSensor = false;
        int pv_sum = 0;
        for (var finger = 0; finger < ForceSensor.FINGER_NUM; finger++)
        {
            for (var x = 0; x < ForceSensor.SENSOR_X_NUM; x++)
            {
                for (var y = 0; y < ForceSensor.SENSOR_Y_NUM; y++)
                {
                    if (m_SensorsObjects[index] == null)
                    {
                        continue;
                    }
                    var img = m_SensorsObjects[index].GetComponent<Image>();
                    int pv = pressureDistribution[finger, x, y];
                    pv_sum += pressureDistribution[finger, x, y];
                    m_SensorValTexts[index].GetComponent<TextMeshProUGUI>().text = pv.ToString();
                    float maxVal = 2000f;
                    float g = Mathf.Clamp(pv / maxVal, 0f, 1f);
                    //Debug.Log($"{this.GetType()}: {pv}");
                    index++;
                    //Debug.Log($"pressureDistribution : {pv}");

                    //if (pv >= 100)
                    //{
                    //    TouchedSensor = true;
                    //    continue;
                    //}
                }
            }
        }
        //pv_sum = pv_sum >= 1000 ? pv_sum/15000 : 0;
        pv_sum = pv_sum >= 1000 ? pv_sum*127/150000 : 0;
        pv_sum = pv_sum > 127 ? (int)127 : pv_sum;
        //Debug.Log($"pressureDistribution : {pv_sum}");
        return pv_sum;
        //return TouchedSensor;
    }

    public float[] SensorTouched_Raw(ref int[,,] pressureDistribution)
    {
        int index = 0;
        //var TouchedSensor = false;
        float[] pv_sum = {0, 0};
        for (var finger = 0; finger < ForceSensor.FINGER_NUM; finger++)
        {
            for (var x = 0; x < ForceSensor.SENSOR_X_NUM; x++)
            {
                for (var y = 0; y < ForceSensor.SENSOR_Y_NUM; y++)
                {
                    if (m_SensorsObjects[index] == null)
                    {
                        continue;
                    }
                    var img = m_SensorsObjects[index].GetComponent<Image>();
                    float pv = pressureDistribution[finger, x, y];
                    if(finger == 0)
                    {
                        pv_sum[0] += pressureDistribution[finger, x, y];
                    }else if(finger == 1)
                    {
                        pv_sum[1] += pressureDistribution[finger, x, y];
                    }
                    index++;
                }
            }
        }
        return pv_sum;
        //return TouchedSensor;
    }

}
