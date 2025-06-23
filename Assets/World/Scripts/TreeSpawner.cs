using System;
using Roots.World.Chunking;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Roots.World
{
    public class TreeSpawner : MonoBehaviour
    {
        public static event Action<Transform> treeSpawned;
        
        [SerializeField] private ChunkLoader chunkLoader;
        
        [Space]
        [SerializeField] private RngSeedProvider seedProvider;
        [SerializeField] private float minDistanceFromStart;
        [SerializeField] private float maxDistanceFromStart;
        [SerializeField] private float deviationModifier = 1; // Smaller values cluster the random distance towards the center of the spread.
        [SerializeField] private float maxAltitude = 0.02f;
        
        [Space]
        [SerializeField] private GameObject treePrefab;

        private Random random;
        public Vector3 TreePosition { get; private set; }
        
        private void Awake()
        {
            chunkLoader.InitialChunksLoaded += SpawnTree;
            
            random = seedProvider.GetRngWithOffset(0);
            TreePosition = chunkLoader.RandomLowPointNormalDistribution(ref random, Vector3.zero, minDistanceFromStart, maxDistanceFromStart, deviationModifier, maxAltitude);
        }

        private void OnDestroy()
        {
            chunkLoader.InitialChunksLoaded -= SpawnTree;
        }

        private void SpawnTree()
        {
            GameObject tree = Instantiate(treePrefab, TreePosition, Quaternion.Euler(0, random.NextFloat(0, 360), 0), transform);
            treeSpawned?.Invoke(tree.transform);
            
            chunkLoader.InitialChunksLoaded -= SpawnTree;
        }
    }
}
