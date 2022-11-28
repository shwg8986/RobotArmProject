using System;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoraDataChannel : MonoBehaviour
{
    Sora sora;
    [SerializeField] string ChannelID;
    [SerializeField] string SignalingUrl;
    [SerializeField] string dataChannelLabel;

    List<Sora.DataChannel> dataChannels;
    //Byte [] hapitcs = new byte[1];
    //char sendvalue;
    int sendvalue;

    // Start is called before the first frame update
    void Start()
    {
        dataChannels = new List<Sora.DataChannel>();
        dataChannels.Add(new Sora.DataChannel()
            { Direction = Sora.Direction.Sendrecv,  Label = dataChannelLabel});
        sora = new Sora();
        Sora.Config config = new Sora.Config()
        {
            ChannelId = ChannelID,
            SignalingUrl = SignalingUrl,
            Multistream = true,
            Role = Sora.Role.Sendonly,
            Video = false,
            Audio = true,
            UnityAudioOutput = false,
            UnityAudioInput = true,
            DataChannelSignaling = true,
            EnableDataChannelSignaling = true,
            UnityCamera = Camera.main,
            CapturerType = Sora.CapturerType.UnityCamera,
            UnityCameraRenderTargetDepthBuffer = 16,
            DataChannels = dataChannels,
            Insecure = true
        };
           sora.OnNotify = (message) => { Debug.Log(message); };

        //sora.SendMessage(label, buf);

        //sora.OnMessage = (label, data) =>
        //{
        //};
        sora.Connect(config);
        StartCoroutine(GetStats());
        StartCoroutine(sendValue());

    }

    bool isStart = false;
    // Update is called once per frame
    int send_interval = 0;
    
    //void FixedUpdate()
    //{
    //    send_interval++;
    //    if (send_interval > 100)
    //    {
    //        sora.DispatchEvents();
    //        sora.OnRender();
    //        Debug.Log($"touchedSensor:{ForceSensor.touched}");
    //        if (ForceSensor.touched == true)
    //        {
    //            sendvalue = (char)1;
    //        }
    //        else
    //        {
    //            sendvalue = (char)0;
    //        }
    //        Debug.Log($"short:{sendvalue}");
    //        //byte[] byteArray = BitConverter.GetBytes(sendvalue);
    //        byte[] byte_array = new byte[] { (byte)sendvalue };
    //        sora.SendMessage(dataChannelLabel, byte_array);
    //        Debug.Log($"byte_array[0]:{byte_array[0]}");
    //        send_interval = 0;
    //    }
    //}

    IEnumerator GetStats()
    {
        while (true)
        {
            yield return new WaitForSeconds(10);


            //sora.GetStats((stats) => { 
            //    Debug.LogFormat("GetStats: {0}", stats);
            //});
        }
    }

    IEnumerator sendValue()
    {
        while (true)
        {
            sora.DispatchEvents();
            sora.OnRender();
            //if (ForceSensor.touched == true)
            //{
            //    sendvalue = (char)1;
            //}
            //else
            //{
            //    sendvalue = (char)0;
            //}
            
            sendvalue = (char)ForceSensor.pv_sum;
            //byte[] byteArray = BitConverter.GetBytes(sendvalue);
            byte[] byte_array = new byte[] { (byte)sendvalue };
            //Debug.Log($"SendValue: {byte_array[0]}");
            sora.SendMessage(dataChannelLabel, byte_array);
            //Debug.Log($"byte_array[0]:{byte_array[0]}");
            //Debug.Log($"SendValue: {ForceSensor.pv_sum} : {sendvalue}");
            //send_interval = 0;

            //byte[] bytes_array = new byte[] {(byte)sendvalue};
            //bytes_array = BitConverter.GetBytes(sendvalue);
            //Debug.Log(bytes_array[0]);


            yield return new WaitForSeconds(.1f);
        }

       
    }
}