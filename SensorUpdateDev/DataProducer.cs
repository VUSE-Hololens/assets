// DataProducer
// Produces simulated sensor data for testing purposes
// Mark Scherer, July 2018

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataProducer : MonoBehaviour {

    // inspector vars
    public int StartingHeight = 10;
    public int StartingWidth = 10;
    [Tooltip("Continually randomize sensor data?")]
    public bool Randomize = true;

    // other vars
    public int Height { get; private set; }
    public int Width { get; private set; }
    public byte[] SensorData { get; private set; }

	// Use this for initialization
	void Start () {
        // set to starting values
        Height = StartingHeight;
        Width = StartingWidth;
        SensorData = RandomArray(Height * Width);
	}
	
	// Update is called once per frame
	void Update () {
		if (Randomize)
            SensorData = RandomArray(Height * Width);
    }

    /// <summary>
    /// returns array of random byte values
    /// </summary>
    private byte[] RandomArray(int length)
    {
        byte[] result = new byte[length];

        for (int i = 0; i < length; i++)
            result[i] = (byte)Random.Range(0, 255);

        return result;
    }
}
