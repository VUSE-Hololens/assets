/// Visualizer
/// Class for rendering various objects.
/// Mark Scherer, June 2018

using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using HoloToolkit.Unity.SpatialMapping;

/// <summary>
/// Class for visualizing objects as holograms.
/// </summary>
public class Visualizer
{
    /// <summary>
    /// Single item to be visualized.
    /// </summary>
    public abstract class Content
    {
        public Color color;

        public Content(Color myColor)
        {
            color = myColor;
        }
    }

    /// <summary>
    /// Point marker to be visualized.
    /// </summary>
    public class Marker : Content
    {
        public Vector3 point;
        public Vector3 scale;

        public Marker(Vector3 myPoint, float radius, Color myColor)
            : base(myColor)
        {
            point = myPoint;
            scale = new Vector3(radius, radius, radius);
        }
    }

    /// <summary>
    /// Line to be visualized.
    /// </summary>
    public class Line : Content
    {
        public Vector3[] positions;
        public float width;

        public Line(Vector3[] myPositions, float myWidth, Color myColor)
            : base( myColor)
        {
            positions = myPositions;
            width = myWidth;
        }
    }

    // control variables
    private string MarkerName;
    private string LineName;
    private GameObject Parent;
    private string ParentName;
    private bool ParentCreated = false;
    private Material material;

    // other variables
    private List<GameObject> MarkerGOs = new List<GameObject>();
    private List<GameObject> LineGOs = new List<GameObject>();
    private Vector3 ZeroScale = new Vector3(0, 0, 0);
    private static System.Random Rand = new System.Random();

    // indicators
    public int MarkersInUse { get; private set; }
    public int LinesInUse { get; private set; }
    public int TotalMarkers { get; private set; }
    public int TotalLines { get; private set; }

    public Visualizer(string myParentName, string myMarkerName, string myLineName, Material myMaterial)
    {
        ParentName = myParentName;
        MarkerName = myMarkerName;
        LineName = myLineName;
        material = myMaterial;

        // finish setup
        MarkersInUse = 0;
        LinesInUse = 0;
        TotalMarkers = 0;
        TotalLines = 0;
    }

    /// <summary>
    /// Visualizes all toRender. Hides all other content.
    /// </summary>
    public void Visualize(List<Content> toRender)
    {
        MarkersInUse = 0;
        LinesInUse = 0;

        // visualize all toRender
        foreach (Content con in toRender)
            Visualize(con);

        // hide extra GameObjects
        for (int i = MarkersInUse; i < MarkerGOs.Count; i++)
            HideMarker(i);
        for (int i = LinesInUse; i < LineGOs.Count; i++)
            HideLine(i);

        TotalMarkers = MarkerGOs.Count;
        TotalLines = LineGOs.Count;
    }

    /// <summary>
    /// Deletes all Marker and Line GO's
    /// </summary>
    public void Clear()
    {
        Object.Destroy(Parent);
        ParentCreated = false;

        MarkerGOs = new List<GameObject>();
        MarkersInUse = 0;
        TotalMarkers = 0;

        LineGOs = new List<GameObject>();
        LinesInUse = 0;
        TotalLines = 0;
    }

    /// <summary>
    /// Visualizes an object.
    /// </summary>
    public void Visualize(Content con)
    {
        // create parent (if startup)
        if (!ParentCreated)
        {
            Parent = new GameObject();
            Parent.name = ParentName;
            ParentCreated = true;
        }

        if (con.GetType() == typeof(Marker))
            VisualizeMarker((Marker)con);
        else
            VisualizeLine((Line)con);
    }

    /// <summary>
    /// Returns color scaled between minColor and maxColor in proportion to 
        /// scaling of value between minValue and maxValue.
    /// </summary>
    public static Color ScaleColor(byte value, byte minValue, byte maxValue, Color minColor, Color maxColor)
    {
        float fraction = (float)(value - minValue) / (maxValue - minValue);
        return ColorScale(fraction, minColor, maxColor);
    }

    /// <summary>
    /// Returns random color between minColor and maxColor.
    /// </summary>
    public static Color RandomColor(Color minColor, Color maxColor)
    {
        return new Color(ScaleFloat((float)Rand.NextDouble(), minColor.r, maxColor.r),
            ScaleFloat((float)Rand.NextDouble(), minColor.g, maxColor.g),
            ScaleFloat((float)Rand.NextDouble(), minColor.b, maxColor.b),
            ScaleFloat((float)Rand.NextDouble(), minColor.a, maxColor.a));
    }

