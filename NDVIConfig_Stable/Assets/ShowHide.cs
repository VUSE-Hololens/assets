// ShowHide
// Component for toggling visibility of gameobject
// Mark Scherer, Nov 2018

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowHide : MonoBehaviour {

    public enum Visibility { Shown, Hidden };

    // inspector vars
    public Visibility Default = Visibility.Shown;

    // other vars
    public Visibility State { get; private set; }

	// Use this for initialization
	void Start () {
        State = Default;
        UpdateState();
	}

    public void Toggle()
    {
        if (State == Visibility.Shown)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        State = Visibility.Shown;
        UpdateState();
    }

    public void Hide()
    {
        State = Visibility.Hidden;
        UpdateState();
    }
	
	// Update is called once per frame
	void Update () {
		// nothing to do
	}

    private void UpdateState()
    {
        gameObject.SetActive(State == Visibility.Shown);
    }
}
