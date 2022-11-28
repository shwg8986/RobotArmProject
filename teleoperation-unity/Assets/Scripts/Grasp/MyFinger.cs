using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyFinger : MonoBehaviour
{
    public bool isTouching { get; private set; }
    public GameObject touchingObject { get; private set; }

    void Start() {
        isTouching     = false;
        touchingObject = null;
    }
    
    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.GetComponent<MyGraspable>() != null) {
            Debug.Log($"{gameObject.name}-{this.GetType()}: Touch Graspable");
            isTouching     = true;
            touchingObject = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.gameObject.GetComponent<MyGraspable>() != null) {
            Debug.Log($"{gameObject.name}-{this.GetType()}: Detouch Graspable");
            isTouching     = false;
            touchingObject = null;
        }
    }
}
