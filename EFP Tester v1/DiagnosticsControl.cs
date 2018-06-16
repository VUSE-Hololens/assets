/// Controller for Diagnostics Message
/// Mark Scherer, June 2018

using System;
using System.Diagnostics;
using UnityEngine;

public class DiagnosticsControl : MonoBehaviour {

    /// <summary>
    /// DiagnosticsText GameObject.
    /// </summary>
    public GameObject DiagnosticsText;
    private TextMesh DiagnosticsTextMesh;

    /// <summary>
    /// EFP container GameObject.
    /// </summary>
    public GameObject EFPContainer;
    private EFPDriver Driver;

    private Stopwatch StopWatch = new Stopwatch();
    private long Seconds = 0;
    private string DiagnosticsMessage = "";

	// Use this for initialization
	void Start () {
        DiagnosticsTextMesh = DiagnosticsText.GetComponent<TextMesh>();
        Driver = EFPContainer.GetComponent<EFPDriver>();

        StopWatch.Start();
	}
	
	// Update is called once per frame
	void Update () {
        Seconds = StopWatch.ElapsedTicks / Stopwatch.Frequency;

        // display elasped time
        DiagnosticsMessage = "<size=144><b>External Feed Pathway Diagnostics</b></size>\n" +
            "- Accesses entire cached spatial data,\n" +
            "- Adds to voxel grid with default byte value\n";
        // display EFPDriver metadata
        DiagnosticsMessage += string.Format("<b>Driver</b>\n" +
            "Speed (ms / Hz): {0} / {1}\n" +
            "Total Memory Use: {2}\n" +
            "Elasped Time (s): {3}\n",
            Math.Round(Driver.DriverSpeed * 1000.0, 0), Math.Round(1.0 / Driver.DriverSpeed, 1), 
            MemToStr(GC.GetTotalMemory(false)), Seconds);
        // display MeshManager metadata
        DiagnosticsMessage += string.Format("<b>Mesh Manager</b>\n" +
            "Speed (ms): {0}\n" +
            "Cached Meshes: {1}\n" +
            "Cached Triangles: {2}\n" +
            "Cached Vertices: {3}\n",
            Math.Round(Driver.MeshManSpeed * 1000.0, 0),
            Driver.MeshMan.MeshCount, Driver.MeshMan.TriangleCount, Driver.MeshMan.VertexCount);
        // display VoxelGridManager metadata
        Metadata voxInfo = Driver.VoxGridMan.About();
        DiagnosticsMessage += string.Format("<b>Voxel Grid Manager</b>\n" +
            "Speed (ms): {0}\n" + 
            "Grid Components: {1}\n" +
            "Grid Voxels (non-null): {2} ({3})\n" +
            "Grid Volume (non-null) (m^2): {4} ({5})\n" +
            "Grid Memory Use: {6}\n",
            Math.Round(Driver.VoxGridManSpeed * 1000.0, 0),
            voxInfo.components, voxInfo.voxels, voxInfo.nonNullVoxels, 
            Math.Round(voxInfo.volume, 1), Math.Round(voxInfo.nonNullVolume, 1), MemToStr(voxInfo.memSize));

        DiagnosticsTextMesh.text = DiagnosticsMessage;
	}

    /// <summary>
    /// Returns presentable string of memory size.
    /// </summary>
    static string MemToStr(long bytes)
    {
        if (bytes < 1000) // less than 1 kB
            return String.Format("{0} B", bytes);
        if (bytes < 1000 * 1000) // less than 1 MB
            return String.Format("{0} kB", bytes / 1000);
        if (bytes < 1000 * 1000 * 1000) // less than 1 GB
            return String.Format("{0} MB", bytes / (1000 * 1000));
        return String.Format("{0} GB", bytes / (1000 * 1000 * 1000));
    }
}
