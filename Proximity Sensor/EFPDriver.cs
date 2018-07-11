/// Driver for EFP (External Feed Pathway)
/// EFP Tester v2 uses dummy sensor positioned at user's head with simulated data. Does not execute rendering pathway.
/// Mark Scherer, June 2018
 
/// Notes on dependencies (non-MonoBehavior scripts EFPDriver is dependent on):
    /// 1. Some have inspector variables (MeshManager). These are implemented as required Components.
    /// 2. All others are owned as private variables whose source code must be in top-level of assets folder.
    /// 3. Some are Singletons. Could also use static classes, but seems like Unity prefers Singletons.

/// Note: Would be nice to make switching type of sensor data easier using typedef, but not sure how in C#. 
/// Currently byte.

using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using HoloToolkit.Unity.SpatialMapping;

[RequireComponent(typeof(MeshManager))]
public class EFPDriver : MonoBehaviour {

    // Inspector variables: misc
    [Tooltip("Put default material here")]
    public Material DefaultMaterial;
    // Inspector variables: sensor specs
    [Tooltip("FOV of sensor. Degrees.")]
    public Vector2 SensorFOV = new Vector2(30, 17);
    [Tooltip("Pixel sount of sensor.")]
    public Vector2 SensorPixelCount = new Vector2(4096, 3072);
    // Inspector variables: mesh verte occlusion controls
    [Tooltip("Minimum actual object size to be considered by occlusion alogrithm. Meters.")]
    public float OcclusionObjSize = 0.1f;
    [Tooltip("Distance at which to evaluate object size for occlusion algorithm. Meters.")]
    public float OcclusionObjDistance = 5f;
    // Inspector variables: voxel grid controls
    [Tooltip("Update structure of voxel grid when setting points?")]
    public bool UpdateVoxStruct = true;
    [Tooltip("Starting minimum size of voxel. Meters.")]
    public float DefaultVoxGridRes = 0.1f;
    [Tooltip("Size of new voxels when growing grid. Meters.")]
    public float DefaultVoxelSize = 1f;
    // Inspector variables: mesh vertex rendering controls
    [Tooltip("Default to rendering non-occluded mesh vertices?")]
    public bool DefaultRenderVerts = true;
    [Tooltip("Size of markers of mesh vertices, if rendered. Meters.")]
    public float VertexMarkerSize = 0.05f;
    [Tooltip("Color of markers for vertices with min value, if rendered.")]
    public Color MinColor;
    [Tooltip("Color of markers for vertices with max value, if rendered.")]
    public Color MaxColor;
    // mesh materials
    [Tooltip("Colored mesh material")]
    public Material Material1;
    [Tooltip("Wireframe material")]
    public Material Material2;
    // proximity settings
    public float Closest = 0.5f; // meters
    public float Furthest = 5; // meters

    // metadata
    public double DriverSpeed { get; private set; } // Update(), seconds
    public double MeshManSpeed { get; private set; } // MeshManager:UpdateVertices(), seconds
    public double IntersectSpeed { get; private set; } // Intersector:Intersection(), seconds
    public double VoxGridManSpeed { get; private set; } // VoxelGridManager:Set(), seconds
    public double VertVisSpeed { get; private set; } // VoxelGridManager:Set(), seconds
    public float MeshDensity { get; private set; } // triangles/m^3

    // dependencies
    public MeshManager MeshMan { get; private set; }
    public VoxelGridManager<byte> VoxGridMan { get; private set; }
    public Visualizer VertVis { get; private set; }
    public Intersector VertexInter { get; private set; }
    public SpatialMappingObserver Observer { get; private set; }

    // pre-declarations
    public Intersector.Frustum SensorField = new Intersector.Frustum();
    private Stopwatch StopWatch = new Stopwatch();
    private Stopwatch SubStopWatch = new Stopwatch();
    public Intersector.Occlusion Oc;
    private byte[,] SensorData;
    private List<Visualizer.Content> VertMarkers = new List<Visualizer.Content>();

    // indicator flags
    private bool VerticesRendered;
    private bool ColoredMeshShown; // alternative is wireframe mesh

    // control over vertice rendering
    public bool RenderVerts
    {
        get { return VerticesRendered; }
        set
        {
            if (value != VerticesRendered)
                if (value)
                    VerticesRendered = true; // vertices created on next Update cycle
                else
                {
                    VertVis.Clear();
                    VerticesRendered = false;
                }
        }
    }

