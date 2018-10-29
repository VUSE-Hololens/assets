// Receiver.cs
// Handles communication with host (Pi) for client (Hololens)
// Dual comm: primary socket for receiving data, secondary for two way commands/status updates (both UDP)
// Mark Scherer, September 2018


using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.Foundation;
#endif

public class Receiver : MonoBehaviour {
    public enum Trans_Mode { fullFile, fileName }; // full file in single UDP packet or file name for download

    // transmission paramters
    public const int MAX_PACKET_SIZE = 10000; // bytes, all inclusive max size of packets
    public const int HEADER_SIZE_PRIM = 8; // bytes, size of header of int32 of messages on primary socket, NOT inlcuding first message length int (4 bytes)
    public const int FILENAME_LEN = 48; // bytes, size of filename field in full file transmission mode
    public const int INVALID_DATA_HASH = -1; // hash if valid data is not currently available
    public Trans_Mode Mode = Trans_Mode.fullFile;
    
    // command codes
    public const int CONNECT = 1;
    public const int DISCONNECT = 2;
    
    // comm control vars
    private string LocalIP;
    private string LocalPrimPort;
    private string LocalSecPort;

    public string RemoteIP;
    public string RemotePrimPort;
    public string RemoteSecPort;

    // FullFile mode: jpg decompression vars
    private bool jpgReady = false;
    private bool decoding = false;
    private byte[] waiting_jpg;
    Texture2D tex;

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
            Debug.Log(string.Format("Content Setter: Updated content. New Hash: {0}", CurrentHash));
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

    // LoadImg vars
    // need thread-safe bool
    private struct LoadImgObj
    {
        public string LocalPath;
        public string RemotePath;
    }

    private System.Object LoadImgLock = new System.Object();
    private bool loadImg;
    private bool LoadImg
    {
        get
        {
            bool tmp;
            lock (LoadImgLock) { tmp = loadImg; }
            return tmp;
        }
        set
        {
            lock (LoadImgLock) { loadImg = value; }
        }
    }

    private System.Object LoadImgInfoLock = new System.Object();
    private LoadImgObj loadImgInfo;
    private LoadImgObj LoadImgInfo
    {
        get
        {
            LoadImgObj tmp;
            lock (LoadImgLock) { tmp = loadImgInfo; }
            return tmp;
        }
        set
        {
            lock (LoadImgLock) { loadImgInfo = value; }
        }
    }

    // comm vars
    Serializer Serial = new Serializer();
    private bool Downloading = false; 
#if !UNITY_EDITOR
    DatagramSocket primSocket = new DatagramSocket();
    DatagramSocket secSocket = new DatagramSocket();
    DataWriter primWriter;
    DataWriter secWriter;
#endif

    private void Start()
    {
        // make sure to set RemoteIP, RemotePrimPort, RemoteSecPort and Trans_Mode before calling go()

        // only apply when in fileName mode
        LoadImg = false;
        LoadImgInfo = new LoadImgObj();

        // only apply in full file mode
        tex = new Texture2D(2, 2); // don't mess with TextureFormat. Texture2D.LoadImage() will always load jpg as RGB24 (R, G, B each as 8-bit 0-1, pixel order?)
    }

    private void Update()
    {
        if (Mode == Trans_Mode.fullFile)
            FullFileMode_Update();
        else if (Mode == Trans_Mode.fileName)
            FileNameMode_Update();
    }

    public void Go()
    {
        // debug
        Debug.Log(string.Format("Go: Starting Receiver. Attempting to connect to: {0} - {1}/{2}", RemoteIP, RemotePrimPort, RemoteSecPort));
#if !UNITY_EDITOR
        Configure();
        Connect();
#endif
    }

    // handle update cycle when in Full File transmission mode:
        // all handled in recieve method
    private void FullFileMode_Update()
    {
        if (jpgReady)
        {
            decoding = true;
            
            // decompress jpg
            Debug.Log(string.Format("Loading jpg to texture"));
            try {
                tex.LoadImage(waiting_jpg);
                //byte[] fileData = System.IO.File.ReadAllBytes(@"C:\Data\Users\vadlh\Pictures\spiral.jpg");
                //tex.LoadImage(fileData);
            }
            catch (Exception ex) { Debug.Log(string.Format("Exception loading jpg to texture: {0}", ex.ToString())); }
            Debug.Log(string.Format("Loaded jpg to texture. Texture now: {0}x{1} pixels", tex.width, tex.height));

            Debug.Log(string.Format("Pulling raw texture data"));
            byte[] jpgContent = new byte[tex.width * tex.height];
            Color32[] waste = tex.GetPixels32(); // pixel order: left to right, bottom to top
            for (int i = 0; i < waste.Length; i++)
                jpgContent[i] = (byte)((waste[i].r + waste[i].g + waste[i].b) / 3);
            Debug.Log(string.Format("Pulled raw jpg data, ready to update content. Length: {0}", jpgContent.Length));

            // update content
            Content = jpgContent;
            Pixels = new Vector2Int(tex.width, tex.height);

            jpgReady = false;
            decoding = false;
        } else
        {
            //Debug.Log("Main Thread: No jpg to decompress");
        }
    }

