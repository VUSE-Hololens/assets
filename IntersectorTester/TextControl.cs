/// Script for controlling text attributes from separate TextControlContainer GameObject.
/// Used to run and output test results of Intersector tests.
/// Mark Scherer, June 2018

using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

public class TextControl : MonoBehaviour {

    /// <summary>
    /// GameObject containing TextMesh to display results to.
    /// </summary>
    public GameObject TextContainer;
    private TextMesh TextObj;

    /// <summary>
    /// GameObject containing Intersector.
    /// </summary>
    public GameObject IntersectorContainer;
    private Intersector IntersectorObj;

    // Use this for initialization
    void Start () {
        TextObj = TextContainer.GetComponent<TextMesh>();
        IntersectorObj = IntersectorContainer.GetComponent<Intersector>();
        TextObj.text = TestIntersector(IntersectorObj);
    }

    // Update is called once per frame
    void Update() {
        // nothing to do
	}

    // Tests Intersector class
    // Currently tests:
        // Vector (T1)
        // InverseTransformVector (T2 - not implemented)
        // ViewVector alt. constructor (T3)
        // Intersection (small scale) (T4 - not implemented)
        // Intersection (large scale) (T5 - not implemented)
    static string TestIntersector(Intersector IntersectorObj)
    {
        string output = "\n<size=144>Testing Intersector...</size>\n\n";

        // Test 1: Vector from two points
        output += "<b>Test 1</b>\n";
        Vector3 p1 = new Vector3(0, 0, 0);
        Vector3 p2 = new Vector3(2, 2, 2);
        output += String.Format("Vector from two points: {0} to {1}\n", pointToStr(p1), pointToStr(p2));
        Vector3 result = IntersectorObj.Vector(p1, p2);
        output += String.Format("Resultant vector: {0}\n\n", pointToStr(result));

        // Cannot test Unity Library outside of Unity...
        // Test 2: InverseTransformVector
        output += "<b>Test 2</b>\n";
        GameObject view2 = new GameObject();
        view2.name = "ViewFieldTest2";
        view2.transform.Rotate(new Vector3(90, 90, 0));
        output += String.Format("InverseTransformVector... view-field position: {0}, view-field euler angles: {1}\n",
            pointToStr(view2.transform.position), pointToStr(view2.transform.eulerAngles));
        Vector3[] unitVectors = unitVectorsArray();
        for (int i = 0; i < 8; i++)
            output += String.Format("Vector{0}: World Space: {1}, View-Field Local Space: {2}\n",
                i+1, pointToStr(unitVectors[i]), pointToStr(view2.transform.InverseTransformVector(unitVectors[i])));
        output += "\n";
        // */

        // Test 3: View Vector alt. constructor
        output += "<b>Test 3</b>\n";
        output += String.Format("ViewVector creation from position vector (in local space)... 2D cross-sectional plane is XY plane.\n");
        Vector3 forward = new Vector3(0, 0, 1);
        for (int i = 0; i < 8; i++)
            output += String.Format("Vector{0} {1}: ViewVector{0} (deg): {2}\n",
                i+1, pointToStr(unitVectors[i]), viewVectorToStr(new ViewVector(unitVectors[i])));
        output += String.Format("Vector: World Space: {0}, View-Field Local Space: {1}\n",
            pointToStr(forward), viewVectorToStr(new ViewVector(forward)));
        output += "\n";

        // Cannot test Unity Library outside of Unity...
        // Test 4: Intersection (small scale)
        output += "<b>Test 4</b>\n";
        List<Vector3> unitVectorList = new List<Vector3>();
        for (int i = 0; i < 8; i++)
            unitVectorList.Add(unitVectors[i]);
        GameObject view4 = new GameObject();
        view4.name = "ViewFieldTest4";
        ViewVector FOV4 = new ViewVector(170, 170);
        Frustum projection4 = new Frustum(view4.transform, FOV4);
        int[,] raster4 = new int[100, 100];
        output += String.Format("Intersection... Raster: ({0}x{1}), Frustum FOV: ({2}x{3}), " +
            "Frustum Pos: {4}, Frustum EA's: {5}\n" +
            "Tested all 8 unit vectors, found intersection points...\n",
            raster4.GetLength(0), raster4.GetLength(1), FOV4.Theta, FOV4.Phi, 
            pointToStr(projection4.Transform.position), pointToStr(projection4.Transform.eulerAngles));
        List<PointValue<int>> intersect = Intersector.Instance.Intersection<int>(projection4, raster4, unitVectorList);
        foreach (PointValue<int> pv in intersect)
            output += String.Format("Point: {0}, ViewVector: {1}\n", 
                pointToStr(pv.Point), viewVectorToStr(new ViewVector(pv.Point)));
        output += "\n";
        

        // Cannot test Unity Library outside of Unity...
        // Test 5: Intersection (large scale)
        output += "<b>Test 5</b>\n";
        System.Random rand = new System.Random();
        GameObject view5 = new GameObject();
        view5.name = "ViewFieldTest5";
        ViewVector FOV5 = new ViewVector(179, 179);
        Frustum projection5 = new Frustum(view5.transform, FOV5);
        int[,] raster5 = new int[100, 100];
        int iterations = 100000;
        Stopwatch stopWatch = new Stopwatch();
        Vector3 boundsMin = new Vector3(-5, -5, -5);
        Vector3 boundsMax = new Vector3(5, 5, 5);
        output += String.Format("Intersection... Raster: ({0}x{1}), Frustum FOV: ({2}x{3}). {4} Iterations...\n",
            raster5.GetLength(0), raster5.GetLength(1), FOV5.Theta, FOV5.Phi, iterations);
        List<Vector3> randVectors = new List<Vector3>();
        for (int i = 0; i < iterations; i++)
            randVectors.Add(randomPoint(boundsMin, boundsMax, rand));
        stopWatch.Start();
        List<PointValue<int>> result5 = Intersector.Instance.Intersection<int>(projection5, raster5, randVectors);
        stopWatch.Stop();
        long ms = (long)1000 * stopWatch.ElapsedTicks / Stopwatch.Frequency;
        output += String.Format("Took {0} ms ({1} us / op)... {2} vectors in view\n", ms, ms / 1000, result5.Count);
        output += "\n";

        // Test 6: RequiredGrid
        output += "<b>Test 6</b>\n";
        ViewVector FOV6 = new ViewVector(60, 60);
        output += String.Format("Testing RequiredGrid... FOV: {0}\n", viewVectorToStr(FOV6));
        Dictionary<string, int> pixels = Intersector.Instance.RequiredGrid(FOV6);
        output += String.Format("i: {0}, j: {1}\n", pixels["i"], pixels["j"]);
        output += "\n";

        /// Test 7: Occlusion (basic)
        output += "<b>Test 7</b>\n";
        GameObject view7 = new GameObject();
        view7.name = "ViewFieldTest7";
        ViewVector FOV7 = new ViewVector(170, 170);
        Frustum projection7 = new Frustum(view7.transform, FOV7);
        string[,] raster7 = new string[100, 100];
        for (int i = 0; i < raster7.GetLength(0); i++)
        {
            for (int j = 0; j < raster7.GetLength(1); j++)
                raster7[i, j] = i.ToString() + "," + j.ToString();
        }
        output += String.Format("Created Frustum: pos: {0}, EA's {1}, FOV: {2}\n", 
            pointToStr(projection7.Transform.position), pointToStr(projection7.Transform.eulerAngles), 
            viewVectorToStr(projection7.FOV));
        output += String.Format("Created Raster: {0}x{1}\n", raster7.GetLength(0), raster7.GetLength(1));
        Vector3 v1 = new Vector3(1, 1, 1);
        Vector3 v2 = new Vector3(2, 2, 2);
        Vector3 v3 = new Vector3(0, 0, 1);
        List<Vector3> vertices7 = new List<Vector3>();
        vertices7.Add(v2);
        vertices7.Add(v1);
        vertices7.Add(v2);
        vertices7.Add(v3);
        output += String.Format("Trying vertices: v1: {0}, v2: {1}, v3: {2}. Intersection return...\n", 
            pointToStr(v1), pointToStr(v2), pointToStr(v3));
        List<PointValue<string>> intersectRet = Intersector.Instance.Intersection<string>(projection7,
            raster7, vertices7);
        for (int i = 0; i < intersectRet.Count; i++)
            output += String.Format("Point: {0}, Value: {1}\n", 
                pointToStr(intersectRet[i].Point), intersectRet[i].Value);
        output += "\n";

        // Test 8: Occlusion (large scale)
        output += "<b>Test 8</b>\n";
        GameObject view8 = new GameObject();
        view8.name = "ViewFieldTest8";
        ViewVector FOV8 = new ViewVector(170, 170);
        Frustum projection8 = new Frustum(view8.transform, FOV8);
        byte[,] raster8 = new byte[100, 100];
        output += String.Format("Created Frustum: pos: {0}, EA's {1}, FOV: {2}\n",
            pointToStr(projection8.Transform.position), pointToStr(projection8.Transform.eulerAngles),
            viewVectorToStr(projection8.FOV));
        output += String.Format("Created Raster: {0}x{1}\n", raster8.GetLength(0), raster8.GetLength(1));
        // create 'wall' vertices
        double wallSize = 4.0;
        double wallRes = 0.05;
        double wallDistance = 3.0;
        float wallWiggle = 0.001f;
        int wallPixels = (int)(wallSize / wallRes);
        double wallBotLeft = -(wallSize / 2.0);
        int wallValue = 0;
        int wallCount = (int)Math.Pow(wallPixels, 2);
        List<Vector3> vertices = new List<Vector3>();
        for (int i = 0; i < wallPixels; i++)
        {
            for (int j = 0; j < wallPixels; j++)
            {
                float vertX = (float)(i * wallRes + wallBotLeft);
                float vertY = (float)(j * wallRes + wallBotLeft);
                float vertZ = (float)wallDistance;
                Vector3 vert = new Vector3(vertX, vertY, vertZ);
                vertices.Add(wiggleVert(vert, wallWiggle, rand));
            }
        }
        // create 'box' vertices
        float boxSize = 1f;
        Vector3 boxMin = new Vector3(-boxSize / 2, -boxSize / 2, (float)(wallDistance + 1));
        Vector3 boxMax = new Vector3(boxSize / 2, boxSize / 2, (float)(wallDistance + 1 + boxSize));
        int boxCount = 10000;
        for (int i = 0; i < boxCount; i++)
            vertices.Add(randomPoint(boxMin, boxMax, rand));
        // report progress
        output += String.Format("Created wall... ({0},{0}) to ({1},{1}), z: {2}, wiggle: {3}, vertices: {4}\n",
            wallBotLeft, wallBotLeft + wallSize, wallDistance, wallWiggle, wallCount);
        output += String.Format("Created box: {0} to {1}, vertices: {2}\n", 
            pointToStr(boxMin), pointToStr(boxMax), boxCount);
        // run intersection
        List<PointValue<byte>> intersectRet8 = Intersector.Instance.Intersection<byte>(projection8, raster8, vertices);
        int wallCountRet = 0;
        int boxCountRet = 0;
        for (int i = 0; i < intersectRet8.Count; i++)
        {
            Vector3 point = intersectRet8[i].Point;
            if (within(point, boxMin, boxMax))
                boxCountRet++;
            else
                wallCountRet++;
        }
        output += String.Format("Intersection Return... wall vertices: {0}, box vertices: {1}", 
            wallCountRet, boxCountRet);

        return output;
    }

