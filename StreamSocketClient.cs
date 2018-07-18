using UnityEngine;
using HoloToolkit.Unity;

#if !UNITY_EDITOR && UNITY_METRO
using System;
using System.Linq;
using Windows.Networking;
using Windows.Storage.Streams;
#endif

namespace Client
{

    public class StreamSocketClient : Singleton<StreamSocketClient>
    {
        [Tooltip("IP-Adress of Server.")]
        public string ServerIP = "127.0.0.1";

        [Tooltip("Port-Nmber used to connect to Server.")]
        public string ServerPort = "10000";

        [Tooltip("UserName used to connect to Server.")]
        public string UserName = "UserName";

        [Tooltip("States whether App should connect to Server on Startup")]
        public bool AutoConnect = true;

        public bool IsConnected { get; private set; }

#if !UNITY_EDITOR && UNITY_METRO

    private StreamSocketConnection connector;
    private System.Text.ASCIIEncoding encoder;
    void Start()
    {
      connector = new StreamSocketConnection();
      encoder = new System.Text.ASCIIEncoding();
      connector.LineReceived += OnLineReceived;
      connector.StatusUpdated += OnStatusUpdated;

      if(AutoConnect)
        Connect();
    }

    public void Update()
    {
      IsConnected = connector.IsConnected;
    }

    private void OnApplicationQuit()
    {
      connector.Disconnect();
    }

    public void ChangedIP()
    {
      connector.ChangeIP(ServerIP, ServerPort);
    }

    public void TryReconnect()
    {
      Connect();
    }

    public void Connect()
    {
      if (!connector.IsConnected)
      {
        connector.Connect(ServerIP, ServerPort, UserName);
      }
      else
      {
        Debug.Log("Already connected.");
      }
    }

    public void Disconnect()
    {
      connector.Disconnect();
    }

    public void SendMessage()
    {
      string testMessage = "Test Message from Client.";
      connector.SendData("CHATM|" + testMessage);
    }

    private void OnLineReceived(string type, byte[] data)
    {
      string[] strDataArray = null;

      switch (type)
      {
        case "JOINM":
          Debug.Log("Received JOIN-Message");
          break;
        case "BROAD":
          strDataArray = ByteToString(data);
          Debug.Log("Message from Sever: " + strDataArray[1]);
          break;
        case "CHATM":
          strDataArray = ByteToString(data);
          Debug.Log("Message from Sever: " + strDataArray[1]);
          break;
        case "DSCON":
          debug.Log("Server send Disconnect message.");
          connector.Disconnect();
          break;
        case "SVRCL":
          debug.Log("Server closed down.");
          connector.ServerClosed();
          break;
        default:
          Debug.Warning("Unknown Message received.");
          break;
      }
    }

    private string[] ByteToString(byte[] bData)
    {
      string strData = encoder.GetString(bData);
      string[] strArr = strData.Split("|"[0]);
      return strArr;
    }

    private void OnStatusUpdated(bool status, string message)
    {
      if (status)
      {
        Debug.Log(message);
      }
      else
      {
        Debug.Warning(message);
      }
    }

#else

        public void Start()
        {
            if (AutoConnect)
                Connect();
        }

        public void Connect()
        {
            Debug.Log("Connect to Server.");
        }

        public void Disconnect()
        {
            Debug.Log("Disconnect from Server.");
        }

        public void SendMessage()
        {
            Debug.Log("SendMessage to Server.");
        }

        public void ChangedIP()
        {
            Debug.Log("Changed IP executed.");
        }

        public void TryReconnect()
        {
            Debug.Log("Try Reconnect executed.");
        }
#endif

        private void OnApplicationPause(bool pause)
        {
            // Disconnect when application pauses and reconnect when application resumes.
            if (pause && IsConnected)
            {
                Disconnect();
            }
            else
            {
                if (AutoConnect)
                {
                    Connect();
                }
            }
        }

    }
}