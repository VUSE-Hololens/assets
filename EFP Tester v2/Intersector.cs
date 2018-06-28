/// Intersector
/// Class for calculating visiblility of points within Frustum.
/// Note: Class defines following structs:
    /// PointValue
    /// ViewVector
    /// Frustum
    /// Occlusion (and OcclusionCell)
/// Mark Scherer, June 2018

using System;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.SpatialMapping;
using System.Collections.ObjectModel;
using System.Linq;


/// <summary>
/// Calculates resultant PointValues when 2D Raster is projected along a Frustum at a Mesh.
/// Mesh represented by list of vertices.
/// NOTE: does not currently account for occlusion.
/// </summary>
public class Intersector
{
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
    /// Vector for angle of (x,y,z) vector relative to 2D plane. Vector must originate at origin.
    /// For 2D plane = XY plane, positive Z is forwards, 2D plane coordinates i,j:
        /// Theta (i): angle of vector with YZ plane
        /// Phi (j): angle of vector with XZ plane
    /// </summary>
    public struct ViewVector
    {
        public double Theta;
        public double Phi;

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
            Theta = Intersector.RadToDeg(AdjAtanTheta(spatialVector.x, spatialVector.z));
            Phi = Intersector.RadToDeg(AdjAtanPhi(spatialVector));
        }

        /// <summary>
        /// True if vv is contained within FOV defined by this obj.
        /// </summary>
        public bool Contains(ViewVector vv)
        {
            return (Math.Abs(vv.Theta) < Theta / 2.0 && Math.Abs(vv.Phi) < Phi / 2.0);
        }

        /// <summary>
        /// Returns i,j pixel coordinates of this ViewVector within pixelGrid spanning across FOV
        /// </summary>
        public Vector2 Map<T>(T[,] grid, ViewVector FOV)
        {
            Vector2 result = new Vector2();
            result.x = (int)(grid.GetLength(0) / 2 + grid.GetLength(0) * (Theta / FOV.Theta));
            result.y = (int)(grid.GetLength(1) / 2 + grid.GetLength(1) * (Phi / FOV.Phi));
            return result;
        }

        /// <summary>
        /// Calculates arctan (in radians) so that resultant angle is between -pi and pi.
        /// </summary>
        private static double AdjAtanTheta(double numerator, double denominator)
        {
            if (denominator < 0 && numerator < 0) /// Q3
                return -Math.PI / 2.0 - Math.Atan(denominator / numerator);
            if (numerator > 0 && denominator < 0) /// Q4
                return Math.PI / 2.0 + Math.Atan(Math.Abs(denominator) / numerator);
            return Math.Atan(numerator / denominator);
        }

