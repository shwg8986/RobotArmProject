using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyFingerOffset : MonoBehaviour
{
    // 人差し指と親指とで同じコードを利用して
    // GameObjectにアタッチした際に enum を変える    
    public enum FingerType {
        Thumb,
        Index,
    }

    public FingerType fingerType = default;
    void Start() {
        SetPositionFromCOG();

    }

    /// <summary>
    /// Rigidbodyの重心位置を計算して，マーカーの付け根の位置を計算．
    /// さらに，指が接触する位置までオフセットを引くことで
    /// 接触位置のオフセットを計算して，座標移動する
    /// 2022/5/17 現在のマーカーの設計をベースに計算される
    /// TODO: マーカーの形状を変える場合は、要修正
    /// </summary>
    void SetPositionFromCOG() {
        if (fingerType == FingerType.Thumb) {
            float thumbOffset = 30f;                                              // mm
            float posZ        = ((-22.5f + 0f + 37.5f) / 3f) * 0.001f;          // mm -> m
            float posX        = 1 * (thumbOffset + (20f + 47.5f + 15f) / 3f) * 0.001f; // mm -> m
            var   _p          = transform.position;
            transform.position = new Vector3(_p.x + posX, _p.y, _p.z + posZ);
        } else if (fingerType == FingerType.Index) {
            float indexOffset = 30f;
            float posZ        = -1 * ((-32.5f + 0f + 37.5f) / 3f) * 0.001f;       // mm -> m
            float posX        = -1 * (indexOffset +(15f + 49.5f + 10f) / 3f) * 0.001f; // mm -> m
            var   _p          = transform.position;
            transform.position = new Vector3(_p.x + posX, _p.y, _p.z + posZ);
        }
    }
    

}
