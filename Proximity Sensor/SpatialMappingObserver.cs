// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_WSA
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR.WSA;
#else
using UnityEngine.VR.WSA;
#endif
#endif

namespace HoloToolkit.Unity.SpatialMapping
{
    /// <summary>
    /// Spatial Mapping Observer states.
    /// </summary>
    public enum ObserverStates
    {
        /// <summary>
        /// The SurfaceObserver is currently running.
        /// </summary>
        Running = 0,

        /// <summary>
        /// The SurfaceObserver is currently idle.
        /// </summary>
        Stopped = 1
    }

    /// <summary>
    /// Spatial Mapping Volume Type
    /// </summary>
    public enum ObserverVolumeTypes
    {
        /// <summary>
        /// The observed volume is an axis aligned box.
        /// </summary>
        AxisAlignedBox = 0,

        /// <summary>
        /// The observed volume is an oriented box.
        /// </summary>
        OrientedBox = 1,

        /// <summary>
        /// The observed volume is a sphere.
        /// </summary>
        Sphere = 2
    }

    /// <summary>
    /// The SpatialMappingObserver class encapsulates the SurfaceObserver into an easy to use
    /// object that handles managing the observed surfaces and the rendering of surface geometry.
    /// </summary>
    public class SpatialMappingObserver : SpatialMappingSource
    {
        // inspector vars
        [Tooltip("Include dummy mesh for testing in Unity Editor?")]
        public bool Test = false;

        [Tooltip("The number of triangles to calculate per cubic meter.")]
        public float DefaultTrianglesPerCubicMeter = 500f;

        [Tooltip("How long to wait (in sec) between Spatial Mapping updates.")]
        public float TimeBetweenUpdates = 3.5f;

        /// <summary>
        /// Indicates the current state of the Surface Observer.
        /// </summary>
        public ObserverStates ObserverState { get; private set; }

        /// <summary>
        /// Indicates the current type of the observed volume
        /// </summary>
        [SerializeField]
        [Tooltip("The shape of the observation volume.")]
        private ObserverVolumeTypes observerVolumeType = ObserverVolumeTypes.AxisAlignedBox;
        public ObserverVolumeTypes ObserverVolumeType
        {
            get
            {
                return observerVolumeType;
            }
            set
            {
                if (observerVolumeType != value)
                {
                    observerVolumeType = value;
                    SwitchObservedVolume();
                }
            }
        }

#if UNITY_WSA
        /// <summary>
        /// Our Surface Observer object for generating/updating Spatial Mapping data.
        /// </summary>
        private SurfaceObserver observer;

        /// <summary>
        /// A queue of surfaces that need their meshes created (or updated).
        /// </summary>
        private readonly Queue<SurfaceId> surfaceWorkQueue = new Queue<SurfaceId>();

        /// <summary>
        /// To prevent too many meshes from being generated at the same time, we will
        /// only request one mesh to be created at a time.  This variable will track
        /// if a mesh creation request is in flight.
        /// </summary>
        private SurfaceObject? outstandingMeshRequest = null;
#endif

        /// <summary>
        /// When surfaces are replaced or removed, rather than destroying them, we'll keep
        /// one as a spare for use in outstanding mesh requests. That way, we'll have fewer
        /// game object create/destroy cycles, which should help performance.
        /// </summary>
        private SurfaceObject? spareSurfaceObject = null;

        /// <summary>
        /// Used to track when the Observer was last updated.
        /// </summary>
        private float updateTime;

        [SerializeField]
        [Tooltip("The extents of the observation volume.")]
        private Vector3 extents = Vector3.one * 10.0f;
        public Vector3 Extents
        {
            get
            {
                return extents;
            }
            set
            {
                if (extents != value)
                {
                    extents = value;
                    SwitchObservedVolume();
                }
            }
        }

        /// <summary>
        /// The origin of the observation volume.
        /// </summary>
        [SerializeField]
        [Tooltip("The origin of the observation volume.")]
        private Vector3 origin = Vector3.zero;
        public Vector3 Origin
        {
            get
            {
                return origin;
            }
            set
            {
                if (origin != value)
                {
                    origin = value;
                    SwitchObservedVolume();
                }
            }
        }