    /// <summary>
    /// Returns presentable string of memory size.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    static string MemToStr(long bytes)
    {
        if (bytes < 1000) // less than 1 kB
            return String.Format("{0} B", bytes);
        if (bytes < 1000 * 1000) // less than 1 MB
            return String.Format("{0} kB", bytes / 1000);
        if (bytes < 1000 * 1000 * 1000) // less than 1 GB
            return String.Format("{0} MB", bytes / (1000 * 1000));
        return String.Format("{0} GB", bytes / (1000 * 1000 * 1000));
    }

    // Returns point coordinates in presentable format.
    static string pointToStr(Vector3 point)
    {
        return String.Format("({0}, {1}, {2})", 
            Math.Round(point.x, 2), Math.Round(point.y, 2), Math.Round(point.z, 2));
    }

    static string viewVectorToStr(ViewVector vector)
    {
        return String.Format("(Theta: {0}, Phi: {1})", vector.Theta, vector.Phi);
    }

    static float randomFloat(float min, float max, System.Random rand)
    {
        double range = max - min;
        double sample = rand.NextDouble();
        double scaled = (sample * range) + min;
        return (float)scaled;
    }

    static Vector3 randomPoint(Vector3 boundsMin, Vector3 boundsMax, System.Random rand)
    {
        return new Vector3(randomFloat(boundsMin.x, boundsMax.x, rand),
            randomFloat(boundsMin.y, boundsMax.y, rand),
            randomFloat(boundsMin.z, boundsMax.z, rand));
    }

