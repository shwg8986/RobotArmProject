using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyGraspable : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnGrasp() {
        var rb = GetComponent<Rigidbody>();
        rb.useGravity  = false;
        rb.isKinematic = true;

        var material = GetComponent<MeshRenderer>().material;
        material.color = Color.yellow;
    }

    public void OnRelease() {
        var rb = GetComponent<Rigidbody>();
        rb.useGravity  = true;
        rb.isKinematic = false;
        
        var material = GetComponent<MeshRenderer>().material;
        material.color = Color.white;
    }
    
}
