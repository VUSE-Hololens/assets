/// MeshManager
/// Interface for Hololens spatial mapping data via HoloToolKit/SpatialMapping/SpatialMappingObserver.
/// Has inspector variables but NOT Monobehavior - whereever used must be included as additional GameObject Component.
/// Singleton - ALWAYS access via Instance. NEVER use constructor.
/// Mark Scherer, June 2018

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    [Tooltip("Render bounding boxes around individual meshes?")]
    public bool VisualizeBounds = true;
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
    private HoloToolkit.Unity.SpatialMapping.SpatialMappingObserver observer;
    private Intersector MeshInter = new Intersector();
    public Visualizer BoundsVis;
    private List<Color> BoundColors = new List<Color>();

    // indicator flags
    private bool BoundsVisualized;

    /// <summary>
    /// Constructor. ONLY to be used within Singleton, elsewhere ALWAYS use Instance.
    /// Must follow Singleton's enforced new constraint.
    /// </summary>
    public MeshManager()
    {
        TotalMeshCount = 0;
        MeshesInView = 0;
        TrianglesInView = 0;
        VerticesInView = 0;

        BoundsVis = new Visualizer("MeshBounds", "Marker", "Line", DefaultMaterial);
        BoundsVisualized = VisualizeBounds;
    }

    /// <summary>
    /// Updates parameter list of cached vertices, updates class metadata.
    /// Renders mesh bounds if VisualizeBounds.
    /// </summary>
    public List<Vector3> UpdateVertices(Intersector.Frustum SensorView)
    {
        // can only call GetComponent in Start() or Awake() but this is not a MonoBehaiour Script
        // SpatialMappingObserver is not a Singleton, cannot use Instance.
        observer = GetComponent<HoloToolkit.Unity.SpatialMapping.SpatialMappingObserver>();
        Density = observer.TrianglesPerCubicMeter;
        // Adjust Sensor.FOV by specified factor
        SensorView.FOV.Theta = FOVFactor * SensorView.FOV.Theta;
        SensorView.FOV.Phi = FOVFactor * SensorView.FOV.Phi;

        // create fresh lists, metadata
        List<Vector3> vertices = new List<Vector3>();
        List<MeshFilter> MeshFilters = observer.GetMeshFilters();
        List<MeshRenderer> MeshRenderers = observer.GetMeshRenderers();
        List<Intersector.PointValue<byte>> BoundPoints = new List<Intersector.PointValue<byte>>();
        List<Visualizer.Content> toRender = new List<Visualizer.Content>(); 
        TotalMeshCount = 0;
        TotalTriangleCount = 0;
        TotalVertexCount = 0;
        VerticesInView = 0;
        TrianglesInView = 0;
        MeshesInView = 0;

        if (MeshFilters.Count != MeshRenderers.Count)
            Debug.Log(string.Format("SpatialMappingObserver's returned MeshFilters count does not match " +
                "returned MeshRenderers count: {0} vs. {1}",
                MeshFilters.Count, MeshRenderers.Count));

        // add colors if necessary
        while (VisualizeBounds && BoundColors.Count < MeshFilters.Count)
            BoundColors.Add(Visualizer.RandomColor(BoundsColor1, BoundsColor2));

        for (int i = 0; i < MeshFilters.Count; i++)
        {
            MeshRenderer tmpRenderer = MeshRenderers[i];
            Transform tmpTransform = tmpRenderer.transform;

            List<Vector3> ThisBoundPoints = MBounds(tmpRenderer.bounds);
            foreach (Vector3 point in ThisBoundPoints)
                BoundPoints.Add(new Intersector.PointValue<byte>(point, default(byte)));

            // check if mesh is visible
            if (MeshInter.AnyInView(ThisBoundPoints, SensorView))
            {
                Mesh tmpMesh = MeshFilters[i].sharedMesh;
                List<Vector3> tmpVertices = tmpMesh.vertices.ToList();

                // update metadata visibles
                MeshesInView++;
                TrianglesInView += tmpMesh.triangles.ToList().Count();
                VerticesInView += tmpMesh.vertices.ToList().Count();

                for (int j = 0; j < tmpVertices.Count; j++)
                {
                    vertices.Add(tmpTransform.TransformPoint(tmpVertices[j]));
                }

                if (VisualizeBounds)
                {
                    // setup bounding box visualization
                    toRender.AddRange(Visualizer.CreateMarkers(ThisBoundPoints,
                        MarkerSize, BoundColors[i]));
                    toRender.AddRange(Visualizer.CreateBoundingLines(tmpRenderer.bounds,
                        LineSize, BoundColors[i]));
                }
            }

            // update metadata totals
            TotalMeshCount++;
            TotalTriangleCount += MeshFilters[i].sharedMesh.triangles.ToList().Count();
            TotalVertexCount += MeshFilters[i].sharedMesh.vertices.ToList().Count();
        }

        /*
        // test code
        BoundColors.Add(Visualizer.RandomColor(BoundsColor1, BoundsColor2));
        Bounds dummyBounds1 = new Bounds(new Vector3(1, 1, 1), new Vector3(1, 1, 1));
        toRender.AddRange(Visualizer.CreateMarkers(MBounds(dummyBounds1), MarkerSize, BoundColors[0]));
        toRender.AddRange(Visualizer.CreateBoundingLines(dummyBounds1, LineSize, BoundColors[0]));

        BoundColors.Add(Visualizer.RandomColor(BoundsColor1, BoundsColor2));
        Bounds dummyBounds2 = new Bounds(new Vector3(-1, 1, 1), new Vector3(1, 1, 1));
        toRender.AddRange(Visualizer.CreateMarkers(MBounds(dummyBounds2), MarkerSize, BoundColors[1]));
        toRender.AddRange(Visualizer.CreateBoundingLines(dummyBounds2, LineSize, BoundColors[1]));
        */

        if (VisualizeBounds)
        {
            BoundsVis.Visualize(toRender);
            BoundsVisualized = true;
        } else if (BoundsVisualized)
        {
            BoundsVis.Clear();
            BoundsVisualized = false;
        }

        return vertices;
    }

    /// <summary>
    /// Assembles bounding points for mesh defined by bounds.
    /// </summary>
    private static List<Vector3> MBounds(Bounds bounds)
    {
        List<Vector3> mbs = new List<Vector3>();
        mbs.AddRange(Corners(bounds));
        mbs.AddRange(FacePoints(bounds));
        mbs.Add(bounds.center);
        return mbs;
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