using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniMapManager : MonoBehaviour {

    // map visibility flag
    private bool MapExists = true;

    // control over map visibility
    public bool ShowMap
    {
        get
        {
            return MapExists;
        }
        set
        {
            if (value != MapExists)
            {
                if (value)
                {
                    ActivateMap();
                    MapExists = true;
                }
                else
                {
                    DeactivateMap();
                    MapExists = false;
                }

            }
        }
    }

    // Use this for initialization
    void Start()
    {
        ActivateMap();

    }

    // Update is called once per frame
    void Update()
    {
        if(!ShowMap)
        {
            UnityEngine.Debug.Log("Deactivating Minimap");
        }
    }

    /// <summary>
    /// Creates contents of minimap as new objects.
    /// </summary>
    private void ActivateMap()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Deletes objects containing contents of minimap.
    /// </summary>
    private void DeactivateMap()
    {
        gameObject.SetActive(false);
    }
}
