/// Script for controlling text attributes from separate TextControlContainer GameObject.
/// Mark Scherer, June 2018

/// NOTE: EFP (External Feed Pathway)

using System;
using System.Collections;
using System.Collections.Generic;
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

	// Use this for initialization
	void Start () {
        TextObj = TextContainer.GetComponent<TextMesh>();
        Driver = EFPContainer.GetComponent<ExternalFeedDriver>();
        MeshManagerObj = EFPContainer.GetComponent<MeshManager>();
        GridManager = EFPContainer.GetComponent<VoxelGridManager>();
	}

    // Update is called once per frame
    void Update() {
        Metadata VoxInfo = GridManager.about();
        TextObj.text = String.Format("<size=144><b>External Feed Pathway Diagnostics</b></size>\n" +
            "Continously accessing entire cached spatial data,\nadding to voxel grid with random byte value...\n" +
            "Total Memory Use: {0}\n" + 
            "<b>Pathway Driver</b>\n" +
            "Driver Speed (Hz): {1}\n" +
            "<b>Mesh Manager</b>\n" +
            "Cached Meshes: {2}\n" +
            "Cached Triangles: {3}\n" +
            "Cached Vertices: {4}\n" +
            "<b>Voxel Grid Manager</b>\n" +
            "Grid Components: {5}\n" +
            "Grid Voxels (non-null): {6} ({7})\n" +
            "Grid Volume (non-null) (m^2): {8} ({9})\n" + 
            "Grid Memory Use: {10}\n",
            MemToStr(GC.GetTotalMemory(false)),
            Math.Round(Driver.speed, 2),
            MeshManagerObj.meshCount, MeshManagerObj.triangleCount, MeshManagerObj.vertexCount,
            VoxInfo.components, VoxInfo.voxels, VoxInfo.nonNullVoxels, 
            Math.Round(VoxInfo.volume, 2), Math.Round(VoxInfo.nonNullVolume, 2), MemToStr(VoxInfo.memSize));
	}

    /// <summary>
    /// Returns presentable string of memory size.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
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
