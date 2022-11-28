using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Threading;

public class CSV_Experiment_Release : MonoBehaviour
{
    int try_num = 1;

    //さまざまなflag
    bool isFirst = true; //最初の1回目のファイルへの数値入力かどうか
    bool canMake = true; //新しいファイルを作成しても良いかどうか
    bool canStart = false; //実験を始めても良いかどうか
    bool canFinish = false; //終了して操作が成功したか失敗したかを入力しても良いかどうか

    StreamWriter sw; //ファイル

    //時間格納
    float firstTime; //タスク始めた際の時間

    void Start()
    {
        StartCoroutine(TimeManager());
        Debug.Log($"{try_num}試行目：Bキーを入力して新しいファイルを作成してください。");
    }

    IEnumerator TimeManager()
    {
        while (true)
        {
            float[] pv_sum_raw = ForceSensor.pv_sum_raw;  //圧力センサーからのrawデータ
            if (isFirst == true)
            {
                if (Input.GetKey(KeyCode.B) && canMake == true) //BuildのB, 新しいファイルの準備(作成)
                {
                    //新しいファイルを作成
                    sw = new StreamWriter(@$"Try_arm_{try_num}.csv", false);
                    string[] s1 = { "time[sec]", "pulse_O", "pulse_1" };
                    string s2 = string.Join(",", s1);
                    sw.WriteLine(s2);
                    canMake = false;
                    Debug.Log($"{try_num}試行目：新しいファイルを作成完了。実験開始の準備ができたらSキーを入力。");
                }
                else if (Input.GetKey(KeyCode.S) && canMake == false) //StartのS, 実験を開始する。
                {
                    canStart = true; //実験を始める準備が整った。
                    Debug.Log($"{try_num}試行目：実験を開始する準備ができました。掴んでスタート！");
                }
                else if (canStart == true)
                {
                    if ((pv_sum_raw[0] >= 4000) && (pv_sum_raw[1] >= 4000)) //圧力がかかった時実験開始
                    {
                        //最初の時間を保持する
                        firstTime = UnityEngine.Time.time;

                        float input_time = 0;
                        string[] str = { "" + input_time, "" + pv_sum_raw[0], "" + pv_sum_raw[1] };
                        string str2 = string.Join(",", str);
                        sw.WriteLine(str2);

                        //最初判定のフラグをfalseにする
                        isFirst = false;
                        canStart = false;
                        Debug.Log($"{try_num}試行目：実験が正常に開始されました。");
                    }
                }
                else if (Input.GetKey(KeyCode.F) && canFinish == true)
                { // FailのF, 操作に失敗した時に入力。
                    string[] str = { "Failed", "Failed", "Failed" };
                    string str2 = string.Join(",", str);
                    sw.WriteLine(str2);
                    sw.Close();
                    canMake = true;
                    canFinish = false;
                    Debug.Log($"{try_num}試行目：操作に失敗! 次にBキーを入力して新規ファイルを作成。");
                    try_num++;
                }
                else if (Input.GetKey(KeyCode.C) && canFinish == true)
                { // CompleteのC, 操作に成功した時に入力。
                    string[] str = { "Completed", "Completed", "Completed" };
                    string str2 = string.Join(",", str);
                    sw.WriteLine(str2);
                    sw.Close();
                    canMake = true;
                    canFinish = false;
                    Debug.Log($"{try_num}試行目：操作に成功! 次にBキーを入力して新規ファイルを作成。");
                    try_num++;
                }
                else if (Input.GetKey(KeyCode.R) && canFinish == true)
                { // RetryのR, 操作に成功した時に入力。
                    string[] str = { "Retry", "Retry", "Retry" };
                    string str2 = string.Join(",", str);
                    sw.WriteLine(str2);
                    sw.Close();
                    canMake = true;
                    canFinish = false;
                    Debug.Log($"{try_num}試行目：やり直し! 次にBキーを入力して新規ファイルを作成。");
                    try_num++;
                }

            }
            else
            {
                float input_time = UnityEngine.Time.time - firstTime;
                string[] str = { "" + input_time, "" + pv_sum_raw[0], "" + pv_sum_raw[1] };
                string str2 = string.Join(",", str);
                sw.WriteLine(str2);
                //Debug.Log(str2);

                if ((pv_sum_raw[0] < 4000) && (pv_sum_raw[1] < 4000)) //両方の指の圧力が十分低くなったら終了判定、
                {
                    //終了処理
                    isFirst = true;
                    canFinish = true;
                    Debug.Log($"{try_num}試行目：操作が終了! 次に失敗のFキーか, 成功のCキーか, やり直しのRキーを入力。");
                }
            }

            yield return new WaitForSeconds(.05f);
        }
    }


}