    // control material of rendered surface mesh
    public bool ColoredMesh
    {
        get { return ColoredMeshShown; }
        set
        {
            if (value != ColoredMeshShown)
            {
                if (value)
                    GetComponent<SpatialMappingManager>().SurfaceMaterial = Material1;
                else
                    GetComponent<SpatialMappingManager>().SurfaceMaterial = Material2;
                ColoredMeshShown = value;
            }
        }
    }

    // control voxel grid resolution
    public float VoxelGridRes
    {
        get { return VoxGridMan.Resolution; }
        set { VoxGridMan.Resolution = value; }
    }

    // Use this for initialization
    void Awake () {
        // gather dependencies
        MeshMan = MeshManager.Instance;
        VoxGridMan = new VoxelGridManager<byte>(myMinSize: DefaultVoxGridRes, myDefaultSize:DefaultVoxelSize);
        VertVis = new Visualizer("VertexMarkers", "Marker", "Line", DefaultMaterial);
        VertexInter = new Intersector();
        Observer = GetComponent<SpatialMappingObserver>();

        // finish setup
        SensorField.Transform = Camera.main.transform;
        SensorField.FOV = new Intersector.ViewVector((int)SensorFOV.x, (int)SensorFOV.y);
        MeshDensity = MeshMan.Density;
        SensorData = new byte[(int)SensorPixelCount.x, (int)SensorPixelCount.y];
        for (int i = 0; i < SensorPixelCount.x; i++)
        {
            for (int j = 0; j < SensorPixelCount.y; j++)
                SensorData[i, j] = (byte)(255f * (float)(i + j) / (SensorPixelCount.x + SensorPixelCount.y));

        }

        // finish dependendencies setup: some variables must be created in Start()
        MeshMan.BoundsVis = new Visualizer("MeshBounds", "Marker", "Line", DefaultMaterial);
        VerticesRendered = DefaultRenderVerts;
    }

	// Update is called once per frame
	void Update () {
        StopWatch.Reset();
        StopWatch.Start();

        // update sensor position
        UpdateSensor(ref SensorField, ref SensorData);

        // update visible mesh vertices
        SubStopWatch.Reset();
        SubStopWatch.Start();
        List<bool> MeshGuide = MeshMan.UpdateVertices(SensorField);
        MeshDensity = MeshMan.Density;
        SubStopWatch.Stop();
        MeshManSpeed = SubStopWatch.ElapsedTicks / (double)Stopwatch.Frequency;

        // project to non-occluded vertices
        SubStopWatch.Reset();
        SubStopWatch.Start();
        Oc = new Intersector.Occlusion(OcclusionObjSize, OcclusionObjDistance, SensorField.FOV);
        List<Intersector.PointValue<byte>> Updates = 
            VertexInter.ProjectToVisible(SensorData, SensorField, Oc, Observer.ExtraData, MeshGuide,
            Closest, Furthest, SensorField.Transform.position);
        SubStopWatch.Stop();
        IntersectSpeed = SubStopWatch.ElapsedTicks / (double)Stopwatch.Frequency;

        // update voxel grid
        SubStopWatch.Reset();
        SubStopWatch.Start();
        VoxGridMan.Set(Updates, UpdateVoxStruct);
        SubStopWatch.Stop();
        VoxGridManSpeed = SubStopWatch.ElapsedTicks / (double)Stopwatch.Frequency;

        // visualize data
        SubStopWatch.Reset();
        SubStopWatch.Start();
        // render spheres
        if (RenderVerts)
        {
            VertMarkers = Visualizer.CreateMarkers(Updates, 
                VertexMarkerSize, 0, 255, MinColor, MaxColor);
            VertVis.Visualize(VertMarkers);
        }
        // color mesh
        if (ColoredMesh)
        {
            Visualizer.ColorMesh(Observer.SurfaceObjects, Observer.ExtraData, MeshGuide, 
                VoxGridMan, MinColor, MaxColor, 0, 255);
        }
        SubStopWatch.Stop();
        VertVisSpeed = SubStopWatch.ElapsedTicks / (double)Stopwatch.Frequency;

        StopWatch.Stop();
        DriverSpeed = StopWatch.ElapsedTicks / (double)Stopwatch.Frequency;
	}

    private void UpdateSensor(ref Intersector.Frustum sensorField, ref byte[,] sensorData)
    {
        // update position
        sensorField.Transform.position = Camera.main.transform.position;
        sensorField.Transform.eulerAngles = Camera.main.transform.eulerAngles;

        // update data
            // nothing yet
    }
}
