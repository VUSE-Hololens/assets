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
    public Vector3 DiagnosticsPosition = new Vector3(0, -0.5f, 1);
    public GameObject DiagnosticsText;
    public GameObject DiagnosticsBackground;

    // other variables
    private VertexDriver VD;
    private Vector3 TargetMin, TargetMax;
    private float TargetI, TargetJ;
    private GameObject TargetContainer, Diagnostics;
    private Vector3 TargetBotLeft, TargetBotRight, TargetTopLeft, TargetTopRight;

	
    // Use this for initialization
	void Start () {
        VD = GetComponent<VertexDriver>();

        // create target
        TargetI = (float)(TargetDistance * Math.Tan(DegToRad(VD.TargetFOV.x / 2.0)));
        TargetJ = (float)(TargetDistance * Math.Tan(DegToRad(VD.TargetFOV.y / 2.0)));
        TargetBotLeft = new Vector3(-TargetI, -TargetJ, TargetDistance);
        TargetBotRight = new Vector3(TargetI, -TargetJ, TargetDistance);
        TargetTopLeft = new Vector3(-TargetI, TargetJ, TargetDistance);
        TargetTopRight = new Vector3(TargetI, TargetJ, TargetDistance);
        TargetContainer = new GameObject();
        TargetContainer.name = "TargetContainer";
        VertexDriver.DrawLine(TargetBotLeft, TargetBotRight, TargetContainer, TargetSize, TargetMaterial, "bottom");
        VertexDriver.DrawLine(TargetBotRight, TargetTopRight, TargetContainer, TargetSize, TargetMaterial, "right");
        VertexDriver.DrawLine(TargetTopRight, TargetTopLeft, TargetContainer, TargetSize, TargetMaterial, "top");
        VertexDriver.DrawLine(TargetTopLeft, TargetBotLeft, TargetContainer, TargetSize, TargetMaterial, "left");

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
        // bottom
        LineRenderer bottom = TargetContainer.transform.Find("bottom").GetComponent<LineRenderer>();
        Vector3[] bottomPos = { TargetContainer.transform.TransformPoint(TargetBotLeft),
            TargetContainer.transform.TransformPoint(TargetBotRight) };
        bottom.SetPositions(bottomPos);
        // right
        LineRenderer right = TargetContainer.transform.Find("right").GetComponent<LineRenderer>();
        Vector3[] rightPos = { TargetContainer.transform.TransformPoint(TargetBotRight),
            TargetContainer.transform.TransformPoint(TargetTopRight) };
        right.SetPositions(rightPos);
        // top
        LineRenderer top = TargetContainer.transform.Find("top").GetComponent<LineRenderer>();
        Vector3[] topPos = { TargetContainer.transform.TransformPoint(TargetTopRight),
            TargetContainer.transform.TransformPoint(TargetTopLeft) };
        top.SetPositions(topPos);
        // left
        LineRenderer left = TargetContainer.transform.Find("left").GetComponent<LineRenderer>();
        Vector3[] leftPos = { TargetContainer.transform.TransformPoint(TargetTopLeft),
            TargetContainer.transform.TransformPoint(TargetBotLeft) };
        left.SetPositions(leftPos);

        // control diagnostics text
        String DMessage = String.Format("In View: {0} \t\t Out of View: {1}",
            VD.Inter.InViewCount, VD.Inter.OutViewCount);
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
