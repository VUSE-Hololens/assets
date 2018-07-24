// Lite version of displayer class for testing
// Mark Scherer, July 2018

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayerLite : MonoBehaviour {

    // inspector vars
    public GameObject TransmitterParent;

    // other vars
    private Transmitter trans;

	// Use this for initialization
	void Start () {
        trans = TransmitterParent.GetComponent<Transmitter>();
	}
	
	// Update is called once per frame
	void Update () {
        if (trans.Updated)
        {
            Transmitter.Frame nf = trans.GetUpdate;
            Debug.Log(string.Format("New frame recognized internally. Pixels: {0}x{1}x{2}, FOV: {3}x{4}, Data: {5}, {6}, {7}, {8}...",
                nf.height, nf.width, nf.bands, nf.FOVx, nf.FOVy, nf.data[0], nf.data[1], nf.data[2], nf.data[3]));
        }
	}
}
