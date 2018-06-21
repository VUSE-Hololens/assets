/// Visualizer
/// Class for rendering various objects.
/// Mark Scherer, June 2018


using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class for rendering various objects.
/// </summary>
public class Visualizer {
    // control variables
    private string ParentName;
    private string MarkerName;
    private string LineName;
    private Material material;

    // other variables
    private Vector3 ZeroScale = new Vector3(0, 0, 0);
    private List<GameObject> Markers = new List<GameObject>();
    private List<GameObject> Lines = new List<GameObject>();
    private bool ParentCreated = false;
    private GameObject Parent;

    // metadata
    public int RenderedMarkers { get; private set; }
    public int TotalMarkers { get; private set; }
    public int RenderedLines { get; private set; }
    public int TotalLines { get; private set; }

    // constructor
    public Visualizer(string myParentName, string myMarkerName, string myLineName, Material myMaterial)
    {
        ParentName = myParentName;
        MarkerName = myMarkerName;
        LineName = myLineName;
        material = myMaterial;
    }

    /// <summary>
    /// Renders list of points.
    /// </summary>
    public void VisualizePoints(List<Intersector.PointValue<byte>> points, float radius, 
        Color minColor, Color maxColor, byte minValue, byte maxValue)
    {
        // create parent (if startup)
        if (!ParentCreated)
        {
            Parent = new GameObject();
            Parent.name = ParentName;
            ParentCreated = true;
        }

        Vector3 Scale = new Vector3(radius, radius, radius);

        // create new marker game objects (if nec)
        while (points.Count > Markers.Count)
            Markers.Add(NewMarker());

        // reassign markers to current vertices
        for (int i = 0; i < points.Count; i++)
        {
            Markers[i].transform.position = points[i].Point;
            Markers[i].transform.localScale = Scale;
            Markers[i].GetComponent<MeshRenderer>().material.color = 
                ScaleColor(Fraction(points[i].Value, minValue, maxValue), minColor, maxColor);
        }

        // remove remaining markers from view
        for (int i = points.Count; i < Markers.Count; i++)
            Markers[i].transform.localScale = ZeroScale;

        RenderedMarkers = points.Count;
        TotalMarkers = Markers.Count;
    }

    /// <summary>
    /// Renders list of bounding boxes defined by (mins, maxes).
    /// </summary>
    public void VisualizeBoxes(List<Vector3> mins, List<Vector3> maxes, float width, Color color)
    {
        // create parent (if startup)
        if (!ParentCreated)
        {
            Parent = new GameObject();
            Parent.name = ParentName;
            ParentCreated = true;
        }

        List<Vector3[]> positions = new List<Vector3[]>();
        for (int i = 0; i < mins.Count; i++)
        {
            positions.AddRange(BoxLines(mins[i], maxes[i]));
        }

        while (positions.Count > Lines.Count)
            Lines.Add(NewLine());

        // reassign lines to current boxes
        for (int i = 0; i < positions.Count; i++)
        {
            Lines[i].GetComponent<LineRenderer>().SetPositions(positions[i]);
            Lines[i].GetComponent<LineRenderer>().widthMultiplier = width;
            Lines[i].GetComponent<MeshRenderer>().material.color = color;
        }

        // remove remaining lines from view
        for (int i = positions.Count; i < Lines.Count; i++)
            Lines[i].GetComponent<LineRenderer>().widthMultiplier = 0;

        RenderedLines = mins.Count;
        TotalLines = Lines.Count;
    }

    /// <summary>
    /// Returns bounding lines for box (min, max).
    /// </summary>
    private List<Vector3[]> BoxLines(Vector3 min, Vector3 max)
    {
        List<Vector3[]> result = new List<Vector3[]>();

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
        result.Add(new Vector3[] { frontbotleft, frontbotright });
        result.Add(new Vector3[] { frontbotright, backbotright });
        result.Add(new Vector3[] { backbotright, backbotleft });
        result.Add(new Vector3[] { backbotleft, frontbotleft });
        // add top face
        result.Add(new Vector3[] { fronttopleft, fronttopright });
        result.Add(new Vector3[] { fronttopright, backtopright });
        result.Add(new Vector3[] { backtopright, backtopleft });
        result.Add(new Vector3[] { backtopleft, fronttopleft });
        // add verticals
        result.Add(new Vector3[] { frontbotleft, fronttopleft });
        result.Add(new Vector3[] { frontbotright, fronttopright });
        result.Add(new Vector3[] { backbotleft, backtopleft });
        result.Add(new Vector3[] { backbotright, backtopright });
        // add outer diagonals 1
        result.Add(new Vector3[] { frontbotleft, fronttopright});
        result.Add(new Vector3[] { frontbotright, backtopright});
        result.Add(new Vector3[] { backbotright, backtopleft});
        result.Add(new Vector3[] { backbotleft, fronttopleft});
        result.Add(new Vector3[] { fronttopleft, backtopright});
        result.Add(new Vector3[] { frontbotright, backbotleft});
        // add outer diagonals 2
        result.Add(new Vector3[] { frontbotright, fronttopleft });
        result.Add(new Vector3[] { backbotright, fronttopright});
        result.Add(new Vector3[] { backbotleft, backtopright});
        result.Add(new Vector3[] { frontbotleft, backtopleft});
        result.Add(new Vector3[] { fronttopright, backtopleft});
        result.Add(new Vector3[] { frontbotleft, backbotright});
        // add inner diagonals
        result.Add(new Vector3[] { frontbotleft, backtopright});
        result.Add(new Vector3[] { frontbotright, backtopleft});

        return result;
    }

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
        Child.GetComponent<Renderer>().material = material;
        return Child;
    }

    private GameObject NewLine()
    {
        GameObject Child = new GameObject();
        if (Child.GetComponent<LineRenderer>() == null)
            Child.AddComponent<LineRenderer>();
        Child.name = LineName;
        Child.transform.parent = Parent.transform;
        Child.GetComponent<Renderer>().material = material;
        return Child;
    }

    /// <summary>
    /// Returns Color scaled between min and max according to fraction (0-1)
    /// </summary>
    private Color ScaleColor(float fraction, Color min, Color max)
    {

        if (fraction < 0 || fraction > 1)
            throw new System.ArgumentOutOfRangeException("fraction", "not 0-1");

        Color result = new Color();
        result.a = ScaleFraction(fraction, min.a, max.a);
        result.r = ScaleFraction(fraction, min.r, max.r);
        result.g = ScaleFraction(fraction, min.g, max.g);
        result.b = ScaleFraction(fraction, min.b, max.b);
        return result;
    }

    private float ScaleFraction(float fraction, float min, float max)
    {
        return min + fraction * (max - min);
    }

    private float Fraction(float value, float min, float max)
    {
        return (value - min) / (max - min);
    }
}
