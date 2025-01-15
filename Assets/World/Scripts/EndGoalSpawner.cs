using Roots.Util;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Roots.World
{
    public class EndGoalSpawner : MonoBehaviour
    {
        [SerializeField] private SeedProvider seedProvider;
        [SerializeField] private float minDistanceFromStart;
        [SerializeField] private float maxDistanceFromStart;
        [SerializeField] private float deviationModifier = 1; // Smaller values cluster the random distance towards the center of the spread.
        [SerializeField] private float sunOffsetAngleFromBeam = 146;
        
        [Space]
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private ChunkLoader chunkLoader;
        [SerializeField] private Transform endGoalPrefab;
        [SerializeField] private Light lightPrefab;
        [SerializeField] private Transform directionalLight;
        
        private void Start()
        {
            Random random = new Random(seedProvider.SeedAsUint());
            float randomDistance = Math.RandomNormalDistribution(random, minDistanceFromStart, maxDistanceFromStart, deviationModifier);
            
            Vector2 randomXZ = random.NextFloat2Direction() * randomDistance;
            Vector3 randomPosition = new Vector3(randomXZ.x, endGoalPrefab.localScale.y / 2, randomXZ.y);

            float beamAngle = Quaternion.FromToRotation(Vector3.forward, new Vector3(randomXZ.x, 0, randomXZ.y)).eulerAngles.y;
            Vector3 dirLightRotation = directionalLight.rotation.eulerAngles;
            dirLightRotation.y = beamAngle + sunOffsetAngleFromBeam + 180;
            directionalLight.rotation = Quaternion.Euler(dirLightRotation);
            
            Transform endGoal = Instantiate(endGoalPrefab, randomPosition, Quaternion.identity, transform);
            
            float3 lightPos = randomPosition;
            lightPos.y = chunkLoader.GetGroundHeightAt(randomPosition) + 0.1f;
            Instantiate(lightPrefab, lightPos, Quaternion.identity);
        }
    }
}