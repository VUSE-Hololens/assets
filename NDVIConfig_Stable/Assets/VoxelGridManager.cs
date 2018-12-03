/// VoxelGridManager
/// Generic interface for a voxelated data structure. 
/// Mark Scherer, June 2018

/// NOTE: Tested via VoxelGridTester/Program.cs/TestVoxelGridManager() (6/11/2018)

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if WINDOWS_UWP
using Windows.Storage;
#endif


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
    /// List of all the saved points of interest
    /// </summary>
    private List<POI> savedPoints;

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
        savedPoints = new List<POI>();
    }

    // updating resolution resets all data.
    public float Resolution
    {
        get { return voxGrid.MinSize; }
        set
        {
            if (value != voxGrid.MinSize)
            {
                minSize = value;
                Reset();
            }
        }
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

    /// <summary>
    /// Return list of all voxels in voxGrid.
    /// </summary>
    public List<Voxel<T>> Voxels()
    {
        return voxGrid.Voxels();
    }

    public void Reset()
    {
        voxGrid = new Octree<T>(startingPoint, minSize, defaultSize);
        lastUpdate = DateTime.Now;
    }

    public bool Contains(Vector3 pt)
    {
        return voxGrid.root.contains(pt);
    }

    /// <summary>
    /// returns if voxel containing point is non-null
    /// </summary>
    public bool NonNullCell(Vector3 pointToGet)
    {
        return voxGrid.NonNullCell(pointToGet);
    }

    /// <summary>
    /// saves the point specified by user as a POI in savedPoints
    /// </summary>
    public void SavePoint(Vector3 pointToSave, string labelToSave)
    {
        if (!Contains(pointToSave))
        {
            throw new ArgumentOutOfRangeException("pointToSave", "not contained in Voxel Grid.");
        }
        savedPoints.Add(new POI(pointToSave, labelToSave));

    }

    public void ExportVoxelGrid()
    {
#if WINDOWS_UWP
        RunExportVoxelGrid();
#endif
    }

#if WINDOWS_UWP
    async private void RunExportVoxelGrid()
    {
        //Folder where the export should be placed
        var pictureLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
        string folderName = pictureLibrary.SaveFolder.Path;
        Debug.Log(String.Format("folderName creation success, folderName: {0}", folderName));

        //List to hold the values of the voxel grid
        List<Voxel<T>> exportList = Voxels();
        //String to hold the output for the voxel grid files
        StringBuilder exportOut = new StringBuilder();

        //path for Voxel Grid Export
        string fileName = "VoxGrid " + DateTime.Now.ToString("yyyy-MM-dd HH_mm") + ".csv";
        string pathString = System.IO.Path.Combine(folderName, fileName);


        //Add the VoxelGrid Sensor values to the .csv
        exportOut.Append("Sensor Value,X,Y,Z\n");
        foreach (var vox in exportList)
        {
            if (!vox.nullVox)
            {
                exportOut.AppendFormat("{0},{1},{2},{3}\n", vox.value, vox.point.x, vox.point.y, vox.point.z);
            }

        }

        CreateCSV(pathString, exportOut);
    }
#endif

    private Boolean CreateCSV(string pathString, StringBuilder exportOut)
    {
        //create voxGrid file
        try
        {
            Debug.Log(String.Format("Exporting VoxelGrid Success... File Path: {0}", pathString));
            //Check not to overwrite
            if (!System.IO.File.Exists(pathString))
            {
                //create file
                using (System.IO.StreamWriter sw = System.IO.File.CreateText(pathString))
                {
                    sw.WriteLine(exportOut);
                }
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.Log(String.Format("Exception exporting VoxelGrid... File Path: {0}", pathString));
            return false;
        }

    }

}