using System.Collections;
using Roots.World.Chunking;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Serialization;
using Random = Unity.Mathematics.Random;

namespace Roots.World
{
    [BurstCompile]
    public struct UpdateInstanceGrowthJob : IJobParallelForTransform
    {
        public NativeArray<float> ages;
        public float growthTime;
        public float deltaTime;

        public void Execute(int i, TransformAccess transform)
        {
            float veggieAge = ages[i];
            if (veggieAge < growthTime)
            {
                veggieAge += deltaTime;
                ages[i] = veggieAge;
                float progress = veggieAge / growthTime;
                progress = math.saturate(progress);

                transform.localScale = new Vector3(progress, progress, progress);
            }
        }
    }

    public class VegetationRoot : MonoBehaviour
    {
        public enum GrowthType { Circle, Tendrils }

        [SerializeField] private RngSeedProvider seedProvider;
        [FormerlySerializedAs("vegetationType")] [SerializeField] private VegetationAsset currentVegetation;
        [FormerlySerializedAs("growthTime")] [SerializeField] private float expansionTime = 9;

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
        private bool isVisible;
        private bool jobScheduledThisFrame;

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

            jobScheduledThisFrame = false;
            if (fullGrownInstances != currentInstanceCount)
            {
                UpdateInstanceGrowthJob job = new()
                {
                    ages = ages.AsArray(),
                    growthTime = currentVegetation.growthTime,
                    deltaTime = Time.deltaTime,
                };
                jobHandle = job.Schedule(transforms);
                jobScheduledThisFrame = true;

                // TODO::NOTE: transformaccessarray is only truly parallel if the transforms in it are in different "root objects" (see https://medium.com/toca-boca-tech-blog/unitys-transformaccessarray-internals-and-best-practices-2923546e0b41)
            }
        }

        private void LateUpdate()
        {
            if (!initialized || !jobScheduledThisFrame) return;

            jobHandle.Complete();

            UpdateGrownInstanceCount();
        }

        private void UpdateGrownInstanceCount()
        {
            int fullGrownInstanceCount = 0;
            for (int i = 0; i < ages.Length; i++)
            {
                if (ages[i] >= currentVegetation.growthTime)
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
            types.Dispose();
            statuses.Dispose();
        }

        public void Initialize(float radius, VegetationAsset vegetationAsset, GrowthType growthType)
        {
            // do some stupid hashing so that not every root starts out with the same rng state
            random = seedProvider.GetRngWithOffset(math.hash((int3)((float3)transform.position * 10000)));

            currentVegetation = vegetationAsset;

            int predictedInstanceCount = growthType switch
            {
                GrowthType.Circle => CalcInstanceCountCircle(radius, currentVegetation.density),
                GrowthType.Tendrils => CalcInstanceCountTendrils(5, radius, currentVegetation.density),
            };
            transforms = new TransformAccessArray(predictedInstanceCount);
            GrowContainers(predictedInstanceCount);

            initialized = true;
            isVisible = true;

            IEnumerator routine = growthType switch
            {
                GrowthType.Circle => ExpandAreaCoroutine(radius, expansionTime),
                GrowthType.Tendrils => GrowTendrilsCoroutine(radius, expansionTime)
            };
            
            StartCoroutine(routine);
        }

        public void ReplaceVegetation(VegetationAsset newVegetation)
        {
            int newInstanceCount = CalcInstanceCountCircle(Radius, newVegetation.density);
            currentVegetation = newVegetation;

            // Replace existing objects/transforms
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
            isVisible = visible;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = visible;
            }
        }

        private IEnumerator GrowTendrilsCoroutine(float targetLength, float duration)
        {
            const int tendrilCount = 5;
            const float angleStep = math.PI2 / tendrilCount;
            
            int tendrilStepCount = CalcInstanceCountTendrils(tendrilCount, targetLength, currentVegetation.density);

            for (int t = 0; t < tendrilCount; t++)
            {
                float angle = angleStep * t + random.NextFloat(-0.2f, 0.2f);
                float3 baseDirection = new float3(math.cos(angle), 0, math.sin(angle));
                float3 startPos = baseDirection * .1f;

                StartCoroutine(GrowTendril(startPos, (int)math.ceil(tendrilStepCount / (float)tendrilCount), targetLength, baseDirection, duration));
            }

            yield return null;
        }

