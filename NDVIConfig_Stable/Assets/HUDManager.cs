// component to move HUD t match user's head position
// Mark Scherer, Nov 2018

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDManager : MonoBehaviour {

    // inspector vars
    [Tooltip("Position of HUD in relation to user. Adjusting HUD GameObject's transform will not do anything.")]
    public Vector3 Position = new Vector3(0, 0, 1);
    [Tooltip("Used to adjust size of HUD.")]
    public Vector2 Size = new Vector2(0.577f, 0.344f); 

	// Use this for initialization
	void Start () {
        UpdatePos();
	}
	
	// Update is called once per frame
	void Update () {
        UpdatePos();
	}

    private void UpdatePos()
    {
        Transform headPos = Camera.main.transform;
        gameObject.transform.position = headPos.TransformPoint(Position);
        gameObject.transform.LookAt(headPos);
        gameObject.transform.Rotate(new Vector3(0, 180, 0)); // face away from camera
    }
}
