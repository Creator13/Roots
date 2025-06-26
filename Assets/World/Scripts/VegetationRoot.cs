using System.Collections;
using Roots.World.Chunking;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Serialization;
using Random = Unity.Mathematics.Random;

namespace Roots.World
{
    [BurstCompile]
    public struct UpdateGrowthJob : IJobParallelForTransform
    {
        public NativeArray<float> ages;
        public float growthTime;
        public float deltaTime;

        public void Execute(int i, TransformAccess transform)
        {
            float veggieAge = ages[i];
            if (veggieAge < growthTime)
            {
                ages[i] = veggieAge += deltaTime;
                float progress = veggieAge / growthTime;
                progress = math.saturate(progress);

                transform.localScale = new Vector3(progress, progress, progress);
            }
        }
    }

    public class VegetationRoot : MonoBehaviour
    {
        [SerializeField] private RngSeedProvider seedProvider;
        [FormerlySerializedAs("vegetationType")] [SerializeField] private VegetationAsset currentVegetation;
        [SerializeField] private float growthTime = 9;

        public float Radius { get; private set; }
        public float FullGrownInstanceRatio => fullGrownInstances / (float)currentInstanceCount;

        private TransformAccessArray transforms;
        private NativeList<float> ages;
        private NativeList<bool> statuses;
        private NativeList<int> types;

        private int currentInstanceCount;
        private int fullGrownInstances;
        private Random random;
        private ChunkLoader chunkLoader;
        private JobHandle jobHandle;

        private bool initialized;

        private void Awake()
        {
            chunkLoader = FindFirstObjectByType<ChunkLoader>();
            ages = new NativeList<float>(Allocator.Persistent);
            statuses = new NativeList<bool>(Allocator.Persistent);
            types = new NativeList<int>(Allocator.Persistent);
        }

        private void Update()
        {
            if (!initialized) return;

            if (fullGrownInstances != currentInstanceCount)
            {
                UpdateGrowthJob job = new()
                {
                    ages = ages.AsArray(),
                    growthTime = growthTime,
                    deltaTime = Time.deltaTime,
                };
                jobHandle = job.Schedule(transforms);

                // TODO::NOTE: transformaccessarray is only truly parallel if the transforms in it are in different "root objects" (see https://medium.com/toca-boca-tech-blog/unitys-transformaccessarray-internals-and-best-practices-2923546e0b41)
            }
        }

        private void LateUpdate()
        {
            if (!initialized) return;

            jobHandle.Complete();

            UpdateGrownInstanceCount();
        }

        private void UpdateGrownInstanceCount()
        {
            int fullGrownInstanceCount = 0;
            for (int i = 0; i < ages.Length; i++)
            {
                if (ages[i] >= growthTime)
                {
                    fullGrownInstanceCount++;
                }
            }

            fullGrownInstances = fullGrownInstanceCount;
        }

        private void OnDestroy()
        {
            ages.Dispose();
            transforms.Dispose();
        }

        public void Initialize(float radius, VegetationAsset vegetationAsset = null)
        {
            if (vegetationAsset != null) currentVegetation = vegetationAsset;

            // do some stupid hashing so that not every root starts out with the same rng state
            random = seedProvider.GetRngWithOffset(math.hash((int3)((float3)transform.position * 10000)));
            int predictedInstanceCount = GetInstanceCount(radius, currentVegetation.density);
            ages.Capacity = predictedInstanceCount;
            transforms = new TransformAccessArray(predictedInstanceCount);

            StartCoroutine(GrowCoroutine(radius, growthTime));
            initialized = true;
        }

        private IEnumerator GrowCoroutine(float targetRadius, float duration)
        {
            float startRadius = Radius;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;
                t = math.saturate(t);

                Radius = math.lerp(startRadius, targetRadius, t);

                currentInstanceCount = GetInstanceCount(Radius, currentVegetation.density);
                MatchInstanceTarget();

                yield return null;
            }

            Radius = targetRadius;
            currentInstanceCount = GetInstanceCount(Radius, currentVegetation.density);
            MatchInstanceTarget();
        }

