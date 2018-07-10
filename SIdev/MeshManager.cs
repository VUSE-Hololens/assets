/// MeshManager
/// Interface for Hololens spatial mapping data via HoloToolKit/SpatialMapping/SpatialMappingObserver.
/// Has inspector variables but NOT Monobehavior - whereever used must be included as additional GameObject Component.
/// Singleton - ALWAYS access via Instance. NEVER use constructor.
/// Mark Scherer, June 2018

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HoloToolkit.Unity.SpatialMapping;

/// <summary>
/// Accessor class to Hololens spatial mapping data via HoloToolKit/SpatialMapping/SpatialMappingObserver.
/// </summary>
[RequireComponent(typeof(HoloToolkit.Unity.SpatialMapping.SpatialMappingObserver))]
public class MeshManager : HoloToolkit.Unity.Singleton<MeshManager>
{
    // Inspector Var: misc
    [Tooltip("Put default material here.")]
    public Material DefaultMaterial;
    // Inspector Vars
    [Tooltip("Factor to multiply EFPDriver SensorFOV by for determining if a mesh is in view.")]
    public float FOVFactor = 2f;
    [Tooltip("Default to rendering bounding boxes around individual meshes?")]
    public bool DefaultVisualizeBounds = false;
    [Tooltip("Size of markers for intersector points of mesh bounding boxes, if rendered. Meters.")]
    public float MarkerSize = 0.05f;
    [Tooltip("Width of lines of mesh bounding boxes, if rendered. Meters.")]
    public float LineSize = 0.02f;
    [Tooltip("First edge of random colors of markers and lines of mesh bounding boxes, if rendered.")]
    public Color BoundsColor1;
    [Tooltip("Second edge of random colors of markers and lines of mesh bounding boxes, if rendered.")]
    public Color BoundsColor2;

    /// <summary>
    /// Metadata: number of independent meshes returned by SpatialMappingManager.
    /// NOTE: only updated when getVertices() is called.
    /// </summary>
    public int TotalMeshCount { get; private set; }

    /// <summary>
    /// Metadata: number of independent visible meshes.
    /// NOTE: only updated when getVertices() is called.
    /// </summary>
    public int MeshesInView { get; private set; }

    /// <summary>
    /// Metadata: number of triangles in all meshes.
    /// NOTE: only updated when getVertices() is called.
    /// </summary>
    public int TotalTriangleCount { get; private set; }

    /// <summary>
    /// Metadata: number of triangles in all visible meshes.
    /// NOTE: only updated when getVertices() is called.
    /// </summary>
    public int TrianglesInView { get; private set; }

    /// <summary>
    /// Metadata: number of vertices in all meshes.
    /// NOTE: only updated when getVertices() is called.
    /// </summary>
    public int TotalVertexCount { get; private set; }

    /// <summary>
    /// Metadata: number of vertices in all visible meshes.
    /// NOTE: only updated when getVertices() is called.
    /// </summary>
    public int VerticesInView { get; private set; }

    /// <summary>
    /// Density of mesh data in triangles / m^3
    /// </summary>
    public float Density { get; private set; }

    // other variables
    private SpatialMappingObserver observer;
    private Intersector MeshInter;
    public Visualizer BoundsVis;
    private List<Color> BoundColors = new List<Color>();
    public Intersector.ViewVector FOV { get; private set; }

    // indicator flags
    private bool BoundsVisualized;

    void Start()
    {
        observer = GetComponent<SpatialMappingObserver>();
        Density = observer.Density;
        MeshInter = new Intersector();
        BoundsVis = new Visualizer("MeshBounds", "Marker", "Line", DefaultMaterial);
        BoundsVisualized = DefaultVisualizeBounds;

        TotalMeshCount = 0;
        MeshesInView = 0;
        TrianglesInView = 0;
        VerticesInView = 0;
    }

    // control over mesh bounds visualization
    public bool VB
    {
        get { return BoundsVisualized; }
        set
        {
            if (value != BoundsVisualized)
            {
                if (value)
                    BoundsVisualized = true; // create bounds on next Update cycle
                else
                {
                    BoundsVis.Clear();
                    BoundsVisualized = false;
                }
            }
        }
    }

