/// MeshManager
/// Interface for Hololens spatial mapping data via HoloToolKit/SpatialMapping/SpatialMappingManager. 
/// Mark Scherer, June 2018

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Accessor class to Hololens spatial mapping data via HoloToolKit/SpatialMapping/SpatialMappingManager.
/// </summary>
[RequireComponent(typeof(HoloToolkit.Unity.SpatialMapping.SpatialMappingManager))]
public class MeshManager : HoloToolkit.Unity.Singleton<MeshManager>
{
    /// <summary>
    /// Metadata: number of independent meshes returned by SpatialMappingManager.
    /// NOTE: only updated when getVertices() is called.
    /// </summary>
    public int meshCount { get; private set; }

    /// <summary>
    /// Metadata: number of triangles in all meshes returned by SpatialMappingManager.
    /// NOTE: only updated when getVertices() is called.
    /// </summary>
    public int triangleCount { get; private set; }

    /// <summary>
    /// Metadata: number of vertices in all meshes returned by SpatialMappingManager.
    /// NOTE: only updated when getVertices() is called.
    /// </summary>
    public int vertexCount { get; private set; }

    /// <summary>
    /// Constructor. ONLY to be used within Singleton, elsewhere ALWAYS use Instance().
    /// Must follow Singleton's enforced new constraint.
    /// </summary>
    public MeshManager()
    {
        vertexCount = 0;
        meshCount = 0;
    }

    /// <summary>
    /// Returns list of vertices of all meshes in caches spatial mapping data via SpatialMappingManager/GetMeshes().
    /// NOTE: Will have repeats as vertex is added each time it is included in a mesh.
    /// </summary>
    public List<Vector3> getVertices()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Mesh> allMeshes = 
            HoloToolkit.Unity.SpatialMapping.SpatialMappingManager.Instance.GetMeshes();
        triangleCount = 0;
        foreach (Mesh mesh in allMeshes)
        {
            vertices.AddRange(mesh.vertices);
            triangleCount += mesh.triangles.Length;
        } 
        meshCount = allMeshes.Count;
        vertexCount = vertices.Count;
        return vertices;
    }
}