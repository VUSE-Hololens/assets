// Data Displayer
// Pulls sensor data from DataProducer, displays
// Mark Scherer, July 2018

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataDisplayer : MonoBehaviour {

    // inspector vars
    [Tooltip("GO container DataProducer component")]
    public GameObject TransmitterContainer;
    [Tooltip("Drag default material here.")]
    public Material DefaultMaterial;
    // Visualization Options
    public float PixelSpacing = 0.05f;
    public float PixelSize = 0.04f;
    public Color MinColor;
    public Color MaxColor;

    // dependencies
    private Visualizer Vis; // for updating pixel colors
    private Receiver Rec; // for grabbing data

    // other variables
    private int Height; // pixel counts
    private int Width;
    private byte[] SensorData; // incoming array of data
    private int Hash;
    private List<Vector3> PixelPositions;
    private List<Visualizer.Content> PixelContent; // list of pixel color control
    

	// Use this for initialization
	void Start () {
        // finish initialization
        Vis = new Visualizer("PixelParent", "Pixel", "Line", DefaultMaterial);
        Rec = TransmitterContainer.GetComponent<ConnectTester>().rec;
        Hash = Rec.CurrentHash;

        // Unity editor testing
#if UNITY_EDITOR
        Hash = Hash + 1;
#endif
    }
	
	// Update is called once per frame
	void Update () {
       if (Hash != Rec.CurrentHash)
        {
#if UNITY_EDITOR
            // dummy visualizer test
            Width = 20;
            Height = 15;
            SensorData = new byte[Width*Height];
#else
            // grab new data
            Hash = Rec.CurrentHash;
            SensorData = Rec.Content;
            Vector2Int pixelCount = Rec.Pixels;
            Width = pixelCount.x;
            Height = pixelCount.y;

            // debug
            Debug.Log(string.Format("Displayed loading new hash: {0}", Rec.CurrentHash));
#endif

            // update pixel positions
            PixelPositions = new List<Vector3>();
            for (int i = 0; i < SensorData.Length; i++)
                PixelPositions.Add(PixelPos(i));

            // update pixel content
            PixelContent = new List<Visualizer.Content>();
            for (int i = 0; i < SensorData.Length; i++)
            {
                // create temporary list to hold single point-value to accomadate CreateMarkers.
                List<Visualizer.PointValue<byte>> tmp = new List<Visualizer.PointValue<byte>>();
                tmp.Add(new Visualizer.PointValue<byte>(PixelPositions[i], SensorData[i]));

                // add single new Content object
                PixelContent.AddRange(Visualizer.CreateMarkers(tmp, PixelSize, 0, 255, MinColor, MaxColor));
            }

            // visualize
            Vis.Visualize(PixelContent);
        }
    }

    /// <summary>
    /// Calculates postion of pixel specified by index within SensorData list.
    /// </summary>
    private Vector3 PixelPos(int index)
    {
        int i = index % Width;
        int j = index / Width;

        Vector3 Offset = new Vector3(PixelSpacing * (i - Width / 2), -1 * PixelSpacing * (j - Height / 2), 0);
        return gameObject.transform.position + Offset;
    }
}
