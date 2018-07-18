using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Net;

#if !UNITY_EDITOR
using Windows.Networking.Sockets;
using Windows.Networking.Connectivity;
using Windows.Networking;
using Windows.Graphics.Imaging; // for BitmapDecoder
using Windows.Storage.Streams;
#endif

namespace Receiving
{
    public class ImageReceiver : MonoBehaviour
    {
        private int ImgCount = 0;

        static readonly string RemoteIP = "10.67.144.129";
        static readonly string RemotePort = "5000";
        static readonly string ServerPort = "5001";

        private const int PACKET_SIZE = 1000;
        private const int NORMAL_PACKET_INDEX_BYTES = 3;
        private const int START_PACKET_SIZE = 3;

        private const char START_RGB = 'M';
        private const char START_ONE_BAND = 'I';

        private const int AWAITING_IMAGE = 0;
        private const int RECEIVING_RGB = 1;
        private const int RECEIVING_ONE_BAND = 2;

        private const int UNCOMPRESSED_RGB = RECEIVING_RGB;
        private const int UNCOMPRESSED_BAND = RECEIVING_ONE_BAND;
        private const int COMPRESSED_JPEG = 3;

        private int ReceivingStatus = AWAITING_IMAGE;
        private byte[] ImageBuffer = null;

        private byte[] ID_ImageData1D;
        private int ID_ImageType;
        private int ID_ImageWidth;
        private int ID_ImageHeight;
        private string ID_Message = "testing";
        private bool ID_NewImage = false;

        private DateTime time;
        private int testIterator = 0;

        // private readonly ConcurrentQueue<Action> ExecuteOnMainThread = new ConcurrentQueue<Action>();
        // private readonly Queue<Action> ExecuteOnMainThread = new Queue<Action>();

#if !UNITY_EDITOR
        DatagramSocket ServerSocket;
#endif

        // The singleton instance
        private static ImageReceiver sReceiver;
        public static ImageReceiver GetInstance()
        {
            if (sReceiver == null)
            {
                sReceiver = new ImageReceiver();
            }
            return sReceiver;
        }

        //get methods
        public string Get_Message() { return ID_Message; }

        public bool CheckNewImage() { return ID_NewImage; }

        public int Get_ImageType() { return ID_ImageType; }
        
        public int Get_ImageWidth() { return ID_ImageWidth; }

        public int Get_ImageHeight() { return ID_ImageHeight; }

        public byte[] Get_ImageData1D()
        {
            ID_NewImage = false;
            Debug.Log("Polled to get Image Data - length: " + ID_ImageData1D.Length);
            return ID_ImageData1D;
        }

        private void Init_TestRGB(int width, int height)
        {
            ID_ImageWidth = width;
            ID_ImageHeight = height;
            ID_ImageData1D = new byte[ID_ImageWidth * ID_ImageHeight * 4];
            for (int i = 0; i < ID_ImageData1D.Length; i += 4)
            {
                ID_ImageData1D[i + 0] = 0x00; // r
                ID_ImageData1D[i + 1] = 0xff; // g
                ID_ImageData1D[i + 2] = 0x00; // b
                ID_ImageData1D[i + 3] = 0xff; // a
            }
            Debug.Log("Width: " + ID_ImageWidth + ", Height: " + ID_ImageHeight + ", Length: " + ID_ImageData1D.Length);
            ID_NewImage = true;
        }

        private void Init_TestBand(int width, int height, int num = 2)
        {
            ID_ImageWidth = width;
            ID_ImageHeight = height;
            ID_ImageData1D = new byte[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    switch (num)
                    {
                        case (0):
                            float val = 255.0f * ((x + y + 0.0f) / (width + height));
                            ID_ImageData1D[x + y * width] = (byte)val;
                            break;
                        case (1):
                            ID_ImageData1D[x + y * width] =  255;
                            break;
                        default:
                            ID_ImageData1D[x + y * width] = 0;
                            break;
                    }
                }
            }
            Debug.Log("Width: " + ID_ImageWidth + ", Height: " + ID_ImageHeight + ", Length: " + ID_ImageData1D.Length);
            ID_NewImage = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (DateTime.Now.Subtract(time).Seconds > 1)
            {
                Init_TestBand(400, 400, (++testIterator) % 3);
                time = DateTime.Now;
            }

