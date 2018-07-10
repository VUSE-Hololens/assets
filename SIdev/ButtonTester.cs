// Simulates button taps for testing within Unity editor
// Mark Scherer, June 2018

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

// simulates button clicks.
public class ButtonTester : MonoBehaviour
{

    public bool Enabled = false;
    // button to click
    public GameObject ButtonGO;
    // clicking interval
    public float Cycle = 1;

    // other variables
    private Stopwatch Watch = new Stopwatch();
    private HoloToolkit.Unity.Buttons.Button ButtonObj;

    // Use this for initialization
    void Start()
    {
        ButtonObj = ButtonGO.GetComponent<HoloToolkit.Unity.Buttons.Button>();
        Watch.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (Enabled && Watch.ElapsedTicks / Stopwatch.Frequency > Cycle)
        {
            UnityEngine.Debug.Log(string.Format("Triggering click: {0}", ButtonGO.name));

            ButtonObj.TriggerClicked();
            Watch.Reset();
            Watch.Start();
        }
    }
}
