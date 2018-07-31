﻿using UnityEngine;
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
    public enum DefaultData { diagonalGradient, verticalGradient, allMax, preloadedImg}

    public class ImageReceiver : MonoBehaviour
    {
        // inspector vars
        [Tooltip("Default sensor data to send until actual sensor data recieved.")]
        public DefaultData InitializedData = DefaultData.diagonalGradient;
        
        // Server/Client Settings
        static readonly string RemoteIP = "10.67.87.102"; // needed only if broadcasting message
        static readonly string RemotePort = "5000";
        static readonly string LocalPort = "5001";

        //packet attributes
        private const int PACKET_SIZE = 30000;
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

        // initializes array
        private void Init_TestBand(int width, int height)
        {
            // determine code for Init_TestBand
            int numCode = -1;
            if (InitializedData == DefaultData.diagonalGradient)
                numCode = 0;
            else if (InitializedData == DefaultData.allMax)
                numCode = 1;
            else if (InitializedData == DefaultData.verticalGradient)
                numCode = 2;
            else if (InitializedData == DefaultData.preloadedImg)
                numCode = 3;

            lock (syncLock)
            {
                ID_ImageHeight = height;
                ID_ImageWidth = width;
                ID_ImageData1D = new byte[width * height];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        // control output data until data recieved from sensor
                        switch (numCode)
                        {
                            case (0):
                                // diagonal gradient
                                float val = 255.0f * ((x + y + 0.0f) / (width + height));
                                ID_ImageData1D[x + y * width] = (byte)val;
                                break;
                            case (1):
                                // all max
                                ID_ImageData1D[x + y * width] = 255;
                                break;
                            case (2):
                                // vertical gradient
                                ID_ImageData1D[x + y*width] = (byte)(255.0*(height-y)/height);
                                break;
                            default:
                                // preloaded images
                                ID_ImageWidth = NDVIDataBytes.width;
                                ID_ImageHeight = NDVIDataBytes.height;
                                ID_ImageData1D = NDVIDataBytes.NDVI_right;
                                break;
                        }
                    }
                }
                Debug.Log(string.Format("Height: {0}, Width: {1}, Length: {2}", ID_ImageHeight, ID_ImageWidth, ID_ImageData1D.Length));
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
        async void Start() //async
        {
            
        
            // initialized ID_ImageData1D to all Green to prove displayer working correctly
            Init_TestBand(300, 400);
            ServerSocket = new DatagramSocket();
            await StartServer();
            // await SendBroadcast("Hello World!"); // dont need it 
        }

        private async System.Threading.Tasks.Task StartServer()
        {
            ServerSocket.MessageReceived += ServerSocket_MessageReceived;
            Debug.Log("Server Socker Event Handler Added");
            try
            {
                await ServerSocket.BindServiceNameAsync(LocalPort);
                Debug.Log("Bound Socket to Port " + LocalPort);
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
                var outputStream = await ServerSocket.GetOutputStreamAsync(new HostName(RemoteIP), RemotePort);
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
            Debug.Log("MessageReceivedEvent");
            if (ReceivingStatus != AWAITING_IMAGE) return; // dont do anything if not ready to receive

            await System.Threading.Tasks.Task.Run(() =>
            {
                Debug.Log("Getting Data Async");
                Stream streamIn = args.GetDataStream().AsStreamForRead();
                byte[] data = ToMemoryStream(streamIn).ToArray();
                Debug.Log("Data array created of length " + data.Length);
                // enqueue all, continually stack // used for managing events, creates a group of actions
                ProcessPacket(data);
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
                    //byte[] processingBuffer = ImageBuffer;
                    break;
                // It's only possible for start packets to be of size 3 (others must be >= 4)
                case (START_PACKET_SIZE):
                    // do nothing because all is coming in one packet
                    break;
                default:
                    ExecuteOnMainThread.Enqueue(() =>
                    {
                        // add jpeg compressed byte[] received to image buffer
                        byte[] strippedData = new byte[data.Length - NORMAL_PACKET_INDEX_BYTES];
                        Debug.Log("Stripped Data Array created of length " + strippedData.Length);
                        Array.Copy(data, NORMAL_PACKET_INDEX_BYTES, strippedData, 0, data.Length - NORMAL_PACKET_INDEX_BYTES);
                        ProcessImageArr(strippedData);
                    });
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
                Debug.Log("Pixel Data Created. Format: " + decoder.BitmapPixelFormat);

                uint width = decoder.PixelWidth;
                uint height = decoder.PixelHeight;
                byte[] tmp = pixelData.DetachPixelData();
                Debug.Log("Pixel Data Detached. Length: " + tmp.Length + ", Size: (" + width + ", " + height + ")");
                for (int i = 0; i < 10; i++)
                {
                    Debug.Log(string.Format("Pixel {0}: RGBA = ({1}, {2}, {3}, {4})", 1000*i, tmp[1000*i], tmp[1000*i+1], tmp[1000*i+2], tmp[1000*i+3]));
                }

                //synthesize to one band
                byte[] tmpImgData = new byte[(int)width * (int)height];
                for (int i = 0; i < tmp.Length; i += 4)
                {
                    tmpImgData[i / 4] = RGBAToByte(tmp[i + 0], tmp[i + 1], tmp[i + 2], tmp[i + 3]);
                }

                lock (syncLock)
                {
                    ID_NewImage = false; // unnecesary, but ensures cant be accessed while updating.

                    lock (ID_ImageData1D)
                    {
                        ID_ImageWidth = (int)width;
                        ID_ImageHeight = (int)height;
                        ID_ImageData1D = tmpImgData;
                    }

                    ID_NewImage = true;

                    ID_fps = 1.0 / (DateTime.Now.Subtract(time).TotalSeconds);
                    time = DateTime.Now;
                }
                //tmp = null;
                tmpImgData = null;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }
#endif
        private byte RGBAToByte(byte r, byte g, byte b, byte a)
        {
            //float green = (float)g;
            //float red = (float)r;
            //float blue = (float)b;
            //return (byte)(255f * (green / (red + green + blue)));
            return Math.Max(Math.Max(r, g), b);
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