    // handle update cycle when in filename transmission mode:
        // check if new img ready
        // download newest file from given path
        // save locally, update content
    private void FileNameMode_Update()
    {
        // if new image to load, load
        if (LoadImg)
        {
            LoadImgObj tmp = LoadImgInfo;
            try
            {
                /*// try sync'd
                Debug.Log(string.Format("Downloading new image on main thread from: {0}", tmp.RemotePath));
                WWW www = new WWW(tmp.RemotePath);
                while (!www.isDone)
                {
                    if (www.error != null)
                    {
                        Debug.Log(string.Format("Error downloading image: {0}", www.error));
                        break;
                    }
                }

                Debug.Log(string.Format("Downloaded successfully. Attempting to save locally... localPath: {0}\n", tmp.LocalPath));
                System.IO.File.WriteAllBytes(tmp.LocalPath, www.bytes);

                // update content, pixel count
                //Content = body;
                //Pixels = new Vector2Int(header[0], header[1]);
                */

                // use coroutine
                if (!Downloading) { StartCoroutine(DownloadAndUpdate(tmp.RemotePath, tmp.LocalPath)); }
                else { Debug.Log("Update: Would've started new download but one already executing..."); }


                // debug
                //Debug.Log(string.Format("Completed img download. LocalPath: {0}, RemotePath: {1}", tmp.LocalPath, tmp.RemotePath));

                LoadImg = false;
            }
            catch (Exception ex)
            {
                Debug.Log(string.Format("Update: Exception downloading file... remotePath: {0}, localPath: {1}, exception: {2}",
                    tmp.RemotePath, tmp.LocalPath, ex.ToString()));
            }

        }
    }

    // coroutine: downloads jpg at url in background
    // for filepath transmission mode
    public IEnumerator DownloadAndUpdate(string url, string localAddress)
    {
        Downloading = true;

        // debug
        Debug.Log(string.Format("DownloadAndUpdate: Starting download from {0}", url));

        UnityWebRequest www = UnityWebRequest.Get(url);

        // config request: should match chrome request
        www.SetRequestHeader("Accept", "text / html, application / xhtml + xml, application / xml; q = 0.9,image / webp,image / apng,*/*;q=0.8");
        www.SetRequestHeader("Accept-Encoding", "deflate"); // NOT reccommended by SetRequestHeader documentation
        www.SetRequestHeader("Accept-Language", "en - US, en; q = 0.9");
        www.timeout = 3;

        UnityWebRequestAsyncOperation request = www.SendWebRequest();

        while (!request.isDone)
        {
            try
            {
                Debug.Log(string.Format("DownloadAndUpdate: Downloading jpg: progress: {0}. Method: {1}, Response Code: {2}, \nResponse Header: {3},\n" +
                "Request Header: Accept: {4}, Accept-Encoding: {5}, Accept-Language: {6}, User-Agent: {7}",
                request.progress, www.method, www.responseCode, PrintResponse(www), 
                www.GetRequestHeader("Accept"), www.GetRequestHeader("Accept-Encoding"), www.GetRequestHeader("Accept-Language"), www.GetRequestHeader("User-Agent")));
            } catch (Exception ex)
            {
                Debug.Log(string.Format("Caught exception accessing HTTP header: {0}", ex.ToString()));
            }
            yield return null;
        } 

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(string.Format("DownloadAndUpdate: Download from {0} failed: {1}", www.url, www.error));
        }
        else
        {
            // debug
            Debug.Log(string.Format("DownloadAndUpdate: Download from {0} complete. Saving to {1}", www.url, localAddress));

#if !UNITY_EDITOR
            //Save(www.downloadHandler.data, localAddress);
#endif
        }

        Downloading = false;
    }

    private string PrintResponse(UnityWebRequest www)
    {
        string output = "";
        if (www.GetResponseHeaders() != null)
        {
            foreach (KeyValuePair<string, string> kvp in www.GetResponseHeaders())
                output += string.Format("({0}, {1}); ", kvp.Key, kvp.Value);
        }
        else
            output = "No response received";
        return output;
    }

#if !UNITY_EDITOR
    private async void Save(byte[] data, string fullAddress)
    {
        // debug
        //Debug.Log(String.Format("Save: Saving data to: {0}", fullAddress));

        // save
        try {
            if (!System.IO.File.Exists(fullAddress))
                System.IO.File.WriteAllBytes(fullAddress, data);
        } catch (Exception ex) {
            Debug.Log(string.Format("Exception saving {0}", ex.ToString()));
        }

        // debug
        Debug.Log(String.Format("Save: Finished saving to: {0}", fullAddress));
    }