    public static List<Content> CreateMarkers(List<Intersector.PointValue<byte>> pointValues, float radius, 
        byte minValue, byte maxValue, Color minColor, Color maxColor)
    {
        List<Content> result = new List<Content>();

        foreach (Intersector.PointValue<byte> pv in pointValues)
        {
            result.Add(new Marker(pv.Point.point, radius, 
                ScaleColor(pv.Value, minValue, maxValue, minColor, maxColor)));
        }

        return result;
    }

    public static List<Content> CreateMarkers(List<Vector3> points, float radius, Color color)
    {
        List<Content> result = new List<Content>();

        foreach (Vector3 pt in points)
        {
            result.Add(new Marker(pt, radius, color));
        }

        return result;
    }

    /// <summary>
    /// Returns list of monochromatic lines bounding bounds.
    /// </summary>
    public static List<Content> CreateBoundingLines(Bounds bounds, float width, Color color)
    {
        List<Content> result = new List<Content>();
        List<Vector3[]> positions = new List<Vector3[]>();
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        // create points
        Vector3 frontbotleft = min;
        Vector3 frontbotright = new Vector3(max.x, min.y, min.z);
        Vector3 fronttopleft = new Vector3(min.x, max.y, min.z);
        Vector3 fronttopright = new Vector3(max.x, max.y, min.z);
        Vector3 backbotleft = new Vector3(min.x, min.y, max.z);
        Vector3 backbotright = new Vector3(max.x, min.y, max.z);
        Vector3 backtopleft = new Vector3(min.x, max.y, max.z);
        Vector3 backtopright = max;

        // add bottom face
        positions.Add(new Vector3[] { frontbotleft, frontbotright });
        positions.Add(new Vector3[] { frontbotright, backbotright });
        positions.Add(new Vector3[] { backbotright, backbotleft });
        positions.Add(new Vector3[] { backbotleft, frontbotleft });
        // add top face
        positions.Add(new Vector3[] { fronttopleft, fronttopright });
        positions.Add(new Vector3[] { fronttopright, backtopright });
        positions.Add(new Vector3[] { backtopright, backtopleft });
        positions.Add(new Vector3[] { backtopleft, fronttopleft });
        // add verticals
        positions.Add(new Vector3[] { frontbotleft, fronttopleft });
        positions.Add(new Vector3[] { frontbotright, fronttopright });
        positions.Add(new Vector3[] { backbotleft, backtopleft });
        positions.Add(new Vector3[] { backbotright, backtopright });
        // add outer diagonals 1
        positions.Add(new Vector3[] { frontbotleft, fronttopright });
        positions.Add(new Vector3[] { frontbotright, backtopright });
        positions.Add(new Vector3[] { backbotright, backtopleft });
        positions.Add(new Vector3[] { backbotleft, fronttopleft });
        positions.Add(new Vector3[] { fronttopleft, backtopright });
        positions.Add(new Vector3[] { frontbotright, backbotleft });
        // add outer diagonals 2
        positions.Add(new Vector3[] { frontbotright, fronttopleft });
        positions.Add(new Vector3[] { backbotright, fronttopright });
        positions.Add(new Vector3[] { backbotleft, backtopright });
        positions.Add(new Vector3[] { frontbotleft, backtopleft });
        positions.Add(new Vector3[] { fronttopright, backtopleft });
        positions.Add(new Vector3[] { frontbotleft, backbotright });
        // add inner diagonals
        positions.Add(new Vector3[] { frontbotleft, backtopright });
        positions.Add(new Vector3[] { frontbotright, backtopleft });

        for (int i = 0; i < positions.Count; i++)
        {
            result.Add(new Line(positions[i], width, color));
        }

        return result;
    }

    /// <summary>
    /// Sets vertex colors in all meshes ID'd as visible by meshGuide with values in voxGrid.
    /// </summary>
    public static void ColorMesh(ReadOnlyCollection<SpatialMappingSource.SurfaceObject> allMeshes, 
        List<SurfacePoints> extraData, List<bool> meshGuide, VoxelGridManager<byte> voxGrid,
        Color32 color1, Color32 color2, byte value1, byte value2)
    {
        for (int i = 0; i < allMeshes.Count; i++)
        {
            if (meshGuide[i])
            {
                List<Color32> coloring = new List<Color32>();
                for (int j = 0; j < extraData[i].Wvertices.Count; j++)
                {
                    float scaledVal = (float)(voxGrid.Get(extraData[i].Wvertices[j]) - value1)
                        / (float)(value2 - value1);
                    coloring.Add(LerpViaHSV(color1, color2, scaledVal));
                }
                allMeshes[i].Filter.sharedMesh.SetColors(coloring);
            }
        }
    }

