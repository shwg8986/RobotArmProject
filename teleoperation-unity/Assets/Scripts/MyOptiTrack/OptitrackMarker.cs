using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptitrackMarker : MonoBehaviour {
    public OptitrackStreamingClient optitrackStreamingClient=default;
    // Start is called before the first frame update
    void Start() {
        
    }
    
    // Update is called once per frame
    void Update() {
        List<OptitrackMarkerState> optitrackMarkerState = optitrackStreamingClient.GetLatestMarkerStates();
        foreach (var marker in optitrackMarkerState) {
            Vector3 pos = marker.Position;
            Debug.Log($"{this.GetType()}| " +
                      $"marker labeled: {marker.Labeled}, id: {marker.Id}, " +
                      $"position: {pos.x}, {pos.y}, {pos.z}");
        }
    }
}
