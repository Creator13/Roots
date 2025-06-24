using System.Collections;
using Roots.World.Chunking;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Random = Unity.Mathematics.Random;

namespace Roots.World
{
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
                
                transform.localScale = new Vector3(progress, progress, progress);
            }
        }
    }

    public class VegetationRoot : MonoBehaviour
    {
        [SerializeField] private RngSeedProvider seedProvider;
        [SerializeField] private VegetationAsset vegetationType;
        [SerializeField] private float growthTime = 9;

        public float Radius { get; private set; }
        public float FullGrownInstanceRatio => fullGrownInstances / (float)instanceCountTarget;

        private TransformAccessArray transforms;
        private NativeList<float> ages;
        
        private int instanceCountTarget;
        private int fullGrownInstances;
        private Random random;
        private ChunkLoader chunkLoader;
        private JobHandle jobHandle;
        
        private bool initialized;

        private void Awake()
        {
            chunkLoader = FindFirstObjectByType<ChunkLoader>();
            ages = new NativeList<float>(Allocator.Persistent);
        }

        private void Update()
        {
            if (!initialized) return;
            
            if (fullGrownInstances != instanceCountTarget)
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
            if (vegetationAsset != null) vegetationType = vegetationAsset;
            
            // do some stupid hashing so that not every root starts out with the same rng state
            random = seedProvider.GetRngWithOffset(math.hash((int3)((float3)transform.position * 10000)));
            int predictedInstanceCount = GetInstanceCount(radius, vegetationType.density);
            ages.Capacity = predictedInstanceCount;
            transforms = new TransformAccessArray(predictedInstanceCount);

            StartCoroutine(GrowCoroutine(radius, growthTime));
            initialized = true;
        }

        public IEnumerator GrowCoroutine(float targetRadius, float duration)
        {
            float startRadius = Radius;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;
                t = math.clamp(t, 0, 1);
                
                Radius = math.lerp(startRadius, targetRadius, t);

                instanceCountTarget = GetInstanceCount(Radius, vegetationType.density);
                MatchInstanceTarget();

                yield return null;
            }

            Radius = targetRadius;
            instanceCountTarget = GetInstanceCount(Radius, vegetationType.density);
            MatchInstanceTarget();
        }

        private void MatchInstanceTarget()
        {
            if (instanceCountTarget > transforms.capacity)
            {
                transforms.capacity = instanceCountTarget;
                ages.Capacity = instanceCountTarget;
                Debug.LogWarning("Growing vegetation root arrays should be avoided.");
            }

            for (int i = 0; i < instanceCountTarget - transforms.length; i++)
            {
                AddInstance();
            }
        }

        private void AddInstance()
        {
            float3 relativePos = random.NextFloat3Direction() * (math.sqrt(random.NextFloat()) * Radius);
            float rotation = random.NextFloat(360);

            Vector3 pos = transform.position + (Vector3)relativePos;
            pos.y = chunkLoader.GetInterpolatedGroundHeightAt(pos);

            var instance = Instantiate(vegetationType.GetPlantType(ref random).prefab, pos, Quaternion.Euler(0, rotation, 0), transform);
            instance.transform.localScale = Vector3.zero;

            transforms.Add(instance.transform);
            ages.Add(2f);
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