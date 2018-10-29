using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Tests slider in unity by forcing motion.
/// </summary>
public class SliderTester : MonoBehaviour
{

    // inspector variables
    public bool Enabled = false;
    public GameObject Slider;
    public float Cycle = 2f;

    // other vars
    private HoloToolkit.Examples.InteractiveElements.SliderGestureControl SliderGC;
    private Stopwatch Watch = new Stopwatch();

    // Use this for initialization
    void Start()
    {
        SliderGC = Slider.GetComponent<HoloToolkit.Examples.InteractiveElements.SliderGestureControl>();

        Watch.Start();
    }

    // Update is called once per frame
    void Update()
    {
        float time = (float)Watch.ElapsedTicks / (float)Stopwatch.Frequency;
        float value = SliderGC.MinSliderValue + (time / Cycle) * (SliderGC.MaxSliderValue - SliderGC.MinSliderValue);

        if (Enabled)
            SliderGC.SetSliderValue(value);

        if (time > Cycle)
        {
            Watch.Reset();
            Watch.Start();
        }
    }
}