        /// <summary>
        /// Calculates arctan (in radians) always in reference to XZ plane.
        /// </summary>
        private static double AdjAtanPhi(Vector3 spatialVector)
        {
            Vector3 proj = new Vector3(spatialVector.x, 0, spatialVector.z);
            float mag = proj.magnitude;
            return Math.Atan(spatialVector.y / mag);
        }
    }

    /// <summary>
    /// Spatially-aware view field of view.
    /// FOV defines full angle of frustum field of view, centered at transform. 
    /// </summary>
    public struct Frustum
    {
        public Transform Transform;
        public ViewVector FOV;

        public Frustum(Transform myTransform, ViewVector myFOV)
        {
            Transform = myTransform;
            FOV = myFOV;
        }

        /// <summary>
        /// Returns view vector from Frustum to point
        /// </summary>
        public ViewVector ViewVec(Vector3 point)
        {
            Vector3 Pworld = Vector(Transform.position, point);
            Vector3 PLocal = Transform.InverseTransformDirection(Pworld);
            return new ViewVector(PLocal);
        }
    }

    /// <summary>
    /// Occlusion grid obj and controls
    /// </summary>
    public struct Occlusion
    {
        /// <summary>
        /// Cell of Occlusion.grid for closest-in-grid occlusion approximation strategy.
        /// </summary>
        public struct OcclusionCell
        {
            public Vector3 closest;
            public float distance;
            public bool nullCell;
        }

        public OcclusionCell[,] grid;
        public float objSize;
        public float objDistance;
        public ViewVector FOV;

        public Occlusion(float myObjSize, float myObjDistance, ViewVector myFOV)
        {
            // set data
            objSize = myObjSize;
            objDistance = myObjDistance;
            FOV = myFOV;

            // calculate necessary pixel counts
            float totalViewSizeX = 2.0f * objDistance * (float)Math.Tan(DegToRad(FOV.Theta) / 2.0);
            float totalViewSizeY = 2.0f * objDistance * (float)Math.Tan(DegToRad(FOV.Phi) / 2.0);
            Vector2 dims = new Vector2();
            dims.x = (int)Math.Round(totalViewSizeX / objSize);
            dims.y = (int)Math.Round(totalViewSizeY / objSize);

            // create grid
            grid = new OcclusionCell[(int)dims.x, (int)dims.y];
            Reset();
        }

        public Occlusion(int i, int j, ViewVector myFOV)
        {
            FOV = myFOV;
            objSize = -1f;
            objDistance = -1f;
            grid = new OcclusionCell[i, j];
            Reset();
        }

        public void Reset()
        {
            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                    grid[i, j].nullCell = true;
            }
        }

        public List<Vector3> Points()
        {
            List<Vector3> pts = new List<Vector3>();
            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    if (!grid[i, j].nullCell)
                        pts.Add(grid[i, j].closest);
                }
            }
            return pts;
        }
    }


    /// <summary>
    /// Metadata: how many vertices total vertices were checked.
    /// Updated by each public method in Intersector.
    /// </summary>
    public int CheckedVertices { get; private set; }

    /// <summary>
    /// Metadata: how many vertices were in passed Frustum.
    /// Updated by each public method in Intersector.
    /// </summary>
    public int VerticesInView { get; private set; }

    /// <summary>
    /// Metadata: how many vertices were in passed Frustum and non-occluded.
    /// Updated by each public method in Intersector.
    /// </summary>
    public int NonOccludedVertices { get; private set; }

    /// <summary>
    /// Returns true if any of points are within ViewField.
    /// DOES NOT update metadata.
    /// </summary>
    public bool AnyInView(List<Vector3> points, Frustum viewField)
    {
        foreach (Vector3 pt in points)
            if (viewField.FOV.Contains(viewField.ViewVec(pt)))
                return true;
        return false;
    }

    /// <summary>
    /// Returns list of PointValues representing raster values projected onto visible points from viewField.
    /// </summary>
    public List<PointValue<T>> ProjectToVisible<T>(List<SurfacePoints> extras, List<bool> meshGuide, Frustum viewField, Occlusion oc, T[,] raster)
    {   
        // reset metadata
        CheckedVertices = 0;
        VerticesInView = 0;
        NonOccludedVertices = 0;

        // gather visible vertices from all meshes
        List<Vector3> visible = AllInView(extras, meshGuide, viewField, oc);
        
        // project to visible
        return Project<T>(visible, viewField, raster);
    }

    /// <summary>
    /// Returns list of all points in viewField using oc for occlusion approximation.
    /// Will update occluded and notInView to lists of vertices in those categories.
    /// NOTE: clearing lists, resetting metadata must be done BEFORE calling method.
    /// </summary>
    public List<Vector3> AllInView(List<SurfacePoints> extras, List<bool> meshGuide, Frustum viewField, Occlusion oc)
    {
        for (int i = 0; i < extras.Count; i++)
        {
            // check visibility
            if (meshGuide[i])
            {
                foreach (Vector3 pt in extras[i].Wvertices)
                {
                    Vector3 Pvec = Vector(viewField.Transform.position, pt);
                    ViewVector vv = viewField.ViewVec(pt);

                    if (viewField.FOV.Contains(vv))
                    {
                        Vector2 coords = vv.Map<Occlusion.OcclusionCell>(oc.grid, viewField.FOV);

                        if (oc.grid[(int)coords.x, (int)coords.y].nullCell ||
                            Pvec.magnitude < oc.grid[(int)coords.x, (int)coords.y].distance)
                        {
                            oc.grid[(int)coords.x, (int)coords.y].closest = pt;
                            oc.grid[(int)coords.x, (int)coords.y].distance = Pvec.magnitude;
                            oc.grid[(int)coords.x, (int)coords.y].nullCell = false;
                            NonOccludedVertices++;
                        }
                        VerticesInView++;
                    }
                    CheckedVertices++;
                }
            }
        }
        return oc.Points();
    }

    /// <summary>
    /// Returns list of PointValues representing raster values projected onto points from viewField.
    /// </summary>
    private List<PointValue<T>> Project<T>(List<Vector3> points, Frustum viewField, T[,] raster)
    {
        List<PointValue<T>> result = new List<PointValue<T>>();
        foreach (Vector3 pt in points)
        {
            ViewVector vv = viewField.ViewVec(pt);
            if (!viewField.FOV.Contains(vv))
                throw new ArgumentOutOfRangeException("pt", "not contained in viewField.FOV");

            Vector2 coords = vv.Map<T>(raster, viewField.FOV);
            result.Add(new PointValue<T>(pt, raster[(int)coords.x, (int)coords.y]));
        }
        return result;
    }

    /// <summary>
    /// Returns vector FROM point1 TO point2.
    /// </summary>
    private static Vector3 Vector(Vector3 point1, Vector3 point2)
    {
        return point2 - point1;
    }

    /// <summary>
    /// Converts radians to degrees.
    /// </summary>
    private static double RadToDeg(double rad)
    {
        return rad * (180.0 / Math.PI);
    }

    private static double DegToRad(double deg)
    {
        return deg * (Math.PI / 180.0);
    }
}