/// Intersector
/// Class for calculating resultant PointValues when 2D Raster is project along a Frustum at a Mesh.
/// Content:
/// PointValue (struct)
/// ViewVector (struct)
/// Frustum (struct)
/// Intersector (class)

/// NOTE: Tried to make Intersector:Intersection method generic so it was ambivalent to type of img. 
/// This caused strange Unity build errors when accessing and mutating values within cells of ocGrid.
/// Currently set to byte, if img data type changes will have to manually change code.

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic spatially-aware values.
/// </summary>
public struct PointValue<T>
{
    public Vector3 Point { get; private set; }
    public T Value { get; private set; }

    public PointValue(Vector3 myPoint, T myValue)
    {
        Point = myPoint;
        Value = myValue;
    }

    /// <summary>
    /// Same as constructor, except no new memory allocation.
    /// </summary>
    public void Update(Vector3 newPoint, T newValue)
    {
        Point = newPoint;
        Value = newValue;
    }
}

/// <summary>
/// Vector for relative angle of (x,y,z) vector relative to 2D plane. Vector must originate at origin.
/// For 2D plane = XY plane, positive Z is forwards, 2D plane coordinates i,j:
/// Theta (i): angle of vector with YZ plane
/// Phi (j): angle of vector with XZ plane
/// </summary>
public struct ViewVector
{
    public double Theta { get; private set; }
    public double Phi { get; private set; }

    public ViewVector(double myTheta, double myPhi)
    {
        Theta = myTheta;
        Phi = myPhi;
    }

    /// <summary>
    /// Creates ViewVector from spatialVector.
    /// Note: spatialVector MUST originate from origin.
    /// </summary>
    public ViewVector(Vector3 spatialVector)
    {
        Theta = Intersector.RadToDeg(Intersector.AdjAtanTheta(spatialVector.x, spatialVector.z));
        Phi = Intersector.RadToDeg(Intersector.AdjAtanPhi(spatialVector));
    }
}

/// <summary>
/// Spatially-aware view field of a sensor.
/// FOV defines full angle of frustum field of view, centered at transform. 
/// </summary>
public struct Frustum
{
    public Transform Transform;
    public ViewVector FOV { get; private set; }

    public Frustum(Transform myTransform, ViewVector myFOV)
    {
        Transform = myTransform;
        FOV = myFOV;
    }
}

/// <summary>
/// Cell of Occlusion grid using closest-in-grid occlusion approximation strategy.
/// </summary>
public struct OcclusionCell<T>
{
    public Vector3 closest;
    public float distance;
    public bool nullCell;
    public List<Vector3> occluded;
}


/// <summary>
/// Calculates resultant PointValues when 2D Raster is projected along a Frustum at a Mesh.
/// Mesh represented by list of vertices.
/// NOTE: does not currently account for occlusion.
/// </summary>
public class Intersector : HoloToolkit.Unity.Singleton<Intersector>
{
    // Inspector Variables
    public Vector2 OccGridSize = new Vector2(6, 3);
    
    /*
    /// <summary>
    /// Metadata: how many vertices were in passed Frustum for last call to Intersection().
    /// </summary>
    public int VerticesInView { get; private set; }

    /// <summary>
    /// Metadata: how many vertices were in passed Frustum and non-occluded for last call to Intersection().
    /// </summary>
    public int NonOccludedVertices { get; private set; }
    */

    public int InViewCount { get; private set; }
    public int OutViewCount { get; private set; }
    public int OccludedCount { get; private set; }

    /// pre-declarations of variables in Intersection() for memory efficiency
    private int i, j, iOc, jOc;
    private Vector3 Pworld, Plocal;
    private ViewVector Vlocal;

