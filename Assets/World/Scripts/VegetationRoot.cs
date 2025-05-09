using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Roots.World
{
    public class VegetationRoot : MonoBehaviour
    {
        [SerializeField] private SeedProvider seedProvider;
        [SerializeField] private GameObject plantPrefab;
        [SerializeField] private float density = 1;

        public float Radius { get; private set; }

        private List<Transform> instances = new List<Transform>();
        private int instanceCountTarget;
        private Random random;
        private ChunkLoader chunkLoader;

        private void Awake()
        {
            chunkLoader = FindFirstObjectByType<ChunkLoader>();
        }

        public void Initialize(int radius)
        {
            // do some stupid hashing so that not every root starts out with the same rng state
            random = new Random(seedProvider.SeedAsUint() ^ math.hash((int3)((float3)transform.position * 10000)));
            
            Radius = radius;
            instanceCountTarget = GetInstanceCount(Radius, density);
            MatchInstanceTarget();
        }

        public void Grow()
        {
            
        }

        private void MatchInstanceTarget()
        {
            instances.Capacity = instanceCountTarget;
            for (int i = 0; i < instanceCountTarget - instances.Count; i++)
            {
                AddInstance();
            }
        }

        private void AddInstance()
        {
            float3 relativePos = random.NextFloat3Direction() * math.sqrt(random.NextFloat());
            float rotation = random.NextFloat(0, 360);

            Vector3 pos = transform.position + (Vector3)relativePos;
            pos.y = chunkLoader.GetInterpolatedGroundHeightAt(pos);
            
            var instance= Instantiate(plantPrefab, pos, Quaternion.Euler(0, rotation, 0), transform);
            instances.Add(instance.transform);
        }

        private static int GetInstanceCount(float radius, float density)
        {
            // Casting rounds down but accurate rounding is unnecessary
            return (int)(density * radius * radius * math.PI);
        }
    }
}