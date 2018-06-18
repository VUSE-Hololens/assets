// Generates random point, visually ID's those ID'd by Intersector as in FOV
// Mark Scherer, June 2018

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Intersector))]
public class VertexDriver : MonoBehaviour {

    // Inspector variables 
    public Vector3 CubeCenter = new Vector3(0, 0, 5);
    public float CubeSize = 2f;
    public int Vertices = 100;
    public float VertexSize = 0.1f;
    public float LineSize = 0.05f;
    public Vector2 TargetFOV = new Vector2(10, 10);
    public Material InViewMaterial;
    public Material OutViewMaterial;
    public Material LineMaterial;
    public bool ViewVectorLabels = true;
    public GameObject VVLabelText;
    public GameObject VVLabelBackground;
    public Vector3 VVLabelOffset = new Vector3(0, 0, -.1f);

    // other variables
    private ViewVector FOV;
    public Frustum ViewField { get; private set; }
    private Vector3 CubeMin, CubeMax;
    private System.Random Rand = new System.Random();
    private List<Vector3> Points = new List<Vector3>();
    private Vector3 Scale;
    public Intersector Inter { get; private set; }
    private List<Vector3> InView, OutView;
    private List<ViewVector> VV;
    private List<Vector3> Pvecs;
    private GameObject Parent;


    // Use this for initialization
    void Start () {
        // finish setup
        FOV = new ViewVector(TargetFOV.x, TargetFOV.y);
        ViewField = new Frustum(Camera.main.transform, FOV);
        CubeMin = new Vector3(CubeCenter.x - CubeSize / 2f, CubeCenter.y - CubeSize / 2f,
            CubeCenter.z - CubeSize / 2f);
        CubeMax = new Vector3(CubeCenter.x + CubeSize / 2f, CubeCenter.y + CubeSize / 2f,
            CubeCenter.z + CubeSize / 2f);
        for (int i = 0; i < Vertices; i++)
            Points.Add(RandomPoint(CubeMin, CubeMax, Rand));
        Scale = new Vector3(VertexSize, VertexSize, VertexSize);
        Inter = Intersector.Instance;

        // create point markers
        Parent = new GameObject();
        Parent.name = "VertexMarkers";
        for (int i = 0; i < Vertices; i++)
        {
            GameObject Child = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Child.AddComponent<MeshFilter>();
            Child.AddComponent<MeshRenderer>();
            Child.AddComponent<SphereCollider>();
            Child.name = pointToStr(Points[i]);
            Child.transform.parent = Parent.transform;
            Child.transform.position = Points[i];
            Child.transform.localScale = Scale;
            Child.GetComponent<MeshRenderer>().material = OutViewMaterial;

            if (ViewVectorLabels)
            {
                GameObject VVLabelT = Instantiate(VVLabelText);
                VVLabelT.transform.parent = Parent.transform;
                VVLabelT.name = pointToStr(Points[i]) + "text";
                VVLabelT.transform.position = Points[i] + VVLabelOffset;
                GameObject VVLabelB = Instantiate(VVLabelBackground);
                VVLabelB.transform.parent = Parent.transform;
                VVLabelB.name = pointToStr(Points[i]) + "background";
                VVLabelB.transform.position = Points[i] + VVLabelOffset + new Vector3(0, 0, 0.01f);
            }
        }

        // draw bounds box
        GameObject BoxParent = new GameObject();
        BoxParent.name = "BoxParent";
        DrawBox(CubeMin, CubeMax, BoxParent, LineSize, LineMaterial);
    }
	