    /* OLD INTERSECTION VERSION
    /// <summary>
    /// Calculates resultant PointValues when 2D Raster (img) is projected along Frustum at Mesh (vertices).
    /// Mesh represented by list of vertices.
    /// NOTE 1: does not currently account for occlusion.
    /// NOTE 2: projection's FOV should be full FOV angles, not half-angles.
    /// NOTE 3: Attempted to make generic. See note at begining of file.
    /// </summary>
    public List<PointValue<byte>> Intersection(Frustum projection, byte[,] img, List<Vector3> vertices)
    {
        /// setup
        ImgPCi = img.GetLength(0);
        ImgPCj = img.GetLength(1);
        VerticesInView = 0;

        /// update occlusion grid
        /// Cannot predeclare OcPixels because cannot add Dictionary components in declaration.
        Dictionary<string, int> OcPixels = RequiredGrid(projection.FOV);
        // Predeclaration of ocGrid has no benefit due to array type an dynamic size.
        OcclusionCell<byte>[,] ocGrid = new OcclusionCell<byte>[OcPixels["i"], OcPixels["j"]];
        for (i = 0; i < OcPixels["i"]; i++)
        {
            for (j = 0; j < OcPixels["j"]; j++)
            {
                ocGrid[i, j].nullCell = true;
            }
        }

        /// try each vertex
        for (i = 0; i < vertices.Count; i++)
        {
            /// calculate position vector (world space)
            Vector(projection.Transform.position, vertices[i], ref Pworld);

            /// convert position vector (world space) to position vector (Frustum space)
            Plocal = projection.Transform.InverseTransformVector(Pworld);

            /// convert position vector (Frustum space) to view vector (Frustum space)
            Vlocal.Update(Plocal);

            /// check if view vector is within Frustum FOV
            if (Math.Abs(Vlocal.Theta) < projection.FOV.Theta / 2.0 &&
                Math.Abs(Vlocal.Phi) < projection.FOV.Phi / 2.0)
            {
                VerticesInView++;

                /// map view vector to occlusion grid
                iOc = (int)(OcPixels["i"] / 2 + OcPixels["i"] * (Vlocal.Theta / projection.FOV.Theta));
                jOc = (int)(OcPixels["j"] / 2 + OcPixels["j"] * (Vlocal.Phi / projection.FOV.Phi));

                /// add to occlusion grid as new PointValue if not occluded

                if (ocGrid[iOc, jOc].nullCell || Pworld.magnitude < ocGrid[iOc, jOc].distance)
                {
                    /// map view vector to img pixel grid
                    iImg = (int)(ImgPCi / 2 + ImgPCi * (Vlocal.Theta / projection.FOV.Theta));
                    jImg = (int)(ImgPCj / 2 + ImgPCj * (Vlocal.Phi / projection.FOV.Phi));

                    /// update occlusion grid
                    ocGrid[iOc, jOc].pv.Update(vertices[i], img[iImg, jImg]);
                    ocGrid[iOc, jOc].distance = Pworld.magnitude;
                    ocGrid[iOc, jOc].nullCell = false;
                }
            }
        }

        /// Alteratively could prevent reallocating using Result.Clear(), however is slow
        Result = new List<PointValue<byte>>();
        for (i = 0; i < ocGrid.GetLength(0); i++)
        {
            for (j = 0; j < ocGrid.GetLength(1); j++)
            {
                if (!ocGrid[i, j].nullCell)
                    Result.Add(ocGrid[i, j].pv);
            }
        }
        NonOccludedVertices = Result.Count;
        return Result;
    }
    */

