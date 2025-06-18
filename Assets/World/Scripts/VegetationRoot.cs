using System.Collections;
using System.Collections.Generic;
using Roots.World.Chunking;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Roots.World
{
    public class VegetationRoot : MonoBehaviour
    {
        private struct Veggie
        {
            public Transform transform;
            public float age;
        }
        
        [SerializeField] private RngSeedProvider seedProvider;
        [SerializeField] private VegetationAsset vegetationType;
        [SerializeField] private float density = 1;
        [SerializeField] private float growthTime = 9;

        public float Radius { get; private set; }
        public float FullGrownInstanceRatio => fullGrownInstances / (float)instanceCountTarget; 

        private List<Veggie> veggies = new();
        private int instanceCountTarget;
        private int fullGrownInstances;
        private Random random;
        private ChunkLoader chunkLoader;

        private void Awake()
        {
            chunkLoader = FindFirstObjectByType<ChunkLoader>();
        }

        private void Start()
        {
            Initialize(5);
        }

        private void Update()
        {
            if (fullGrownInstances != instanceCountTarget)
            {
                UpdateInstanceGrowth();
            }
        }

        public void Initialize(int radius)
        {
            // do some stupid hashing so that not every root starts out with the same rng state
            random = seedProvider.GetRngWithOffset(math.hash((int3)((float3)transform.position * 10000)));

            StartCoroutine(GrowCoroutine(radius, growthTime));
        }

        public IEnumerator GrowCoroutine(float targetRadius, float duration)
        {
            float startRadius = Radius;
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;

                Radius = math.lerp(startRadius, targetRadius, t);

                instanceCountTarget = GetInstanceCount(Radius, density);
                MatchInstanceTarget();

                yield return null;
            }

            Radius = targetRadius;
            instanceCountTarget = GetInstanceCount(Radius, density);
            MatchInstanceTarget();
        }

        private void UpdateInstanceGrowth()
        {
            int fullGrownInstanceCount = 0;
            for (int i = 0; i < veggies.Count; i++)
            {
                var veggie = veggies[i];
                if (veggie.age < growthTime)
                {
                    veggie.age += Time.deltaTime;
                    veggies[i] = veggie;

                    float progress = veggie.age / growthTime;
                    veggie.transform.localScale = new Vector3(progress, progress, progress);
                }
                else
                {
                    fullGrownInstanceCount++;
                }
            }

            fullGrownInstances = fullGrownInstanceCount;
        }

        private void MatchInstanceTarget()
        {
            // veggies.Capacity = instanceCountTarget;
            for (int i = 0; i < instanceCountTarget - veggies.Count; i++)
            {
                AddInstance();
            }
        }

        private void AddInstance()
        {
            float3 relativePos = random.NextFloat3Direction() * math.sqrt(random.NextFloat()) * Radius;
            float rotation = random.NextFloat(0, 360);

            Vector3 pos = transform.position + (Vector3)relativePos;
            pos.y = chunkLoader.GetInterpolatedGroundHeightAt(pos);

            var instance = Instantiate(vegetationType.GetPlantType(ref random), pos, Quaternion.Euler(0, rotation, 0), transform);
            instance.transform.localScale = Vector3.zero;
            veggies.Add(new Veggie
            {
                transform = instance.transform,
                age = 2f,
            });
        }

        private static int GetInstanceCount(float radius, float density)
        {
            return (int)math.ceil(density * radius * radius * math.PI);
        }

        private void OnDrawGizmos()
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