// Transmitter: Hololens
// Client: sets up in and out socket, at startup pings Windows server to register device
// Mark Scherer, July 2018

using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class Transmitter : MonoBehaviour {

    public struct Frame
    {
        public int height;
        public int width;
        public int bands;
        public float FOVx;
        public float FOVy;
        public byte[] data;

        public Frame(int myHeight, int myWidth, int myBands, float myFOVx, float myFOVy, byte[] myData)
        {
            height = myHeight;
            width = myWidth;
            bands = myBands;
            FOVx = myFOVx;
            FOVy = myFOVy;
            data = new byte[myData.Length];
            Array.Copy(myData, data, myData.Length);
        }
    }

    public const int MAX_PACKET_SIZE = 10000;

    // inspector vars
    public string localIPNum = "10.66.194.16";
    public int localInPort = 1002;
    public int localOutPort = 1003;
    public string hostIPNum = "10.66.247.250";
    public int hostInPort = 1000;
    public int hostOutPort = 1001;

    // other vars
    byte[] inBuffer = new byte[MAX_PACKET_SIZE];
    Thread listeningThread;

    // connection vars
    Socket inSocket;
    Socket outSocket;
    IPAddress localIP;
    IPEndPoint localInEP;
    IPEndPoint localOutEP;

    IPAddress hostIP;
    IPEndPoint hostInEP;
    IPEndPoint hostOutEP;

    // component communication
    private System.Object threadLock = new System.Object();
    private bool updated = false;
    public bool Updated
    {
        get
        {
            lock (threadLock)
            {
                return updated;
            }
        }
    }

    private int height;
    private int width;
    private int bands;
    private float fovx;
    private float fovy;
    private byte[] data;

    public Frame GetUpdate
    {
        get
        {
            lock (threadLock)
            {
                updated = false;
                Frame curFrame = new Frame(height, width, bands, fovx, fovy, data);
                return curFrame;
            }
        }
    }

    // Use this for initialization
    void Start () {
        try
        {
            configure();
            start();
            register();
        }
        catch (Exception e)
        {
            Debug.Log(string.Format("Exception : {0}", e.ToString()));
        }
    }

    // Update is called once per frame
	void Update () {
		// nothing to do
	}

    // configures and connects local sockets to host endpoints
    void configure()
    {
        try
        {
            // cannot compile with localIPNum not set to actual local IP number... tricks unity.
            // Note that hostIPNum must be the IP number of the comilation machine
            #if UNITY_EDITOR
            localIPNum = hostIPNum;
            #endif

            // create local endpoints
            localIP = new IPAddress(IPstrToByte(localIPNum));
            localInEP = new IPEndPoint(localIP, localInPort);
            localOutEP = new IPEndPoint(localIP, localOutPort);

            // create host endpoints
            hostIP = new IPAddress(IPstrToByte(hostIPNum));
            hostInEP = new IPEndPoint(hostIP, hostInPort);
            hostOutEP = new IPEndPoint(hostIP, hostOutPort);

            // Debug
            //Debug.Log(string.Format("IP addresses: local: {0}, remote: {1}", localIP.ToString(), hostIP.ToString()));

            // create sockets
            inSocket = new Socket(hostIP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            outSocket = new Socket(hostIP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            // bind sockets to local endpoints
            inSocket.Bind(localInEP);
            outSocket.Bind(localOutEP);

            // connect local sockets to remote endpoints
            inSocket.Connect(hostOutEP);
            outSocket.Connect(hostInEP);

            // debug
            Debug.Log(string.Format("Configured transmission... local IP: {0}, local port (in): {1}, local port (out): {2}, " +
                "Host IP: {3}, Host port (in): {4}, Host port (out): {5}",
                localIP.ToString(), localInPort, localOutPort, hostIP.ToString(), hostInPort, hostOutPort));
        } catch (SocketException se)
        {
            Debug.Log(string.Format("ArgumentNullException : {0}", se.ToString()));
        }
    }

    // starts inSocket receiving from hostEP on own thread
    void start()
    {
        listeningThread = new Thread(receive);
        listeningThread.Start();

        // Debug
        Debug.Log("Started listening on inSocket on own thread.");
    }

    // loop for inSocket
    // NOTE: will block until data is receives, must execute on own thread!
    void receive()
    {
        while (true)
        {
            // blocks until data is received
            int bytesRecieved = inSocket.Receive(inBuffer);

            // Debug
            Debug.Log(string.Format("Received data on inSocket. Length: {0}", bytesRecieved));

            // handle incoming data
            handleData(bytesRecieved);
        }
    }

    // interprets inBuffer, updating class variables as necessary
    // for buffer serialization, see Frame struct (Sensor.h)
    // Conversion scheme (height*width*bands + 20 total bytes): 
        // bytes 1-12: height, width, bands (4 bytes each)
        // bytes 12-20: FOVx, FOVy (4 bytes each)
        // bytes 21+: pixels
    void handleData(int bytes)
    {
        int tmpHeight = BitConverter.ToInt32(inBuffer, 0);
        int tmpWidth = BitConverter.ToInt32(inBuffer, 4);
        int tmpBands = BitConverter.ToInt32(inBuffer, 8);

        float tmpFOVx = BitConverter.ToSingle(inBuffer, 12);
        float tmpFOVy = BitConverter.ToSingle(inBuffer, 16);

        // previously in lock
        lock (threadLock)
        {
            updated = true;

            height = tmpHeight;
            width = tmpWidth;
            bands = tmpBands;

            fovx = tmpFOVx;
            fovy = tmpFOVy;

            data = new byte[bytes - 20];
            Array.Copy(inBuffer, 20, data, 0, bytes - 20);
        }

        // debug
        Debug.Log(string.Format("Processed received frame... pixels: {0}x{1}x{2}, FOV: {3}x{4}, Data: {5}, {6}, {7}, {8}, ...",
            tmpHeight, tmpWidth, tmpBands, tmpFOVx, tmpFOVy, inBuffer[20], inBuffer[21], inBuffer[22], inBuffer[23]));
    }

    // registers local device with host in order to receive data
    // 'registering' means sending local in port number to host's in port
    void register()
    {
        byte[] regist = BitConverter.GetBytes(localInPort);

        outSocket.Send(regist);

        // Debug
        Debug.Log(string.Format("Sent registration request to host. Sent {0} ({1})", 
            localInPort, BitConverter.ToString(regist)));
    }

    // converts IP address as string (with dots) to byte[]
    byte[] IPstrToByte(string IPAddress)
    {
        byte[] ip = new byte[4];

        int firstDot = IPAddress.IndexOf('.');
        int secondDot = IPAddress.IndexOf('.', firstDot + 1);
        int thirdDot = IPAddress.IndexOf('.', secondDot + 1);

        string first = IPAddress.Substring(0, firstDot);
        string second = IPAddress.Substring(firstDot + 1, secondDot - firstDot- 1);
        string third = IPAddress.Substring(secondDot + 1, thirdDot - secondDot - 1);
        string fourth = IPAddress.Substring(thirdDot + 1, IPAddress.Length - thirdDot - 1);

        ip[0] = Convert.ToByte(first, 10);
        ip[1] = Convert.ToByte(second, 10);
        ip[2] = Convert.ToByte(third, 10);
        ip[3] = Convert.ToByte(fourth, 10);

        return ip;
    }
}
