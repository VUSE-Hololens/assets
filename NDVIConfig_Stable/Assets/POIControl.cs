using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System.Text;
using System;
#if WINDOWS_UWP
using Windows.Storage;
#endif

/// <summary>
/// POI data struct used to hold user determined points of interest
/// </summary>
public struct POI
{
    public Vector3 point { get; private set; }
    public string label { get; private set; }

    public POI(Vector3 myPoint, string myLabel)
    {
        point = myPoint;
        label = myLabel;
    }
}

// manages placement, export of POI markers
// should belong to container gameobject that will be parent of POI gameobjects
public class POIControl : MonoBehaviour
{
    //Inspector Variables
    [Tooltip("Prefab for Point Marker")]
    public GameObject POIPrefab;

    //Dependencies
    private EFPDriver Driver;

    //Other Vars
    private int POIcount = 0;

    // Use this for initialization
    void Start()
    {
        // nothing to do
    }


    // Update is called once per frame
    void Update()
    {
        // nothing to do
    }

    /// <summary>
    /// Places POI Marker
    /// </summary>
    public void PlaceMarker()
    {
        POIcount++;
        GameObject marker = Instantiate(POIPrefab, gameObject.transform);
        marker.name = string.Format("POI_{0}", POIcount);
    }

    public void ExportPOIs()
    {
#if WINDOWS_UWP
        RunExportPOIs();
#endif
    }

#if WINDOWS_UWP
    async private void RunExportPOIs()
    {
        // get folder for export
        var pictureLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
        string folderName = pictureLibrary.SaveFolder.Path;
        //Debug.Log(String.Format("folderName creation success, folderName: {0}", folderName));

        //assemble POI list
        List<POI> POIs = new List<POI>();
        foreach (Transform child in transform)
            POIs.Add(new POI(child.gameObject.transform.position, child.name));
        // String to hold the output for the voxel grid files
        StringBuilder exportOut = new StringBuilder();

        // path for export
        string fileName = "POI_" + DateTime.Now.ToString("yyyy-MM-dd HH_mm") + ".csv";
        string pathString = System.IO.Path.Combine(folderName, fileName);

        // Add the POI's to the .csv
        exportOut.Append("Label,X,Y,Z\n");
        foreach (POI p in POIs)
            exportOut.AppendFormat("{0},{1},{2},{3}\n", p.label, p.point.x, p.point.y, p.point.z);

        CreateCSV(pathString, exportOut);
    }
#endif

    private Boolean CreateCSV(string pathString, StringBuilder exportOut)
    {
        //create POI file
        try
        {
            //Debug.Log(String.Format("Exporting POI Success... File Path: {0}", pathString));
            //Check not to overwrite
            if (!System.IO.File.Exists(pathString))
            {
                //create file
                using (System.IO.StreamWriter sw = System.IO.File.CreateText(pathString))
                {
                    sw.WriteLine(exportOut);
                }
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.Log(String.Format("Exception exporting POI... File Path: {0}", pathString));
            return false;
        }
    }
}
