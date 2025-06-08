using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Roots.World
{
    public class SeedPointSpawner : MonoBehaviour
    {
        [SerializeField] private SeedProvider seedProvider;
        [SerializeField] private ChunkLoader chunkLoader;

        [Space]
        [SerializeField] private int seedCount;
        [SerializeField] private int minDistance;
        [SerializeField] private int maxDistance;
        [SerializeField] private int maxAltitude;
        
        public void SpawnSeeds(Vector3 rootPointPosition)
        {
            Random random = seedProvider.RandomFromSeed(1998);

            for (int i = 0; i < seedCount; i++)
            {
                Vector3 position = chunkLoader.RandomLowPointNormalDistribution(random, minDistance, maxDistance, 1, maxAltitude);
            }
        }
    }
}