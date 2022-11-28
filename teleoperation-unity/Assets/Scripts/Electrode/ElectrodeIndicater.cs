using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Teleoperation {

    public class ElectrodeIndicater : MonoBehaviour {
        private const int ELECTRODE_NUM = 128;
        public GameObject electrodePrefab;
        public GameObject stimParamsPrefab;
        private GameObject[] m_Electrodes = new GameObject[ELECTRODE_NUM];

        private GameObject stimParamsIndicater;
        void Start() {
            // 上部の4点を描画する
            float xscale = transform.localScale.x;
            float yscale = transform.localScale.y;
            for (var ch = 0; ch < ELECTRODE_NUM; ch++) {
                if (ch < 64) {
                    int   x    = ch % 6;
                    x = 11 - HV513.ElectrodePosX[ch];
                    int   y    = ch / 6;
                    y = 10 - HV513.ElectrodePosY[ch];
                    float xpos = (x + x * 0f) * xscale;
                    float ypos = (y + y * 0f) * yscale;
                    m_Electrodes[ch] = Instantiate(electrodePrefab, new Vector3(xpos, -ypos, 0f),
                            Quaternion.identity,
                            transform);
                } else {
                    int   x    = (ch-64) % 6 + 9; // 9 <= 6 * 1.5
                    x = 11 - HV513.ElectrodePosX[ch];
                    int   y    = (ch-64) / 6;
                    y = 10 - HV513.ElectrodePosY[ch];
                    float xpos = (x + x * 0f) * xscale;
                    float ypos = (y + y * 0f) * yscale;
                    m_Electrodes[ch] = Instantiate(electrodePrefab, new Vector3(xpos, -ypos, 0f),
                            Quaternion.identity,
                            transform);
                }
            }

            stimParamsIndicater =
                    Instantiate(stimParamsPrefab, new Vector3(0f, 0f, 1f), Quaternion.identity, transform);
            stimParamsIndicater.GetComponent<MeshRenderer>().material.color = Color.green;

            // StartCoroutine(ColorTest());

        }

        IEnumerator ColorTest() {
            for (var i = 0; i < ELECTRODE_NUM; i++) {
                for (var ch = 0; ch < ELECTRODE_NUM; ch++) {
                    if (ch == i) m_Electrodes[ch].GetComponent<MeshRenderer>().material.color = Color.red;
                    else m_Electrodes[ch].GetComponent<MeshRenderer>().material.color         = Color.white;
                }
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        public void UpdateIndicater(ref byte[] stimPattern) {
            for (var ch = 0; ch < ELECTRODE_NUM; ch++) {
                if (m_Electrodes[ch] is null) continue;
                if (stimPattern[ch] == 1) m_Electrodes[ch].GetComponent<MeshRenderer>().material.color = Color.red;
                else m_Electrodes[ch].GetComponent<MeshRenderer>().material.color         = Color.white;
            }
        }

        public void UpdateStimParams(int amp, int width) {
            if (stimParamsIndicater is null) return;
            var scl = stimParamsIndicater.transform.localScale;
            stimParamsIndicater.transform.localScale = new Vector3(width / 10f, amp / 10f, scl.z);
        }
    }
}
