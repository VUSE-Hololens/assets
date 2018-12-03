/// DiagnosticControl
/// Controller for Diagnostics Message for EFP Tester v3
/// Mark Scherer, June 2018; updated Nov 2018

using System;
using System.Text;
using System.Diagnostics;
using UnityEngine;
using HoloToolkit.Unity.SpatialMapping;

public class DiagnosticsControl : MonoBehaviour {

    // Inspector variables
    [Tooltip("Diagnostic board text")]
    public GameObject DiagText;
    [Tooltip("EFP container GameObject.")]
    public GameObject EFPContainer;

    // dependencies
    private TextMesh DiagnosticsTextMesh;
    private EFPDriver Driver;
    private SpatialMappingObserver Observer;

    // other variables
    private Stopwatch StopWatch = new Stopwatch();
    private long Seconds = 0;
    private StringBuilder DiagnosticsMessage = new StringBuilder(" ", 1000);

    // Use this for initialization
    void Start () {
        // gather static dependenices
        Driver = EFPContainer.GetComponent<EFPDriver>();
        Observer = EFPContainer.GetComponent<SpatialMappingObserver>();
        DiagnosticsTextMesh = DiagText.GetComponent<TextMesh>();

        StopWatch.Start();
	}
	
	// Update is called once per frame
	void Update () {
            UpdateDiagnostics();
            DiagnosticsTextMesh.text = DiagnosticsMessage.ToString();
	}

    /// <summary>
    /// Updates contents of DiagnosticsMessage.
    /// </summary>
    private void UpdateDiagnostics()
    {
        Seconds = StopWatch.ElapsedTicks / Stopwatch.Frequency;
        DiagnosticsMessage.Remove(0, DiagnosticsMessage.Length);

        // display title
        DiagnosticsMessage.Append("<size=144><b>Diagnostics</b></size>\n" +
            "- Determines visibility of mesh sections\n" +
            "- Calculates intersection with simulated sensor at main camera\n" +
            "- Updates non-occluded vertices in voxel grid\n" +
            "- Renders all vertices in visibile meshes via voxel grid data\n");
        // display EFPDriver metadata
        DiagnosticsMessage.AppendFormat("<b>Driver</b>\n" +
            "Speed (ms / Hz): {0} / {1}\n" +
            "Total Memory Use: {2}\n" +
            "Elasped Time (s): {3}\n" +
            "Sensor Position: {4}, Euler Angles: {5}\n",
            Math.Round(Driver.DriverSpeed * 1000.0, 0), Math.Round(1.0 / Driver.DriverSpeed, 1),
            MemToStr(GC.GetTotalMemory(false)), Seconds,
            pointToStr(Driver.SensorField.Transform.position), pointToStr(Driver.SensorField.Transform.eulerAngles));
        // display SpatialMappingObserver controls
        DiagnosticsMessage.AppendFormat("<b>Spatial Mapping Manager</b>\n" +
            "Mesh Density (triangles/m^3): {0}\n" +
            "Mesh Refresh Time (s): {1}\n" +
            "Cached Meshes: {2}\n",
            Math.Round(Driver.MeshDensity, 0), Math.Round(Observer.TimeBetweenUpdates, 1), 
            Observer.SurfaceObjects.Count);
        // display MeshManager metadata
        DiagnosticsMessage.AppendFormat("<b>Mesh Manager</b>\n" +
            "Speed (ms): {0}\n" +
            "Visible Meshes: {1}\n" +
            "Triangles (in visible meshes / total): {2} / {3}\n" +
            "Vertices (in visible meshes / total): {4} / {5}\n",
            Math.Round(Driver.MeshManSpeed * 1000.0, 0),
            Driver.MeshMan.MeshesInView,
            Driver.MeshMan.TrianglesInView, Driver.MeshMan.TotalTriangleCount,
            Driver.MeshMan.VerticesInView, Driver.MeshMan.TotalVertexCount);
        // display Intersector metadata
        DiagnosticsMessage.AppendFormat("<b>Intersector</b>\n" +
            "Speed (ms): {0}\n" +
            "Occlusion Grid Resolution: {1}cm @ {2}m\n" +
            "Vertices in FOV (total / non-occluded): {3} / {4}\n",
            Math.Round(Driver.IntersectSpeed * 1000.0, 0), 
            Math.Round(Driver.OcclusionObjSize * 100, 1), Driver.OcclusionObjDistance,
            Driver.VertexInter.VerticesInView, Driver.VertexInter.NonOccludedVertices);
        // display VoxelGridManager metadata
        VoxelGridManager<byte>.Metadata voxInfo = Driver.VoxGridMan.About();
        DiagnosticsMessage.AppendFormat("<b>Voxel Grid Manager</b>\n" +
            "Speed (ms): {0}\n" +
            "Minimum Voxel Size (cm) {1}\n" +
            "Grid Components: {2}\n" +
            "Grid Voxels (non-null): {3} ({4})\n" +
            "Grid Volume (non-null) (m^2): {5} ({6})\n" +
            "Grid Memory Use: {7}\n",
            Math.Round(Driver.VoxGridManSpeed * 1000.0, 0), Math.Round(Driver.VoxelGridRes * 100.0, 0),
            voxInfo.components, voxInfo.voxels, voxInfo.nonNullVoxels,
            Math.Round(voxInfo.volume, 1), Math.Round(voxInfo.nonNullVolume, 1), MemToStr(voxInfo.memSize));
        // display VertexVisualizer metadata
        DiagnosticsMessage.AppendFormat("<b>Vertex Visualizer</b>\n" +
            "Speed (ms): {0}\n",
            Math.Round(Driver.VertVisSpeed * 1000.0, 0));
    }

    /// <summary>
    /// Returns presentable string of memory size.
    /// </summary>
    public static string MemToStr(long bytes)
    {
        if (bytes < 1000) // less than 1 kB
            return String.Format("{0} B", bytes);
        if (bytes < 1000 * 1000) // less than 1 MB
            return String.Format("{0} kB", bytes / 1000);
        if (bytes < 1000 * 1000 * 1000) // less than 1 GB
            return String.Format("{0} MB", bytes / (1000 * 1000));
        return String.Format("{0} GB", bytes / (1000 * 1000 * 1000));
    }

    // Returns point coordinates in presentable format.
    static string pointToStr(Vector3 point)
    {
        return String.Format("({0}, {1}, {2})",
            Math.Round(point.x, 1), Math.Round(point.y, 1), Math.Round(point.z, 1));
    }
}
