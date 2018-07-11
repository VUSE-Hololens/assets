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
    // inspector variables: buttons containers
    [Tooltip("Button Object for diagnostics toggle")]
    public GameObject DiagButtonContainer;
    [Tooltip("Button Object for mesh vertices toggle")]
    public GameObject VertButtonContainer;
    [Tooltip("Button Object for mesh bounds toggle")]
    public GameObject BoundsButtonContainer;
    [Tooltip("Button object for wireframe toggle")]
    public GameObject MaterialButtonContainer;
    // sliders
    public GameObject OcclusionSlider;
    public GameObject MeshDensitySlider;
    public GameObject VoxGridMinSizeSlider;
    public GameObject MeshUpdateTimeSlider;
    // parent objs
    [Tooltip("Diagnostics parent GameObject")]
    public GameObject DiagParent;
    public GameObject EFP;


    // other variables
    private HoloToolkit.Unity.Buttons.CompoundButton DiagButton;
    private HoloToolkit.Unity.Buttons.CompoundButton VertButton;
    private HoloToolkit.Unity.Buttons.CompoundButton BoundsButton;
    private HoloToolkit.Unity.Buttons.CompoundButton MaterialButton;

    private HoloToolkit.Examples.InteractiveElements.SliderGestureControl OcSliderGC;
    private HoloToolkit.Examples.InteractiveElements.SliderGestureControl MeshDensitySliderGC;
    private HoloToolkit.Examples.InteractiveElements.SliderGestureControl VoxGridMinSizeSliderGC;
    private HoloToolkit.Examples.InteractiveElements.SliderGestureControl MeshUpdateTimeSliderGC;

    private void Start()
    {
        // grab button component
        DiagButton = DiagButtonContainer.GetComponent<HoloToolkit.Unity.Buttons.CompoundButton>();
        VertButton = VertButtonContainer.GetComponent<HoloToolkit.Unity.Buttons.CompoundButton>();
        BoundsButton = BoundsButtonContainer.GetComponent<HoloToolkit.Unity.Buttons.CompoundButton>();
        MaterialButton = MaterialButtonContainer.GetComponent<HoloToolkit.Unity.Buttons.CompoundButton>();

        // declare event handlers
        DiagButton.OnButtonPressed += new System.Action<GameObject>(ToggleDiag);
        VertButton.OnButtonPressed += new System.Action<GameObject>(ToggleVerts);
        BoundsButton.OnButtonPressed += new System.Action<GameObject>(ToggleBounds);
        MaterialButton.OnButtonPressed += new System.Action<GameObject>(ToggleMaterial);

        // add sliders as listeners
        OcSliderGC = OcclusionSlider.GetComponent<HoloToolkit.Examples.InteractiveElements.SliderGestureControl>();
        OcSliderGC.OnUpdateEvent.AddListener(UpdateOc);
        MeshDensitySliderGC = MeshDensitySlider.GetComponent<HoloToolkit.Examples.InteractiveElements.SliderGestureControl>();
        MeshDensitySliderGC.OnUpdateEvent.AddListener(UpdateMeshDensity);
        VoxGridMinSizeSliderGC = VoxGridMinSizeSlider.GetComponent<HoloToolkit.Examples.InteractiveElements.SliderGestureControl>();
        VoxGridMinSizeSliderGC.OnUpdateEvent.AddListener(UpdateVoxGrid);
        MeshUpdateTimeSliderGC = MeshUpdateTimeSlider.GetComponent<HoloToolkit.Examples.InteractiveElements.SliderGestureControl>();
        MeshUpdateTimeSliderGC.OnUpdateEvent.AddListener(UpdateMeshUpdateTime);

        // set original button labels
        UpdateDiagLabel();
        UpdateVertLabel();
        UpdateBoundsLabel();
        UpdateMaterialLabel();

        // finish syncing state
        UpdateOc(OcSliderGC.SliderValue);
        UpdateMeshDensity(MeshDensitySliderGC.SliderValue);
        UpdateVoxGrid(VoxGridMinSizeSliderGC.SliderValue);
        UpdateMeshUpdateTime(MeshUpdateTimeSliderGC.SliderValue);
    }

    /// <summary>
    /// Toggle visibility of diagnostics board.
    /// </summary>
    private void ToggleDiag(GameObject button)
    {
        DiagParent.GetComponent<DiagnosticsControl>().ShowBoard = 
            !DiagParent.GetComponent<DiagnosticsControl>().ShowBoard;

        UpdateDiagLabel();
    }

    /// <summary>
    /// Updates diagnostic toggle button label to current diagnostics board state.
    /// </summary>
    private void UpdateDiagLabel()
    {
        string On = "Hide Diagnostics";
        string Off = "Show Diagnostics";

        if (DiagParent.GetComponent<DiagnosticsControl>().ShowBoard)
            DiagButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = On;
        else
            DiagButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = Off;
    }

    /// <summary>
    /// Toggle visibility of diagnostics board.
    /// </summary>
    private void ToggleVerts(GameObject button)
    {
        EFP.GetComponent<EFPDriver>().RenderVerts = !EFP.GetComponent<EFPDriver>().RenderVerts;

        UpdateVertLabel();
    }

    /// <summary>
    /// Updates mesh vertices toggle button label to current state.
    /// </summary>
    private void UpdateVertLabel()
    {
        string On = "Hide Vertices";
        string Off = "Show Vertices";

        if (EFP.GetComponent<EFPDriver>().RenderVerts)
            VertButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = On;
        else
            VertButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = Off;
    }

    /// <summary>
    /// Toggle visibility of diagnostics board.
    /// </summary>
    private void ToggleBounds(GameObject button)
    {
        EFP.GetComponent<EFPDriver>().MeshMan.VB = !EFP.GetComponent<EFPDriver>().MeshMan.VB;

        UpdateBoundsLabel();
    }

    /// <summary>
    /// Updates mesh bounds toggle button label to current state.
    /// </summary>
    private void UpdateBoundsLabel()
    {
        string On = "Hide Mesh Bounds";
        string Off = "Show Mesh Bounds";

        if (EFP.GetComponent<EFPDriver>().MeshMan.VB)
            BoundsButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = On;
        else
            BoundsButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = Off;
    }

    /// <summary>
    /// Toggles material for mesh rendering.
    /// </summary>
    private void ToggleMaterial(GameObject button)
    {
        EFP.GetComponent<EFPDriver>().ColoredMesh = !EFP.GetComponent<EFPDriver>().ColoredMesh;

        UpdateMaterialLabel();
    }

    /// <summary>
    /// Updates wireframe toggle button label to current state.
    /// </summary>
    private void UpdateMaterialLabel()
    {
        string Material1 = "Wireframe";
        string Material2 = "Colored Mesh";

        if (EFP.GetComponent<EFPDriver>().ColoredMesh)
            MaterialButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = Material1;
        else
            MaterialButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = Material2;
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
    private void UpdateMeshDensity(float value)
    {
        EFP.GetComponent<SpatialMappingObserver>().Density = value;
    }

    /// <summary>
    /// Updates voxel grid with new minimum size.
    /// </summary>
    private void UpdateVoxGrid(float value)
    {
        EFP.GetComponent<EFPDriver>().VoxelGridRes = value;
    }

    /// <summary>
    /// Updates mesh refresh cyle time.
    /// </summary>
    private void UpdateMeshUpdateTime(float value)
    {
        EFP.GetComponent<SpatialMappingObserver>().TimeBetweenUpdates = value;
    }
}
