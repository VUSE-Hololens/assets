/// ExternalFeedDriver
/// Driver for Hololens based tests of External Feed pathway of Sensor Integrator Application.
/// Mark Scherer, June 2018

using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Driver of External Feed pathway of Sensor Integrator Application.
/// </summary>
[RequireComponent(typeof(MeshManager))]
[RequireComponent(typeof(VoxelGridManager))]
public class ExternalFeedDriver : MonoBehaviour
{
    /// <summary>
    /// Tracks speed of execution of pathway.
    /// </summary>
    public double speed { get; private set; }

    /// <summary>
    /// Central control for VoxelGridManager.
    /// </summary>
    public bool updateVoxelStructure = true;

    private Stopwatch stopWatch = new Stopwatch();

    /// <summary>
    /// Called once at startup.
    /// </summary>
    void Start()
    {
        VoxelGridManager.Instance.updateStruct = updateVoxelStructure;
    }
    
    /// <summary>
    /// Called once per frame.
    /// </summary>
    void Update()
    {
        stopWatch.Reset();
        stopWatch.Start();

        /// get mesh vertex list from MeshManager
        List<Vector3> vertices = MeshManager.Instance.getVertices();

        // create list of update values
        byte defaultValue = 0;
        List<byte> updateValues = Enumerable.Repeat(defaultValue, vertices.Count).ToList();

        // push updates to VoxelGridManager
        VoxelGridManager.Instance.set(vertices, updateValues);

        stopWatch.Stop();
        speed = (double)Stopwatch.Frequency / (double)stopWatch.ElapsedTicks;
    }
}