#endif

    /* old constructor
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
    */

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
        if (jpgReady || decoding) {
            //Debug.Log("Ignoring recieved jpg, already one ready for processing");
            return;
        }
        
        Debug.Log("Prim_MessageReceived Event: primary socket... updating content.");

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

        // debug
        Debug.Log(string.Format("PMRE: About to process message body. Body length: {0}", body.Length));

        // split handling based on mode
        if (Mode == Trans_Mode.fullFile)
            FullFileMode_PMRE(body);
        else if (Mode == Trans_Mode.fileName)
            FileNameMode_PMRE(body);
        
        // debug
        Debug.Log(string.Format("Prim_MessageReceived Event: Completed PMRE"));
    }

    // handle transmission body processing in file name mode:
        // save jpg locally
        // decompress and update content
    // NOTE: need to get filename to hololens
    private void FullFileMode_PMRE(byte[] body)
    {
        // separate filename, jpg
        byte[] filename_raw = new byte[FILENAME_LEN];
        try {
            Array.Copy(body, filename_raw, FILENAME_LEN);
        } catch (Exception ex) {
            Debug.Log(string.Format("Exception parsing filename from transmission body (tried to copy {0} bytes): {1}", FILENAME_LEN, ex.ToString()));
        }
        
        byte[] jpg_raw = new byte[body.Length - FILENAME_LEN];
        try {
            Array.Copy(body, FILENAME_LEN, jpg_raw, 0, body.Length - FILENAME_LEN);
        } catch (Exception ex) {
            Debug.Log(string.Format("Exception parsing jpg from transmission body (tried to copy {0} bytes): {1}", body.Length - FILENAME_LEN, ex.ToString()));
        }

        // save jpg locally
        string localPath = "tmp1";
        string cleanLocalPath = "tmp2";
        string fullPath = "tmp3";
        try {
            //localPath = @"\NDVI\" + System.Text.Encoding.UTF8.GetString(filename_raw);
            localPath = System.Text.Encoding.UTF8.GetString(filename_raw);

            // clean localPath
            char[] invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
            cleanLocalPath = new string(localPath.Where(ch => !invalidFileNameChars.Contains(ch)).ToArray());
            
            // remove characters after .jpg
            string target = ".jpg";
            int jpgIndex = cleanLocalPath.IndexOf(target);
            cleanLocalPath = cleanLocalPath.Substring(0, jpgIndex + target.Length);

            // get full path
            fullPath = System.IO.Path.Combine(@"C:\Data\Users\vadlh\Pictures\NDVI\", cleanLocalPath);
        } catch (Exception ex) {
            Debug.Log(string.Format("Exception created full path from {0} and {1}: {2}", 
                @"C:\Data\Users\vadlh\Pictures\" , cleanLocalPath, ex.ToString()));
        }

        try {
            Save(jpg_raw, fullPath);
        } catch (Exception ex) {
            Debug.Log(string.Format("Exception saving jpg to {0}: {1}", fullPath, ex.ToString()));
        }

        // prep jpg for decompression on main thread
        if (!decoding) {
            Debug.Log("Added jpg to be decompressed on main thread");
            waiting_jpg = jpg_raw;
            jpgReady = true;
        }
    }
    
    // handle transmission body processing in file name mode:
        // specify remote and local paths to be downloaded on main thread
    private void FileNameMode_PMRE(byte[] body)
    {
        // download file at specified address
        string remotePath = "http://" + RemoteIP + "/NDVI/" + System.Text.Encoding.UTF8.GetString(body);
        string localPath = KnownFolders.PicturesLibrary.Path + "/NDVI/" + System.Text.Encoding.UTF8.GetString(body);

        // debug
        //Debug.Log(string.Format("Attempting to download: localPath: {0}\n", localPath));
        //Debug.Log(string.Format("Attempting to download: remotePath: {0}\n", remotePath));

        // download, save and update content
        LoadImgObj tmp = new LoadImgObj();
        tmp.RemotePath = remotePath;
        tmp.LocalPath = localPath;
        LoadImgInfo = tmp;
        LoadImg = true;
    }

    private async void Sec_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
    {
        Debug.Log("MessageReceivedEvent: secondary socket... handling.");
        // no currently supported Host->Client secondary-socket messages...
    }

    /*
    // downloadFile and downloadFileCOR taken from: stackoverflow.com/questions/42449210/webclient-class-in-build-for-windows-store-build-errors-in-unity
    void downloadFile(string url, string localPath)
    {
        StartCoroutine(downloadFileCOR(url, localPath));
    }

    IEnumerator downloadFileCOR(string url, string localPath)
    {
        WWW www = new WWW(url);
        yield return www;

        Debug.Log("Finished Downloading file");

        byte[] yourBytes = www.bytes;

        // now Save it
        System.IO.File.WriteAllBytes(localPath, yourBytes);
    }
    */
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
