/// Script for controlling text attributes from separate TextControlContainer GameObject.
/// Mark Scherer, June 2018

/// NOTE: EFP (External Feed Pathway)

using System;
using UnityEngine;

public class TextControl : MonoBehaviour {

    /// <summary>
    /// GameObject containing TextMesh to display results to.
    /// </summary>
    public GameObject TextContainer;
    private TextMesh TextObj;

    /// <summary>
    /// GameObject containing components of the EFP.
    /// </summary>
    public GameObject EFPContainer;
    private ExternalFeedDriver Driver;
    private MeshManager MeshManagerObj;
    private VoxelGridManager GridManager;
    private Intersector Intersect;

	// Use this for initialization
	void Start () {
        TextObj = TextContainer.GetComponent<TextMesh>();

        Driver = EFPContainer.GetComponent<ExternalFeedDriver>();
        MeshManagerObj = EFPContainer.GetComponent<MeshManager>();
        GridManager = EFPContainer.GetComponent<VoxelGridManager>();
        Intersect = EFPContainer.GetComponent<Intersector>();
    }

    // Update is called once per frame
    void Update() {
        Metadata VoxInfo = GridManager.about();
        TextObj.text = String.Format("<size=144><b>External Feed Pathway Diagnostics</b></size>\n" +
            "- Accesses entire cached spatial data\n" +
            "- Calculates sensor-projection/mesh intersection (simulated sensor values)\n" +
            "- Updates voxel grid\n" +
            "<b>Pathway Driver</b>\n" +
            "Total Memory Use: {0}\n" +
            "Driver Speed (ms / Hz): {1} / {2}\n" +
            "<b>Mesh Manager</b>\n" +
            "Speed (ms): {3}\n" +
            "Cached Meshes: {4}\n" +
            "Cached Triangles: {5}\n" +
            "Cached Vertices: {6}\n" +
            "<b>Intersector</b>\n" +
            "Speed (ms): {7}\n" +
            "Vertices in View (FOV: {8}x{9} deg): {10}\n" +
            "Non-occluded Vertices: {11}\n" +
            "<b>Voxel Grid Manager</b>\n" +
            "Speed (ms): {12}\n" +
            "Grid Components: {13}\n" +
            "Grid Voxels (non-null): {14} ({15})\n" +
            "Grid Volume (non-null) (m^2): {16} ({17})\n" +
            "Grid Memory Use: {18}\n",
            MemToStr(GC.GetTotalMemory(false)), 
            Math.Round(Driver.ProcessSpeed * 1000.0, 0), Math.Round(1.0 / Driver.ProcessSpeed, 1),
            Math.Round(Driver.MeshManagerSpeed * 1000.0, 0),
            MeshManagerObj.meshCount, MeshManagerObj.triangleCount, MeshManagerObj.vertexCount,
            Math.Round(Driver.IntersectorSpeed * 1000.0, 0), Driver.sensorView.FOV.Theta, Driver.sensorView.FOV.Phi,
            Intersect.VerticesInView, Intersect.nonOccludedVertices,
            Math.Round(Driver.SetSpeed * 1000.0, 0), VoxInfo.components, VoxInfo.voxels, VoxInfo.nonNullVoxels, 
            Math.Round(VoxInfo.volume, 2), Math.Round(VoxInfo.nonNullVolume, 2), MemToStr(VoxInfo.memSize));
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