        /// <summary>
        /// The direction of the observed volume, if an oriented box is chosen.
        /// </summary>
        [SerializeField]
        [Tooltip("The direction of the observation volume.")]
        private Quaternion orientation = Quaternion.identity;
        public Quaternion Orientation
        {
            get
            {
                return orientation;
            }
            set
            {
                if (orientation != value)
                {
                    orientation = value;
                    // Only needs to be changed if the corresponding mode is active.
                    if (ObserverVolumeType == ObserverVolumeTypes.OrientedBox)
                    {
                        SwitchObservedVolume();
                    }
                }
            }
        }

        // Density control
        private float density;
        private bool needRebake = false;
        public float Density
        {
            get { return density; }
            set
            {
                if (value != density)
                {
                    needRebake = true;
                    density = value;
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();

            ObserverState = ObserverStates.Stopped;
            density = DefaultTrianglesPerCubicMeter;

            // debug
            StartObserving();

            if (Test)
            {
                // create dummy SurfaceObject
                List<Vector3> testVerts = new List<Vector3>();
                testVerts.Add(new Vector3(-0.5f, -0.5f, -0.5f));
                testVerts.Add(new Vector3(0, 0, 0));
                testVerts.Add(new Vector3(0.5f, 0.5f, 0.5f));
                Mesh testMesh = new Mesh();
                testMesh.SetVertices(testVerts);
                SurfaceObject testSO1 = CreateSurfaceObject(testMesh, "test", gameObject.transform, 0);
                testSO1.Renderer.transform.position = new Vector3(1, 1, 1);
                SurfaceObject testSO2 = CreateSurfaceObject(testMesh, "test", gameObject.transform, 1);
                testSO2.Renderer.transform.position = new Vector3(-1, -1, -1);

                // create matching dummy SurfacePoints: 1
                Transform tmpTransform1 = testSO1.Renderer.transform;
                List<Vector3> tmpWvertices1 = testSO1.Filter.sharedMesh.vertices.ToList();
                int tmpTriCount1 = testSO1.Filter.sharedMesh.triangles.ToList().Count;
                for (int i = 0; i < tmpWvertices1.Count; i++)
                    tmpWvertices1[i] = tmpTransform1.TransformPoint(tmpWvertices1[i]);
                SurfacePoints extras1 = new SurfacePoints(
                    MeshManager.IntersectionPoints(testSO1.Renderer.bounds), tmpWvertices1,
                    tmpTriCount1, testSO1.Renderer.bounds, Density);
                // create matching dummy SurfacePoints: 2
                Transform tmpTransform2 = testSO2.Renderer.transform;
                List<Vector3> tmpWvertices2 = testSO2.Filter.sharedMesh.vertices.ToList();
                int tmpTriCount2 = testSO2.Filter.sharedMesh.triangles.ToList().Count;
                for (int i = 0; i < tmpWvertices2.Count; i++)
                    tmpWvertices2[i] = tmpTransform2.TransformPoint(tmpWvertices2[i]);
                SurfacePoints extras2 = new SurfacePoints(
                    MeshManager.IntersectionPoints(testSO2.Renderer.bounds), tmpWvertices2,
                    tmpTriCount1, testSO2.Renderer.bounds, Density);

                // Log
                UpdateOrAddSurfaceObject(testSO1, extras1);
                UpdateOrAddSurfaceObject(testSO2, extras2);
            }
        }

#if UNITY_WSA
        /// <summary>
        /// Called once per frame.
        /// </summary>
        private void Update()
        {
            if ((ObserverState == ObserverStates.Running) && (outstandingMeshRequest == null))
            {
                if (needRebake)
                    CallForRebake(density);

                if (surfaceWorkQueue.Count > 0)
                {
                    // We're using a simple first-in-first-out rule for requesting meshes, but a more sophisticated algorithm could prioritize
                    // the queue based on distance to the user or some other metric.
                    SurfaceId surfaceID = surfaceWorkQueue.Dequeue();

                    string surfaceName = ("Surface-" + surfaceID.handle);

                    SurfaceObject newSurface;
                    WorldAnchor worldAnchor;

                    if (spareSurfaceObject == null)
                    {
                        newSurface = CreateSurfaceObject(
                            mesh: null,
                            objectName: surfaceName,
                            parentObject: transform,
                            meshID: surfaceID.handle,
                            drawVisualMeshesOverride: false
                            );

                        worldAnchor = newSurface.Object.AddComponent<WorldAnchor>();
                    }
                    else
                    {
                        newSurface = spareSurfaceObject.Value;
                        spareSurfaceObject = null;

                        Debug.Assert(!newSurface.Object.activeSelf);
                        newSurface.Object.SetActive(true);

                        Debug.Assert(newSurface.Filter.sharedMesh == null);
                        Debug.Assert(newSurface.Collider.sharedMesh == null);
                        newSurface.Object.name = surfaceName;
                        Debug.Assert(newSurface.Object.transform.parent == transform);
                        newSurface.ID = surfaceID.handle;
                        newSurface.Renderer.enabled = false;

                        worldAnchor = newSurface.Object.GetComponent<WorldAnchor>();
                        Debug.Assert(worldAnchor != null);
                    }

                    // Debug
                    Debug.Log(string.Format("Requesting baking of Mesh {0} at Density {1}", surfaceID, Density));

                    var surfaceData = new SurfaceData(
                        surfaceID,
                        newSurface.Filter,
                        worldAnchor,
                        newSurface.Collider,
                        Density,
                        _bakeCollider: true
                        );

                    if (observer.RequestMeshAsync(surfaceData, SurfaceObserver_OnDataReady))
                    {
                        outstandingMeshRequest = newSurface;
                    }
                    else
                    {
                        Debug.LogErrorFormat("Mesh request for failed. Is {0} a valid Surface ID?", surfaceID.handle);

                        Debug.Assert(outstandingMeshRequest == null);
                        ReclaimSurface(newSurface);
                    }
                }
                else if ((Time.unscaledTime - updateTime) >= TimeBetweenUpdates)
                {
                    observer.Update(SurfaceObserver_OnSurfaceChanged);
                    updateTime = Time.unscaledTime;
                }
            }
        }
#endif

        /// <summary>
        /// Starts the Surface Observer.
        /// </summary>
        public void StartObserving()
        {
#if UNITY_WSA
            if (observer == null)
            {
                observer = new SurfaceObserver();
                SwitchObservedVolume();
            }

            if (ObserverState != ObserverStates.Running)
            {
                Debug.Log("Starting the observer.");
                ObserverState = ObserverStates.Running;

                // We want the first update immediately.
                updateTime = -TimeBetweenUpdates;
            }
#endif
        }

        /// <summary>
        /// Stops the Surface Observer.
        /// </summary>
        /// <remarks>Sets the Surface Observer state to ObserverStates.Stopped.</remarks>
        public void StopObserving()
        {
#if UNITY_WSA
            if (ObserverState == ObserverStates.Running)
            {
                Debug.Log("Stopping the observer.");
                ObserverState = ObserverStates.Stopped;

                surfaceWorkQueue.Clear();
                updateTime = 0;
            }
#endif
        }

        /// <summary>
        /// pauses observer.
        public void PauseObserver()
        {
#if UNITY_WSA
            if (ObserverState == ObserverStates.Running)
            {
                Debug.Log("pausing the observer.");
                ObserverState = ObserverStates.Stopped;
            }
#endif
        }

        /// <summary>
        /// resumes observer.
        public void ResumeObserver()
        {
#if UNITY_WSA
            if (ObserverState == ObserverStates.Stopped)
            {
                Debug.Log("resuming the observer.");
                ObserverState = ObserverStates.Running;
            }
#endif
        }

        /// <summary>
        /// Cleans up all memory and objects associated with the observer.
        /// </summary>
        public void CleanupObserver()
        {
#if UNITY_WSA
            StopObserving();

            if (observer != null)
            {
                observer.Dispose();
                observer = null;
            }

            if (outstandingMeshRequest != null)
            {
                CleanUpSurface(outstandingMeshRequest.Value);
                outstandingMeshRequest = null;
            }

            if (spareSurfaceObject != null)
            {
                CleanUpSurface(spareSurfaceObject.Value);
                spareSurfaceObject = null;
            }

            Cleanup();
#endif
        }

        /// <summary>
        /// Can be called to override the default origin for the observed volume.  Can only be called while observer has been started.
        /// Kept for compatibility with Examples/SpatialUnderstanding
        /// </summary>
        public bool SetObserverOrigin(Vector3 origin)
        {
            bool originUpdated = false;

#if UNITY_WSA
            if (observer != null)
            {
                Origin = origin;
                originUpdated = true;
            }
#endif

            return originUpdated;
        }

        /// <summary>
        /// Change the observed volume according to ObserverVolumeType.
        /// </summary>
        private void SwitchObservedVolume()
        {
#if UNITY_WSA
            if (observer == null)
            {
                return;
            }

            switch (observerVolumeType)
            {
                case ObserverVolumeTypes.AxisAlignedBox:
                    observer.SetVolumeAsAxisAlignedBox(origin, extents);
                    break;
                case ObserverVolumeTypes.OrientedBox:
                    observer.SetVolumeAsOrientedBox(origin, extents, orientation);
                    break;
                case ObserverVolumeTypes.Sphere:
                    observer.SetVolumeAsSphere(origin, extents.magnitude); //workaround
                    break;
                default:
                    observer.SetVolumeAsAxisAlignedBox(origin, extents);
                    break;
            }
#endif
        }

#if UNITY_WSA
        /// <summary>
        /// Handles the SurfaceObserver's OnDataReady event.
        /// </summary>
        /// <param name="cookedData">Struct containing output data.</param>
        /// <param name="outputWritten">Set to true if output has been written.</param>
        /// <param name="elapsedCookTimeSeconds">Seconds between mesh cook request and propagation of this event.</param>
        private void SurfaceObserver_OnDataReady(SurfaceData cookedData, bool outputWritten, float elapsedCookTimeSeconds)
        {
            if (outstandingMeshRequest == null)
            {
                Debug.LogErrorFormat("Got OnDataReady for surface {0} while no request was outstanding.",
                    cookedData.id.handle
                    );

                return;
            }

            if (!IsMatchingSurface(outstandingMeshRequest.Value, cookedData))
            {
                Debug.LogErrorFormat("Got mismatched OnDataReady for surface {0} while request for surface {1} was outstanding.",
                    cookedData.id.handle,
                    outstandingMeshRequest.Value.ID
                    );

                ReclaimSurface(outstandingMeshRequest.Value);
                outstandingMeshRequest = null;

                return;
            }

            if (ObserverState != ObserverStates.Running)
            {
                Debug.LogFormat("Got OnDataReady for surface {0}, but observer was no longer running.",
                    cookedData.id.handle
                    );

                ReclaimSurface(outstandingMeshRequest.Value);
                outstandingMeshRequest = null;

                return;
            }

            if (!outputWritten)
            {
                ReclaimSurface(outstandingMeshRequest.Value);
                outstandingMeshRequest = null;

                return;
            }

            Debug.Assert(outstandingMeshRequest.Value.Object.activeSelf);

            // alters properites of mesh as desired: first sets renderer enabled field
            outstandingMeshRequest.Value.Renderer.enabled = SpatialMappingManager.Instance.DrawVisualMeshes;

            // create extra data
            SurfaceObject tmpSO = outstandingMeshRequest.Value;
            Transform tmpTransform = tmpSO.Renderer.transform;
            // Wvertices
            List<Vector3> tmpWvertices = tmpSO.Filter.sharedMesh.vertices.ToList();
            for (int i = 0; i < tmpWvertices.Count; i++)
                tmpWvertices[i] = tmpTransform.TransformPoint(tmpWvertices[i]);
            // TriCount
            int tmpTriCount = tmpSO.Filter.sharedMesh.triangles.ToList().Count;
            // SurfacePoints obj
            SurfacePoints extras = new SurfacePoints(
                MeshManager.IntersectionPoints(outstandingMeshRequest.Value.Renderer.bounds), tmpWvertices, 
                tmpTriCount, tmpSO.Renderer.bounds, cookedData.trianglesPerCubicMeter);

            // updates/adds mesh in source stored list of SurfaceObjects.
            SurfaceObject? replacedSurface = UpdateOrAddSurfaceObject(outstandingMeshRequest.Value, extras, destroyGameObjectIfReplaced: false);
            outstandingMeshRequest = null;

            if (replacedSurface != null)
            {
                ReclaimSurface(replacedSurface.Value);
            }
        }

        /// <summary>
        /// Handles the SurfaceObserver's OnSurfaceChanged event.
        /// </summary>
        /// <param name="id">The identifier assigned to the surface which has changed.</param>
        /// <param name="changeType">The type of change that occurred on the surface.</param>
        /// <param name="bounds">The bounds of the surface.</param>
        /// <param name="updateTime">The date and time at which the change occurred.</param>
        private void SurfaceObserver_OnSurfaceChanged(SurfaceId id, SurfaceChange changeType, Bounds bounds, DateTime updateTime)
        {
            // Verify that the client of the Surface Observer is expecting updates.
            if (ObserverState != ObserverStates.Running)
            {
                return;
            }

            switch (changeType)
            {
                case SurfaceChange.Added:
                case SurfaceChange.Updated:
                    surfaceWorkQueue.Enqueue(id);
                    break;

                case SurfaceChange.Removed:
                    SurfaceObject? removedSurface = RemoveSurfaceIfFound(id.handle, destroyGameObject: false);
                    if (removedSurface != null)
                    {
                        ReclaimSurface(removedSurface.Value);
                    }
                    break;

                default:
                    Debug.LogErrorFormat("Unexpected {0} value: {1}.", changeType.GetType(), changeType);
                    break;
            }
        }
        private bool IsMatchingSurface(SurfaceObject surfaceObject, SurfaceData surfaceData)
        {
            return (surfaceObject.ID == surfaceData.id.handle)
                && (surfaceObject.Filter == surfaceData.outputMesh)
                && (surfaceObject.Collider == surfaceData.outputCollider)
                ;
        }
#endif

        /// <summary>
        /// Called when the GameObject is unloaded.
        /// </summary>
        private void OnDestroy()
        {
            CleanupObserver();
        }

        private void ReclaimSurface(SurfaceObject availableSurface)
        {
            if (spareSurfaceObject == null)
            {
                CleanUpSurface(availableSurface, destroyGameObject: false);

                availableSurface.Object.name = "Unused Surface";
                availableSurface.Object.SetActive(false);

                spareSurfaceObject = availableSurface;
            }
            else
            {
                CleanUpSurface(availableSurface);
            }
        }

        /// <summary>
        /// Calls for rebaking of non-conforming surfaces upon updating of Density
        /// </summary>
        private void CallForRebake(float newDensity)
        {
            for (int i = 0; i < SurfaceObjects.Count; i++) // 
            {
                UnityEngine.XR.WSA.SurfaceId ID = new UnityEngine.XR.WSA.SurfaceId();
                ID.handle = SurfaceObjects[i].ID;

                // check if non-conforming and not already staged for rebaking
                if (ExtraData[i].Density != newDensity && !surfaceWorkQueue.Contains(ID))
                {
                    surfaceWorkQueue.Enqueue(ID);

                    // debug
                    Debug.Log(string.Format("Mesh {0}: Density does not conform. Was {1}, should be {2}. Added to surfaceWorkQuene.",
                        ID.handle, ExtraData[i].Density, newDensity));
                }
            }
            needRebake = false;
        }
    }
}
