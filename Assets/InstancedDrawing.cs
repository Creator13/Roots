using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Utils;

public class InstancedDrawing : MonoBehaviour
{
    private static readonly int InstancePositionsSPID = Shader.PropertyToID("_InstancePositions");
    private static readonly int PlayerPositionSPID = Shader.PropertyToID("_PlayerPosition");

    [SerializeField] private Transform followObject;
    
    [SerializeField] private float noiseScale = .1f;
    [SerializeField] private float noiseStrength = 3f;
    [SerializeField] private int size = 20;

    [Space]
    [SerializeField] private Mesh pointMesh;
    [SerializeField] private Material material;

    private GraphicsBuffer commandBuffer;
    private GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    private const int commandCount = 1;
    
    private RenderParams rp;
    private Bounds bounds;
    private GraphicsBuffer pointDataBuffer;

    private List<Vector3> points;

    private MeshCollider floorCollider;

    private void Awake()
    {
        floorCollider = GetComponent<MeshCollider>();
    }

    private void Start()
    {
        Regenerate();
    }

    private void Update()
    {
        var pos = followObject.transform.position;
        rp.material.SetVector(PlayerPositionSPID, pos);
        Graphics.RenderMeshIndirect(rp, pointMesh, commandBuffer, commandCount);
    }

    private void Regenerate()
    {
        GeneratePoints();
        UpdateBuffers();
        GenerateMesh();
    }

    private void UpdateBuffers()
    {
        ReleaseBuffers();

        commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];

        commandData[0].indexCountPerInstance = pointMesh.GetIndexCount(0);
        commandData[0].instanceCount = (uint)points.Count;
        commandBuffer.SetData(commandData);

        pointDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, points.Count, sizeof(float) * 3);
        pointDataBuffer.SetData(points);

        rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, new Vector3(size * 2.1f, noiseStrength * 2, size * 2.1f));
        rp.material.SetBuffer(InstancePositionsSPID, pointDataBuffer);
    }

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

    private void GenerateMesh()
    {
        MeshBuilder mb = new MeshBuilder();
        for (int x = 0; x < size - 1; x++)
        {
            for (int z = 0; z < size - 1; z++)
            {
                mb.AddQuadNew(points[z + size * x], points[z + 1 + size * x], points[z + 1 + size * (x + 1)], points[z + size * (x + 1)]);
            }
        }

        floorCollider.sharedMesh = mb.GetMesh();
    }

    private void ReleaseBuffers()
    {
        commandBuffer?.Release();
        commandBuffer = null;

        pointDataBuffer?.Release();
        pointDataBuffer = null;
    }
    
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

    private void OnDisable()
    {
#if UNITY_EDITOR
        Undo.undoRedoEvent -= Editor_OnUndoRedo;
#endif
        ReleaseBuffers();
    }
}