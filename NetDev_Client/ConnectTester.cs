// Tests connection, on demand transmit via Reciever

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectTester : MonoBehaviour {
    // inspector vars
    public string remoteIP;
    public string remotePrimPort;
    public string remoteSecPort;

    // other vars
    public Receiver rec;
    int curHash;
    byte[] content;

	// Use this for initialization
	void Awake () {
        rec = new Receiver(remoteIP, remotePrimPort, remoteSecPort);
        curHash = rec.CurrentHash;
    }
	
	// Update is called once per frame
	void Update () {
	    if (rec.CurrentHash != curHash)
        {
            curHash = rec.CurrentHash;
            content = rec.Content;

            // debug
            Debug.Log(string.Format("Tester fetched updated content (hash: {0}). Content: {1}, Length: {2}",
                curHash, Receiver.ArrayToString(content), rec.ContentLen));
        }	
	}
}
