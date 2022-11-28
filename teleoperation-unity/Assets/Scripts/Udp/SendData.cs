using System;
using System.Collections;
using System.Collections.Generic;
using Teleoperation;
using UnityEngine;

public class SendData : MonoBehaviour {

    [SerializeField] CustomUdpClient _udpClient;

    private int value = 0;

    private byte b_val = 0;

    private float f_val = 0f;
    // Start is called before the first frame update
    void Start() {
        int val = 10;
        // _udpClient.Send(val);

        StartCoroutine(SendCoroutine());
    }

    private void OnDisable() {
        StopCoroutine("SendCoroutine");
    }

    IEnumerator SendCoroutine() {
        for (int i = 0; i < 10000; i++) {
            // byte a = (byte)i;
            f_val = (f_val + 1f);
            float[] a = new float[] {f_val, f_val + 1f, f_val + 2f};
            _udpClient.Send(a);
            yield return new WaitForSeconds(1f / 100f);
        }
    }

    void Update() {
        // value = (value + 1) % 100;
        // f_val = (f_val + 1f);
        // float[] a = new float[] {f_val, f_val + 1f, f_val + 2f};
        // _udpClient.Send(a);

    }
}