        private void MatchInstanceTarget()
        {
            if (currentInstanceCount > transforms.capacity)
            {
                transforms.capacity = currentInstanceCount;
                ages.Capacity = currentInstanceCount;
                statuses.Capacity = currentInstanceCount;
                types.Capacity = currentInstanceCount;
#if DEBUG
                if (math.abs(currentInstanceCount - transforms.capacity) > 2) Debug.LogWarning("Growing vegetation root arrays should be avoided.");
#endif
            }

            for (int i = 0; i < currentInstanceCount - transforms.length; i++)
            {
                AddInstance();
            }
        }

        public void ReplaceVegetation(VegetationAsset newVegetation)
        {
            int newInstanceCount = GetInstanceCount(Radius, newVegetation.density);
            currentVegetation = newVegetation;

            if (newInstanceCount < currentInstanceCount)
            {
                float step = (float)currentInstanceCount / newInstanceCount;

                for (int i = 0; i < statuses.Length; i++)
                {
                    statuses[i] = false;
                }

                for (int i = 0; i < newInstanceCount; i++)
                {
                    int index = Mathf.FloorToInt(i * step);
                    if (index < statuses.Length)
                    {
                        statuses[index] = true;
                    }
                }
                
                for (int i = 0; i < transforms.length; i++)
                {
                    if (statuses[i])
                    {
                        ReplaceInstanceAt(i);
                    }
                    else
                    {
                        transforms[i].gameObject.SetActive(false);
                    }
                }

                UpdateGrownInstanceCount();
            }
            else if (newInstanceCount > currentInstanceCount)
            {
                for (int i = 0; i < transforms.length; i++)
                {
                    ReplaceAtOrWake(i);
                    statuses[i] = true;
                }
                
                MatchInstanceTarget();
            }
            
            currentInstanceCount = newInstanceCount;
        }

        public void SetVisible(bool visible)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = visible;
            }
        }
        
        private Transform CreateInstance()
        {
            float3 relativePos = random.NextFloat3Direction() * (math.sqrt(random.NextFloat()) * Radius);
            float rotation = random.NextFloat(360);

            Vector3 pos = transform.position + (Vector3)relativePos;
            pos.y = chunkLoader.GetInterpolatedGroundHeightAt(pos);

            Transform instanceParent = new GameObject("Veg").transform;
            instanceParent.SetParent(transform);
            instanceParent.SetPositionAndRotation(pos, Quaternion.Euler(0, rotation, 0));
            instanceParent.localScale = Vector3.zero;

            Instantiate(currentVegetation.GetPlantType(ref random).prefab, instanceParent, false);
            return instanceParent;
        }

        private void AddInstance()
        {
            Transform instance = CreateInstance();
            transforms.Add(instance);
            ages.Add(2f);
            statuses.Add(true);
            types.Add(currentVegetation.GetInstanceID());
        }

        private void ReplaceAtOrWake(int index)
        {
            if (types[index] == currentVegetation.GetInstanceID())
            {
                transforms[index].gameObject.SetActive(true);
            }
            else
            {
                ReplaceInstanceAt(index);
            }
        }
        
        private void ReplaceInstanceAt(int index)
        {
            var parent = transforms[index];
            Destroy(parent.GetChild(0).gameObject);
            Instantiate(currentVegetation.GetPlantType(ref random).prefab, parent, false);
            types[index] = currentVegetation.GetHashCode();
        }

        private static int GetInstanceCount(float radius, float density)
        {
            return (int)math.ceil(density * radius * radius * math.PI);
        }

        private void OnDrawGizmosSelected()
        {
            const int segments = 9;
            Vector3[] points = new Vector3[segments];

            float angleStep = math.PI2 / segments;

            for (int i = 0; i < segments; i++)
            {
                Vector3 pos = Vector3.zero;
                math.sincos(angleStep * i, out pos.z, out pos.x);

                pos *= Radius;
                pos += transform.position;
                pos.y = chunkLoader.GetInterpolatedGroundHeightAt(pos);

                points[i] = pos;
            }

            Gizmos.color = Color.olive;
            Gizmos.DrawLineStrip(points, true);
        }
    }
}