    static Vector3[] unitVectorsArray()
    {
        Vector3[] unitVectors = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            Vector3 vec = new Vector3();
            // set x
            if (i == 0 || i == 2 || i == 4 || i == 6)
                vec.x = 1;
            else
                vec.x = -1;
            // set y
            if (i == 0 || i == 1 || i == 4 || i == 5)
                vec.y = 1;
            else
                vec.y = -1;
            // set z
            if (i == 0 || i == 1 || i == 2 || i == 3)
                vec.z = 1;
            else
                vec.z = -1;
            unitVectors[i] = vec;
        }
        return unitVectors;
    }

    static Vector3 wiggleVert(Vector3 vert, float wiggleMag, System.Random rand)
    {
        float wiggleX = randomFloat(-wiggleMag, wiggleMag, rand);
        float wiggleY = randomFloat(-wiggleMag, wiggleMag, rand);
        float wiggleZ = randomFloat(-wiggleMag, wiggleMag, rand);
        return vert + new Vector3(wiggleX, wiggleY, wiggleZ);
    }

    static bool within(Vector3 point, Vector3 min, Vector3 max)
    {
        if (point.x >= min.x && point.y >= min.y && point.z >= min.z)
        {
            if (point.x < max.x && point.y < max.y && point.z < max.z)
                return true;
        }
        return false;
    }
}
