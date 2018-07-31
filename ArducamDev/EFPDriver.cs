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

// DataFilter: controls what sections of VoxelGrid are rendered
    // mesh: all mesh vertices are rendered with values of their voxel cell.
    // all: all vertices in voxel grid are rendered.
    // live: only vertices currently in sensor FOV are rendered with voxel grid data, else'no data'
public enum DataFilter { mesh, all, live};

[RequireComponent(typeof(SpatialMappingManager))]
[RequireComponent(typeof(SpatialMappingObserver))]
public class EFPDriver : MonoBehaviour {

    // Inspector variables: Dependencies
    [Header("Dependencies")]
    [Tooltip("put gameobject containing data update script here.")]
    public GameObject DataUpdateContainer;
    [Tooltip("Put default material here")]
    public Material DefaultMaterial;
    [Tooltip("Material for mesh colored by data values.")]
    public Material ColoredMeshMaterial;
    [Tooltip("Material for wireframe mesh.")]
    public Material WireframeMaterial;

    // Inspector variables: sensor specs
    [Header("Sensor Specs")]
    [Tooltip("Actual FOV of sensor. Degrees.")]
    public Vector2 SensorFOV = new Vector2(30, 17);
    [Tooltip("Reduction factor for used FOV of sensor from actual FOV.")]
    public Vector2 SensorFOVReduction; // reduce FOV of sensor by this factor
    [Tooltip("Offset of sensor's position in Hololens local coordinates. Meters.")]
    public Vector3 Offset;
    
    // Inspector variables: mesh vertice occlusion controls
    [Header("Occlusion and Voxel Grid")]
    [Tooltip("Minimum actual object size to be considered by occlusion alogrithm. Meters.")]
    public float OcclusionObjSize = 0.1f;
    [Tooltip("Distance at which to evaluate object size for occlusion algorithm. Meters.")]
    public float OcclusionObjDistance = 5f;

    // Inspector variables: voxel grid controls
    [Tooltip("Size of new voxels when growing grid. Meters.")]
    public float DefaultVoxelSize = 1f;

    // Inspector variables: data rendering controls
    [Header("Rendering")]
    [Tooltip("Color of matching min data value.")]
    public Color MinColor;
    [Tooltip("Color of matching max data value.")]
    public Color MaxColor;
    [Tooltip("Color for points without attached data.")]
    public Color NoDataColor;
    [Space(10)]
    [Tooltip("Default to rendering non-occluded mesh vertices?")]
    public bool DefaultRenderVerts = true;
    [Tooltip("Size of markers of mesh vertices, if rendered. Meters.")]
    public float VertexMarkerSize = 0.05f;
    [Space(10)]

    // Inspector vars: bounds rendering
    [Tooltip("First edge of random colors of markers and lines of mesh bounding boxes, if rendered.")]
    public Color BoundsColor1;
    [Tooltip("Second edge of random colors of markers and lines of mesh bounding boxes, if rendered.")]
    public Color BoundsColor2;
    public float BoundsLineWidth = 0.02f;

    // Inspector variables: proximity settings
    // Note: Only applies if code changed to proximity sensor configuration.
    [Header("Proximity Sensor Settings")]
    [Tooltip("Default to proximity sensor configuration?")]
    public bool DefaultToProximity = false;
    [Tooltip("Min distance, meters. ONLY applies if code changed to proximity sensor configuration.")]
    public float Closest = 0.5f;
    [Tooltip("Max distance, meters. ONLY applies if code changed to proximity sensor configuration.")]
    public float Furthest = 5;
    
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
    public Receiving.ImageReceiver SensorDataUpdater { get; private set; }

    // other req'd variables
    private float DefaultVoxGridRes = 0.1f;
    private List<Voxel<byte>> Voxels; // script's cache of voxels ONLY for bounds visualization
    private List<Bounds> MeshBounds; // script's cache of mesh bounds
    private List<float> BoundsRandNums; // script's cache of colors for bounds visualization

    // pre-declarations of class variables
    public Intersector.Frustum SensorField = new Intersector.Frustum();
    private Stopwatch StopWatch = new Stopwatch();
    private Stopwatch SubStopWatch = new Stopwatch();
    public Intersector.Occlusion Oc;
    private byte[] SensorData;
    private int SensorDataHeight;
    private int SensorDataWidth;
    private List<Visualizer.Content> content = new List<Visualizer.Content>();
    private List<Color> BoundColors = new List<Color>();

