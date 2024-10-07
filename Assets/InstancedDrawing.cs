using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class InstancedDrawing : MonoBehaviour
{
    private static readonly int InstancePositionsPID = Shader.PropertyToID("_InstancePositions");

    [SerializeField] private float noiseScale = .1f;
    [SerializeField] private float noiseStrength = 3f;
    [SerializeField] private int size = 20;

    [Space]
    [SerializeField] private Mesh pointMesh;
    [SerializeField] private Material material;

    private GraphicsBuffer commandBuffer;
    private GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    private const int commandCount = 1;
    private GraphicsBuffer pointDataBuffer;
    private RenderParams rp;

    private Bounds bounds;
    private int instanceCount;
    private List<Vector3> points;


#if UNITY_EDITOR
    private void OnEnable()
    {
        Undo.undoRedoEvent += Editor_OnUndoRedo;
    }

    private void Editor_OnUndoRedo(in UndoRedoInfo undo)
    {
        UpdateBuffers();
    }

    private void OnValidate()
    {
        if (EditorApplication.isPlaying)
        {
            Regenerate();
        }
    }
#endif

    private void Start()
    {
        Regenerate();
    }

    private void Regenerate()
    {
        GeneratePoints();
        UpdateBuffers();
    }

    private void Update()
    {
        if (size * size != instanceCount)
        {
            Regenerate();
        }

        Graphics.RenderMeshIndirect(rp, pointMesh, commandBuffer, commandCount);
    }

    private void UpdateBuffers()
    {
        ReleaseBuffers();

        instanceCount = size * size;

        commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];

        commandData[0].indexCountPerInstance = pointMesh.GetIndexCount(0);
        commandData[0].instanceCount = (uint)instanceCount;
        commandBuffer.SetData(commandData);

        pointDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, instanceCount, sizeof(float) * 3);
        pointDataBuffer.SetData(points);

        rp = new RenderParams(material);
        rp.worldBounds = new Bounds(transform.position, new Vector3(size + 2, noiseStrength * 2, size + 2));
        rp.material.SetBuffer(InstancePositionsPID, pointDataBuffer);
    }

    private void ReleaseBuffers()
    {
        commandBuffer?.Release();
        commandBuffer = null;

        pointDataBuffer?.Release();
        pointDataBuffer = null;
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        Undo.undoRedoEvent -= Editor_OnUndoRedo;
#endif
        ReleaseBuffers();
    }

    [ContextMenu("Generate points")]
    private void GeneratePoints()
    {
        points = new List<Vector3>();
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                points.Add(new Vector3(x, (Mathf.PerlinNoise(x * noiseScale, z * noiseScale) - .5f) * noiseStrength, z));
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}