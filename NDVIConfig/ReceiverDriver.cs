// ReceiverDriver.cs
// Shell class to hold receiver object and its inspector variables.
// Mark Scherer, September 2018

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReceiverDriver : MonoBehaviour
{
    // inspector vars
    public string remoteIP = "10.67.134.150";
    public string remotePrimPort = "8888";
    public string remoteSecPort = "8889";

    // other vars
    public Receiver rec;
    public byte[] dummyData { get; private set; }
    public int dummySize = 25;
    public float period = 3;
    public float periodStart = 0;

    // Use this for initialization
    void Awake()
    {
        rec = new Receiver(remoteIP, remotePrimPort, remoteSecPort);

        dummyData = new byte[dummySize * dummySize];
    }

    // Update is called once per frame
    void Update()
    {
        if (rec.CurrentHash == Receiver.INVALID_DATA_HASH)
        {
            UpdateDummy();
        }
    }

    private void UpdateDummy()
    {
        float curTime = Time.time;
        if (curTime - periodStart > period)
        {
            periodStart = curTime;
        }

        float offset = (curTime - periodStart) / period;

        for (int i = 0; i < dummySize * dummySize; i++)
        {
            //float col = i % dummySize;
            //dummyData[i] = (byte)((byte)255 + (byte)(255f/2f * Math.Sin(2*Math.PI * (col + offset) / dummySize/2)));
            dummyData[i] = (byte)(255 / 2 + 255f / 2f * Math.Sin(2 * Math.PI * offset));
        }
    }
}
