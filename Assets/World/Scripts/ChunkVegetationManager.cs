using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Roots.World
{
    [BurstCompile]
    public struct UpdateGrowthProgressJob : IJobParallelFor
    {
        public float minDistance;
        public float maxDistance;
        public float3 playerPos;
        public int stageCount;

        public NativeArray<ChunkVegetationManager.VegetationUpdateData> data;

        public void Execute(int index)
        {
            var veg = data[index];
            float3 objectPos = veg.position;
            objectPos.y = 0;

            float distance = math.distance(objectPos, playerPos);
            distance = math.clamp(distance, minDistance, maxDistance);

            float progress = math.remap(maxDistance, minDistance, 0, 1, distance);
            progress = 1 - (1 - progress) * (1 - progress);
            veg.growthProgress = math.max(veg.growthProgress, progress);
            veg.stageIndex = math.min((int)(veg.growthProgress * stageCount), stageCount - 1);

            data[index] = veg;
        }
    }

    public class ChunkVegetationManager : MonoBehaviour
    {
        public struct VegetationInstance
        {
            public Transform transform;
            public MeshFilter meshFilter;
            public MeshRenderer meshRenderer;
        }

        public struct VegetationUpdateData
        {
            public float3 position;
            public float growthProgress;
            public int stageIndex;
        }

        [SerializeField] private GameObject prefab;
        [SerializeField] private GrowthParameters growthParams;

        private int objectCount;
        private List<VegetationInstance> objects;
        private NativeList<VegetationUpdateData> jobData;

        private Transform player;
        private JobHandle updateJobHandle;

        #region Unity Hooks

        private void Update()
        {
            var job = new UpdateGrowthProgressJob
            {
                data = jobData,
                maxDistance = growthParams.maxDistance,
                minDistance = growthParams.minDistance,
                playerPos = player.position,
                stageCount = growthParams.growthStageMeshes.Length
            };
            updateJobHandle = job.Schedule(jobData.Length, 16);
        }

        private void LateUpdate()
        {
            updateJobHandle.Complete();
            UpdateMeshesToProgress();
        }

        private void OnDestroy()
        {
            jobData.Dispose();
        }

        #endregion
        
        public void SetPrefab(GameObject prefab, GrowthParameters growthParams)
        {
            this.prefab = prefab;
            this.growthParams = growthParams;
        }

        public void Initialize(int count)
        {
            objects = new List<VegetationInstance>(count);
            jobData = new NativeList<VegetationUpdateData>(count, Allocator.Persistent);
            jobData.Resize(count, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < count; i++)
            {
                AddObject();
            }

            player = GameObject.FindWithTag("Player").transform;
        }

        public void SetVegetation(IList<float4> positions)
        {
            objectCount = positions.Count;

            // Grow list if necessary
            if (positions.Count > objects.Count)
            {
                jobData.Resize(positions.Count, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < objects.Count - positions.Count; i++)
                {
                    AddObject();
                }
            }
            
            // Set positions of all vegetation instances
            for (int i = 0; i < objectCount; i++)
            {
                float3 pos = positions[i].xyz;
                float rot = positions[i].w;

                objects[i].transform.localPosition = pos;
                objects[i].transform.rotation = Quaternion.Euler(0, rot, 0);
                objects[i].transform.gameObject.SetActive(true);

                jobData[i] = new VegetationUpdateData
                {
                    position = objects[i].transform.position,
                    growthProgress = 0,
                    stageIndex = 0,
                };
                
                objects[i].meshFilter.sharedMesh = growthParams.growthStageMeshes[jobData[i].stageIndex];
            }
            
            UpdateMeshesToProgress();

            // Disable all unnecessary objects
            if (objects.Count > positions.Count)
            {
                for (int i = positions.Count; i < objectCount; i++)
                {
                    objects[i].transform.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateMeshesToProgress()
        {
            for (int i = 0; i < objectCount; i++)
            {
                if (jobData[i].growthProgress < 0.001f)
                {
                    objects[i].meshRenderer.enabled = false;
                }
                else
                {
                    objects[i].meshRenderer.enabled = true;
                    objects[i].meshFilter.sharedMesh = growthParams.growthStageMeshes[jobData[i].stageIndex];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddObject()
        {
            GameObject obj = Instantiate(prefab, this.transform);
            objects.Add(new VegetationInstance
            {
                transform = obj.transform,
                meshFilter = obj.GetComponent<MeshFilter>(),
                meshRenderer = obj.GetComponent<MeshRenderer>(),
            });
            obj.SetActive(false);
        }
    }
}