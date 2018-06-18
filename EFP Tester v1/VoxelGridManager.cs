/// VoxelGridManager
/// Interface for voxelated data structure. 
/// Mark Scherer, June 2018

/// NOTE: Tested via VoxelGridTester/Program.cs/TestVoxelGridManager() (6/11/2018)

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for packaging voxGrid metadata because Unity does not allow Tuples
/// </summary>
public struct Metadata
{
    public int components, voxels, nonNullVoxels, memSize;
    public double volume, nonNullVolume;
    public Vector3 min, max;
    public DateTime lastUpdated;

    public Metadata(int myComponents, int myVoxels, int myNonNullVoxels, int myMemSize, double myVolume, double myNonNullVolume,
        Vector3 myMin, Vector3 myMax, DateTime myLastUpdated)
    {
        components = myComponents;
        voxels = myVoxels;
        nonNullVoxels = myNonNullVoxels;
        memSize = myMemSize;
        volume = myVolume;
        nonNullVolume = myNonNullVolume;
        min = myMin;
        max = myMax;
        lastUpdated = myLastUpdated;
    }
}

/// <summary>
/// Interface for voxelated data structure. Currently setup to store byte data.
/// NOTE: Because singleton, voxGrid constructor values must be set with static control variables within this class.
/// </summary>
public class VoxelGridManager : HoloToolkit.Unity.Singleton<VoxelGridManager>
{
    /// <summary>
    /// Control for Octree
    /// </summary>
    private static Vector3 startingPoint = new Vector3(0f, 0f, 0f); // meters
    private static float minSize = 0.1f; // meters
    private static float defaultSize = 1.0f; // meters

    /// <summary>
    /// Voxel grid data structure.
    /// </summary>
    private Octree<byte> voxGrid;

    /// <summary>
    /// Runtime control for updateStruct of voxGrid set method.
    /// </summary>
    public bool updateStruct;

    /// <summary>
    /// DateTime of last voxGrid update.
    /// </summary>
    public DateTime lastUpdate { get; private set; }

    // indicator for outside classes
    // Necessary b/c cannot preset and assign public get, private set to variable.
    // One readonly variable would work, but cannot be changed at runtime.
    public static float MinSizeSpec { get; private set; }

    /// <summary>
    /// Constructor.
    /// NOTE: Singleton<T> enforces new constraint on T... must have public, parameterless constructor.
    /// </summary>
    public VoxelGridManager()
    {
        voxGrid = new Octree<byte>(startingPoint, minSize, defaultSize);
        updateStruct = true;
        MinSizeSpec = minSize;
        lastUpdate = DateTime.Now;
    }

    /// <summary>
    /// Accessor for voxGrid metadata.
    /// </summary>
    public Metadata About()
    {
        Metadata info = new Metadata(voxGrid.numComponents, voxGrid.numVoxels, voxGrid.numNonNullVoxels, voxGrid.memSize,
            voxGrid.volume, voxGrid.nonNullVolume, voxGrid.root.min, voxGrid.root.max, lastUpdate);
        return info;
    }

    /// <summary>
    /// Public interface for voxGrid set method.
    /// </summary>
    /// <param name="updates">
    /// List of points to update
    /// </param>
    public void Set(List<Vector3> points)
    {
        for (int i = 0; i < points.Count; i++)
            voxGrid.set(points[i], default(byte), updateStruct);
        lastUpdate = DateTime.Now;
    }
}