/// Driver for EFP (External Feed Pathway)
/// Mark Scherer, June 2018
 
/// Currently owns dependencies as private variables. Not sure of best strategy. Could also:
    /// Require them as components.
    /// Have them as public variables of a separate GameObject.
    /// Implement them as static classes

using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(MeshManager))]
[RequireComponent(typeof(VoxelGridManager))]
public class EFPDriver : MonoBehaviour {

    // metadata
    public double DriverSpeed { get; private set; } // Update(), seconds
    public double MeshManSpeed { get; private set; } // MeshManager:UpdateVertices(), seconds
    public double VoxGridManSpeed { get; private set; } // VoxelGridManager:Set(), seconds
    // must access SpatialMappingManager.Instance in a MonoBehaviour Start() method
    public float MeshDensity { get; private set; } // triangles/m^3

    // dependencies
    public MeshManager MeshMan { get; private set; }
    public VoxelGridManager VoxGridMan { get; private set; }

    // pre-declarations
    private List<Vector3> Vertices = new List<Vector3>();
    private Stopwatch StopWatch = new Stopwatch();
    private Stopwatch SubStopWatch = new Stopwatch();

    // Use this for initialization
    void Start () {
        MeshMan = MeshManager.Instance;
        VoxGridMan = VoxelGridManager.Instance;
        MeshDensity = HoloToolkit.Unity.SpatialMapping.
            SpatialMappingManager.Instance.SurfaceObserver.TrianglesPerCubicMeter;
    }

	// Update is called once per frame
	void Update () {
        StopWatch.Reset();
        StopWatch.Start();

        // call MeshManager to update
        SubStopWatch.Reset();
        SubStopWatch.Start();
        MeshMan.UpdateVertices(ref Vertices);
        SubStopWatch.Stop();
        MeshManSpeed = (double)SubStopWatch.ElapsedTicks / (double)Stopwatch.Frequency;

        // update VoxelGrid
        SubStopWatch.Reset();
        SubStopWatch.Start();
        VoxGridMan.Set(Vertices);
        SubStopWatch.Stop();
        VoxGridManSpeed = (double)SubStopWatch.ElapsedTicks / (double)Stopwatch.Frequency;

        StopWatch.Stop();
        DriverSpeed = (double)StopWatch.ElapsedTicks / (double)Stopwatch.Frequency;
	}
}