            /* int queueCount = 0;
            // execute everything queued
            while (ExecuteOnMainThread.Count > 0)
            {
                ExecuteOnMainThread.Dequeue().Invoke();
                Debug.Log("Dequeued (" + ++queueCount + " of " + (ExecuteOnMainThread.Count + queueCount) + ")");
            }
            if (ExecuteOnMainThread.Count == 0)
            {
                Debug.Log("Empty Queue!");
                ReceivingStatus = AWAITING_IMAGE;
            } */
        }

        void Start() //async
        {
            // initialized ID_ImageData1D to all Green to prove displayer working correctly
            Init_TestBand(400, 400, testIterator);
            time = DateTime.Now;
            // ServerSocket = new DatagramSocket();
            // await StartServer();
            // await System.Threading.Tasks.Task.Delay(3000);
            // await SendBroadcast("Hello World!");
            Debug.Log("Exit Start");
        }

#if !UNITY_EDITOR
        private async System.Threading.Tasks.Task StartServer()
        {
            Debug.Log("Waiting for Connection...");
            ServerSocket.MessageReceived += ServerSocket_MessageReceived;

            try
            {
                await ServerSocket.BindServiceNameAsync(ServerPort);
                Debug.Log("Connected to " + RemoteIP + ":" + RemotePort);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                Debug.Log(SocketError.GetStatus(e.HResult).ToString());
                return;
            }
        }

        private async System.Threading.Tasks.Task SendBroadcast(string message)
        {
            try  // send out a message, otherwise receiving does not work ?!
            {
                Debug.Log("Sending broadcast...");
                var outputStream = await ServerSocket.GetOutputStreamAsync(new HostName(RemoteIP), RemotePort);
                DataWriter writer = new DataWriter(outputStream);
                writer.WriteString(message);
                await writer.StoreAsync();
                Debug.Log("Sent Broadcast: "+ message);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
                Debug.Log(SocketError.GetStatus(ex.HResult).ToString());
                return;
            }
        }

        //async 
        private async void ServerSocket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            if (ReceivingStatus != AWAITING_IMAGE) return; // dont do anything if not ready to receive

