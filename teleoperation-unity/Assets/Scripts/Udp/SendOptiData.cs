using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

namespace Teleoperation {
    public class SendOptiData : MonoBehaviour {
        enum DataMode {
            OnlyPos,
            PosAndHandRot,
        }
        [SerializeField] private CustomUdpClient udpClient;
        [SerializeField] private Transform thumb; // 送信するデータを持ったGameobjectのTransformをアタッチする
        [SerializeField] private Transform index; // 同上
        [SerializeField] private Transform hand; // 同上
        [SerializeField] private DataMode dataMode = DataMode.PosAndHandRot;
        private float _timestamp; // データを送信する際にタイムスタンプを使用する際に利用 (現在は未使用)
        private Stopwatch _sw; // timestamp を作るために stopwatch を利用
        void Start() {
            _sw = Stopwatch.StartNew();
            // 一定の周期で送りたいので Coroutine を使ってデータを送信
            StartCoroutine(SendData()); 
        }

        private void OnDisable() {
            // ここで，通信終了時に何かパケットを送るとかもできる
            // ---
            _sw.Stop();
            StopCoroutine(SendData());
        }

        // 位置をUnity から ROSの座標系 (MyCobotはROSの座標系と同じ) に変換する
        // https://kato-robotics.hatenablog.com/entry/2018/10/24/195041
        Vector3 ConvertCoordinatePosition(Vector3 pos) {
            return new Vector3(pos.z, -pos.x, pos.y);
        }
        // 回転 (Quaternion) を Unityから ROSの座標系 (My CobotはROSの座標系と同じ) に変換する
        Quaternion ConvertCoordinateQuaternion(Quaternion quat) {
            return new Quaternion(quat.z, -quat.x, quat.y, -quat.w);
        }

        // UDPを使って MyCobotにデータを送るためのデータを整形する
        // Thumb (x, y, z) -> Hand (x, y, z) -> Index (x, y, z) の順番で9個データを送る
        // 現状は Coroutine (下記) を使って送信している
        float[] MakeSendData() {
            _timestamp = _sw.ElapsedMilliseconds / 1000f;
            Vector3    thumbPos = ConvertCoordinatePosition(thumb.position);
            Vector3    indexPos = ConvertCoordinatePosition(index.position);
            Vector3    handPos  = ConvertCoordinatePosition(hand.position);
            Quaternion handQuat = ConvertCoordinateQuaternion(hand.rotation);
            Vector3    handRot  = handQuat.eulerAngles;
            // Unityは Z->X->Y の順番で回転する (world座標をベースにしている. Excentric)
            // https://docs.unity3d.com/Packages/com.unity.mathematics@0.0/api/Unity.Mathematics.math.RotationOrder.html
            float[]    data     = new []{ 0f };
            if (dataMode == DataMode.PosAndHandRot) {
                data = new[] {
                        // _timestamp,
                        thumbPos.x,
                        thumbPos.y,
                        thumbPos.z, 
                        handPos.x,
                        handPos.y,
                        handPos.z,
                        indexPos.x,
                        indexPos.y,
                        indexPos.z,
                        handRot.x,
                        handRot.y,
                        handRot.z,
                };
            } else if (dataMode == DataMode.OnlyPos) {
                data = new[] {
                        thumbPos.x,
                        thumbPos.y,
                        thumbPos.z,
                        handPos.x,
                        handPos.y,
                        handPos.z,
                        indexPos.x,
                        indexPos.y,
                        indexPos.z,
                };
            }

            return data;
        }

        IEnumerator SendData() {
            // 実行中は継続してデータを送り続けるため、無限ループにしている
            while (true) {
                float[] data = MakeSendData();
                udpClient.Send(data);
                // yield return new WaitForSeconds(1 / 1000f); // 時間を指定する
                yield return null; // return null によって 1frame だけストップする
            }
        }
        
}
}