        private IEnumerator GrowTendril(float3 startPos, int instanceCount, float targetLength, float3 baseDirection, float duration)
        {
            float stepLength = targetLength / instanceCount;
            float3 pos = startPos;
            float timePerStep = duration / instanceCount;

            float2 noiseSeed = random.NextFloat2(1000f);

            for (int i = 0; i < instanceCount; i++)
            {
                float2 p = noiseSeed + new float2(pos.x, pos.z) * .3f;

                float2 curl = CurlNoise(p, 5f);
                float3 direction = math.normalize(new float3(curl.x, 0, curl.y) * 15 + baseDirection);

                pos += direction * stepLength;

                AddInstance(pos);
                currentInstanceCount++;

                yield return new WaitForSeconds(timePerStep);
            }
        }

        private IEnumerator ExpandAreaCoroutine(float targetRadius, float duration)
        {
            float startRadius = Radius;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;
                t = math.saturate(t);

                Radius = math.lerp(startRadius, targetRadius, t);

                currentInstanceCount = CalcInstanceCountCircle(Radius, currentVegetation.density);
                MatchInstanceTarget();

                yield return null;
            }

            Radius = targetRadius;
            currentInstanceCount = CalcInstanceCountCircle(Radius, currentVegetation.density);
            MatchInstanceTarget();
        }

        private void GrowContainers(int targetSize)
        {
            transforms.capacity = targetSize;
            ages.Capacity = targetSize;
            statuses.Capacity = targetSize;
            types.Capacity = targetSize;
        }
        
        private void MatchInstanceTarget()
        {
            if (currentInstanceCount > transforms.capacity)
            {
#if DEBUG
                if (currentInstanceCount - transforms.capacity > 2)
                    Debug.LogWarning("Growing vegetation root arrays should be avoided for small increments.");
#endif
                GrowContainers(currentInstanceCount);
            }

            for (int i = 0; i < currentInstanceCount - transforms.length; i++)
            {
                float3 relativePos = random.NextFloat3Direction() * (math.sqrt(random.NextFloat()) * Radius);
                AddInstance(relativePos);
            }
        }

        private Transform CreateInstance(float3 relativePos)
        {
            float rotation = random.NextFloat(360);

            Vector3 pos = transform.position + (Vector3)relativePos;
            pos.y = chunkLoader.GetInterpolatedGroundHeightAt(pos);

            Transform instanceParent = new GameObject("Veg").transform;
            instanceParent.SetParent(transform);
            instanceParent.SetPositionAndRotation(pos, Quaternion.Euler(0, rotation, 0));
            instanceParent.localScale = Vector3.zero;

            Instantiate(currentVegetation.GetPlantType(ref random).prefab, instanceParent, false);
            if (!isVisible)
            {
                foreach (var renderer in instanceParent.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = false;
                }
            }

            return instanceParent;
        }

        private void AddInstance(float3 relativePos, bool fulLGrown = false)
        {
            Transform instance = CreateInstance(relativePos);
            transforms.Add(instance);
            ages.Add(fulLGrown ? currentVegetation.growthTime : 2f);
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
            types[index] = currentVegetation.GetInstanceID();
        }

        private static int CalcInstanceCountCircle(float radius, float density)
        {
            return (int)math.ceil(density * radius * radius * math.PI);
        }

        private static int CalcInstanceCountTendrils(int tendrilCount, float radius, float density)
        {
            return (int)math.ceil(tendrilCount * radius * density) * 2;
        }

        private static float2 CurlNoise(float2 p, float frequency = 1f, float epsilon = 0.01f)
        {
            float2 dx = new float2(epsilon, 0);
            float2 dy = new float2(0, epsilon);

            float a = noise.cnoise((p + dy) * frequency);
            float b = noise.cnoise((p - dy) * frequency);
            float c = noise.cnoise((p + dx) * frequency);
            float d = noise.cnoise((p - dx) * frequency);

            float ddx = (a - b) * 0.5f;
            float ddy = (c - d) * 0.5f;

            return new float2(ddx, -ddy);
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