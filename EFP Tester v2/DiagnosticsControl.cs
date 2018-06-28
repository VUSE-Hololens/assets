/// DiagnosticControl
/// Controller for Diagnostics Message for EFP Tester v2
/// Mark Scherer, June 2018

using System;
using System.Text;
using System.Diagnostics;
using UnityEngine;

public class DiagnosticsControl : MonoBehaviour {

    // Inspector variables
    [Tooltip("Prefab of diagnostic board text")]
    public GameObject DiagTextPrefab;
    [Tooltip("Prefab of diagnostics board background")]
    public GameObject DiagBackgroundPrefab;
    [Tooltip("EFP container GameObject.")]
    public GameObject EFPContainer;
    [Tooltip("Default to showing diagnostics board?")]
    public bool DefaultToShow = true;

    // dependencies
    private GameObject DiagText;
    private GameObject DiagBackground;
    private TextMesh DiagnosticsTextMesh;
    private EFPDriver Driver;

    // other variables
    private Stopwatch StopWatch = new Stopwatch();
    private long Seconds = 0;
    private StringBuilder DiagnosticsMessage = new StringBuilder(" ", 1000);

    // board visibility flag
    private bool BoardExists;

    // control over board visibility
    public bool ShowBoard
    {
        get
        {
            return BoardExists;
        }
        set
        {
            if (value != BoardExists)
            {
                if (value)
                {
                    CreateDiagnostics();
                    BoardExists = true;
                } else
                {
                    DeleteContent();
                    BoardExists = false;
                }

            }
        }
    }

    // Use this for initialization
    void Start () {
        // gather static dependenices
        Driver = EFPContainer.GetComponent<EFPDriver>();

        BoardExists = DefaultToShow;
        if (ShowBoard)
            CreateDiagnostics();

        StopWatch.Start();
	}
	
	// Update is called once per frame
	void Update () {
        if (ShowBoard)
        {
            UpdateDiagnostics();
            DiagnosticsTextMesh.text = DiagnosticsMessage.ToString();
        }
	}

    /// <summary>
    /// Creates contents of diagnostics board as new objects.
    /// </summary>
    private void CreateDiagnostics()
    {
        // double check board does not exist

        // create text
        DiagText = Instantiate(DiagTextPrefab, gameObject.transform, false);
        DiagnosticsTextMesh = DiagText.GetComponent<TextMesh>();

        // create background
        DiagBackground = Instantiate(DiagBackgroundPrefab, gameObject.transform, false);
    }

    /// <summary>
    /// Deletes objects containing contents of diagnostics board.
    /// </summary>
    private void DeleteContent()
    {
        foreach (Transform child in gameObject.transform)
            Destroy(child.gameObject);
    }

    /// <summary>
    /// Updates contents of DiagnosticsMessage.
    /// </summary>
    private void UpdateDiagnostics()
    {
        Seconds = StopWatch.ElapsedTicks / Stopwatch.Frequency;
        DiagnosticsMessage.Remove(0, DiagnosticsMessage.Length);

        // display title
        DiagnosticsMessage.Append("<size=144><b>External Feed Pathway Diagnostics</b></size>\n" +
            "- Accesses entire cached spatial data\n" +
            "- Calculates intersection with simulated sensor at main camera\n" +
            "- Updates non-occluded vertices in voxel grid\n");
        // display EFPDriver metadata
        DiagnosticsMessage.AppendFormat("<b>Driver</b>\n" +
            "Speed (ms / Hz): {0} / {1}\n" +
            "Total Memory Use: {2}\n" +
            "Elasped Time (s): {3}\n" +
            "Sensor Position: {4}, Euler Angles: {5}\n",
            Math.Round(Driver.DriverSpeed * 1000.0, 0), Math.Round(1.0 / Driver.DriverSpeed, 1),
            MemToStr(GC.GetTotalMemory(false)), Seconds,
            pointToStr(Driver.SensorField.Transform.position), pointToStr(Driver.SensorField.Transform.eulerAngles));
        // display MeshManager metadata
        DiagnosticsMessage.AppendFormat("<b>Mesh Manager</b>\n" +
            "Speed (ms): {0}\n" +
            "Mesh Density (triangles/m^3): {1}\n" +
            "Mesh Visiblity FOV Factor: {2}\n" +
            "Visible Meshes (total cached): {3} ({4})\n" +
            "Triangles (visible meshes / total): {5} / {6}\n" +
            "Vertices (visible meshes / total): {7} / {8}\n",
            Math.Round(Driver.MeshManSpeed * 1000.0, 0), Driver.MeshDensity, Driver.MeshMan.FOVFactor,
            Driver.MeshMan.MeshesInView, Driver.MeshMan.TotalMeshCount,
            Driver.MeshMan.TrianglesInView, Driver.MeshMan.TotalTriangleCount,
            Driver.MeshMan.VerticesInView, Driver.MeshMan.TotalVertexCount);
        // display Intersector metadata
        DiagnosticsMessage.AppendFormat("<b>Mesh Intersector</b>\n" +
            "Speed (ms): {0}\n" +
            "Occlusion Grid Resolution: {1}cm @ {2}m\n" +
            "Total Vertices (in FOV): {3} ({4})\n" +
            "Non-Occluded Vertices {5}\n",
            Math.Round(Driver.IntersectSpeed * 1000.0, 0), Driver.OcclusionObjSize * 100, Driver.OcclusionObjDistance,
            Driver.VertexInter.CheckedVertices, Driver.VertexInter.VerticesInView, Driver.VertexInter.NonOccludedVertices);
        // display VoxelGridManager metadata
        VoxelGridManager<byte>.Metadata voxInfo = Driver.VoxGridMan.About();
        DiagnosticsMessage.AppendFormat("<b>Voxel Grid Manager</b>\n" +
            "Speed (ms): {0}\n" +
            "Minimum Voxel Size (cm) {1}\n" +
            "Grid Components: {2}\n" +
            "Grid Voxels (non-null): {3} ({4})\n" +
            "Grid Volume (non-null) (m^2): {5} ({6})\n" +
            "Grid Memory Use: {7}\n",
            Math.Round(Driver.VoxGridManSpeed * 1000.0, 0), Math.Round(Driver.VoxGridMan.minSize * 100.0, 0),
            voxInfo.components, voxInfo.voxels, voxInfo.nonNullVoxels,
            Math.Round(voxInfo.volume, 1), Math.Round(voxInfo.nonNullVolume, 1), MemToStr(voxInfo.memSize));
        // display VertexVisualizer metadata
        DiagnosticsMessage.AppendFormat("<b>Vertex Visualizer</b>\n" +
            "Speed (ms): {0}\n" +
            "Rendered Mesh Vertices (total markers): {1} ({2})\n" +
            "Rendered Mesh-Bounds Vertices (total markers): {3} ({4})\n" +
            "Rendered Mesh-Bounds Lines (total lines): {5} ({6})\n",
            Math.Round(Driver.VertVisSpeed * 1000.0, 0),
            Driver.VertVis.MarkersInUse, Driver.VertVis.TotalMarkers,
            Driver.MeshMan.BoundsVis.MarkersInUse, Driver.MeshMan.BoundsVis.TotalMarkers,
            Driver.MeshMan.BoundsVis.LinesInUse, Driver.MeshMan.BoundsVis.TotalLines);
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
