// HUDPosition
// used by HUD gameobjects to position themselves on HUD simply
// note: must be immediate HUD children
// Mark Scherer, Nov 2018

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDPosition : MonoBehaviour {

    // inspector vars
    [Tooltip("Positioning on HUD. (0, 0) is bottom left, (1.0, 1.0) is top right.")]
    public Vector2 Position;

	// Use this for initialization
	void Start () {
        Vector2 parentSize = gameObject.transform.parent.gameObject.GetComponent<HUDManager>().Size;
 
        Vector3 coords = new Vector3((float)(Position.x - 0.5) * parentSize.x, (float)(Position.y - 0.5) * parentSize.y, 0);

        gameObject.transform.localPosition = coords;
	}
	
	// Update is called once per frame
	void Update () {
		// nothing to do
	}
}
