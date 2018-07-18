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
    public class ImageReceiver : MonoBehaviour
    {
        static readonly string RemoteIP = "10.67.87.102";
        static readonly string RemotePort = "5000";
        static readonly string LocalPort = "5001";

        private const int PACKET_SIZE = 20000;
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
        //private int ID_ImageType;
        private int ID_ImageWidth;
        private int ID_ImageHeight;
        private string ID_Message = "testing";
        private bool ID_NewImage = false;
        private double ID_fps = 0;
        private DateTime time;
        private int ImgCount = 0;

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
        public string Get_Message() { return ID_Message; }

        public bool CheckNewImage() { return ID_NewImage; }

        // public int Get_ImageType() { return ID_ImageType; }
        
        public int Get_ImageWidth() { return ID_ImageWidth; }

        public int Get_ImageHeight() { return ID_ImageHeight; }

        public byte[] Get_ImageData1D()
        {
            ID_NewImage = false;
            return ID_ImageData1D;
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
            ID_NewImage = true;
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
            Init_TestBand(240, 320, 0);
            ServerSocket = new DatagramSocket();
            await StartServer();
            // await SendBroadcast("Hello World!"); // dont need it 
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
                    byte[] processingBuffer = ImageBuffer;
                    ImageBuffer = null;
                    ProcessImageArr(processingBuffer);
                    break;
                // It's only possible for start packets to be of size 3 (others must be >= 4)
                case (START_PACKET_SIZE):
                    // do nothing because all is coming in one packet
                    break;
                default:
                    // add jpeg compressed byte[] received to image buffer
                    ImageBuffer = new byte[data.Length - NORMAL_PACKET_INDEX_BYTES];
                    Array.Copy(data, NORMAL_PACKET_INDEX_BYTES, ImageBuffer, 0, data.Length - NORMAL_PACKET_INDEX_BYTES);
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
               
                ID_ImageWidth = (int)decoder.PixelWidth;
                ID_ImageHeight = (int)decoder.PixelHeight;

                byte[] tmp = pixelData.DetachPixelData();

                ID_NewImage = false; // unnecesary, but ensures cant be accessed while updating.
                ID_ImageData1D = new byte[ID_ImageWidth * ID_ImageHeight];
                //synthesize to one band
                for (int i = 0; i < tmp.Length; i += 4)
                {
                    ID_ImageData1D[i / 4] = RGBAToByte(tmp[i+0], tmp[i+1], tmp[i+2], tmp[i+3]);
                }
                tmp = null;

                ID_NewImage = true;
                ID_fps = 1.0/(DateTime.Now.Subtract(time).TotalSeconds);
                time = DateTime.Now;
            }
            catch (Exception ex)
            {
                if (debug) Debug.Log(ex.Message);
            }
        }
#endif
        private byte RGBAToByte(byte r, byte g, byte b, byte a)
        {
            return (byte)((r + g + b) / 3);
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
