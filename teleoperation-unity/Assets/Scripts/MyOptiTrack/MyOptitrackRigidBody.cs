using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// OptitrackRigidBody を別ファイルとして改変する
/// </summary>
public class MyOptitrackRigidBody : MonoBehaviour
{
    [Tooltip("The object containing the OptiTrackStreamingClient script.")]
    public OptitrackStreamingClient StreamingClient;

    [Tooltip("The Streaming ID of the rigid body in Motive")]
    public Int32 RigidBodyId;

    [Tooltip("Subscribes to this asset when using Unicast streaming.")]
    public bool NetworkCompensation = true;

    void Start()
    {
        // If the user didn't explicitly associate a client, find a suitable default.
        if ( this.StreamingClient == null )
        {
            this.StreamingClient = OptitrackStreamingClient.FindDefaultClient();

            // If we still couldn't find one, disable this component.
            if ( this.StreamingClient == null )
            {
                Debug.LogError( GetType().FullName + ": Streaming client not set, and no " + typeof( OptitrackStreamingClient ).FullName + " components found in scene; disabling this component.", this );
                this.enabled = false;
                return;
            }
        }

        this.StreamingClient.RegisterRigidBody( this, RigidBodyId );

        StartCoroutine(UpdateCoroutine());
    }


#if UNITY_2017_1_OR_NEWER
    void OnEnable()
    {
        Application.onBeforeRender += OnBeforeRender;
    }


    void OnDisable()
    {
        Application.onBeforeRender -= OnBeforeRender;
    }


    void OnBeforeRender()
    {
        UpdatePose();
    }
#endif


    // void Update()
    // {
    //     UpdatePose();
    // }


    IEnumerator UpdateCoroutine() {
        while (true) {
            if (!Application.isPlaying) break;
            UpdatePose();
            yield return new WaitForSeconds(0.001f); // 100 Hz
        }
    }

    void UpdatePose()
    {
        OptitrackRigidBodyState rbState = StreamingClient.GetLatestRigidBodyState( RigidBodyId, NetworkCompensation);
        if ( rbState != null ) {
            Vector3 pos = rbState.Pose.Position;
            this.transform.localPosition = rbState.Pose.Position;
            // this.transform.localPosition = new Vector3(-pos.x, pos.y, pos.z);

            Quaternion rot = rbState.Pose.Orientation;
            this.transform.localRotation = rot;
            // this.transform.localRotation = new Quaternion(-rot.x, rot.y, rot.z, -rot.w);
        }
    }
}
