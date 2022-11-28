using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Threading;

public class CSV_Experiment_Release : MonoBehaviour
{
    int try_num = 1;

    //���܂��܂�flag
    bool isFirst = true; //�ŏ���1��ڂ̃t�@�C���ւ̐��l���͂��ǂ���
    bool canMake = true; //�V�����t�@�C�����쐬���Ă��ǂ����ǂ���
    bool canStart = false; //�������n�߂Ă��ǂ����ǂ���
    bool canFinish = false; //�I�����đ��삪�������������s����������͂��Ă��ǂ����ǂ���

    StreamWriter sw; //�t�@�C��

    //���Ԋi�[
    float firstTime; //�^�X�N�n�߂��ۂ̎���

    void Start()
    {
        StartCoroutine(TimeManager());
        Debug.Log($"{try_num}���s�ځFB�L�[����͂��ĐV�����t�@�C�����쐬���Ă��������B");
    }

    IEnumerator TimeManager()
    {
        while (true)
        {
            float[] pv_sum_raw = ForceSensor.pv_sum_raw;  //���̓Z���T�[�����raw�f�[�^
            if (isFirst == true)
            {
                if (Input.GetKey(KeyCode.B) && canMake == true) //Build��B, �V�����t�@�C���̏���(�쐬)
                {
                    //�V�����t�@�C�����쐬
                    sw = new StreamWriter(@$"Try_arm_{try_num}.csv", false);
                    string[] s1 = { "time[sec]", "pulse_O", "pulse_1" };
                    string s2 = string.Join(",", s1);
                    sw.WriteLine(s2);
                    canMake = false;
                    Debug.Log($"{try_num}���s�ځF�V�����t�@�C�����쐬�����B�����J�n�̏������ł�����S�L�[����́B");
                }
                else if (Input.GetKey(KeyCode.S) && canMake == false) //Start��S, �������J�n����B
                {
                    canStart = true; //�������n�߂鏀�����������B
                    Debug.Log($"{try_num}���s�ځF�������J�n���鏀�����ł��܂����B�͂�ŃX�^�[�g�I");
                }
                else if (canStart == true)
                {
                    if ((pv_sum_raw[0] >= 4000) && (pv_sum_raw[1] >= 4000)) //���͂����������������J�n
                    {
                        //�ŏ��̎��Ԃ�ێ�����
                        firstTime = UnityEngine.Time.time;

                        float input_time = 0;
                        string[] str = { "" + input_time, "" + pv_sum_raw[0], "" + pv_sum_raw[1] };
                        string str2 = string.Join(",", str);
                        sw.WriteLine(str2);

                        //�ŏ�����̃t���O��false�ɂ���
                        isFirst = false;
                        canStart = false;
                        Debug.Log($"{try_num}���s�ځF����������ɊJ�n����܂����B");
                    }
                }
                else if (Input.GetKey(KeyCode.F) && canFinish == true)
                { // Fail��F, ����Ɏ��s�������ɓ��́B
                    string[] str = { "Failed", "Failed", "Failed" };
                    string str2 = string.Join(",", str);
                    sw.WriteLine(str2);
                    sw.Close();
                    canMake = true;
                    canFinish = false;
                    Debug.Log($"{try_num}���s�ځF����Ɏ��s! ����B�L�[����͂��ĐV�K�t�@�C�����쐬�B");
                    try_num++;
                }
                else if (Input.GetKey(KeyCode.C) && canFinish == true)
                { // Complete��C, ����ɐ����������ɓ��́B
                    string[] str = { "Completed", "Completed", "Completed" };
                    string str2 = string.Join(",", str);
                    sw.WriteLine(str2);
                    sw.Close();
                    canMake = true;
                    canFinish = false;
                    Debug.Log($"{try_num}���s�ځF����ɐ���! ����B�L�[����͂��ĐV�K�t�@�C�����쐬�B");
                    try_num++;
                }
                else if (Input.GetKey(KeyCode.R) && canFinish == true)
                { // Retry��R, ����ɐ����������ɓ��́B
                    string[] str = { "Retry", "Retry", "Retry" };
                    string str2 = string.Join(",", str);
                    sw.WriteLine(str2);
                    sw.Close();
                    canMake = true;
                    canFinish = false;
                    Debug.Log($"{try_num}���s�ځF��蒼��! ����B�L�[����͂��ĐV�K�t�@�C�����쐬�B");
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

                if ((pv_sum_raw[0] < 4000) && (pv_sum_raw[1] < 4000)) //�����̎w�̈��͂��\���Ⴍ�Ȃ�����I������A
                {
                    //�I������
                    isFirst = true;
                    canFinish = true;
                    Debug.Log($"{try_num}���s�ځF���삪�I��! ���Ɏ��s��F�L�[��, ������C�L�[��, ��蒼����R�L�[����́B");
                }
            }

            yield return new WaitForSeconds(.05f);
        }
    }


}
