using Roots.Util;
using UnityEngine;

namespace Roots.World
{
    public class EndGoalSpawner : MonoBehaviour
    {
        [SerializeField] private SeedProvider seedProvider;
        [SerializeField] private float minDistanceFromStart;
        [SerializeField] private float maxDistanceFromStart;
        [SerializeField] private float deviationModifier = 1; // Smaller values cluster the random distance towards the center of the spread.
        
        [Space]
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private ChunkLoader chunkLoader;
        [SerializeField] private Transform endGoalPrefab;
        [SerializeField] private Light lightPrefab;
        
        private void Start()
        {
            float randomDistance = Math.RandomNormalDistribution(minDistanceFromStart, maxDistanceFromStart, deviationModifier);
            Vector3 randomPosition = Random.insideUnitSphere * randomDistance;
            randomPosition.y = endGoalPrefab.localScale.y / 2;
            
            Transform endGoal = Instantiate(endGoalPrefab, randomPosition, Quaternion.identity, transform);
            
            Vector3 lightPos = randomPosition;
            lightPos.y = chunkLoader.GetGroundHeightAt(randomPosition) + 0.1f;
            Instantiate(lightPrefab, lightPos, Quaternion.identity);
        }
    }
}