using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Random = Unity.Mathematics.Random;

namespace Roots.World.Chunking
{
    [BurstCompile]
    public struct GenerateVegetationPositionsJob : IJobParallelFor
    {
        [ReadOnly] public ChunkHeightmap heights;
        [WriteOnly] public NativeArray<VegetationInstance> instances;

        public float chunkSize;
        public uint hashedSeed;

        public void Execute(int i)
        {
            Random random = Random.CreateFromIndex((uint)i + hashedSeed);
            float3 pos = random.NextFloat3(chunkSize);
            pos.y = heights.Interpolate(pos);

            VegetationInstance instance;
            instance.pos = pos;
            instance.yRot = random.NextFloat(360);
            instance.roll = random.NextFloat();
            instances[i] = instance;
        }
    }

    // [StructLayout(LayoutKind.Sequential)]
    // [BurstCompile]
    public struct VegetationInstance
    {
        public float3 pos;
        public float yRot;
        public float roll;
    }

    public struct ChunkVegetation
    {
        private NativeArray<VegetationInstance> instances;

        private GameObject parent;

        private Transform[] transforms;
        private MeshFilter[] meshFilters;
        private MeshRenderer[] renderers;

        private VegetationAsset vegetationAsset;

        private float chunkSize;
        private int targetInstanceCount;

        public JobHandle RegenerateJobified(Chunk chunk, JobHandle dep = default)
        {
            Assert.IsTrue(instances.IsCreated);

            GenerateVegetationPositionsJob job = new GenerateVegetationPositionsJob
            {
                instances = instances,
                heights = chunk.heightmap,
                chunkSize = chunkSize,
                hashedSeed = math.hash(chunk.coords) ^ 43,
            };

            return job.Schedule(instances.Length, 12, dep);
        }

        public void ApplyAfterGeneration()
        {
            Assert.IsNotNull(vegetationAsset);

            for (int i = 0; i < targetInstanceCount; i++)
            {
                transforms[i].localPosition = instances[i].pos;
                transforms[i].localRotation = Quaternion.Euler(0, instances[i].yRot, 0);

                VegetationType vegType = vegetationAsset.GetAssetFromRoll(instances[i].roll);
                meshFilters[i].sharedMesh = vegType.mesh;
                renderers[i].sharedMaterial = vegType.material;
            }
        }

        public void SetVegetationAsset(VegetationAsset vegetationAsset) => this.vegetationAsset = vegetationAsset;
        public void SetVisible(bool visible) => parent.gameObject.SetActive(visible); 

        public void Initialize(float density, float chunkSize, Transform parent)
        {
            this.parent = new GameObject();
            this.parent.transform.SetParent(parent, false);

            targetInstanceCount = (int)math.floor(chunkSize * chunkSize * density);
            this.chunkSize = chunkSize;

            instances = new NativeArray<VegetationInstance>(targetInstanceCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            CreateGameObjects(this.parent.transform);
        }

        public void Render()
        {
            // todo implement
        }
        
        private void CreateGameObjects(Transform parent)
        {
            transforms = new Transform[targetInstanceCount];
            meshFilters = new MeshFilter[targetInstanceCount];
            renderers = new MeshRenderer[targetInstanceCount];

            for (int i = 0; i < targetInstanceCount; i++)
            {
                var obj = new GameObject("Plant");

                var transform = obj.transform;
                transform.SetParent(parent, false);
                transforms[i] = obj.transform;

                meshFilters[i] = obj.AddComponent<MeshFilter>();

                var meshRenderer = obj.AddComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                renderers[i] = meshRenderer;
            }
        }

        public void Dispose()
        {
            instances.Dispose();
        }
    }
}