    // new Intersection
    /// <summary>
    /// Calculates resultant PointValues when 2D Raster (img) is projected along Frustum at Mesh (vertices).
    /// Mesh represented by list of vertices.
    /// NOTE 1: does not currently account for occlusion.
    /// NOTE 2: projection's FOV should be full FOV angles, not half-angles.
    /// NOTE 3: Attempted to make generic. See note at begining of file.
    /// </summary>
    public void Intersection(Frustum projection, List<Vector3> vertices, 
        out List<Vector3> InView, out List<Vector3> OutView, out List<Vector3> Occluded,
        out List<ViewVector> VV, out List<Vector3> PVecs)
    {
        /// setup
        InView = new List<Vector3>();
        OutView = new List<Vector3>();
        Occluded = new List<Vector3>();
        VV = new List<ViewVector>();
        PVecs = new List<Vector3>();

        // Predeclaration of ocGrid has no benefit due to array type an dynamic size.
        OcclusionCell<byte>[,] ocGrid = new OcclusionCell<byte>[(int)OccGridSize.x, 
            (int)OccGridSize.y];
        for (i = 0; i < OccGridSize.x; i++)
        {
            for (j = 0; j < OccGridSize.y; j++)
            {
                ocGrid[i, j].nullCell = true;
                ocGrid[i, j].occluded = new List<Vector3>();
            }
        }

        /// try each vertex
        for (i = 0; i < vertices.Count; i++)
        {
            /// calculate position vector (world space)
            Pworld = Vector(projection.Transform.position, vertices[i]);

            /// convert position vector (world space) to position vector (Frustum space)
            Plocal = projection.Transform.InverseTransformVector(Pworld);
            PVecs.Add(Plocal);

            /// convert position vector (Frustum space) to view vector (Frustum space)
            ViewVector Vlocal = new ViewVector(Plocal);
            VV.Add(Vlocal);

            /// check if view vector is within Frustum FOV
            if (Math.Abs(Vlocal.Theta) < projection.FOV.Theta / 2.0 &&
                Math.Abs(Vlocal.Phi) < projection.FOV.Phi / 2.0)
            {
                /// map view vector to occlusion grid
                iOc = (int)(OccGridSize.x / 2 + OccGridSize.x * (Vlocal.Theta / projection.FOV.Theta));
                jOc = (int)(OccGridSize.y / 2 + OccGridSize.y * (Vlocal.Phi / projection.FOV.Phi));

                if (ocGrid[iOc,jOc].nullCell)
                {
                    ocGrid[iOc, jOc].closest = vertices[i];
                    ocGrid[iOc, jOc].distance = Pworld.magnitude;
                    ocGrid[iOc, jOc].nullCell = false;
                } else if (Pworld.magnitude < ocGrid[iOc, jOc].distance)
                {
                    ocGrid[iOc, jOc].occluded.Add(ocGrid[iOc, jOc].closest);
                    ocGrid[iOc, jOc].closest = vertices[i];
                    ocGrid[iOc, jOc].distance = Pworld.magnitude;
                } else
                    ocGrid[iOc, jOc].occluded.Add(vertices[i]);
            }
            else
                OutView.Add(vertices[i]);
        }

        // add vertices in occluded grid
        for (i = 0; i < OccGridSize.x; i++)
        {
            for (j = 0; j < OccGridSize.y; j++)
            {
                InView.Add(ocGrid[i, j].closest);
                Occluded.AddRange(ocGrid[i, j].occluded);
            }
        }

        InViewCount = InView.Count;
        OutViewCount = OutView.Count;
        OccludedCount = Occluded.Count;
    }


    /// <summary>
    /// Returns vector FROM point1 TO point2.
    /// </summary>
    public Vector3 Vector(Vector3 point1, Vector3 point2)
    {
        return point2 - point1;
    }

    /// <summary>
    /// Converts radians to degrees.
    /// </summary>
    public static double RadToDeg(double rad)
    {
        return rad * (180.0 / Math.PI);
    }

    public static double DegToRad(double deg)
    {
        return deg * (Math.PI / 180.0);
    }

    /// <summary>
    /// Calculates arctan (in radians) so that resultant angle is between -pi and pi.
    /// </summary>
    public static double AdjAtanTheta(double numerator, double denominator)
    {
        if (denominator < 0 && numerator < 0) /// Q3
            return -Math.PI / 2.0 - Math.Atan(denominator / numerator);
        if (numerator > 0 && denominator < 0) /// Q4
            return Math.PI / 2.0 + Math.Atan(Math.Abs(denominator) / numerator);
        return Math.Atan(numerator / denominator);
    }

    /// <summary>
    /// Calculates arctan (in radians) always in reference to plane of numerator.
    /// </summary>
    public static double AdjAtanPhi(Vector3 spatialVector)
    {
        Vector3 proj = new Vector3(spatialVector.x, 0, spatialVector.z);
        float mag = proj.magnitude;
        return Math.Atan(spatialVector.y / mag);
    }

    /// <summary>
    /// Determines the required grid size given hard-coded parameters for occlusion approximation via closest in grid.
    /// </summary>
    public Dictionary<string, int> RequiredGrid(ViewVector FOV)
    {
        double size = 0.10; // minimum object size to not be considered occluded (m)
        double angle = 45.0; // maximum surface angle with line-of-sight normal to not be considered occluded (deg)
        double distance = 3.0; // maximum object distance from sensor to not be considered occluded (m)

        double apparentSize = size * Math.Cos(DegToRad(angle));
        double totalViewSizeX = 2.0 * distance * Math.Tan(DegToRad(FOV.Theta) / 2.0);
        double totalViewSizeY = 2.0 * distance * Math.Tan(DegToRad(FOV.Phi) / 2.0);

        Dictionary<string, int> pixels = new Dictionary<string, int>();
        pixels["i"] = (int)Math.Round(totalViewSizeX / apparentSize);
        pixels["j"] = (int)Math.Round(totalViewSizeY / apparentSize);
        return pixels;
    }
}