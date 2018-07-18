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
    enum BoundsState { off, mesh, voxels };
    
    // inspector variables: buttons containers
    [Tooltip("Button Object for diagnostics toggle")]
    public GameObject DiagButtonContainer;
    [Tooltip("Button Object for mesh vertices toggle")]
    public GameObject VertButtonContainer;
    [Tooltip("Button Object for mesh bounds toggle")]
    public GameObject BoundsButtonContainer;
    [Tooltip("Button object for wireframe toggle")]
    public GameObject MaterialButtonContainer;
    [Tooltip("Button object for live data only toggle")]
    public GameObject FilterButtonContainer;
    // sliders
    public GameObject OcclusionSlider;
    public GameObject MeshDensitySlider;
    public GameObject VoxGridMinSizeSlider;
    public GameObject MeshUpdateTimeSlider;
    public GameObject MinValSlider;
    public GameObject MaxValSlider;
    public GameObject ShowFracSlider;
    // parent objs
    [Tooltip("Diagnostics parent GameObject")]
    public GameObject DiagParent;
    public GameObject EFP;


    // other variables
    private HoloToolkit.Unity.Buttons.CompoundButton DiagButton;
    private HoloToolkit.Unity.Buttons.CompoundButton VertButton;
    private HoloToolkit.Unity.Buttons.CompoundButton BoundsButton;
    private HoloToolkit.Unity.Buttons.CompoundButton MaterialButton;
    private HoloToolkit.Unity.Buttons.CompoundButton FilterButton;

    private HoloToolkit.Examples.InteractiveElements.SliderGestureControl OcSliderGC;
    private HoloToolkit.Examples.InteractiveElements.SliderGestureControl MeshDensitySliderGC;
    private HoloToolkit.Examples.InteractiveElements.SliderGestureControl VoxGridMinSizeSliderGC;
    private HoloToolkit.Examples.InteractiveElements.SliderGestureControl MeshUpdateTimeSliderGC;
    private HoloToolkit.Examples.InteractiveElements.SliderGestureControl MinValSliderGC;
    private HoloToolkit.Examples.InteractiveElements.SliderGestureControl MaxValSliderGC;
    private HoloToolkit.Examples.InteractiveElements.SliderGestureControl ShowFracSliderGC;

    private BoundsState BState = BoundsState.off;
    private DataFilter FilterState = DataFilter.mesh;

    private void Start()
    {
        // grab button component
        DiagButton = DiagButtonContainer.GetComponent<HoloToolkit.Unity.Buttons.CompoundButton>();
        VertButton = VertButtonContainer.GetComponent<HoloToolkit.Unity.Buttons.CompoundButton>();
        BoundsButton = BoundsButtonContainer.GetComponent<HoloToolkit.Unity.Buttons.CompoundButton>();
        MaterialButton = MaterialButtonContainer.GetComponent<HoloToolkit.Unity.Buttons.CompoundButton>();
        FilterButton = FilterButtonContainer.GetComponent<HoloToolkit.Unity.Buttons.CompoundButton>();

        // declare event handlers
        DiagButton.OnButtonPressed += new System.Action<GameObject>(ToggleDiag);
        VertButton.OnButtonPressed += new System.Action<GameObject>(ToggleVerts);
        BoundsButton.OnButtonPressed += new System.Action<GameObject>(ToggleBounds);
        MaterialButton.OnButtonPressed += new System.Action<GameObject>(ToggleMaterial);
        FilterButton.OnButtonPressed += new System.Action<GameObject>(ToggleFilter);

        // add sliders as listeners
        OcSliderGC = OcclusionSlider.GetComponent<HoloToolkit.Examples.InteractiveElements.SliderGestureControl>();
        OcSliderGC.OnUpdateEvent.AddListener(UpdateOc);
        MeshDensitySliderGC = MeshDensitySlider.GetComponent<HoloToolkit.Examples.InteractiveElements.SliderGestureControl>();
        MeshDensitySliderGC.OnUpdateEvent.AddListener(UpdateMeshDensity);
        VoxGridMinSizeSliderGC = VoxGridMinSizeSlider.GetComponent<HoloToolkit.Examples.InteractiveElements.SliderGestureControl>();
        VoxGridMinSizeSliderGC.OnUpdateEvent.AddListener(UpdateVoxGrid);
        MeshUpdateTimeSliderGC = MeshUpdateTimeSlider.GetComponent<HoloToolkit.Examples.InteractiveElements.SliderGestureControl>();
        MeshUpdateTimeSliderGC.OnUpdateEvent.AddListener(UpdateMeshUpdateTime);
        MinValSliderGC = MinValSlider.GetComponent<HoloToolkit.Examples.InteractiveElements.SliderGestureControl>();
        MinValSliderGC.OnUpdateEvent.AddListener(UpdateMinVal);
        MaxValSliderGC = MaxValSlider.GetComponent<HoloToolkit.Examples.InteractiveElements.SliderGestureControl>();
        MaxValSliderGC.OnUpdateEvent.AddListener(UpdateMaxVal);
        ShowFracSliderGC = ShowFracSlider.GetComponent<HoloToolkit.Examples.InteractiveElements.SliderGestureControl>();
        ShowFracSliderGC.OnUpdateEvent.AddListener(UpdateShowFrac);

        // set original button labels
        UpdateDiagLabel();
        UpdateVertLabel();
        UpdateBoundsLabel();
        UpdateMaterialLabel();
        UpdateFilterLabel();

        // finish syncing state
        UpdateMeshDensity(MeshDensitySliderGC.SliderValue); // also updates oc, voxel res
        UpdateMeshUpdateTime(MeshUpdateTimeSliderGC.SliderValue);
        UpdateMinVal(MinValSliderGC.SliderValue);
        UpdateMaxVal(MaxValSliderGC.SliderValue);
        UpdateShowFrac(ShowFracSliderGC.SliderValue);
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
        if (BState == BoundsState.off)
        {
            BState = BoundsState.mesh;
            EFP.GetComponent<EFPDriver>().MeshBoundsVis = true;
        } else if (BState == BoundsState.mesh)
        {
            BState = BoundsState.voxels;
            EFP.GetComponent<EFPDriver>().MeshBoundsVis = false;
            EFP.GetComponent<EFPDriver>().VoxVis = true;
        } else
        {
            BState = BoundsState.off;
            EFP.GetComponent<EFPDriver>().VoxVis = false;
        }

        UpdateBoundsLabel();
    }

    /// <summary>
    /// Updates mesh bounds toggle button label to current state.
    /// </summary>
    private void UpdateBoundsLabel()
    {
        string Off = "Show Mesh Bounds";
        string Mesh = "Show Voxel Bounds";
        string Voxels = "Hide Voxel Bounds"; 

        if (BState == BoundsState.off)
            BoundsButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = Off;
        else if (BState == BoundsState.mesh)
            BoundsButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = Mesh;
        else
            BoundsButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = Voxels;
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

    private void ToggleFilter(GameObject button)
    {
        if (FilterState == DataFilter.mesh)
            FilterState = DataFilter.live;
        else if (FilterState == DataFilter.live)
            FilterState = DataFilter.all;
        else
            FilterState = DataFilter.mesh;

        EFP.GetComponent<EFPDriver>().VoxGridFilter = FilterState;

        UpdateFilterLabel();
    }

    /// <summary>
    /// Updates wireframe toggle button label to current state.
    /// </summary>
    private void UpdateFilterLabel()
    {
        string Mesh = "Show Live Data";
        string Live = "Show All Data";
        string All = "Show Mesh Data";

        if (FilterState == DataFilter.mesh)
            FilterButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = Mesh;
        else if (FilterState == DataFilter.live)
            FilterButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = Live;
        else
            FilterButtonContainer.transform.Find("Text").GetComponent<TextMesh>().text = All;
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

        // adjust Ocgrid, voxel density to match
        OcSliderGC.SetSliderValue(AutoOcRes(value)*100);
        VoxGridMinSizeSliderGC.SetSliderValue(AutoVoxRes(OcSliderGC.SliderValue));
    }

    /// <summary>
    /// Updates voxel grid with new minimum size.
    /// </summary>
    private void UpdateVoxGrid(float value)
    {
        EFP.GetComponent<EFPDriver>().VoxelGridRes = value / 100;
    }

    /// <summary>
    /// Updates mesh refresh cyle time.
    /// </summary>
    private void UpdateMeshUpdateTime(float value)
    {
        EFP.GetComponent<SpatialMappingObserver>().TimeBetweenUpdates = value;
    }

    /// <summary>
    /// Updates value rendered as MinColor
    /// </summary>
    private void UpdateMinVal(float value)
    {
        EFP.GetComponent<EFPDriver>().MinColorVal = (byte)value;
    }

    /// <summary>
    /// Updates value rendered as MaxColor
    /// </summary>
    private void UpdateMaxVal(float value)
    {
        EFP.GetComponent<EFPDriver>().MaxColorVal = (byte)value;
    }

    /// <summary>
    /// Updates EFP's show fraction for rendering mesh and voxel bounding boxes
    /// </summary>
    private void UpdateShowFrac(float value)
    {
        EFP.GetComponent<EFPDriver>().ShowFrac = value;
    }

    /// <summary>
    /// Calculates appropriate Occlusion grid resolution (m/vertex) from mesh resolution (triangles/m^3)
    /// </summary>
    private float AutoOcRes(float MeshRes)
    {
        // Occlusion Resolution is the minimum distance apart of two adjacent mesh vertices.
        // This can be approimated by finding the avg radius of a sphere of the space contained by one triangle (m/triangle)
        return (float)System.Math.Pow(MeshRes, -1.0 / 3.0);
    }

    /// <summary>
    /// Calculates appropriate voxel grid resolution (m) from occlusion grid resolution (m/vertex)
    /// </summary>
    private float AutoVoxRes(float OcRes)
    {
        // Voxel grid res must be set to avoid dead spots: visible rendered vertices in voxels not updated 
            // because they do not include a 'non-occluded' vertex.
        // Each Oc cell with any visible rendered vertices will contain a 'non-occluded' vertex.
        // Furthest apart pair of 'non-occluded' vertices in adjacent oc cells can be is OcRes*sqrt(5) (via geometry)
        // To prevent dead spots, VoxRes >= sqrt(5)*OcRes
        return (float)System.Math.Sqrt(5) * OcRes;
    }
}
