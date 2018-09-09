// Receiver.cs
// Handles communication with host (Pi) for client (Hololens)
// Dual comm: primary socket for receiving data, secondary for two way commands/status updates (both UDP)
// Mark Scherer, September 2018


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
using Windows.Foundation;
#endif

public class Receiver {
    // transmission paramters
    public const int MAX_PACKET_SIZE = 10000; // bytes, all inclusive max size of packets
    public const int HEADER_SIZE_PRIM = 8; // bytes, size of header of int32 of messages on primary socket
    public const int INVALID_DATA_HASH = -1; // hash if valid data is not currently available
    
    // command codes
    public const int CONNECT = 1;
    public const int DISCONNECT = 2;
    
    // comm control vars
    private string LocalIP;
    private string LocalPrimPort;
    private string LocalSecPort;

    private string RemoteIP;
    private string RemotePrimPort;
    private string RemoteSecPort;

    // Hashing vars
    // hash is used by clients to check for new data... each update the hash is changed. -1 indicates no valid data
    private int HashMax = 100;
    private System.Object HashLock = new System.Object();
    private int currentHash = INVALID_DATA_HASH;
    public int CurrentHash
    {
        get
        {
            int tmp;
            lock (HashLock) { tmp = currentHash; }
            return tmp;
        }
        private set
        {
            lock (HashLock) { currentHash = value; }
        }
    }

    // ONLY to be called by content setter
    private int NewHash()
    {
        // simple iteration hashing scheme
        if (CurrentHash + 1 <= HashMax) { return CurrentHash + 1; }
        else { return 0; }
    }

    // Content vars
    private System.Object ContentLock = new System.Object();
    private byte[] content;
    public byte[] Content
    {
        get
        {
            byte[] tmp = (byte[])Array.CreateInstance(typeof(byte), ContentLen);
            lock (ContentLock) { Array.Copy(content, tmp, ContentLen); }
            return tmp;
        }
        private set
        {
            content = (byte[])Array.CreateInstance(typeof(byte), value.Length);
            lock (ContentLock) { Array.Copy(value, content, value.Length); }
            ContentLen = value.Length;
            CurrentHash = NewHash();

            // debug
            Debug.Log(string.Format("Updated content. New Hash: {0}", CurrentHash));
        }
    }

    // ContentLen vars
    // ContentLen is ONLY to be set by Content set method.
    private System.Object ContentLenLock = new System.Object();
    private int contentLen;
    public int ContentLen
    {
        get
        {
            int tmp;
            lock (ContentLenLock) { tmp = contentLen; }
            return tmp;
        }
        private set
        {
            lock (ContentLenLock) { contentLen = value; }
        }
    }

    // Pixel vars
    // Note: width, height
    private System.Object PixelLock = new System.Object();
    private Vector2Int pixels;
    public Vector2Int Pixels
    {
        get
        {
            Vector2Int tmp;
            lock (PixelLock) { tmp = pixels; }
            return tmp;
        }
        private set
        {
            lock (PixelLock) { pixels = value; }
        }
    }

    // comm vars
    Serializer Serial = new Serializer();
#if !UNITY_EDITOR
    DatagramSocket primSocket = new DatagramSocket();
    DatagramSocket secSocket = new DatagramSocket();
    DataWriter primWriter;
    DataWriter secWriter;
#endif


    // constructor
    public Receiver(string remoteIP_, string remotePrimPort_, string remoteSecPort_)
    {
        // assign remote comm controls
        RemoteIP = remoteIP_;
        RemotePrimPort = remotePrimPort_;
        RemoteSecPort = remoteSecPort_;

        // debug
        Debug.Log(string.Format("Creating Receiver. Attempting to connect to: {0} - {1}/{2}", RemoteIP, RemotePrimPort, RemoteSecPort));
#if !UNITY_EDITOR
        Configure();
        Connect();
#endif
    }

#if !UNITY_EDITOR
    // configures sockets locally
    async void Configure() {
        // add message received handlers
        primSocket.MessageReceived += Prim_MessageReceived;
        secSocket.MessageReceived += Sec_MessageReceived;

        // connect local sockets to remote sockets, record local ports / local IP
        try {
            await primSocket.ConnectAsync(new HostName(RemoteIP), RemotePrimPort);
            LocalIP = primSocket.Information.LocalAddress.DisplayName;
            LocalPrimPort = primSocket.Information.LocalPort;
            Debug.Log(string.Format("Connected primPort to: {0}-{1}. Bound locally to: {2}-{3}",
                RemoteIP, RemotePrimPort, LocalIP, LocalPrimPort));
        } catch (Exception ex) {
            Debug.Log(string.Format("Exception at primSocket.ConnectAsync: {0}", ex.ToString()));
            Debug.Log(SocketError.GetStatus(ex.HResult).ToString());
        }
        try {
            await secSocket.ConnectAsync(new HostName(RemoteIP), RemoteSecPort);
            LocalSecPort = secSocket.Information.LocalPort;
            Debug.Log(string.Format("Connected secPort to: {0}-{1}. Bound locally to: {2}-{3}",
                RemoteIP, RemoteSecPort, LocalIP, LocalSecPort));
        } catch (Exception ex) {
            Debug.Log(string.Format("Exception at secSocket.ConnectAsync: {0}", ex.ToString()));
            Debug.Log(SocketError.GetStatus(ex.HResult).ToString());
        }

        // get socket output streams
        try {
            primWriter = new DataWriter(primSocket.OutputStream);
            secWriter = new DataWriter(secSocket.OutputStream);

            // debug
            Debug.Log("Output streams fetched for both sockets.");
        } catch (Exception ex) {
            Debug.Log(string.Format("Exception fetching socket output streams: {0}", ex.ToString()));
            Debug.Log(SocketError.GetStatus(ex.HResult).ToString());
        }
    }
#endif

