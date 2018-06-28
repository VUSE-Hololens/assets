// SurfacePoints
// Shell class to act as component in SurfaceObject.Object GameObject to hold IntersectionPoints and Wvertices.
// Mark Scherer, June 2018

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfacePoints {

    public List<Vector3> IntersectPts { get; private set; }
    public List<Vector3> Wvertices { get; private set; }
    public int TriangleCount { get; private set; }
    public Bounds BoundsBox { get; private set; }

    public bool Visible = false;

    public SurfacePoints(List<Vector3> myIntersectPts, List<Vector3> myWvertics, int myTriCount, Bounds myBounds)
    {
        IntersectPts = myIntersectPts;
        Wvertices = myWvertics;
        TriangleCount = myTriCount;
        BoundsBox = myBounds;
    }
}
