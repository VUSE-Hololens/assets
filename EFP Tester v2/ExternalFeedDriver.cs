/// ExternalFeedDriver
/// Driver for Hololens based tests of External Feed pathway of Sensor Integrator Application.
/// Mark Scherer, June 2018

using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Driver of External Feed pathway of Sensor Integrator Application.
/// </summary>
[RequireComponent(typeof(MeshManager))]
[RequireComponent(typeof(VoxelGridManager))]
[RequireComponent(typeof(Intersector))]
public class ExternalFeedDriver : MonoBehaviour
{
    /// <summary>
    /// Tracks speed of execution of pathway.
    /// </summary>
    public double ProcessSpeed { get; private set; }
    public double MeshManagerSpeed { get; private set; }
    public double IntersectorSpeed { get; private set; }
    public double SetSpeed { get; private set; }

    /// <summary>
    /// Central control for VoxelGridManager.
    /// </summary>
    public bool updateVoxelStructure = true;

    private Stopwatch ProcessWatch = new Stopwatch();
    private Stopwatch SubprocessWatch = new Stopwatch();
    // had to make public to allow mutating value type return (Transform) and public accessing.
    public Frustum sensorView = new Frustum(default(Transform), new ViewVector(60, 30));
    private byte[,] sensorFeed = new byte[200, 100];

    /// <summary>
    /// Called once at startup.
    /// </summary>
    void Start()
    {
        /// sync control values
        VoxelGridManager.Instance.updateStruct = updateVoxelStructure;
    }
    
    /// <summary>
    /// Called once per frame.
    /// </summary>
    void Update()
    {
        ProcessWatch.Reset();
        ProcessWatch.Start();

        /// get mesh vertex list from MeshManager
        SubprocessWatch.Reset();
        SubprocessWatch.Start();
        List<Vector3> vertices = MeshManager.Instance.getVertices();
        SubprocessWatch.Stop();
        MeshManagerSpeed = (double)SubprocessWatch.ElapsedTicks / (double)Stopwatch.Frequency;

        /// find projection-mesh intersection PointValues
        SubprocessWatch.Reset();
        SubprocessWatch.Start();
        sensorView.Transform = Camera.main.transform;
        List<PointValue<byte>> updates = Intersector.Instance.Intersection(sensorView, sensorFeed, vertices);
        SubprocessWatch.Stop();
        IntersectorSpeed = (double)SubprocessWatch.ElapsedTicks / (double)Stopwatch.Frequency;

        // push updates to VoxelGridManager
        SubprocessWatch.Reset();
        SubprocessWatch.Start();
        VoxelGridManager.Instance.set(updates);
        SubprocessWatch.Stop();
        SetSpeed = (double)SubprocessWatch.ElapsedTicks / (double)Stopwatch.Frequency;

        ProcessWatch.Stop();
        ProcessSpeed = (double)ProcessWatch.ElapsedTicks / (double)Stopwatch.Frequency;
    }
}