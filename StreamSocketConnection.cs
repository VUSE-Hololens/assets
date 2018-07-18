#if !UNITY_EDITOR && UNITY_METRO
using System;
using System.Linq;

using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Foundation;
using System.Threading.Tasks;

using System.Diagnostics;
#endif


namespace Client
{
    public delegate void LineReceive(string type, byte[] data);
    public delegate void ConnectorStatus(bool state, string message);

    public class StreamSocketConnection
    {
#if !UNITY_EDITOR && UNITY_METRO
    private string strMessage = string.Empty;
    public string res = string.Empty;
    public event LineReceive LineReceived;
    public event ConnectorStatus StatusUpdated;
    public bool IsConnected { get; private set; }
    private string userName = string.Empty;

    private bool IsSending = false;
    private bool IsReading = false;
    private bool isConnecting = false;

    private StreamSocket socket = null;
    
    private Task ListenerTask;

    private System.Text.ASCIIEncoding encoder;

    public StreamSocketConnection()
    {
      strMessage = string.Empty;
      res = string.Empty;
      IsConnected = false;
      userName = string.Empty;
      socket = null;
      IsSending = false;
      IsReading = false;

      encoder = new System.Text.ASCIIEncoding();
    }

    ~StreamSocketConnection()
    {
      Disconnect();
    }

    public void Connect(string ServerIP, string ServerPort, string UserName)
    {
      isConnecting = true;
      userName = UserName;
      socket = new StreamSocket();
      HostName networkHost = new HostName(ServerIP.Trim());
      IAsyncAction outstandingAction = socket.ConnectAsync(networkHost, ServerPort);
      AsyncActionCompletedHandler aach = new AsyncActionCompletedHandler(NetworkConnectedHandler);
      outstandingAction.Completed = aach;
    }

    public void Disconnect()
    {
      SendData("DSCON|" + userName);
      CloseConnection();
    }

    public void ServerClosed()
    {
      CloseConnection();
    }

    public async void ChangeIP(string ServerIP, string ServerPort)
    {
      while (isConnecting)
      {
        Task.Delay(10).Wait();
      }

      if (socket != null)
      {
        IsReading = false;
        IsSending = false;
        IsConnected = false;
        ListenerTask.Wait(100);

        await socket.CancelIOAsync();
        socket.Dispose();
        socket = null;

        Debug.WriteLine("Disconnected to change IP-Adress");
      }

      Connect(ServerIP, ServerPort, userName);
    }

    private void DoListen()
    {
      while (IsConnected)
        Listen();
    }

    private async void Listen()
    {
      if (!IsConnected || IsReading)
        return;

      IsReading = true;

      string strType = string.Empty;
      byte[] bData = null;
      DataReader reader = new DataReader(socket.InputStream);
      System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();

      try
      {
        uint sizeFieldCount = await reader.LoadAsync(sizeof(uint));
        if (sizeFieldCount != sizeof(uint))
        {
          res = "Failed to read stream.";
          return;
        }

        uint dataLength = reader.ReadUInt32();
        uint actualDataLength = await reader.LoadAsync(dataLength);
        if (dataLength != actualDataLength)
        {
          res = "Failed to read stream.";
          return;
        }

        byte[] receivedData = new byte[actualDataLength];
        reader.ReadBytes(receivedData);
        byte[] bType = receivedData.Take(5).ToArray();
        bData = receivedData.Skip(5).Take((int)actualDataLength - 5).ToArray();
        strType = enc.GetString(bType);

        res = "Successfully received Message";
        LineReceived(strType, bData);
      }
      catch (ObjectDisposedException)
      {
        //Client was to fast for server and tried to read old stream.
        Task.Delay(10).Wait();
      }
      catch (System.Runtime.InteropServices.COMException)
      {
        res = "Lost connection to server.";
        CloseConnection();
      }
      catch (Exception e)
      {
        switch (SocketError.GetStatus(e.HResult))
        {
          case SocketErrorStatus.Unknown:
            Debug.WriteLine("Exception from Listening: Error Type: " + e.GetType().ToString() + " Message: " + e.Message);
            throw;
          case SocketErrorStatus.HostNotFound:
            res = "Reading Failed. Host not found.";
            break;
          case SocketErrorStatus.OperationAborted:
            res = "Connection was terminated correctly.";
            break;
          default:
            res = "Reading failed. Other Exception.";
            LineReceived("DSCON", null);
            break;
        }
      }
      finally
      {
        log.Trace("StreamSocketConnection | Listen | Finished.");
        reader.DetachStream();
        IsReading = false;
      }
    }

    public async void SendData(string data)
    {
      while (IsSending)
        await Task.Delay(10);
      
      IsSending = true;
      bool sendStatus = false;
      DataWriter writer = new DataWriter(socket.OutputStream);
      byte[] bMsg = encoder.GetBytes(data);
      writer.WriteUInt32((uint)bMsg.Length);
      writer.WriteBytes(bMsg);
      string msgType = data.Take(5).ToString();
      try
      {
        await writer.StoreAsync();
        await writer.FlushAsync();
        res = "Succesfully send message of type: " + msgType;
        sendStatus = true;
      }
      catch (Exception e)
      {
        switch (SocketError.GetStatus(e.HResult))
        {
          case SocketErrorStatus.Unknown:
            StatusUpdated(false, "Sending Failed: SocketErrorUnknow");
            throw;
          case SocketErrorStatus.HostNotFound:
            res = "Sending Failed: Host not found.";
            break;
          case SocketErrorStatus.OperationAborted:
            res = "Sending Failed: Execution Aborted.";
            break;
          default:
            res = "Sending Failed: Other Exception occured";
            break;
        }
      }
      finally
      {
        writer.DetachStream();
        IsSending = false;
        StatusUpdated(sendStatus, res);
      }
    }

    private void NetworkConnectedHandler(IAsyncAction asyncInfo, AsyncStatus status)
    {
      string cRes = string.Empty;
      if (status == AsyncStatus.Completed)
      {
        IsConnected = true;
        cRes = "Succesfully connected to server.";
        SendData("CONCT|" + userName);
        ListenerTask = new Task(DoListen);
        ListenerTask.Start();
      }
      else
      {
        IsConnected = false;
        socket.Dispose();
        socket = null;
        cRes = "Failed to connect to server.";
      }

      isConnecting = false;
      StatusUpdated(IsConnected, cRes);
    }

    private async void CloseConnection()
    {
      if (socket != null)
      {
        IsReading = false;
        IsSending = false;
        IsConnected = false;
        ListenerTask.Wait(100);

        await socket.CancelIOAsync();
        socket.Dispose();
        socket = null;

        res = "Disconnected from server.";
      }
      else
      {
        IsReading = false;
        IsSending = false;
        IsConnected = false;
        socket = null;
        res = "No connection to server present.";
      }

      StatusUpdated(true, res);
    }
#endif
    }
}