	// Update is called once per frame
	void Update () {
        // update ViewField to camera position
        ViewField.Transform.position = Camera.main.transform.position;
        ViewField.Transform.eulerAngles = Camera.main.transform.eulerAngles;

        // run intersection analysis
        Inter.Intersection(ViewField, Points, out InView, out OutView, out VV, out Pvecs);

        // paint points accordingly
        foreach (Vector3 pt in InView)
        {
            GameObject marker = Parent.transform.Find(pointToStr(pt)).gameObject;
            marker.GetComponent<MeshRenderer>().material = InViewMaterial;
        }
        foreach (Vector3 pt in OutView)
        {
            GameObject marker = Parent.transform.Find(pointToStr(pt)).gameObject;
            marker.GetComponent<MeshRenderer>().material = OutViewMaterial;
        }

        if (ViewVectorLabels)
        {
            // control View Vector Labels
            for (int i = 0; i < Points.Count; i++)
            {
                Vector3 pt = Points[i];
                GameObject VVLabelText = Parent.transform.Find(pointToStr(pt) + "text").gameObject;
                VVLabelText.GetComponent<TextMesh>().text = viewVectorToStr(VV[i]) + 
                    "\nP. vector: " + pointToStr(Pvecs[i]);
            }
        }
    }

    private static void DrawBox(Vector3 min, Vector3 max, GameObject parent, float width, Material material)
    {
        // create points
        Vector3 frontbotleft = min;
        Vector3 frontbotright = new Vector3(max.x, min.y, min.z);
        Vector3 fronttopleft = new Vector3(min.x, max.y, min.z);
        Vector3 fronttopright = new Vector3(max.x, max.y, min.z);
        Vector3 backbotleft = new Vector3(min.x, min.y, max.z);
        Vector3 backbotright = new Vector3(max.x, min.y, max.z);
        Vector3 backtopleft = new Vector3(min.x, max.y, max.z);
        Vector3 backtopright = max;

        // draw front face
        DrawLine(frontbotleft, frontbotright, parent, width, material);
        DrawLine(frontbotright, fronttopright, parent, width, material);
        DrawLine(fronttopright, fronttopleft, parent, width, material);
        DrawLine(fronttopleft, frontbotleft, parent, width, material);
        // draw back face
        DrawLine(backbotleft, backbotright, parent, width, material);
        DrawLine(backbotright, backtopright, parent, width, material);
        DrawLine(backtopright, backtopleft, parent, width, material);
        DrawLine(backtopleft, backbotleft, parent, width, material);
        // draw connectors
        DrawLine(frontbotleft, backbotleft, parent, width, material);
        DrawLine(frontbotright, backbotright, parent, width, material);
        DrawLine(fronttopright, backtopright, parent, width, material);
        DrawLine(fronttopleft, backtopleft, parent, width, material);
    }

    public static void DrawLine(Vector3 start, Vector3 finish, GameObject parent, float width,
        Material material, string name="")
    {
        GameObject Line = new GameObject();
        Line.name = name;
        Line.transform.parent = parent.transform;
        Line.AddComponent(typeof(LineRenderer));
        LineRenderer DrawnLine = Line.GetComponents<LineRenderer>()[0];
        DrawnLine.startWidth = 1f;
        DrawnLine.endWidth = 1f;
        DrawnLine.widthMultiplier = width;
        DrawnLine.material = material;
        Vector3[] LinePoints = new Vector3[] { start, finish };
        DrawnLine.SetPositions(LinePoints);
    }

    /// <summary>
    /// Generates random float from rand between min and max. 
    /// </summary>
    private static float RandomFloat(float min, float max, System.Random rand)
    {
        double range = max - min;
        double sample = rand.NextDouble();
        double scaled = (sample * range) + min;
        return (float)scaled;
    }

    /// <summary>
    /// Generates random point with rand between boundsMin and boundsMax
    /// </summary>
    private static Vector3 RandomPoint(Vector3 boundsMin, Vector3 boundsMax, System.Random rand)
    {
        return new Vector3(RandomFloat(boundsMin.x, boundsMax.x, rand),
            RandomFloat(boundsMin.y, boundsMax.y, rand),
            RandomFloat(boundsMin.z, boundsMax.z, rand));
    }

    // Returns point coordinates in presentable format.
    private static string pointToStr(Vector3 point)
    {
        return String.Format("({0}, {1}, {2})",
            Math.Round(point.x, 1), Math.Round(point.y, 1), Math.Round(point.z, 1));
    }

    private static string viewVectorToStr(ViewVector vector)
    {
        return String.Format("(Theta: {0}, Phi: {1})", Math.Round(vector.Theta, 0), Math.Round(vector.Phi, 0));
    }
}
