﻿/// Driver for EFP (External Feed Pathway)
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
    public Color NoDataColor;
    // mesh materials
    [Tooltip("Colored mesh material")]
    public Material Material1;
    [Tooltip("Wireframe material")]
    public Material Material2;
    // proximity settings
    public float Closest = 0.5f; // meters, corresponds to 0 in stored data
    public float Furthest = 5; // meters, corresponds to 255 in stored data
    public byte MinColorVal = 0; // stored value rendered as MinColor
    public byte MaxColorVal = 255; // stored value rendered as MaxColor
    public float ShowFrac = 0.1f; // fraction of voxels to visualize
    public Color BoundsColor1;
    [Tooltip("Second edge of random colors of markers and lines of mesh bounding boxes, if rendered.")]
    public Color BoundsColor2;
    public float BoundsLineWidth = 0.02f;
    public DataFilter VoxGridFilter = DataFilter.mesh;

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
    private List<Visualizer.Content> content = new List<Visualizer.Content>();
    private List<Color> BoundColors = new List<Color>();

    // indicator flags
    private bool VerticesRendered;
    private bool ColoredMeshShown; // alternative is wireframe mesh
    private bool voxVis = false;
    private bool meshBoundsVis = false;

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

    // control of bounds rendering
    private List<Voxel<byte>> Voxels;
    private List<Bounds> MeshBounds;
    private List<float> BoundsRandNums;
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
        // reset if filtered to live data only
        if (VoxGridFilter == DataFilter.live)
            VoxGridMan.Reset();
        VoxGridMan.Set(Updates, UpdateVoxStruct);
        SubStopWatch.Stop();
        VoxGridManSpeed = SubStopWatch.ElapsedTicks / (double)Stopwatch.Frequency;

        // visualize data
        SubStopWatch.Reset();
        SubStopWatch.Start();
        content = new List<Visualizer.Content>();
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
        // color mesh
        if (ColoredMesh )
        {
            // color mesh by voxel grid value if Data Filter is not .all. If so, colored all 'no data'
            if (VoxGridFilter != DataFilter.all)
                Visualizer.ColorMesh(Observer.SurfaceObjects, Observer.ExtraData, MeshGuide,
                    VoxGridMan, MinColor, MaxColor, NoDataColor, MinColorVal, MaxColorVal);
            else
                Visualizer.ColorMesh(Observer.SurfaceObjects, Observer.ExtraData, MeshGuide,
                    VoxGridMan, NoDataColor, NoDataColor, NoDataColor, MinColorVal, MaxColorVal);
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

    private void UpdateVoxels()
    {
        Voxels = VoxGridMan.Voxels();
        BoundsRandNums = new List<float>();
        while (BoundsRandNums.Count < Voxels.Count)
            BoundsRandNums.Add(Random.value);
        while (BoundColors.Count < Voxels.Count)
            BoundColors.Add(Visualizer.RandomColor(BoundsColor1, BoundsColor2));
    }

    private void UpdateMeshBounds()
    {
        MeshBounds = MeshMan.AllMeshBounds();
        while (BoundsRandNums.Count < MeshBounds.Count)
            BoundsRandNums.Add(Random.value);
        while (BoundColors.Count < MeshBounds.Count)
            BoundColors.Add(Visualizer.RandomColor(BoundsColor1, BoundsColor2));
    }
}
