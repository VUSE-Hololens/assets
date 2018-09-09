using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Networking.Connectivity;
using Windows.Storage.Streams;
using System.Threading.Tasks;
#endif

public class ReceiverTester : MonoBehaviour {
#if !UNITY_EDITOR
    // extend lifetime of socket
    private StreamSocket sock;
#endif

    // inspector vars
    /*
    public string LocalIP;
    public string LocalOutPort;
    public string LocalInPort;

    public string RemoteIP;
    public string RemoteOutPort;
    public string RemoteInPort;

    // vars
    Receiver Rec;
    */

    // Use this for initialization
#if !UNITY_EDITOR
    async void Start () {
        // simple TCP connect, recv and echo
        string remoteIP = "10.67.134.150";
        string remotePort = "8888";

        sock = await SetupTCP(remoteIP, remotePort, sock);

        Debug.Log("Startup complete");

        /*// UDP remotely
        string localPort = "8888";
        string remoteIP = "10.67.134.150";
        string remotePort = "8888";

        Sender s = new Sender(localPort);
        string message = "test";
        s.Send(message, remoteIP, remotePort);

        Debug.Log("Test complete.");
        */

        /*// UDP locally
        string localIP = "10.66.19.210";
        string port1 = "8888";
        string port2 = "8889";

        Sender s1 = new Sender(port1);
        Sender s2 = new Sender(port2);

        string message = "test test";
        s1.Send(message, localIP, port2); // send to s2

        Debug.Log("Test complete.");
        */

        /*// Receiver - test connection request to host
        // NOTE: Receiver attempts to connect to host in constructor
        string LocalIP = "10.66.19.210";
        string LocalUDPPort = "9000";
        string LocalTCPPort = "9001";

        string RemoteIP = "10.67.134.150";
        string RemoteUDPPort = "8888";
        string RemoteTCPPort = "8889";
        Receiver Rec = new Receiver(LocalIP, LocalUDPPort, LocalTCPPort, RemoteIP, RemoteUDPPort, RemoteTCPPort);

        // wait
        StartCoroutine(WaitAndRec(5, Rec));
        */

        /*
        // test bit converter
        Serializer serial = new Serializer();
        int[] test = new int[] { 1, 2, 3 };
        Debug.Log(string.Format("Testing serialization... input: {0}, serialized: {1}, deserialized: {2}",
            ArrayToString(test), serial.Serialize(test), ArrayToString(serial.DeserializeInt(serial.Serialize(test)))));
            */
    }

    // Update is called once per frame
    void Update () {
        SendMessage();
	}

    // waits then closes socket
    private IEnumerator WaitAndClose(StreamSocket socket, int seconds)
    {
        yield return new WaitForSeconds(seconds);

        Debug.Log(string.Format("Closing socket"));

        socket.Dispose();
    }

    private string ArrayToString(int[] array)
    {
        string result = "{";
        for (int i = 0; i < array.Length; i++)
        {
            result += array[i].ToString() + ",";
        }
        result += "}";
        return result;
    }

    private string ArrayToString(byte[] array)
    {
        string result = "{";
        for (int i = 0; i < array.Length; i++)
        {
            result += array[i].ToString() + ",";
        }
        result += "}";
        return result;
    }

    IEnumerator WaitAndRec(int sec)
    {
        yield return new WaitForSeconds(sec);
    }
#endif

#if !UNITY_EDITOR
    // connects to host (TCP), receives message and echos
    async Task<StreamSocket> SetupTCP(string remoteIP, string remoteTCPPort, StreamSocket TCPSocket)
    {
        // create TCP socket
        TCPSocket = new StreamSocket();

        Debug.Log(string.Format("Successfully created TCP socket. Attempting connect to: {0} - {1}",
            remoteIP, remoteTCPPort));

        // connect TCP socket
        try
        {
            await TCPSocket.ConnectAsync(new HostName(remoteIP), remoteTCPPort);
            Debug.Log(string.Format("Successfully connected TCP socket to: {0} - {1}. Bound locally to {2} - {3}",
                remoteIP, remoteTCPPort, TCPSocket.Information.LocalAddress.DisplayName, TCPSocket.Information.LocalPort));
        }
        catch (Exception ex)
        {
            Debug.Log("Exception thrown at connection attempt...");
            Debug.Log(ex.ToString());
            Debug.Log(SocketError.GetStatus(ex.HResult).ToString());
        }

        return TCPSocket;

        /* recieve echo seems to be closing socket...
        // receive echo
        Debug.Log("Attempting to receive echo...");
        bool received = false;

        while (!received)
        {
            int recvLen = -1;   
    
            Debug.Log("About to try DataReader.LoadAsync");

            // think DataReader.LoadAsync was throwing exception, causing Hololens socket to go out of scope and close
            try {
                await inReader.LoadAsync(255);

                Debug.Log("Completed DataReader.LoadAsync");            

                recvLen = (int)inReader.UnconsumedBufferLength;
                Debug.Log(string.Format("Loaded {0} bytes from inStream", recvLen));
            }
            catch (Exception ex) {
                Debug.Log("Exception thrown at LoadAsync...");
                Debug.Log(ex.ToString());
                Debug.Log(SocketError.GetStatus(ex.HResult).ToString());
                return;
            }        

            if (recvLen > 0)
            {
                byte[] inBuf = new byte[recvLen];
                inReader.ReadBytes(inBuf);

                Debug.Log(string.Format("Received message: {0}", inBuf));
            }
        }
        */
    }

    private async void SendMessage() 
    {
        // send message
        DataReader inReader = new DataReader(sock.InputStream);
        DataWriter outWriter = new DataWriter(sock.OutputStream);
        
        try
        {
            string message = "test";
            outWriter.WriteString(message);
            await outWriter.StoreAsync();
            await outWriter.FlushAsync();
            Debug.Log(string.Format("Sent message: {0}", message));
        }
        catch (Exception ex)
        {
            Debug.Log("Exception thrown at send...");
            Debug.Log(ex.ToString());
            Debug.Log(SocketError.GetStatus(ex.HResult).ToString());
            return;
        }
    }

#endif

#if !UNITY_EDITOR
    private async void TCP_ConnectRecv(StreamSocketListener sockListen, StreamSocketListenerConnectionReceivedEventArgs args)
    {
        Debug.Log("ConnectionReceivedEvent triggered...");

        // fetch TCP socket
        StreamSocket TCPSocket = args.Socket;
        DataReader inReader = new DataReader(TCPSocket.InputStream);
        DataWriter outWriter = new DataWriter(TCPSocket.OutputStream);

        Debug.Log(string.Format("Connected successfully to: {0} - {1}", 
            TCPSocket.Information.RemoteHostName.DisplayName,  TCPSocket.Information.RemotePort));

        // recv message
        Debug.Log("Attempting to receive message...");
        
        byte[] message = new byte[5];
        await inReader.LoadAsync(5);
        inReader.ReadBytes(message);

        Debug.Log(string.Format("Received message: {0}, attempting to echo...", message));

        outWriter.WriteBytes(message);
        await outWriter.StoreAsync();

        Debug.Log("Echoed message");
    }
#endif


}
