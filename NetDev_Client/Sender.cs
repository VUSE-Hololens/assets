// Sender.cs
// very lightweight class for transmitting UDP FROM hololens

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

public class Sender {

#if !UNITY_EDITOR
    // comm vars
    DatagramSocket outSock;
#endif

#if !UNITY_EDITOR
    // constructor
    public Sender(string outSockLocalPort) {
        outSock = new DatagramSocket();
        Configure(outSockLocalPort);
    }

    private async void Configure(string outSockLocalPort) {
        // add message received event handler
        outSock.MessageReceived += outSock_MessageReceived;
        Debug.Log("outSock Event Handler Added");
        
        // bind outSock locally
        try
        {
            await outSock.BindServiceNameAsync(outSockLocalPort);
            Debug.Log("Bound outSock to Port " + outSockLocalPort);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            Debug.Log(SocketError.GetStatus(e.HResult).ToString());
            return;
        }
    }
#endif

#if !UNITY_EDITOR
    //async 
    private async void outSock_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
    {
        DataReader reader = args.GetDataReader();
        string message = reader.ReadString(5); // reads first 5 characters
        
        Debug.Log(string.Format("Recieved message: {0}, from: {1} - {2}", message, args.RemoteAddress, args.RemotePort));
    }
#endif

#if !UNITY_EDITOR
    public async System.Threading.Tasks.Task Send(string message, string RemoteIP, string RemotePort)
    {
        try
        {
            var outputStream = await outSock.GetOutputStreamAsync(new HostName(RemoteIP), RemotePort);
            DataWriter writer = new DataWriter(outputStream);
            writer.WriteString(message);
            await writer.StoreAsync();
            Debug.Log(string.Format("sent: {0}, to: {1}, - {2}", message, RemoteIP, RemotePort));
        }
        catch (Exception ex)
        {
            Debug.Log(ex.ToString());
            Debug.Log(SocketError.GetStatus(ex.HResult).ToString());
            return;
        }
    }
#endif
}
