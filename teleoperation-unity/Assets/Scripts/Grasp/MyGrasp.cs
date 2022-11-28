using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyGrasp : MonoBehaviour {
    [SerializeField] private MyFinger fingerOneObj = default;
    [SerializeField] private MyFinger fingerTwoObj = default;
    [SerializeField] private GameObject controlPoint = default;
    private GameObject m_GraspingTarget = default;
    private bool m_IsGrasping = false;
    void Start() {
        
    }

    void CheckGrasp() {
        bool isTouching = fingerOneObj.isTouching && fingerTwoObj.isTouching;
        if (isTouching) {
            bool isSameObj = fingerOneObj.touchingObject.name == fingerTwoObj.touchingObject.name;
            if (isSameObj) {
                var pos1     = fingerOneObj.transform.position;
                var pos2     = fingerTwoObj.transform.position;
                var distance = Vector3.Distance(pos1, pos2);
                // 一度把持したら，ある程度把持状態を続ける？
                if (distance <= 0.1f) {
                    m_IsGrasping     = true;
                    m_GraspingTarget = fingerOneObj.touchingObject;
                    m_GraspingTarget.GetComponent<MyGraspable>().OnGrasp();
                    m_GraspingTarget.transform.parent = controlPoint.transform;
                }
            } 
        } else {
            if (m_IsGrasping) {
                m_IsGrasping                      = false;
                m_GraspingTarget.transform.parent = null;
                m_GraspingTarget.GetComponent<MyGraspable>().OnRelease();
                m_GraspingTarget = null;
            }
        }
    }
    
    void Update() {
        CheckGrasp();
        
        var pos1 = fingerOneObj.transform.position;
        var pos2 = fingerTwoObj.transform.position;
        controlPoint.transform.position = new Vector3((pos1.x + pos2.x)/2f, (pos1.y + pos2.y)/2f, (pos1.z + pos2.z)/2f);
        var dirVec = fingerOneObj.transform.forward;
        var quat   = Quaternion.LookRotation(dirVec);
        controlPoint.transform.rotation = quat;
    }
}