    /// <summary>
    /// Lerp between color1 and color2 via scaled value using an hsv scale
    /// </summary>
    public static Color32 LerpViaHSV(Color32 color1, Color32 color2, float scaleVal)
    {
        float h1;
        float h2;
        float s1;
        float s2;
        float v1;
        float v2;
        float hNew;
        float sNew;
        float vNew;

        Color.RGBToHSV(color1, out h1, out s1, out v1);
        Color.RGBToHSV(color2, out h2, out s2, out v2);

        hNew = h1 + (h2 - h1) * scaleVal;
        sNew = s1 + (s2 - s1) * scaleVal;
        vNew = v1 + (v2 - v1) * scaleVal;

        Color32 lerpedColor = Color.HSVToRGB(hNew, sNew, vNew);

        // fix transparency
        byte aNew = (byte)(((int)color1.a + (int)color2.a)/2);
        lerpedColor.a = aNew;

        return lerpedColor;
    }

    /// <summary>
    /// Visualizes a Marker object.
    /// </summary>
    private void VisualizeMarker(Marker marker)
    {
        // create new Marker GameObject if needed
        if (MarkersInUse == MarkerGOs.Count)
            MarkerGOs.Add(NewMarker());

        // assign marker properties
        MarkerGOs[MarkersInUse].transform.position = marker.point;
        MarkerGOs[MarkersInUse].transform.localScale = marker.scale;
        MarkerGOs[MarkersInUse].GetComponent<Renderer>().sharedMaterial.color = marker.color;

        MarkersInUse++;
    }

    // Hides marker at index in MarkerGOs
    private void HideMarker(int index)
    {
        MarkerGOs[index].transform.localScale = ZeroScale;
    }

    /// <summary>
    /// Visualizes a Line object.
    /// </summary>
    private void VisualizeLine(Line line)
    {
        // create new Line GameObject if needed
        if (LinesInUse == LineGOs.Count)
            LineGOs.Add(NewLine());

        // assign line properties
        LineGOs[LinesInUse].GetComponent<LineRenderer>().SetPositions(line.positions);
        LineGOs[LinesInUse].GetComponent<LineRenderer>().widthMultiplier = line.width;
        LineGOs[LinesInUse].GetComponent<Renderer>().sharedMaterial.color = line.color;

        LinesInUse++;
    }

    /// <summary>
    /// Hides Line obj at index of LineGOs.
    /// </summary>
    private void HideLine(int index)
    {
        LineGOs[LinesInUse].GetComponent<LineRenderer>().widthMultiplier = 0;
    }


    /// <summary>
    /// Creates new Marker GameObject.
    /// </summary>
    private GameObject NewMarker()
    {
        GameObject Child = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        if (Child.GetComponent<MeshFilter>() == null)
            Child.AddComponent<MeshFilter>();
        if (Child.GetComponent<MeshRenderer>() == null)
            Child.AddComponent<MeshRenderer>();
        if (Child.GetComponent<SphereCollider>() == null)
            Child.AddComponent<SphereCollider>();

        Child.name = MarkerName;
        Child.transform.parent = Parent.transform;
        Child.GetComponent<Renderer>().material = Material.Instantiate(material);

        return Child;
    }

    /// <summary>
    /// Creates new Line GameObject.
    /// </summary>
    private GameObject NewLine()
    {
        GameObject Child = new GameObject();

        if (Child.GetComponent<LineRenderer>() == null)
            Child.AddComponent<LineRenderer>();

        Child.name = LineName;
        Child.transform.parent = Parent.transform;
        Child.GetComponent<Renderer>().material = Material.Instantiate(material);

        return Child;
    }

    /// <summary>
    /// Returns color at fraction on scale between minColor and maxColor
    /// </summary>
    private static Color ColorScale(float fraction, Color minColor, Color maxColor)
    {
        if (fraction < 0 || fraction > 1)
            throw new System.ArgumentOutOfRangeException("fraction", "not 0-1");

        Color result = new Color();
        result.a = ScaleFloat(fraction, minColor.a, maxColor.a);
        result.r = ScaleFloat(fraction, minColor.r, maxColor.r);
        result.g = ScaleFloat(fraction, minColor.g, maxColor.g);
        result.b = ScaleFloat(fraction, minColor.b, maxColor.b);
        return result;
    }

    /// <summary>
    /// Returns value at fraction on scale between min and max.
    /// </summary>
    private static float ScaleFloat(float fraction, float min, float max)
    {
        return min + fraction * (max - min);
    }
}