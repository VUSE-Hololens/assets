// Visualizes process diagnostics
// Mark Scherer, June 2018

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(VertexDriver))]
public class DiagnosticsDriver : MonoBehaviour
{
    // Inspector variables
    public Material TargetMaterial;
    public float TargetDistance = 1f;
    public float TargetSize = 0.01f;
    public float OcGridSize = 0.005f;
    public Vector3 DiagnosticsPosition = new Vector3(0, -0.5f, 1);
    public GameObject DiagnosticsText;
    public GameObject DiagnosticsBackground;

    // other variables
    private VertexDriver VD;
    private Vector3 TargetMin, TargetMax;
    private float TargetI, TargetJ;
    private GameObject TargetContainer, OcContainer, Diagnostics;
    private List<Vector3[]> TargetLines, OcLines;
    private Intersector Inter;

	
    // Use this for initialization
	void Start () {
        VD = GetComponent<VertexDriver>();
        Inter = Intersector.Instance;

        // create target
        TargetI = (float)(TargetDistance * Math.Tan(DegToRad(VD.TargetFOV.x / 2.0)));
        TargetJ = (float)(TargetDistance * Math.Tan(DegToRad(VD.TargetFOV.y / 2.0)));
        Vector3 TargetBotLeft = new Vector3(-TargetI, -TargetJ, TargetDistance);
        Vector3 TargetBotRight = new Vector3(TargetI, -TargetJ, TargetDistance);
        Vector3 TargetTopLeft = new Vector3(-TargetI, TargetJ, TargetDistance);
        Vector3 TargetTopRight = new Vector3(TargetI, TargetJ, TargetDistance);

        TargetLines = new List<Vector3[]>();
        TargetLines.Add(new Vector3[] { TargetBotLeft, TargetBotRight }); // bottom
        TargetLines.Add(new Vector3[] { TargetBotRight, TargetTopRight }); // right
        TargetLines.Add(new Vector3[] { TargetTopRight, TargetTopLeft }); // top
        TargetLines.Add(new Vector3[] { TargetTopLeft, TargetBotLeft }); // left

        OcLines = new List<Vector3[]>();
        for (int i = 1; i < Inter.OccGridSize.x; i++)
        {
            float offset = i * (2f * Math.Abs(TargetI)) / Inter.OccGridSize.x;
            Vector3 bot = new Vector3(TargetBotLeft.x + offset, TargetBotLeft.y, TargetBotLeft.z);
            Vector3 top = new Vector3(TargetTopLeft.x + offset, TargetTopLeft.y, TargetTopLeft.z);
            OcLines.Add(new Vector3[] { bot, top });
        }
        for (int i = 1; i < Inter.OccGridSize.y; i++)
        {
            float offset = i * (2f * Math.Abs(TargetJ)) / Inter.OccGridSize.y;
            Vector3 left = new Vector3(TargetBotLeft.x, TargetBotLeft.y + offset, TargetBotLeft.z);
            Vector3 right = new Vector3(TargetBotRight.x, TargetBotRight.y + offset, TargetBotRight.z);
            OcLines.Add(new Vector3[] { left, right });
        }

        TargetContainer = new GameObject();
        TargetContainer.name = "TargetContainer";
        for (int i = 0; i < TargetLines.Count; i++)
        {
            Vector3[] line = TargetLines[i];
            VertexDriver.DrawLine(line[0], line[1], TargetContainer, TargetSize, TargetMaterial, i.ToString());
        }
        OcContainer = new GameObject();
        OcContainer.name = "OcContainer";
        for (int i = 0; i < OcLines.Count; i++)
        {
            Vector3[] line = OcLines[i];
            VertexDriver.DrawLine(line[0], line[1], OcContainer, OcGridSize, TargetMaterial, i.ToString());
        }


        // create diagnostics panel
        Diagnostics = new GameObject();
        Diagnostics.transform.position = DiagnosticsPosition;
        DiagnosticsText.transform.parent = Diagnostics.transform;
        DiagnosticsText.transform.position = Diagnostics.transform.TransformPoint(DiagnosticsText.transform.position);
        DiagnosticsBackground.transform.parent = Diagnostics.transform;
        DiagnosticsBackground.transform.position = Diagnostics.transform.
            TransformPoint(DiagnosticsBackground.transform.position);
    }
	
	// Update is called once per frame
	void Update () {
        // update target position
        TargetContainer.transform.position = Camera.main.transform.position;
        TargetContainer.transform.eulerAngles = Camera.main.transform.eulerAngles;
        OcContainer.transform.position = Camera.main.transform.position;
        OcContainer.transform.eulerAngles = Camera.main.transform.eulerAngles;

        for (int i = 0; i < TargetLines.Count; i++)
        {
            LineRenderer LR = TargetContainer.transform.Find(i.ToString()).GetComponent<LineRenderer>();
            LR.SetPosition(0, TargetContainer.transform.TransformPoint(TargetLines[i][0]));
            LR.SetPosition(1, TargetContainer.transform.TransformPoint(TargetLines[i][1]));
        }
        for (int i = 0; i < OcLines.Count; i++)
        {
            LineRenderer LR = OcContainer.transform.Find(i.ToString()).GetComponent<LineRenderer>();
            LR.SetPosition(0, OcContainer.transform.TransformPoint(OcLines[i][0]));
            LR.SetPosition(1, OcContainer.transform.TransformPoint(OcLines[i][1]));
        }

        // control diagnostics text
        String DMessage = String.Format("In View: {0} \t\t Out of View: {1} \t\t Occluded: {2}",
            VD.Inter.InViewCount, VD.Inter.OutViewCount, VD.Inter.OccludedCount);
        DiagnosticsText.GetComponent<TextMesh>().text = DMessage;
    }

    private static double RadToDeg(double rad)
    {
        return rad * (180.0 / Math.PI);
    }

    private static double DegToRad(double deg)
    {
        return deg * (Math.PI / 180.0);
    }
}
