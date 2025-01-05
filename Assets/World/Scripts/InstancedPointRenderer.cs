using UnityEngine;

namespace Roots.World
{
    public class InstancedPointRenderer : MonoBehaviour
    {
        private static readonly int InstancePositionsSPID = Shader.PropertyToID("_InstancePositions");
        private static readonly int PlayerPositionSPID = Shader.PropertyToID("_PlayerPosition");

        [SerializeField] private Transform followObject;

        [Space]
        [SerializeField] private PointProvider pointProvider;
        [SerializeField] private Mesh pointMesh;
        [SerializeField] private Material material;

        private GraphicsBuffer commandBuffer;
        private GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
        private const int commandCount = 1;

        private RenderParams rp;
        private Bounds bounds;
        private GraphicsBuffer pointDataBuffer;

        private Vector3[] points;

        private void OnEnable()
        {
            pointProvider.PointDataChanged += Regenerate;
            Regenerate();

        }

        private void OnDisable()
        {
            pointProvider.PointDataChanged -= Regenerate;
            ReleaseBuffers();
        }

        private void Update()
        {
            var pos = followObject.transform.position;
            rp.material.SetVector(PlayerPositionSPID, pos);
            Graphics.RenderMeshIndirect(rp, pointMesh, commandBuffer, commandCount);
        }

        private void Regenerate()
        {
            points = pointProvider.GetPointData();
            UpdateBuffers();
        }

        private void UpdateBuffers()
        {
            ReleaseBuffers();

            commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];

            commandData[0].indexCountPerInstance = pointMesh.GetIndexCount(0);
            commandData[0].instanceCount = (uint)points.Length;
            commandBuffer.SetData(commandData);

            pointDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, points.Length, sizeof(float) * 3);
            pointDataBuffer.SetData(points);

            rp = new RenderParams(material);
            rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * float.MaxValue);
            rp.material.SetBuffer(InstancePositionsSPID, pointDataBuffer);
        }

        private void ReleaseBuffers()
        {
            commandBuffer?.Release();
            commandBuffer = null;

            pointDataBuffer?.Release();
            pointDataBuffer = null;
        }
    }
}