    /// <summary>
    /// ID's all of SpatialMappingObserver's store meshes as visible or not, updates class metadata.
    /// Renders mesh bounds if VisualizeBounds.
    /// </summary>
    public List<bool> UpdateVertices(Intersector.Frustum SensorView)
    {
        Density = observer.Density;

        // Adjust Sensor.FOV by specified factor...
        FOV = new Intersector.ViewVector(FOVFactor * SensorView.FOV.Phi, FOVFactor * SensorView.FOV.Phi);
        SensorView.FOV = FOV;

        // create fresh lists, metadata
        List<bool> visiblilty = new List<bool>();
        List<Visualizer.Content> toRender = new List<Visualizer.Content>();
        TotalMeshCount = 0;
        TotalTriangleCount = 0;
        TotalVertexCount = 0;
        MeshesInView = 0;
        TrianglesInView = 0;
        VerticesInView = 0;

        // add colors if necessary
        while (VB && BoundColors.Count < observer.SurfaceObjects.Count)
            BoundColors.Add(Visualizer.RandomColor(BoundsColor1, BoundsColor2));

        // check meshes for visiblity
        for (int i = 0; i < observer.ExtraData.Count; i++)
        {
            SurfacePoints extras = observer.ExtraData[i];
            bool isVisible = MeshInter.AnyInView(extras.IntersectPts, SensorView);

            visiblilty.Add(isVisible);
            
            if (isVisible)
            {
                // update metadata
                MeshesInView++;
                TrianglesInView += extras.TriangleCount;
                VerticesInView += extras.Wvertices.Count;

                // setup bounding box visualization
                if (VB)
                {
                    toRender.AddRange(Visualizer.CreateMarkers(extras.IntersectPts, MarkerSize, BoundColors[i]));
                    toRender.AddRange(Visualizer.CreateBoundingLines(extras.BoundsBox, LineSize, BoundColors[i]));
                }
            }
            // update metadata totals
            TotalMeshCount++;
            TotalTriangleCount += extras.TriangleCount;
            TotalVertexCount += extras.Wvertices.Count;
        }

        if (VB)
            BoundsVis.Visualize(toRender);

        return visiblilty;
    }
    
    /// <summary>
    /// Assembles bounding points for mesh defined by bounds.
    /// </summary>
    public static List<Vector3> IntersectionPoints(Bounds bounds)
    {
        List<Vector3> pts = new List<Vector3>();
        pts.AddRange(Corners(bounds));
        return pts;
    }

    /// <summary>
    /// Returns list of corner vertices from Axis Aligned Bounding Box
    /// </summary>
    private static List<Vector3> Corners(Bounds bounds)
    {
        Vector3 Wmin = bounds.min;
        Vector3 Wmax = bounds.max;

        List<Vector3> corners = new List<Vector3>();
        corners.Add(Wmin); // front bottom left
        corners.Add(new Vector3(Wmax.x, Wmin.y, Wmin.z)); // front bottom right
        corners.Add(new Vector3(Wmax.x, Wmax.y, Wmin.z)); // front top right
        corners.Add(new Vector3(Wmin.x, Wmax.y, Wmin.z)); // front top left
        corners.Add(new Vector3(Wmin.x, Wmin.y, Wmax.z)); // back bottom left
        corners.Add(new Vector3(Wmax.x, Wmin.y, Wmax.z)); // back bottom right
        corners.Add(Wmax); // back top right
        corners.Add(new Vector3(Wmin.x, Wmax.y, Wmax.z)); // back top left
        return corners;
    }

    /// <summary>
    /// Returns list of face vertices from Axis Aligned Bounding Box
    /// </summary>
    private static List<Vector3> FacePoints(Bounds bounds)
    {
        Vector3 Wcenter = bounds.center;
        List<Vector3> points = new List<Vector3>();
        Vector3 adj = new Vector3(0, 0, -bounds.extents.z); // front
        points.Add(Wcenter + adj);
        adj = new Vector3(0, 0, bounds.extents.z); // back
        points.Add(Wcenter + adj);
        adj = new Vector3(0, bounds.extents.y, 0); // top
        points.Add(Wcenter + adj);
        adj = new Vector3(0, -bounds.extents.y, 0); // bottom
        points.Add(Wcenter + adj);
        adj = new Vector3(-bounds.extents.x, 0, 0); // left
        points.Add(Wcenter + adj);
        adj = new Vector3(bounds.extents.x, 0, 0); // right
        points.Add(Wcenter + adj);
        return points;
    }
}