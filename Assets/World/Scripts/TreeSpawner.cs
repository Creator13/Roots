using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Roots.World
{
    public class TreeSpawner : MonoBehaviour
    {
        [SerializeField] private ChunkLoader chunkLoader;
        
        [Space]
        [SerializeField] private RngSeedProvider seedProvider;
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
            Vector3 position = chunkLoader.RandomLowPointNormalDistribution(ref random, Vector3.zero, minDistanceFromStart, maxDistanceFromStart, deviationModifier, maxAltitude);
            
            GameObject tree = Instantiate(treePrefab, position, Quaternion.Euler(0, random.NextFloat(0, 360), 0), transform);
            
            chunkLoader.InitialChunksLoaded -= SpawnTree;
        }
    }
}
