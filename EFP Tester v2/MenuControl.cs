/// Menu Control
/// Backbone for executing results of menu button presses.
/// Mark Scherer, June 2018

using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;
using UnityEngine;

/// <summary>
/// Class for executing results of menu button presses.
/// </summary>
public class MenuControl : MonoBehaviour
{
    // inspector variables
    [Tooltip("Button Object for diagnostics toggle")]
    public GameObject DiagButtonContainer;
    [Tooltip("Diagnostics parent GameObject")]
    public GameObject DiagParent;
    [Tooltip("Button Object for mesh vertices toggle")]
    public GameObject VertButtonContainer;
    [Tooltip("Mesh Vertex visualization parent GameObject")]
    public GameObject VertParent;
    [Tooltip("Button Object for mesh bounds toggle")]
    public GameObject BoundsButtonContainer;
    [Tooltip("Diagnostics parent GameObject")]
    public GameObject BoundsParent;
    [Tooltip("Button object for wireframe toggle")]
    public GameObject WireframeButtonContainer;
    [Tooltip("SpatialMappingManager parent GameObject")]
    public GameObject WireframeParent;

    public GameObject OcclusionSlider;
    public GameObject MeshFOVSlider;
    public GameObject EFP;


    // other variables
    HoloToolkit.Unity.Buttons.CompoundButton DiagButton;
    HoloToolkit.Unity.Buttons.CompoundButton VertButton;
    HoloToolkit.Unity.Buttons.CompoundButton BoundsButton;
    HoloToolkit.Unity.Buttons.CompoundButton WireframeButton;

    private HoloToolkit.Examples.InteractiveElements.SliderGestureControl OcSliderGC;
    private HoloToolkit.Examples.InteractiveElements.SliderGestureControl MeshFOVSliderGC;

    private void Start()
    {
        // grab button component
        DiagButton = DiagButtonContainer.GetComponent<HoloToolkit.Unity.Buttons.CompoundButton>();
        VertButton = VertButtonContainer.GetComponent<HoloToolkit.Unity.Buttons.CompoundButton>();
        BoundsButton = BoundsButtonContainer.GetComponent<HoloToolkit.Unity.Buttons.CompoundButton>();
        WireframeButton = WireframeButtonContainer.GetComponent<HoloToolkit.Unity.Buttons.CompoundButton>();

        // declare event handlers
        DiagButton.OnButtonPressed += new System.Action<GameObject>(ToggleDiag);
        VertButton.OnButtonPressed += new System.Action<GameObject>(ToggleVerts);
        BoundsButton.OnButtonPressed += new System.Action<GameObject>(ToggleBounds);
        WireframeButton.OnButtonPressed += new System.Action<GameObject>(ToggleWireframe);

        // set original button labels
        UpdateDiagLabel();
        UpdateVertLabel();
        UpdateBoundsLabel();
        UpdateWireframeLabel();

        // add sliders as listeners
        OcSliderGC = OcclusionSlider.GetComponent<HoloToolkit.Examples.InteractiveElements.SliderGestureControl>();
        OcSliderGC.OnUpdateEvent.AddListener(UpdateOc);
        MeshFOVSliderGC = MeshFOVSlider.GetComponent<HoloToolkit.Examples.InteractiveElements.SliderGestureControl>();
        MeshFOVSliderGC.OnUpdateEvent.AddListener(UpdateMeshFOV);
    }

    /// <summary>
    /// Toggle visibility of diagnostics board.
    /// </summary>
    private void ToggleDiag(GameObject button)
    {
        DiagParent.GetComponent<DiagnosticsControl>().Show = 
            !DiagParent.GetComponent<DiagnosticsControl>().Show;

        UpdateDiagLabel();
    }

    /// <summary>
    /// Updates diagnostic toggle button label to current diagnostics board state.
    /// </summary>
    private void UpdateDiagLabel()
    {
        string On = "Hide Diagnostics";
        string Off = "Show Diagnostics";

        if (DiagParent.GetComponent<DiagnosticsControl>().Show)
            DiagButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = On;
        else
            DiagButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = Off;
    }

    /// <summary>
    /// Toggle visibility of diagnostics board.
    /// </summary>
    private void ToggleVerts(GameObject button)
    {
        VertParent.GetComponent<EFPDriver>().RenderVertices = !VertParent.GetComponent<EFPDriver>().RenderVertices;

        UpdateVertLabel();
    }

    /// <summary>
    /// Updates mesh vertices toggle button label to current state.
    /// </summary>
    private void UpdateVertLabel()
    {
        string On = "Hide Vertices";
        string Off = "Show Vertices";

        if (VertParent.GetComponent<EFPDriver>().RenderVertices)
            VertButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = On;
        else
            VertButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = Off;
    }

    /// <summary>
    /// Toggle visibility of diagnostics board.
    /// </summary>
    private void ToggleBounds(GameObject button)
    {
        BoundsParent.GetComponent<EFPDriver>().MeshMan.VisualizeBounds = 
            !BoundsParent.GetComponent<EFPDriver>().MeshMan.VisualizeBounds;

        UpdateBoundsLabel();
    }

    /// <summary>
    /// Updates mesh bounds toggle button label to current state.
    /// </summary>
    private void UpdateBoundsLabel()
    {
        string On = "Hide Mesh Bounds";
        string Off = "Show Mesh Bounds";

        if (BoundsParent.GetComponent<EFPDriver>().MeshMan.VisualizeBounds)
            BoundsButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = On;
        else
            BoundsButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = Off;
    }

    /// <summary>
    /// Toggles rendering of wireframe mesh material.
    /// </summary>
    private void ToggleWireframe(GameObject button)
    {
        WireframeParent.GetComponent<SpatialMappingManager>().DrawVisualMeshes = 
            !WireframeParent.GetComponent<SpatialMappingManager>().DrawVisualMeshes;

        UpdateWireframeLabel();
    }

    /// <summary>
    /// Updates wireframe toggle button label to current state.
    /// </summary>
    private void UpdateWireframeLabel()
    {
        string On = "Hide Wireframe";
        string Off = "Show Wireframe";

        /*
        if (WireframeParent.GetComponent<SpatialMappingManager>().DrawVisualMeshes)
            WireframeButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = On;
        else
            WireframeButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = Off;
            */

        if (SpatialMappingManager.Instance.DrawVisualMeshes)
            WireframeButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = On;
        else
            WireframeButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = Off;
    }

    /// <summary>
    /// Updates occlusion object size with slider value.
    /// </summary>
    private void UpdateOc(float value)
    {
        EFP.GetComponent<EFPDriver>().OcclusionObjSize = value / 100;
    }

    /// <summary>
    /// Updates mesh visibility FOV factor with slider value.
    /// </summary>
    private void UpdateMeshFOV(float value)
    {
        EFP.GetComponent<EFPDriver>().MeshMan.FOVFactor = value;
    }
}
