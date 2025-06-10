using System.Collections.Generic;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Roots.World
{
    public class SeedPointSpawner : MonoBehaviour
    {
        [SerializeField] private RngSeedProvider seedProvider;
        [SerializeField] private ChunkLoader chunkLoader;

        [SerializeField] private GameObject prefab;
        
        [Space]
        [SerializeField] private int seedCount;
        [SerializeField] private float minDistance = 250;
        [SerializeField] private float maxDistance = 450;
        [SerializeField] private float maxAltitude = .03f;
        [SerializeField] private float minDistanceBetweenSeeds = 150; 
        
        public void SpawnSeeds(Vector3 centerPosition)
        {
            centerPosition.y = 0;
            
            Random random = seedProvider.GetRngWithOffset(1998);
            float sqrMinDistance = minDistanceBetweenSeeds * minDistanceBetweenSeeds;

            List<Vector3> points = new List<Vector3>(seedCount);
            
            while (points.Count < seedCount)
            {
                Vector3 position = chunkLoader.RandomLowPointNormalDistribution(ref random, centerPosition, minDistance, maxDistance, 1, maxAltitude);
                position += centerPosition;

                if (IsTooCloseToOtherPoints(points, sqrMinDistance, position))
                {
                    continue;
                }
                
                points.Add(position);
                Instantiate(prefab, position, Quaternion.identity);
            }
        }

        private static bool IsTooCloseToOtherPoints(List<Vector3> points, float sqrMinDistance, Vector3 pointToCheck)
        {
            foreach (Vector3 point in points)
            {
                if ((point - pointToCheck).sqrMagnitude < sqrMinDistance)
                    return true;
            }

            return false;
        }
    }
}