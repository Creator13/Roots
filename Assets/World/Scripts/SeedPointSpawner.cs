using Roots.World.Chunking;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Roots.World
{
    public class SeedPointSpawner : MonoBehaviour
    {
        [SerializeField] private RngSeedProvider seedProvider;
        [SerializeField] private ChunkLoader chunkLoader;

        [SerializeField] private PlantSeed prefab;

        [Space]
        [SerializeField] private int seedCount;
        [SerializeField] private float minDistance = 250;
        [SerializeField] private float maxDistance = 450;
        [SerializeField] private float maxAltitude = .03f;
        [SerializeField] private float minDistanceBetweenSeeds = 150;

        private Vector3[] destinations;
        private PlantSeed[] plantSeeds;
        private SeedAnimation[] seedAnimations;

        private void Update()
        {
            UpdateAnimations();
        }

        private void UpdateAnimations()
        {
            if (seedAnimations == null || seedAnimations.Length == 0) return;

            for (int i = 0; i < seedAnimations.Length; i++)
            {
                if (seedAnimations[i].IsActive)
                {
                    seedAnimations[i].UpdateAnimation(Time.deltaTime);
                }
            }
        }

        public void SpawnSeeds(Vector3 centerPosition)
        {
            centerPosition.y = 0;

            CreateDestinations(centerPosition);

            plantSeeds = new PlantSeed[seedCount];
            float angleStep = math.PI2 / seedCount;

            for (int i = 0; i < seedCount; i++)
            {
                Vector3 pos = Vector3.zero;
                math.sincos(angleStep * i, out pos.z, out pos.x);

                pos *= 1.5f;
                pos += centerPosition;
                pos.y = chunkLoader.GetInterpolatedGroundHeightAt(pos);

                plantSeeds[i] = Instantiate(prefab, pos + Vector3.up * .2f, Quaternion.identity);
                plantSeeds[i].SetInteractable(false);
            }

            StartAnimations();
        }

        private void StartAnimations()
        {
            seedAnimations = new SeedAnimation[seedCount];
            for (int i = 0; i < seedCount; i++)
            {
                seedAnimations[i] = new SeedAnimation(
                    plantSeeds[i],
                    destinations[i],
                    1.3f,
                    chunkLoader.GetInterpolatedGroundHeightAt
                );
            }
        }

        private void CreateDestinations(Vector3 centerPosition)
        {
            Random random = seedProvider.GetRngWithOffset(1998);
            float sqrMinDistance = minDistanceBetweenSeeds * minDistanceBetweenSeeds;

            destinations = new Vector3[seedCount];

            int i = 0;
            while (i < seedCount)
            {
                Vector3 position = chunkLoader.RandomLowPointNormalDistribution(ref random, centerPosition, minDistance, maxDistance, 1, maxAltitude);
                position += centerPosition;

                if (IsTooCloseToOtherPoints(sqrMinDistance, position))
                {
                    continue;
                }

                destinations[i] = position;
                i++;
            }
        }

        private bool IsTooCloseToOtherPoints(float sqrMinDistance, Vector3 pointToCheck)
        {
            foreach (Vector3 point in destinations)
            {
                if ((point - pointToCheck).sqrMagnitude < sqrMinDistance)
                    return true;
            }

            return false;
        }
    }
}