    // message recieved handlers
#if !UNITY_EDITOR
    private async void Prim_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
    {
        Debug.Log("MessageReceivedEvent: primary socket... updating content.");

        // setup read operation
        int messageLength = -1; // set to invalid state intitally
        DataReader messageReader = args.GetDataReader();
        messageReader.ByteOrder = ByteOrder.LittleEndian;
        try {
            messageLength = messageReader.ReadInt32(); // first 4 bytes of all messages must be int32 indicating length (all inclusive)
            // next line (LoadAsync) causes System.Runtime.InteropServices.COMException... alternate solution implemented below
            //await messageReader.LoadAsync((uint)messageLength);
            // did not fix error.. just not LoadAsync'ing
            //IAsyncOperation<uint> loadTask = messageReader.LoadAsync((uint)messageLength);
            //loadTask.AsTask().Wait();
        } catch (Exception ex) {
            Debug.Log(string.Format("Exception setting up message read: {0}", ex.ToString()));
            Debug.Log(SocketError.GetStatus(ex.HResult).ToString());
        }

        // read header (as int32s)
        int[] header = new int[0]; // set to invalid state initally
        try {
            header = new int[HEADER_SIZE_PRIM/4]; // 4 bytes per int
            for (int i = 0; i < HEADER_SIZE_PRIM/4; i++) {
                header[i] = messageReader.ReadInt32();
            }
        } catch (Exception ex) {
            Debug.Log(string.Format("Exception reading message header: {0}", ex.ToString()));
            Debug.Log(SocketError.GetStatus(ex.HResult).ToString());
        }

        // read body (as bytes)
        byte[] body = new byte[0]; // set to invalid state initally
        try {
            int bodyLength = messageLength - HEADER_SIZE_PRIM - 4;
            body = new byte[bodyLength];
            messageReader.ReadBytes(body);
        } catch (Exception ex) {
            Debug.Log(string.Format("Exception reading message body: {0}", ex.ToString()));
            Debug.Log(SocketError.GetStatus(ex.HResult).ToString());
        }

        // update content, pixel count
        Content = body;
        Pixels = new Vector2Int(header[0], header[1]);

        // debug
        Debug.Log(string.Format("Message received successfuly. Total Length: {0}, header: {1}, body: {2}. Updated content successfully.",
            messageLength, ArrayToString(header), ArrayToString(body)));
    }

    private async void Sec_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
    {
        Debug.Log("MessageReceivedEvent: secondary socket... handling.");
        // no currently supported Host->Client secondary-socket messages...
    }
#endif

#if !UNITY_EDITOR
    // establishes connection link to host
    private async void Connect()
    {
        // Debug
        Debug.Log(string.Format("Sending connect request to host: {0} - {1}/{2}", 
            RemoteIP, RemotePrimPort, RemoteSecPort));

        int[] connectReq = new int[] { CONNECT, Int32.Parse(LocalPrimPort) };
        await Send(Serial.Serialize(connectReq));
}
#endif

#if !UNITY_EDITOR
    // sends content from outSocket
    private async Task Send(byte[] content)
    {
        // Debug
        Debug.Log(string.Format("Sending transmission: {0}", ArrayToString(content)));
        
        try {
            //await secWriter.FlushAsync();
            secWriter.WriteBytes(content);
            await secWriter.StoreAsync();
            
            // debug
            Debug.Log("Transmission sent.");
        } catch (Exception ex) {
            Debug.Log(string.Format("Exception sending: {0}", ex.ToString()));
            Debug.Log(SocketError.GetStatus(ex.HResult).ToString());
        }
    }
#endif

    // destructor
    ~Receiver()
    {
        // debug
        Debug.Log("Destroying Receiver...");

#if !UNITY_EDITOR
        primSocket.Dispose();
        secSocket.Dispose();
#endif
    }

    // helper
    public static string ArrayToString(byte[] array)
    {
        string result = "{";
        for (int i = 0; i < array.Length; i++)
        {
            result += array[i].ToString() + ",";
        }
        result += "}";
        return result;
    }
    public static string ArrayToString(int[] array)
    {
        string result = "{";
        for (int i = 0; i < array.Length; i++)
        {
            result += array[i].ToString() + ",";
        }
        result += "}";
        return result;
    }
}
