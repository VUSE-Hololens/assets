/// Visualizer
/// Class used to visualize Intersector Tests...

/// NOTE: Euler angle rotation not currently functional.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visualizer : MonoBehaviour {

    private GameObject WorldTransform;
    private List<Vector3> WorldVectors;

    private GameObject LocalTransform;
    private List<Vector3> LocalVectors;

    // Use this for initialization
    void Start () {
        Vector3[] unitVectors = unitVectorsArray();

        WorldTransform = new GameObject();
        WorldTransform.name = "WorldTransform";
        WorldTransform.transform.position = new Vector3(1f, -0.75f, 3);
        WorldTransform.transform.eulerAngles = new Vector3(0, 0, 0);
        
        LocalTransform = new GameObject();
        LocalTransform.name = "LocalTransform";
        LocalTransform.transform.position = new Vector3(2f, -0.75f, 3);
        LocalTransform.transform.eulerAngles = new Vector3(90, 90, 0);
        LocalVectors = new List<Vector3>();

        // Draw World System
        CS(WorldTransform, unitVectors);

        // Draw transformed Local System
        CS(LocalTransform, unitVectors);
    }
	
	// Update is called once per frame
	void Update () {
		// nothing to do
	}

    // Draws gizmos. Called once per frame.
    void OnDrawGizmos()
    {
        // nothing to do
    }

    /// <summary>
    /// Draws coordinate system visualization with transformation define via trans.
    /// Draws all vectors (in World Space) as origin vectors.
    /// </summary>
    static void CS(GameObject trans, Vector3[] vectors)
    {
        float length = 0.25f;
        float width = 0.05f;
        float vecLength = 0.15f;

        Vector3 origin = new Vector3(0, 0, 0);
        Material red = Resources.Load("HoloToolkit/UX/Materials/MRTK_Standard_Red.mat", typeof(Material)) as Material;
        Material blue = Resources.Load("HoloToolkit/UX/Materials/MRTK_Standard_Blue.mat", typeof(Material)) as Material;
        Material green = Resources.Load("HoloToolkit/UX/Materials/MRTK_Standard_Green.mat", typeof(Material)) as Material;
        Material white = Resources.Load("HoloToolkit/UX/Materials/MRTK_Standard_White.mat", typeof(Material)) as Material;

        /// draw x axis
        GameObject xAxis = new GameObject();
        xAxis.name = "xAxis";
        xAxis.transform.parent = trans.transform;
        xAxis.AddComponent(typeof(LineRenderer));
        LineRenderer xLine = xAxis.GetComponents<LineRenderer>()[0];
        xLine.startWidth = 1f;
        xLine.endWidth = 0.5f;
        xLine.widthMultiplier = width;
        xLine.material = red;
        xAxis.GetComponent<Renderer>().material.color = Color.red;
        Vector3 xStart = trans.transform.TransformPoint(origin);
        Vector3 xEnd = trans.transform.TransformPoint(origin + new Vector3(length, 0, 0));
        Vector3[] xPoints = new Vector3[] { xStart, xEnd };
        xLine.SetPositions(xPoints);

         /// draw y axis
        GameObject yAxis = new GameObject();
        yAxis.name = "yAxis";
        yAxis.transform.parent = trans.transform;
        yAxis.AddComponent(typeof(LineRenderer));
        LineRenderer yLine = yAxis.GetComponents<LineRenderer>()[0];
        yLine.startWidth = 1f;
        yLine.endWidth = 0.5f;
        yLine.widthMultiplier = width;
        yLine.material = blue;
        yAxis.GetComponent<Renderer>().material.color = Color.blue;
        Vector3 yStart = trans.transform.TransformPoint(origin);
        Vector3 yEnd = trans.transform.TransformPoint(origin + new Vector3(0, length, 0));
        Vector3[] yPoints = new Vector3[] { yStart, yEnd };
        yLine.SetPositions(yPoints);

        /// draw z axis
        GameObject zAxis = new GameObject();
        zAxis.name = "zAxis";
        zAxis.transform.parent = trans.transform;
        zAxis.AddComponent(typeof(LineRenderer));
        LineRenderer zLine = zAxis.GetComponents<LineRenderer>()[0];
        zLine.startWidth = 1f;
        zLine.endWidth = 0.5f;
        zLine.widthMultiplier = width;
        zLine.material = green;
        zAxis.GetComponent<Renderer>().material.color = Color.green;
        Vector3 zStart = trans.transform.TransformPoint(origin);
        Vector3 zEnd = trans.transform.TransformPoint(origin + new Vector3(0, 0, length));
        Vector3[] zPoints = new Vector3[] { zStart, zEnd };
        zLine.SetPositions(zPoints);

        // Add Vectors
        Vector3 localOrigin = trans.transform.position;
        for (int i = 0; i < vectors.Length; i++)
        {
            Vector3 vec = vectors[i];
            Vector3 vecEnd = localOrigin + vecLength * vec;
            /// draw vector
            GameObject vector = new GameObject();
            vector.name = "vector";
            vector.transform.parent = trans.transform;
            vector.AddComponent(typeof(LineRenderer));
            LineRenderer vecLine = vector.GetComponents<LineRenderer>()[0];
            vecLine.startWidth = 1f;
            vecLine.endWidth = 0f;
            vecLine.widthMultiplier = width;
            vecLine.material = white;
            vector.GetComponent<Renderer>().material.color = Color.white;
            Vector3[] vecPoints = new Vector3[] { trans.transform.position, vecEnd };
            vecLine.SetPositions(vecPoints);
        }
    }

    /// <summary>
    /// Returns point rotated with eulerAngles. Rotates ZXY order.
    /// </summary>
    static Vector3 RotatePoint(Vector3 point, Vector3 eulerAngles)
    {
        Vector3 rotated = RotateZ(point, eulerAngles.z);
        rotated = RotateX(rotated, eulerAngles.x);
        rotated = RotateY(rotated, eulerAngles.y);
        return rotated;
    }

    /// <summary>
    /// returns point after rotating dTheta (deg) around Z axis
    /// </summary>
    static Vector3 RotateZ(Vector3 point, double dTheta)
    {
        if (point.x != 0 || point.y != 0)
        {
            dTheta = DegToRad(dTheta);
            double mag = Mathf.Sqrt(Mathf.Pow(point.x, 2) + Mathf.Pow(point.y, 2));
            double Theta1;
            if (point.y > 0)
                Theta1 = Math.Acos(point.x / mag);
            else
                Theta1 = Math.PI - Math.Acos(point.x / mag);
            float Theta2 = (float)(Theta1 + dTheta);
            float x2 = (float)(mag * Mathf.Cos(Theta2));
            float y2 = (float)(mag * Mathf.Sin(Theta2));
            return new Vector3(x2, y2, point.z);
        }
        return point;
    }

    /// <summary>
    /// returns point after rotating dTheta (deg) around Y axis
    /// </summary>
    static Vector3 RotateY(Vector3 point, double dTheta)
    {
        if (point.x != 0 || point.z != 0)
        {
            dTheta = DegToRad(dTheta);
            double mag = Mathf.Sqrt(Mathf.Pow(point.x, 2) + Mathf.Pow(point.z, 2));
            double Theta1;
            if (point.z > 0)
                Theta1 = Math.Acos(point.x / mag);
            else
                Theta1 = Math.PI - Math.Acos(point.x / mag);
            float Theta2 = (float)(Theta1 + dTheta);
            float x2 = (float)(mag * Mathf.Cos(Theta2));
            float z2 = (float)(mag * Mathf.Sin(Theta2));
            return new Vector3(x2, point.y, z2);
        }
        return point;
    }

    /// <summary>
    /// returns point after rotating dTheta (deg) around X axis
    /// </summary>
    static Vector3 RotateX(Vector3 point, double dTheta)
    {
        if (point.y != 0 || point.z != 0)
        {
            dTheta = DegToRad(dTheta);
            double mag = Mathf.Sqrt(Mathf.Pow(point.y, 2) + Mathf.Pow(point.z, 2));
            double Theta1;
            if (point.y > 0)
                Theta1 = Math.Acos(point.z / mag);
            else
                Theta1 = Math.PI - Math.Acos(point.z / mag);
            float Theta2 = (float)(Theta1 + dTheta);
            float z2 = (float)(mag * Mathf.Cos(Theta2));
            float y2 = (float)(mag * Mathf.Sin(Theta2));
            return new Vector3(point.x, y2, z2);
        }
        return point;
    }

    static double DegToRad(double deg)
    {
        return deg * Mathf.PI / 180.0;
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
}
