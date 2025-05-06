using UnityEngine;
using Math = Roots.Util.Math;
using Random = Unity.Mathematics.Random;

namespace Roots.World
{
    public class TreeSpawner : MonoBehaviour
    {
        [SerializeField] private ChunkLoader chunkLoader;
        
        [Space]
        [SerializeField] private SeedProvider seedProvider;
        [SerializeField] private float minDistanceFromStart;
        [SerializeField] private float maxDistanceFromStart;
        [SerializeField] private float deviationModifier = 1; // Smaller values cluster the random distance towards the center of the spread.
        [SerializeField] private float maxAltitude = 0.02f;
        
        [Space]
        [SerializeField] private GameObject treePrefab;
        
        private void Awake()
        {
            chunkLoader.InitialChunksLoaded += SpawnTree;
        }

        private void OnDestroy()
        {
            chunkLoader.InitialChunksLoaded -= SpawnTree;
        }

        private void SpawnTree()
        {
            Random random = new Random(seedProvider.SeedAsUint());
            float randomDistance = Math.RandomNormalDistribution(random, minDistanceFromStart, maxDistanceFromStart, deviationModifier);

            // TODO: Limit this to avoid a worst case scenario (store a best value, use that if a max # of attempts is reached)
            Vector2 randomXZ = random.NextFloat2Direction() * randomDistance;
            float altitude = chunkLoader.GetInterpolatedGroundHeightAt(new Vector3(randomXZ.x, 0, randomXZ.y));
            int tries = 1;
            while (altitude > maxAltitude)
            {
                randomXZ = random.NextFloat2Direction() * randomDistance;
                altitude = chunkLoader.GetInterpolatedGroundHeightAt(new Vector3(randomXZ.x, 0, randomXZ.y));
                tries++;
            }
            
            Debug.Log($"Found location for tree, took {tries} tries.");
            
            GameObject tree = Instantiate(treePrefab, new Vector3(randomXZ.x, altitude, randomXZ.y), Quaternion.Euler(0, random.NextFloat(0, 360), 0), transform);
            
            chunkLoader.InitialChunksLoaded -= SpawnTree;
        }
    }
}