    // control over vertice rendering
    private bool VerticesRendered;
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
    private bool ColoredMeshShown; // alternative is wireframe mesh
    public bool ColoredMesh
    {
        get { return ColoredMeshShown; }
        set
        {
            if (value != ColoredMeshShown)
            {
                if (value)
                    GetComponent<SpatialMappingManager>().SurfaceMaterial = ColoredMeshMaterial;
                else
                    GetComponent<SpatialMappingManager>().SurfaceMaterial = WireframeMaterial;
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

    // control of bounds rendering
    private bool voxVis = false;
    private bool meshBoundsVis = false;
    public bool VoxVis
    {
        get { return voxVis; }
        set
        {
            if (value)
                UpdateVoxels();
            voxVis = value;
        }
    }
    public bool MeshBoundsVis
    {
        get { return meshBoundsVis; }
        set
        {
            if (value)
                UpdateMeshBounds();
            meshBoundsVis = value;
        }
    }

    // control of coloring bounds
    private byte minColorVal = 0;
    public byte MinColorVal
    {
        get { return minColorVal; }
        set { minColorVal = value; }
    }
    private byte maxColorVal = 0;
    public byte MaxColorVal
    {
        get { return maxColorVal; }
        set { maxColorVal = value; }
    }

    // control of bounds show fraction
    private float showFrac = 0.5f;
    public float ShowFrac
    {
        get { return showFrac; }
        set { showFrac = value; }
    }

    // control of data filter
    private DataFilter voxGridFilter = DataFilter.mesh;
    public DataFilter VoxGridFilter
    {
        get { return voxGridFilter; }
        set { voxGridFilter = value; }
    }

    // control over sensor configuration
    private bool proxConfig;
    public bool ProximityConfig
    {
        get { return proxConfig; }
        set { proxConfig = value; }
    }

    // control mesh transparency
    private byte meshAlpha = 255;
    public byte MeshAlpha
    {
        get { return meshAlpha; }
        set { meshAlpha = value; }
    }


    // Use this for initialization
    void Awake () {
        // gather dependencies
        MeshMan = MeshManager.Instance;
        VoxGridMan = new VoxelGridManager<byte>(myMinSize: DefaultVoxGridRes, myDefaultSize:DefaultVoxelSize);
        VertVis = new Visualizer("VertexMarkers", "Marker", "Line", DefaultMaterial);
        VertexInter = new Intersector();
        Observer = GetComponent<SpatialMappingObserver>();
        SensorDataUpdater = DataUpdateContainer.GetComponent<Receiving.ImageReceiver>();

        // finish setup
        SensorField.Transform = Camera.main.transform;
        SensorField.FOV = new Intersector.ViewVector((int)(SensorFOV.x * SensorFOVReduction.x), 
            (int)(SensorFOV.y * SensorFOVReduction.y));
        MeshDensity = MeshMan.Density;
        SensorData = new byte[SensorDataHeight* SensorDataWidth];

        // sync state to default values
        VerticesRendered = DefaultRenderVerts;
        ProximityConfig = DefaultToProximity;
    }

	// Update is called once per frame
	void Update () {
        StopWatch.Reset();
        StopWatch.Start();

        // update sensor position
        UpdateSensor();

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
            VertexInter.ProjectToVisible(SensorData, SensorField, SensorDataHeight, SensorDataWidth, 
            Oc, Observer.ExtraData, MeshGuide,
            Closest, Furthest, SensorField.Transform.position, SensorFOVReduction, ProximityConfig);
        SubStopWatch.Stop();
        IntersectSpeed = SubStopWatch.ElapsedTicks / (double)Stopwatch.Frequency;

        // update voxel grid
        SubStopWatch.Reset();
        SubStopWatch.Start();
        if (VoxGridFilter == DataFilter.live)
            VoxGridMan.Reset(); // reset if filtered to live data only
        VoxGridMan.Set(Updates, true); // true: update voxel structure
        SubStopWatch.Stop();
        VoxGridManSpeed = SubStopWatch.ElapsedTicks / (double)Stopwatch.Frequency;

        // visualize data
        SubStopWatch.Reset();
        SubStopWatch.Start();
        HandleVis(Updates, MeshGuide);
        SubStopWatch.Stop();
        VertVisSpeed = SubStopWatch.ElapsedTicks / (double)Stopwatch.Frequency;

        StopWatch.Stop();
        DriverSpeed = StopWatch.ElapsedTicks / (double)Stopwatch.Frequency;
	}

    private void UpdateSensor()
    {
        // update position
        SensorField.Transform.position = Camera.main.transform.position + Offset;
        SensorField.Transform.eulerAngles = Camera.main.transform.eulerAngles;

        // update data
        // Normal case: pulls data from SensorDataUpdater
        if (SensorDataUpdater.CheckNewImage())
        {
            SensorDataHeight = SensorDataUpdater.Get_ImageHeight();
            SensorDataWidth = SensorDataUpdater.Get_ImageWidth();
            SensorData = SensorDataUpdater.Get_ImageData1D();
         }
    }

    // handles all script's visulation for better organization
    private void HandleVis(List<Intersector.PointValue<byte>> Updates, List<bool> MeshGuide)
    {
        content = new List<Visualizer.Content>();

        // color mesh
        if (ColoredMesh)
        {
            // color mesh by voxel grid value if Data Filter is not .all. If so, colored all 'no data'
            if (VoxGridFilter != DataFilter.all)
                Visualizer.ColorMesh(Observer.SurfaceObjects, Observer.ExtraData, MeshGuide,
                    VoxGridMan, MinColor, MaxColor, NoDataColor, MinColorVal, MaxColorVal, MeshAlpha);
            else
                Visualizer.ColorMesh(Observer.SurfaceObjects, Observer.ExtraData, MeshGuide,
                    VoxGridMan, NoDataColor, NoDataColor, NoDataColor, MinColorVal, MaxColorVal, MeshAlpha);
        }

        // render spheres
        if (RenderVerts)
        {
            if (VoxGridFilter == DataFilter.mesh || VoxGridFilter == DataFilter.live)
                // render all non-occluded mesh vertices
                content.AddRange(Visualizer.CreateMarkers(Updates,
                    VertexMarkerSize, MinColorVal, MaxColorVal, MinColor, MaxColor));
            else
            {
                // render all visible voxel vertices
                List<Voxel<byte>> curVoxels = VoxGridMan.Voxels();
                for (int i = 0; i < curVoxels.Count; i++)
                {
                    List<Intersector.PointValue<byte>> tmp = new List<Intersector.PointValue<byte>>();
                    tmp.Add(new Intersector.PointValue<byte>(curVoxels[i].point, curVoxels[i].value));
                    content.AddRange(Visualizer.CreateMarkers(tmp,
                        VertexMarkerSize, MinColorVal, MaxColorVal, MinColor, MaxColor));
                }
            }
        }

        // visualize voxels
        if (VoxVis)
        {
            for (int i = 0; i < Voxels.Count; i++)
            {
                if (BoundsRandNums[i] <= ShowFrac)
                {
                    Bounds voxBound = new Bounds();
                    voxBound.min = Voxels[i].min;
                    voxBound.max = Voxels[i].max;
                    if (VertexInter.AnyInView(MeshManager.IntersectionPoints(voxBound), SensorField))
                        content.AddRange(Visualizer.CreateBoundingLines(voxBound, BoundsLineWidth, BoundColors[i]));
                }
            }
        }

        // visualize mesh bounds
        if (MeshBoundsVis)
        {
            for (int i = 0; i < MeshBounds.Count; i++)
            {
                if (BoundsRandNums[i] <= ShowFrac
                    && VertexInter.AnyInView(MeshManager.IntersectionPoints(MeshBounds[i]), SensorField))
                    content.AddRange(Visualizer.CreateBoundingLines(MeshBounds[i], BoundsLineWidth, BoundColors[i]));
            }
        }

        VertVis.Visualize(content);
    }

    // updates script's stored list of voxels for visualizing voxel bound
    private void UpdateVoxels()
    {
        Voxels = VoxGridMan.Voxels();
        BoundsRandNums = new List<float>();
        while (BoundsRandNums.Count < Voxels.Count)
            BoundsRandNums.Add(Random.value);
        while (BoundColors.Count < Voxels.Count)
            BoundColors.Add(Visualizer.RandomColor(BoundsColor1, BoundsColor2));
    }

    // updates script's stored list of mesh bounds for visualizing mesh bound
    private void UpdateMeshBounds()
    {
        MeshBounds = MeshMan.AllMeshBounds();
        while (BoundsRandNums.Count < MeshBounds.Count)
            BoundsRandNums.Add(Random.value);
        while (BoundColors.Count < MeshBounds.Count)
            BoundColors.Add(Visualizer.RandomColor(BoundsColor1, BoundsColor2));
    }
}