            await System.Threading.Tasks.Task.Run(() =>
            {
                Stream streamIn = args.GetDataStream().AsStreamForRead();
                //MemoryStream ms = ToMemoryStream(streamIn);
                byte[] data = ToMemoryStream(streamIn).ToArray(); // ms.ToArray();
                Debug.Log("Read in byte array of length " + data.Length);

                ProcessPacket(data);

                // enqueue all, continually stack // used for managing events, creates a group of actions
                /* if (ReceivingStatus == AWAITING_IMAGE) // unnecessary but clean up later if things work?
                {
                    Debug.Log("Queue Count: " + ExecuteOnMainThread.Count);
                    ExecuteOnMainThread.Enqueue(() =>
                    {
                        ProcessPacket(data);
                    });
                }*/ 

                /*
                System.Threading.Tasks.Task taskAsync = new System.Threading.Tasks.Task(async () =>
                {
                   await ProcessPacket(data);
                });
                taskAsync.Start(); */
            });
            // Debug.Log("Data Array: " + (data != null) + " - length: " + data.Length);
            //await ProcessPacket(data);
            //Debug.Log("Processed Packet");

        }

        public void Dispose()
        {
            if (ServerSocket != null)
            {
                ServerSocket.Dispose();
                ServerSocket = null;
            }
        }

        private async System.Threading.Tasks.Task ProcessPacketAsync(byte[] data)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                ProcessPacket(data);
            });
        }

        //process the packet of data we get
        private void ProcessPacket(byte[] data)
        {
           if (data.Length >= 3) // read in string packet  with '___' prefix- this is for testing
            {
                if ((char)data[0] == '_' && (char)data[1] == '_' && (char)data[2] == '_')
                {
                    string message = Encoding.UTF8.GetString(data);
                    Debug.Log("String: " + message);
                    ID_Message = message;
                    Debug.Log("Set ID_Message variable");
                }
            }
            switch (ReceivingStatus)
            {
                case AWAITING_IMAGE:
                    if (data.Length == START_PACKET_SIZE)
                    {
                        StartReceivingImage(data);
                    }
                    break;
                default:
                    if (data.Length == 0)
                    {
                        // Handle image here 
                        ServerSocket.MessageReceived -= ServerSocket_MessageReceived;
                        ReceivingStatus = AWAITING_IMAGE;
                        byte[] processingBuffer = ImageBuffer;
                        ImageBuffer = null;
                        ProcessImageArr(processingBuffer);
                        ImgCount++;
                        // showImage = false;
                        Debug.Log("Image Count: " + ImgCount);
                    }
                    // It's only possible for start packets to be of size 3 (others must be >= 4)
                    else if (data.Length == START_PACKET_SIZE)
                    {
                        StartReceivingImage(data);
                    }
                    else
                    {
                        int ind = data[0];
                        ind |= ((data[1] & 0xff) << 8);
                        ind |= ((data[2] & 0xff) << 16);
                        for (int i = 0; i < data.Length - NORMAL_PACKET_INDEX_BYTES; ++i)
                        {
                            ImageBuffer[(ind * PACKET_SIZE) + i] = data[i + NORMAL_PACKET_INDEX_BYTES];
                        }
                    }
                    break;
            }
        }
        private void StartReceivingImage(byte[] data)
        {
            char nextByte = (char)data[0];
            if (nextByte == START_RGB)
            {
                UInt16 num_packets = data[1];
                num_packets |= (UInt16)((data[2] & 0xff) << 8);
                ReceivingStatus = RECEIVING_RGB;
                ImageBuffer = new byte[PACKET_SIZE * num_packets];
            }
            else if (nextByte == START_ONE_BAND)
            {
                UInt16 num_packets = data[1];
                num_packets |= (UInt16)((data[2] & 0xff) << 8);
                ReceivingStatus = START_ONE_BAND;
                ImageBuffer = new byte[PACKET_SIZE * num_packets];
            }
        }

        // display image when last packet arrives
        private async void ProcessImageArr(byte[] img) //, int imgType)
        {
            try
            {
                // Decode the JPEG
                MemoryStream stream = new MemoryStream(img);
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
                PixelDataProvider pixelData = await decoder.GetPixelDataAsync();
                //SoftwareBitmap sftwareBmp = await decoder.GetSoftwareBitmapAsync();
                //SoftwareBitmap sftwareBmp = SoftwareBitmap.Convert(sftwareBmp, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);

                ID_ImageWidth = (int)decoder.PixelWidth;
                ID_ImageHeight = (int)decoder.PixelHeight;
                Debug.Log(string.Format("ImgSize: ({0},{1})", ID_ImageHeight, ID_ImageWidth));
                ID_ImageData1D = new byte[4 * ID_ImageWidth * ID_ImageHeight];
                Array.Copy(pixelData.DetachPixelData(), ID_ImageData1D, ID_ImageData1D.Length);
                // ID_ImageData1D = pixelData.DetachPixelData();

                Debug.Log("ID_ImageData exists: " + (ID_ImageData1D != null));
                //Debug.Log("Copied Array: Length = " + ID_ImageData1D.Length);

                //RGB1D_ToImageData2D(ref ID_ImageData1D, width, height);
                //RGB1D_ToImageData3D(ref ID_ImageData1D, width, height);
                //ID_ImageType = imgType;
                ID_NewImage = true;

                ServerSocket.MessageReceived += ServerSocket_MessageReceived;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }
#endif
        private static MemoryStream ToMemoryStream(Stream input)
        {
            try
            {                                         // Read and write in
                byte[] block = new byte[0x1000];       // blocks of 4K.
                MemoryStream ms = new MemoryStream();
                while (true)
                {
                    int bytesRead = input.Read(block, 0, block.Length);
                    if (bytesRead == 0) return ms;
                    ms.Write(block, 0, bytesRead);
                }
            }
            finally { }
        }
        /*
        #if !UNITY_EDITOR
                private static IRandomAccessStream ToIRandomAccessStream(byte[] arr)
                {
                    MemoryStream stream = new MemoryStream(arr);
                    return stream.AsRandomAccessStream();
                }
        #endif
                */
    }
}
