using System;
using UnityEngine;
using Util;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshTester : MonoBehaviour
{
    private MeshFilter meshFilter;
    
    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Start()
    {
        GenerateMesh();
    }

    [ContextMenu("Generate Mesh")]
    private void GenerateMesh()
    {
        MeshBuilder mb = new MeshBuilder();

        mb.CreateCircle(new Vector3(1, 1, 0), 1.5f, 18, dir, true);

        meshFilter.sharedMesh = mb.GetMesh("Test mesh");
    }
}