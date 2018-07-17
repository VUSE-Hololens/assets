using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataDisplayer : MonoBehaviour
{

    // inspector vars
    [Tooltip("GO container DataProducer component")]
    public GameObject DataProducerContainer;
    [Tooltip("Drag default material here.")]
    public Material DefaultMaterial;
    // Visualization Options
    public float PixelSpacing = 0.05f;
    public float PixelSize = 0.04f;
    public int StartingHeight = 20;
    public int StartingWidth = 20;
    public Color32 MinColor;
    public Color32 MaxColor;

    // dependencies
    private Visualizer Vis; // for updating pixel colors
    private DataProducer Producer; // for producing simulated data

    // other variables
    private int Height; // pixel counts
    private int Width;
    private int ProducerHeight;
    private int ProducerWidth;
    private byte[] SensorData; // incoming array of data
    private List<Vector3> PixelPositions;
    private List<Visualizer.Content> PixelContent; // list of pixel color control


    // Use this for initialization
    void Start()
    {
        // finish initialization
        Vis = new Visualizer("PixelParent", "Pixel", "Line", DefaultMaterial);
        Producer = DataProducerContainer.GetComponent<DataProducer>();
    }

    // Update is called once per frame
    void Update()
    {
        // grab new data
        Height = StartingHeight;
        Width = StartingWidth;
        SensorData = Producer.SensorData;
        ProducerHeight = Producer.Height;
        ProducerWidth = Producer.Width;

        // update pixel positions
        PixelPositions = new List<Vector3>();
        for (int i = 0; i < Height*Width; i++)
            PixelPositions.Add(PixelPos(i));

        // update pixel content
        PixelContent = new List<Visualizer.Content>();
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                // set indices mapping sphere size to sensor size
                int PixelInd = x + Width * y;
                int SensorInd = (int) (x * (1.0f * ProducerWidth / Width) + ProducerWidth * y * (1.0f* ProducerHeight / Height));

                // create temporary list to hold single point-value to accomadate CreateMarkers.
                List<Visualizer.PointValue<byte>> tmp = new List<Visualizer.PointValue<byte>>();
                tmp.Add(new Visualizer.PointValue<byte>(PixelPositions[PixelInd], SensorData[SensorInd]));

                // add single new Content object
                PixelContent.AddRange(Visualizer.CreateMarkers(tmp, PixelSize, 0, 255, MinColor, MaxColor));
            }
        }

        /* for (int i = 0; i < SensorData.Length; i++)
        {
            // create temporary list to hold single point-value to accomadate CreateMarkers.
            List<Visualizer.PointValue<byte>> tmp = new List<Visualizer.PointValue<byte>>();
            tmp.Add(new Visualizer.PointValue<byte>(PixelPositions[i], SensorData[i]));

            // add single new Content object
            PixelContent.AddRange(Visualizer.CreateMarkers(tmp, PixelSize, 0, 255, MinColor, MaxColor));
        }*/

        // visualize
        Vis.Visualize(PixelContent);
    }

    /// <summary>
    /// Calculates postion of pixel specified by index within SensorData list.
    /// </summary>
    private Vector3 PixelPos(int index)
    {
        int i = index / Width;
        int j = index % Width;

        Vector3 Offset = new Vector3(PixelSpacing * (i - Width / 2), PixelSpacing * (j - Height / 2), 0);
        return gameObject.transform.position + Offset;
    }
}