/// VoxelGridManager
/// Generic interface for a voxelated data structure. 
/// Mark Scherer, June 2018

/// NOTE: Tested via VoxelGridTester/Program.cs/TestVoxelGridManager() (6/11/2018)

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for voxelated data structure. Currently setup to store byte data.
/// </summary>
public class VoxelGridManager<T>
{
    /// <summary>
    /// Used for packaging voxGrid metadata because Unity does not allow Tuples
    /// </summary>
    public struct Metadata
    {
        public int components, voxels, nonNullVoxels, memSize;
        public double volume, nonNullVolume;
        public Vector3 min, max;
        public DateTime lastUpdated;

        public Metadata(int myComponents, int myVoxels, int myNonNullVoxels, int myMemSize, 
            double myVolume, double myNonNullVolume, Vector3 myMin, Vector3 myMax, DateTime myLastUpdated)
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

    // Octree structure control
    public Vector3 startingPoint { get; private set; }
    public float minSize { get; private set; } // meters
    public float defaultSize { get; private set; } // meters

    /// <summary>
    /// Voxel grid data structure.
    /// </summary>
    private Octree<T> voxGrid;

    /// <summary>
    /// DateTime of last voxGrid update.
    /// </summary>
    public DateTime lastUpdate { get; private set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public VoxelGridManager(Vector3 myStartingPoint = new Vector3(), float myMinSize = 0.1f, float myDefaultSize = 1f)
    {
        startingPoint = myStartingPoint;
        minSize = myMinSize;
        defaultSize = myDefaultSize;
        voxGrid = new Octree<T>(startingPoint, minSize, defaultSize);
        lastUpdate = DateTime.Now;
    }

    public float Resolution
    {
        get { return voxGrid.MinSize; }
        set { voxGrid.MinSize = value; }
    }

    /// <summary>
    /// Accessor for voxGrid metadata.
    /// </summary>
    public Metadata About()
    {
        return new Metadata(voxGrid.numComponents, voxGrid.numVoxels, voxGrid.numNonNullVoxels, voxGrid.memSize,
            voxGrid.volume, voxGrid.nonNullVolume, voxGrid.root.min, voxGrid.root.max, lastUpdate);
    }

    /// <summary>
    /// Public interface for voxGrid set method.
    /// </summary>
    public void Set(List<Intersector.PointValue<T>> updates, bool updateStruct)
    {
        for (int i = 0; i < updates.Count; i++)
            voxGrid.set(updates[i].Point.point, updates[i].Value, updateStruct);
        lastUpdate = DateTime.Now;
    }

    public T Get(Vector3 point)
    {
        return voxGrid.get(point);
    }
}