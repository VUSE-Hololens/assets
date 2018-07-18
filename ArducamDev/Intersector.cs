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
        public CachedVertex Point { get; private set; }
        public T Value { get; private set; }

        public PointValue(Vector3 myPoint, T myValue)
        {
            Point = new CachedVertex(myPoint);
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
        /// Returns i,j pixel coordinates of this ViewVector within 1D rep of pixelGrid 
            /// of height, width spanning across FOV
        /// </summary>
        public int Map<T>(T[] grid, int height, int width, ViewVector FOV)
        {
            int i = (int)(width / 2 + width * (Theta / FOV.Theta));
            int j = (int)(height / 2 + height * (Phi / FOV.Phi));

            // ensure in grid
            if (i < 0)
                throw new ArgumentOutOfRangeException("i", "less than zero");
            if (i > width)
                throw new ArgumentOutOfRangeException("i", "greater than width");
            if (j < 0)
                throw new ArgumentOutOfRangeException("j", "less than zero");
            if (j > height)
                throw new ArgumentOutOfRangeException("j", "greater than height");

            int index = j * width + i;

            if (index > grid.Length)
                throw new ArgumentOutOfRangeException("index", "greater than grid length");

            return index;
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
            public CachedVertex closest;
            public float distance;
            public bool nullCell;
        }

        public OcclusionCell[,] grid;
        public float objSize;
        public float objDistance;
        public ViewVector FOV;

        public Occlusion(float myObjSize, float myObjDistance, ViewVector myFOV)
        {
            // control
            // Converts real size myObjSize to apparent size for up to this angle from plane perpindicular to line of sight
            float ObjAngleOffset = 30; // deg
            
            // set data
            objSize = myObjSize * (float)Math.Cos(DegToRad(ObjAngleOffset));
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

        public List<CachedVertex> Points()
        {
            List<CachedVertex> pts = new List<CachedVertex>();
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
    /// Point cached within SpatialMappingObserver.
    /// </summary>
    public struct CachedVertex
    {
        public Vector3 point;
        public int meshIndex;
        public int pointIndex;

        public CachedVertex(Vector3 myPoint, int myMeshIndex, int myPointIndex)
        {
            point = myPoint;
            meshIndex = myMeshIndex;
            pointIndex = myPointIndex;
        }

        public CachedVertex(Vector3 myPoint)
        {
            point = myPoint;
            meshIndex = -1;
            pointIndex = -1;
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
    /// Returns true if any of points are within ViewField, or a pair span it.
    /// Spanning referes to a pair of points located in front of the sensor and in diagonal ViewVector quandrants.
    /// DOES NOT update metadata.
    /// </summary>
    public bool AnyInView(List<Vector3> points, Frustum viewField)
    {
        bool topRight = false;
        bool topLeft = false;
        bool botLeft = false;
        bool botRight = false;
        bool left = false;
        bool right = false;
        bool top = false;
        bool bot = false;

        foreach (Vector3 pt in points)
        {
            ViewVector vv = viewField.ViewVec(pt);

            // check in in FOV
            if (viewField.FOV.Contains(vv))
                return true;

            // check if elligible for span
            if (Math.Abs(vv.Theta) < 90)
            {
                // check corners
                if (vv.Theta > 0 && vv.Phi > 0)
                    topRight = true;
                else if (vv.Theta > 0)
                    botRight = true;
                else if (vv.Phi > 0)
                    topLeft = true;
                else
                    botLeft = true;

                // check edges
                if (vv.Theta > 0 && Math.Abs(vv.Phi) < viewField.FOV.Phi / 2)
                    right = true;
                else if (vv.Theta < 0 && Math.Abs(vv.Phi) < viewField.FOV.Phi / 2)
                    left = true;
                else if (vv.Phi > 0 && Math.Abs(vv.Theta) < viewField.FOV.Theta / 2)
                    top = true;
                else if (vv.Phi < 0 && Math.Abs(vv.Theta) < viewField.FOV.Theta / 2)
                    bot = true;
            }
        }

        // check for spanning
        if ((topRight && botLeft) || (topLeft && botRight))
            return true;
        if ((left && right) || (top & bot))
            return true;
            
        return false;
    }

    /// <summary>
    /// Takes in raster, frustum and projects values onto point cloud. Returns list of point-values.
    /// oc: specs for occlusion approximation of point-cloud.
    /// extras: list containing point-clouds.
    /// meshGuide: guide to visibility of point-cloud sections.
    /// meshList: list of meshes whose colors are updated according to projection
    /// </summary>
    public List<PointValue<byte>> ProjectToVisible(byte[] raster, Frustum viewField, int height, int width, 
        Occlusion oc, List<SurfacePoints> extras, List<bool> meshGuide,
        float closest, float furthest, Vector3 curPos)
    {   
        // reset metadata
        CheckedVertices = 0;
        VerticesInView = 0;
        NonOccludedVertices = 0;

        // gather visible vertices from all meshes
        List<CachedVertex> visible = AllInView(extras, meshGuide, viewField, oc);
        
        // project to visible
        return Project(visible, viewField, raster, height, width, closest, furthest, curPos);
    }

    /// <summary>
    /// Returns list of all points in viewField using oc for occlusion approximation.
    /// Will update occluded and notInView to lists of vertices in those categories.
    /// NOTE: clearing lists, resetting metadata must be done BEFORE calling method.
    /// </summary>
    public List<CachedVertex> AllInView(List<SurfacePoints> extras, List<bool> meshGuide, Frustum viewField, Occlusion oc)
    {
        for (int i = 0; i < extras.Count; i++)
        {
            // check visibility
            if (meshGuide[i])
            {
                for (int j = 0; j < extras[i].Wvertices.Count; j++)
                {
                    Vector3 pt = extras[i].Wvertices[j];
                    Vector3 Pvec = Vector(viewField.Transform.position, pt);
                    ViewVector vv = viewField.ViewVec(pt);

                    if (viewField.FOV.Contains(vv))
                    {
                        Vector2 coords = vv.Map<Occlusion.OcclusionCell>(oc.grid, viewField.FOV);
                        CachedVertex cv = new CachedVertex(pt, i, j);

                        if (oc.grid[(int)coords.x, (int)coords.y].nullCell ||
                            Pvec.magnitude < oc.grid[(int)coords.x, (int)coords.y].distance)
                        {
                            oc.grid[(int)coords.x, (int)coords.y].closest = cv;
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
    /// Projects raster values onto passed list of points from frustum viewField.
    /// Also updates mesh colors of meshes and points defined by meshList, meshIndices, pointIndices 
    /// according to color1 to color2, value1 to value2 scale.
    /// </summary>
    private List<PointValue<byte>> Project(List<CachedVertex> points, Frustum viewField, byte[] raster, 
        int height, int width, float closest, float furthest, Vector3 curPos)
    {
        List<PointValue<byte>> result = new List<PointValue<byte>>();

        for (int i = 0; i < points.Count; i++)
        {

            Vector3 pt = points[i].point;
            ViewVector vv = viewField.ViewVec(pt);
            if (!viewField.FOV.Contains(vv))
                throw new ArgumentOutOfRangeException("pt", "not contained in viewField.FOV");

            int index = vv.Map<byte>(raster, height, width, viewField.FOV);
            PointValue<byte> pv = new PointValue<byte>(pt, raster[index]);

            // add pv to return
            result.Add(pv);
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