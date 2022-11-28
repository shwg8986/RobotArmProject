using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Teleoperation {

    public class ElectrodeIndicater2D : MonoBehaviour {
        private const int ELECTRODE_NUM = 128;
        public HV513 hv513;
        public GameObject electrode2dPrefab;
        public GameObject indexTextPrefab;

        private GameObject[] m_Electrodes = new GameObject[ELECTRODE_NUM];
        private GameObject[] m_IndexTexts = new GameObject[ELECTRODE_NUM];


        private RectTransform canvasRectTrans = default;
        private Canvas canvas = default;
        private float windowWidth = default;
        private float windowHeight = default;
        
        // スタートで電極数だけ Instantiate する
        // 描画の仕方は Processing のコードをベースにした
        void Start() {
            canvasRectTrans = GetComponent<RectTransform>();
            canvas          = GetComponent<Canvas>();
            windowWidth     = canvasRectTrans.rect.width;
            windowWidth     = canvasRectTrans.rect.height;
            // canvas.
            Debug.Log($"Window Width: {windowWidth}, Window Height: {windowHeight}");
            
            Vector2 offset = new Vector2(-200f, -50f);
            InstantiateElectrodes(offset);
           
            // StartCoroutine(ColorTest());
        }

        void InstantiateElectrodes(Vector2 offset) {
            // 上部の4点を描画する
            // float xscale = transform.localScale.x;
            // float yscale = transform.localScale.y;
            float xscale = 1f;
            float yscale = 1f;
            float width  = 25f;
            float height = 25f;
            float gap    = 25f;
            for (var ch = 0; ch < ELECTRODE_NUM; ch++) {
                if (ch < 64) {
                    int   x    = ch % 6;
                    x = 11 - HV513.ElectrodePosX[ch];
                    int   y    = ch / 6;
                    y = 10 - HV513.ElectrodePosY[ch];
                    float xpos = (width + x * width/1.5f) * xscale + offset.x;
                    float ypos = (height + y * height/1.5f) * yscale + offset.y;
                    m_Electrodes[ch] = Instantiate(electrode2dPrefab, transform);
                    // index text
                    // m_IndexTexts[ch] = Instantiate(indexTextPrefab, m_Electrodes[ch].transform);
                    // (int cCh, int __, int _) = HV513.ConvertToCh(ch);
                    // TextMeshProUGUI m_TMP = m_IndexTexts[ch].GetComponent<TextMeshProUGUI>();
                    // m_TMP.text     = (cCh).ToString();
                    // m_TMP.fontSize = width / 2f;
                    // -----
                    // m_IndexTexts[ch].GetComponent<TextMeshProUGUI>().text = (ch).ToString(); // 一応分かりやすいように番号も表示する
                    var rt     = m_Electrodes[ch].GetComponent<RectTransform>();
                    // var width  = rt.rect.width;
                    // var height = rt.rect.height;
                    rt.sizeDelta        = new Vector2(width, height);
                    rt.anchoredPosition = new Vector2(xpos, -ypos); // 2d なので RectTransform で位置を指定してあげる

                } else {
                    int   x    = (ch-64) % 6 + 9; // 9 <= 6 * 1.5
                    x = 11 - HV513.ElectrodePosX[ch];
                    int   y    = (ch-64) / 6;
                    y = 10 - HV513.ElectrodePosY[ch];
                    float xpos = (width + x * width/1.5f) * xscale + offset.x - gap;
                    float ypos = (height + y * height/1.5f) * yscale + offset.y;
                    m_Electrodes[ch] = Instantiate(electrode2dPrefab, transform);
                    // index text
                    // m_IndexTexts[ch] = Instantiate(indexTextPrefab, m_Electrodes[ch].transform);
                    // (int cCh, int __, int _) = HV513.ConvertToCh(ch);
                    // TextMeshProUGUI m_TMP = m_IndexTexts[ch].GetComponent<TextMeshProUGUI>();
                    // m_TMP.text     = (cCh).ToString();
                    // m_TMP.fontSize = width / 2f;
                    // --- 
                    var rt = m_Electrodes[ch].GetComponent<RectTransform>();
                    // var width  = rt.rect.width;
                    // var height = rt.rect.height;
                    rt.sizeDelta        = new Vector2(width, height);
                    rt.anchoredPosition = new Vector2(xpos, -ypos);
                }
            }
        }

        IEnumerator ColorTest() {
            for (var i = 0; i < ELECTRODE_NUM; i++) {
                (int ch, int x, int y) = HV513.ConvertToCh(i);
                m_Electrodes[ch].GetComponent<Image>().color = Color.red;
                yield return new WaitForSeconds(0.1f);
            }
        }

        public void UpdateIndicator() {
            if (hv513.mode == HV513.Mode.Pressure || hv513.mode == HV513.Mode.PressureAndVibration) {
                UpdateIndicatorWithIntensity(ref hv513.electrodeIntensities);
            } else if (hv513.mode == HV513.Mode.HorizontalBar || hv513.mode == HV513.Mode.VerticalBar) {
                UpdateIndicaterSimple(hv513.stimPattern);
            }
        }
        
        // 0 or 1 、刺激するか・しないかで電極の描画を更新する
        public void UpdateIndicaterSimple(byte[] stimPattern) {
            for (var ch = 0; ch < ELECTRODE_NUM; ch++) {
                if (m_Electrodes[ch] is null) continue;
                if ((stimPattern[ch] >> 2 /* 上位2bit*/) == 1) {
                    var c = m_Electrodes[ch].GetComponent<Image>().color;
                    m_Electrodes[ch].GetComponent<Image>().color = new Color(c.r + 0.5f, c.g, c.b);
                } else {
                    var c = m_Electrodes[ch].GetComponent<Image>().color;
                    m_Electrodes[ch].GetComponent<Image>().color = Color.black;
                }
                // else m_Electrodes[ch].GetComponent<Image>().color         = Color.white;
            }
        }

        // 刺激強度を元に、描画を更新する
        public void UpdateIndicatorWithIntensity(ref float[] electrodeIntencities) {
            for (var ch = 0; ch < ELECTRODE_NUM; ch++) {
                var col = Mathf.Clamp(2f * (electrodeIntencities[ch]) / ForceSensor.MAX_12BIT_VALUE, 0f, 1f);
                m_Electrodes[ch].GetComponent<Image>().color =
                        new Color(20f / 255f, col, 20f / 255f);
            }
        }


    }
}
