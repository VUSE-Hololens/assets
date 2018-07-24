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
        // Server/Client Settings
        //static readonly string RemoteIP = "10.67.87.102"; // needed only if broadcasting message
        //static readonly string RemotePort = "5000";
        static readonly string LocalPort = "5001";

        //packet attributes
        private const int PACKET_SIZE = 20000;
        private const int NORMAL_PACKET_INDEX_BYTES = 3;
        private const int START_PACKET_SIZE = 3;

        // Image Status Constants -> wont need when single packet transmission
        private const char START_RGB = 'M';
        private const char START_ONE_BAND = 'I';

        private const int AWAITING_IMAGE = 0;
        private const int RECEIVING_RGB = 1;
        private const int RECEIVING_ONE_BAND = 2;

        private const int UNCOMPRESSED_RGB = RECEIVING_RGB;
        private const int UNCOMPRESSED_BAND = RECEIVING_ONE_BAND;
        private const int COMPRESSED_JPEG = 3;

        private int ReceivingStatus = AWAITING_IMAGE;

        private readonly object syncLock = new object();

        // gettable attributes
        private byte[] ID_ImageData1D;
        private int ID_ImageWidth;
        private int ID_ImageHeight;
        private bool ID_NewImage = false;
        private double ID_fps = 0;
        private DateTime time;
        private int ImgCount = 0;

        // image direction controls
        private int rotationState = 0;
        private int flipState = 0;

        // Queue to execute processing on main thread. 
        private readonly ConcurrentQueue<Action> ExecuteOnMainThread = new ConcurrentQueue<Action>();

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
        public double Get_fps()
        {
            lock (syncLock)
            {
                return ID_fps;
            }
        }

        public bool CheckNewImage() {
            lock (syncLock)
            {
                return ID_NewImage;
            }
        }

        public int Get_ImageWidth()
        {
            lock (syncLock)
            {
                return ID_ImageWidth;
            }
        }

        public int Get_ImageHeight()
        {
            lock (syncLock)
            {
                return ID_ImageHeight;
            }
        }

        public byte[] Get_ImageData1D()
        {
            lock (syncLock)
            {
                ID_NewImage = false;
                return (byte[])ID_ImageData1D.Clone();
            }
        }

        //set methods
        public void Set_Rotation(string direction)
        {
            int state = 0;
            switch (direction.ToUpper())
            {
                case ("CCW"):
                    state = 1;
                    break;
                case ("180"):
                    state = 2;
                    break;
                case ("CW"):
                    state = 3;
                    break;
                default:
                    state = 0;
                    break;
            }
            lock(syncLock)
            {
                rotationState = state;
            }
        }

        public void Set_Flip(string direc)
        {
            int flip = 0;
            switch (direc.ToUpper())
            {
                case ("HORIZONTAL"):
                    flip = 1;
                    break;
                case ("VERTICAL"):
                    flip = 2;
                    break;
                case ("BOTH"):
                    flip = 3;
                    break;
                default:
                    flip = 0;
                    break;
            }
            lock (syncLock)
            {
                flipState = flip;
            }
        }

        // initializes array
        private void Init_TestBand(int width, int height, int num = 2)
        {
            lock (syncLock)
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
                                ID_ImageData1D[x + y * width] = 255;
                                break;
                            default:
                                ID_ImageData1D[x + y * width] = 0;
                                break;
                        }
                    }
                }
                ID_NewImage = true;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // execute everything queued
            while (ExecuteOnMainThread.Count > 0)
            {
                ExecuteOnMainThread.Dequeue().Invoke();
            }
            if (ExecuteOnMainThread.Count == 0)
            {
                ReceivingStatus = AWAITING_IMAGE;
            }
        }

