using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Roots.World.Chunking
{
    // [BurstCompile]
    public struct GenerateVegetationPositionsJob : IJobParallelFor
    {
        [ReadOnly] public ChunkHeightmap heights;
        [WriteOnly] public NativeArray<VegetationInstance> instances;

        public int typeCount;
        public float chunkSize;
        public uint hashedSeed;

        public void Execute(int i)
        {
            Random random = new Random(hashedSeed ^ (uint)i);
            float4 rng = random.NextFloat4(new float4(chunkSize, 1, chunkSize, 360));
            float3 pos = rng.xyz;
            pos.y = heights.Interpolate(pos);
            pos -= new float3(chunkSize * .5f, 0, chunkSize * .5f);

            VegetationInstance instance;
            instance.transform = float4x4.TRS(pos.xyz, quaternion.Euler(0, rng.w, 0), new float3(1, 1, 1));
            instance.type = random.NextInt(typeCount);
            instance.rng = random.NextFloat3();
            instances[i] = instance;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VegetationInstance
    {
        public float4x4 transform;
        public int type;
        public float3 rng;
    }

    public struct ChunkVegetation
    {
        public NativeArray<VegetationInstance> instances;

        private GraphicsBuffer instanceBuffer;
        private GraphicsBuffer commandBuffer;
        private GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
        private const int commandCount = 1;
        private RenderParams renderParams;

        private Mesh mesh;
        private int targetInstanceCount;

        private bool isValidAndBound;

        public JobHandle RegenerateJobified(Chunk chunk, JobHandle dep = default)
        {
            isValidAndBound = false;

            renderParams.worldBounds = chunk.GetBounds(50);

            GenerateVegetationPositionsJob job = new GenerateVegetationPositionsJob
            {
                instances = instances,
                heights = chunk.heightmap,
                chunkSize = chunk.grid.size,
                hashedSeed = 43 ^ math.hash(chunk.coords)
            };

            return job.Schedule(instances.Length, 12, dep);
        }

        public void Rebind()
        {
            instanceBuffer.SetData(instances);
            renderParams.material.SetBuffer("_Instances", instanceBuffer);
            isValidAndBound = true;
        }

        public void Initialize(Mesh mesh, Material material, float chunkSize, float density)
        {
            isValidAndBound = false;
            this.mesh = mesh;
            targetInstanceCount = (int)(chunkSize * chunkSize * density);

            // Instance NativeArray/buffer // TODO directly write to bufer??
            instances = new NativeArray<VegetationInstance>(targetInstanceCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            instanceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, targetInstanceCount, sizeof(float) * 4 * 5);

            // Command buffer
            commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];

            commandData[0].indexCountPerInstance = mesh.GetIndexCount(0);
            commandData[0].instanceCount = (uint)targetInstanceCount;
            commandBuffer.SetData(commandData);

            // Render params
            renderParams = new RenderParams(material);
        }

        public void Render()
        {
            if (!isValidAndBound) return;

            Graphics.RenderMeshIndirect(renderParams, mesh, commandBuffer);
        }

        public void Dispose()
        {
            instanceBuffer?.Release();
            instanceBuffer = null;
            instances.Dispose();
        }
    }
}