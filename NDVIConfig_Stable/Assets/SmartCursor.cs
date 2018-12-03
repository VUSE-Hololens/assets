/// SmartCursor
/// Display for MixedRealityCamera-locked value display
/// Adam Smith, October 2018

using HoloToolkit.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity.InputModule;

public class SmartCursor : MonoBehaviour
{

    // Inspector variables
    public Text InfoDisp;
    public GameObject EFPContainer;
    public GameObject InputManager;

    // dependencies
    private EFPDriver Driver;
    private GazeManager GazeMan;

    // other variables
    private string valString = ""; //string to print to Text UI
    private Vector3 hitPos; //position of hit
    private float collisionVal; //get value in voxel grid associated with position of the collision
    private int nonCollisionLayer = 5; //layer to not to be intersected in a raycast (5 = UI)

    // Use this for initialization
    void Start()
    {
        Driver = EFPContainer.GetComponent<EFPDriver>();
        GazeMan = InputManager.GetComponent<GazeManager>();
        //GazeMan.RaycastLayerMasks = new LayerMask[] { ~nonCollisionLayer };

        hitPos = new Vector3(0.0f, 0.0f, 0.0f); //initialize position of hit
        InfoDisp.text = "";

    }

    // Update is called once per frame
    void Update()
    {

        //position of hit
        hitPos = GazeMan.HitPosition;
        try
        {
            //get value in voxel grid associated with position of the collision
            collisionVal = Driver.VoxGridMan.Get(hitPos) / 255.0f;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log(string.Format("Raycast point: {0}\n throwing exception: {1} ", collisionVal.ToString(), e.ToString()));
        }

        // Format value to 2 decimal places
        valString = string.Format("NDVI: {0:N2}", collisionVal);
        InfoDisp.text = valString;
    }
}