#if !UNITY_EDITOR
        async void Start() 
        {
            // initialize image array values and corresponding attributes
            Init_TestBand(240, 320, 0);

            // create new server socket for host
            ServerSocket = new DatagramSocket();
            await StartServer();

            // await SendBroadcast("Hello World!", RemoteIP, RemotePort); // dont need it 
        }

        private async System.Threading.Tasks.Task StartServer()
        {
            ServerSocket.MessageReceived += ServerSocket_MessageReceived;
            try
            {
                await ServerSocket.BindServiceNameAsync(LocalPort);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                Debug.Log(SocketError.GetStatus(e.HResult).ToString());
                return;
            }
        }

        private async System.Threading.Tasks.Task SendBroadcast(string message, string ip = "255.255.255.255", string port = "5000")
        {
            try  // send out a message, otherwise receiving does not work ?!
            {
                var outputStream = await ServerSocket.GetOutputStreamAsync(new HostName(ip), port);
                DataWriter writer = new DataWriter(outputStream);
                writer.WriteString(message);
                await writer.StoreAsync();
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
                byte[] data = ToMemoryStream(streamIn).ToArray(); 

                // enqueue all, continually stack // used for managing events, creates a group of actions
                if (ReceivingStatus == AWAITING_IMAGE) // unnecessary but clean up later if things work?
                {
                    ExecuteOnMainThread.Enqueue(() =>
                    {
                        ProcessPacket(data);
                    });
                } 
            });
        }

        public void Dispose()
        {
            if (ServerSocket != null)
            {
                ServerSocket.Dispose();
                ServerSocket = null;
            }
        }

        // optional to process packet asynchronously
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
            switch (data.Length)
            {
                case (0):
                    break;
                case (START_PACKET_SIZE):
                    break;
                default:
                    // add jpeg compressed byte[] received to image buffer
                    byte[] strippedData = new byte[data.Length - NORMAL_PACKET_INDEX_BYTES];
                    Array.Copy(data, NORMAL_PACKET_INDEX_BYTES, strippedData, 0, data.Length - NORMAL_PACKET_INDEX_BYTES);
                    ProcessImageArr(strippedData);
                    break;
            }
        }

        // display image when last packet arrives
        private async void ProcessImageArr(byte[] img) // int imgType)
        {
            try
            {
                // Decode the JPEG
                MemoryStream stream = new MemoryStream(img);
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
                PixelDataProvider pixelData = await decoder.GetPixelDataAsync();

                int width = (int)decoder.PixelWidth;
                int height = (int)decoder.PixelHeight;
                byte[] tmp = pixelData.DetachPixelData();

                int bands = 4;
                //synthesize to one band
                byte[] tmpImgData = new byte[width * height];

                for (int i = 0; i < tmp.Length; i += 4)
                {
                    tmpImgData[i / 4] = RGBAToByte(tmp[i + 0], tmp[i + 1], tmp[i + 2], tmp[i + 3]);
                }

                tmpImgData = reformatBandImg(tmpImgData, ref width, ref height);

                lock (syncLock)
                {
                    ID_NewImage = false; // unnecesary, but ensures cant be accessed while updating.

                    lock (ID_ImageData1D)
                    {
                        ID_ImageWidth = width;
                        ID_ImageHeight = height;
                        ID_ImageData1D = tmpImgData;
                    }

                    ID_NewImage = true;

                    ID_fps = 1.0 / (DateTime.Now.Subtract(time).TotalSeconds);
                    time = DateTime.Now;
                }
                tmp = null;
                tmpImgData = null;
            }
            catch (Exception ex)
            {
                ebug.Log(ex.Message);
            }
        }
#endif
        private byte[] reformatBandImg(byte[] imageData, ref int width, ref int height)
        {
            /* Bitmap bmp;
            using (var ms = new MemoryStream(imageData))
            {
                bmp = new Bitmap(ms);
            } */

            byte[] outArr = new byte[img.Length];
            lock (syncLock) {
                int flip = flipState;
                int rotate = rotationState;
            }
            switch ((rotate, flip))
            {
                case ((0, 0)):
                    return x + width * y;
                case ((0, 1)):
                    for (int x = width - 1; x >= 0; --x)
                    {
                        for (int y = 0; y < height; y++)
                        {

                        }
                    }
                    return 0;
                case ((0, 2)):
                    return 0;
                case ((0, 3)):
                    return 0;
                case ((1, 0)):
                    return 0;
                case ((1, 1)):
                    return 0;
                case ((1, 2)):
                    return 0;
                case ((1, 3)):
                    return 0;
                case ((2, 0)):
                    return 0;
                case ((2, 1)):
                    return 0;
                case ((2, 2)):
                    return 0;
                case ((2, 3)):
                    return 0;
                case ((3, 0)):
                    return 0;
                case ((3, 1)):
                    return 0;
                case ((3, 2)):
                    return 0;
                case ((3, 3)):
                    return 0;
            }
        }

        private void IntSwap(ref int a, ref int b)
        {
            int tmp = a;
            a = b;
            b = tmp;
        }

        private byte RGBAToByte(byte r, byte g, byte b, byte a)
        {
            return (byte)((0 + r + g + b) / 3);
        }
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
    }
}
