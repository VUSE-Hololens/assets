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
        Theta = Intersector.Instance.RadToDeg(Intersector.Instance.AdjAtanTheta(spatialVector.z, spatialVector.x));
        Phi = Intersector.Instance.RadToDeg(Intersector.Instance.AdjAtanPhi(spatialVector.z, spatialVector.y));
    }
}

/// <summary>
/// Spatially-aware view field of a sensor.
/// FOV defines full angle of frustum field of view, centered at transform. 
/// </summary>
public struct Frustum
{
    public UnityEngine.Transform Transform;
    public ViewVector FOV { get; private set; }

    public Frustum(UnityEngine.Transform myTransform, ViewVector myFOV)
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
    public PointValue<T> pv;
    public float distance;
    public bool nullCell;
}

/// <summary>
/// Calculates resultant PointValues when 2D Raster is projected along a Frustum at a Mesh.
/// Mesh represented by list of vertices.
/// NOTE: does not currently account for occlusion.
/// </summary>
public class Intersector : HoloToolkit.Unity.Singleton<Intersector>
{
    /// <summary>
    /// Metadata: how many vertices were in passed Frustum for last call to Intersection().
    /// </summary>
    public int VerticesInView { get; private set; }

    /// <summary>
    /// Metadata: how many vertices were in passed Frustum and non-occluded for last call to Intersection().
    /// </summary>
    public int nonOccludedVertices { get; private set; }

    
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
        int imgPCi = img.GetLength(0);
        int imgPCj = img.GetLength(1);
        List<PointValue<byte>> result = new List<PointValue<byte>>();
        VerticesInView = 0;

        /// create occlusion grid
        Dictionary<string, int> ocPixels = RequiredGrid(projection.FOV);
        int ocPCi = ocPixels["i"];
        int ocPCj = ocPixels["j"];
        OcclusionCell<byte>[,] ocGrid = new OcclusionCell<byte>[ocPCi, ocPCj];
        for (int i = 0; i < ocPCi; i++)
        {
            for (int j = 0; j < ocPCj; j++)
            {
                ocGrid[i, j].nullCell = true;
            }
        }

        /// try each vertex
        foreach (Vector3 vertex in vertices)
        {
            /// calculate position vector (world space)
            Vector3 Pworld = Vector(projection.Transform.position, vertex);

            /// convert position vector (world space) to position vector (Frustum space)
            Vector3 Plocal = projection.Transform.InverseTransformVector(Pworld);

            /// convert position vector (Frustum space) to view vector (Frustum space)
            ViewVector Vlocal = new ViewVector(Plocal);

            /// check if view vector is within Frustum FOV
            if (Math.Abs(Vlocal.Theta) < projection.FOV.Theta / 2.0 &&
                Math.Abs(Vlocal.Phi) < projection.FOV.Phi / 2.0)
            {
                VerticesInView++;

                /// map view vector to occlusion grid
                int ioc = (int)(ocPCi / 2 + ocPCi * (Vlocal.Theta / projection.FOV.Theta));
                int joc = (int)(ocPCj / 2 + ocPCj * (Vlocal.Phi / projection.FOV.Phi));

                /// add to occlusion grid as new PointValue if not occluded
                
                if (ocGrid[ioc, joc].nullCell || Pworld.magnitude < ocGrid[ioc, joc].distance)
                {
                    /// map view vector to img pixel grid
                    int iImg = (int)(imgPCi / 2 + imgPCi * (Vlocal.Theta / projection.FOV.Theta));
                    int jImg = (int)(imgPCj / 2 + imgPCj * (Vlocal.Phi / projection.FOV.Phi));

                    /// update occlusion grid
                    ocGrid[ioc, joc].pv = new PointValue<byte>(vertex, img[iImg, jImg]);
                    ocGrid[ioc, joc].distance = Pworld.magnitude;
                    ocGrid[ioc, joc].nullCell = false;
                }
            }
        }

        for (int i = 0; i < ocGrid.GetLength(0); i++)
        {
            for (int j = 0; j < ocGrid.GetLength(1); j++)
            {
                if (!ocGrid[i, j].nullCell)
                    result.Add(ocGrid[i, j].pv);
            }
        }
        nonOccludedVertices = result.Count;
        return result;
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
    public double RadToDeg(double rad)
    {
        return rad * (180.0 / Math.PI);
    }

    public double DegToRad(double deg)
    {
        return deg * (Math.PI / 180.0);
    }

    /// <summary>
    /// Calculates arctan (in radians) so that resultant angle is between -pi and pi.
    /// </summary>
    public double AdjAtanTheta(double denominator, double numerator)
    {
        if (numerator == 0)
        {
            if (denominator > 0)
                return 0.0;
            else
                return 180.0;
        }
        if (denominator < 0 && numerator > 0) /// Q3
            return Math.PI / 2.0 + Math.Atan(-denominator / numerator);
        if (denominator < 0 && numerator < 0) /// Q4
            return -Math.PI / 2.0 - Math.Atan(numerator / denominator);
        return Math.Atan(denominator / numerator);
    }

    /// <summary>
    /// Calculates arctan (in radians) always in reference to plane of numerator.
    /// </summary>
    public double AdjAtanPhi(double denominator, double numerator)
    {
        if (numerator == 0)
        {
            return 0;
        }
        if (denominator < 0 && numerator > 0) /// Q2
            return Math.Atan(-denominator / numerator);
        if (denominator < 0 && numerator < 0) /// Q3
            return -Math.Atan(denominator / numerator);
        return Math.Atan(denominator / numerator);
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
        pixels.Add("i", (int)Math.Round(totalViewSizeX / apparentSize));
        pixels.Add("j", (int)Math.Round(totalViewSizeY / apparentSize));
        return pixels;
    }
}