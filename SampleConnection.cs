using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System;

public class SampleConnection : MonoBehaviour
{
    //public RawImage image;
    public bool enableLog = true;

    public int port = 38000;
    public string IP = "10.66.188.188";
    static IPAddress ipAddress;

    TcpClient client;
    Socket socket;
    Texture2D tex;

    private bool stop = false;

    //This must be the-same with SEND_COUNT on the server
    const int SEND_RECEIVE_COUNT = 15;

    // Use this for initialization
    void Start()
    {
        Application.runInBackground = true;

        // tex = new Texture2D(0, 0);
        client = new TcpClient();
        ipAddress = new IPAddress(System.Text.Encoding.ASCII.GetBytes(IP));
        socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp); 

        //Connect to server from another Thread
        Loom.RunAsync(() =>
        {
            LOGWARNING("Connecting to server...");
            // if on desktop
            //client.Connect(IPAddress.Loopback, port);

            // if using external device
            client.Connect(IPAddress.Parse(IP), port);
            LOGWARNING("Connected!");

            // ImageReceiver();
        });
    }

    // Update is called once per frame
    void Update ()
    {
		
	}

    void LOG(string messsage)
    {
        if (enableLog)
            Debug.Log(messsage);
    }

    void LOGWARNING(string messsage)
    {
        if (enableLog)
            Debug.LogWarning(messsage);
    }

    void OnApplicationQuit()
    {
        LOGWARNING("OnApplicationQuit");
        stop = true;

        if (client != null)
        {
            client.Close();
        }
    }
}
