using System.Collections.Generic;
using UnityEngine;

public class InstancedDrawing : MonoBehaviour
{
    private static readonly int PointsBufferPID = Shader.PropertyToID("pointsBuffer");
    [SerializeField] private float noiseScale = .1f;
    [SerializeField] private float noiseStrength = 3f;
    [SerializeField] private int size = 20;

    [Space]
    [SerializeField] private Mesh pointMesh;
    [SerializeField] private Material material;

    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    const int commandCount = 1;

    private Bounds bounds;
    private int instanceCount;
    private RenderParams rp;

    // private void OnEnable()
    // {
    //     argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
    //     argsBuffer.SetData(new uint[]
    //     {
    //         (uint)pointMesh.GetIndexCount(0),
    //         (uint)(size * size),
    //         (uint)pointMesh.GetIndexStart(0),
    //         (uint)pointMesh.GetBaseVertex(0),
    //         0
    //     });
    // }

    private void Start()
    {
        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];

        GeneratePoints();
        UpdateBuffers();
    }

    private void OnValidate()
    {
        // GeneratePoints();
    }

    private void Update()
    {
        if (size * size != instanceCount) UpdateBuffers();
        
        Graphics.RenderMeshIndirect(rp, pointMesh, commandBuf, commandCount);
    }

    private void UpdateBuffers()
    {
        rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds for better FOV culling
        rp.matProps = new MaterialPropertyBlock();
        rp.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.Translate(transform.position));
        rp.matProps.SetInt("_GridWidth", size);
        rp.matProps.SetInt("_GridHeight", size);
        rp.matProps.SetFloat("_Spacing", 1);

        instanceCount = size * size;
        commandData[0].indexCountPerInstance = pointMesh.GetIndexCount(0);
        commandData[0].instanceCount = (uint)instanceCount;
        commandBuf.SetData(commandData);
    }

    private void OnDisable()
    {
        commandBuf?.Release();
        commandBuf = null;
    }

    [ContextMenu("Generate points")]
    private void GeneratePoints()
    {
        bounds = new Bounds(new Vector3(size * .5f, 0, size * .5f), new Vector3(size + 2, noiseStrength * 2, size + 2));

        var points = new List<Matrix4x4>();
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                Vector3 pos = new Vector3(x, (Mathf.PerlinNoise(x * noiseScale, z * noiseScale) - .5f) * noiseStrength, z);
                Quaternion rot = Quaternion.identity;
                Vector3 scale = Vector3.one;
                points.Add(Matrix4x4.TRS(pos, rot, scale));
            }
        }

        // pointsBuffer = new ComputeBuffer(size * size, sizeof(float) * 4 * 4);
        // pointsBuffer.SetData(points);
        // material.SetBuffer(PointsBufferPID, pointsBuffer);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}