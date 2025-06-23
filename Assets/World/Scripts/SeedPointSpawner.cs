using Roots.World.Chunking;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using Math = Roots.Util.Math;
using Random = Unity.Mathematics.Random;
using Timer = Roots.Util.Timer;

namespace Roots.World
{
    public class SeedPointSpawner : MonoBehaviour
    {
        [SerializeField] private RngSeedProvider seedProvider;
        [SerializeField] private ChunkLoader chunkLoader;

        [SerializeField] private PlantSeed prefab;

        [Space]
        [SerializeField] private LineRenderer linePrefab;
        [SerializeField] private int seedCount;
        [SerializeField] private float minDistance = 250;
        [SerializeField] private float maxDistance = 450;
        [SerializeField] private float maxAltitude = .03f;
        [SerializeField] private float minDistanceBetweenSeeds = 150;

        [Space]
        [SerializeField] private FollowLine pulsePrefab;
        [SerializeField] private float pulseInterval;

        private Vector3[] destinations;
        private PlantSeed[] plantSeeds;
        private SeedAnimation[] seedAnimations;
        private Vector3[][] lines;

        private bool spawned = false;
        private bool seedsVisible;
        private Timer intervalTimer;
        private ObjectPool<FollowLine> pulses;

        private void Update()
        {
            UpdateAnimations();
        }

        public void SetSeedPathsVisible(bool visible)
        {
            if (!spawned) return;

            if (!seedsVisible && visible)
            {
                intervalTimer.Reset();
                DoPulse();
            }
            else if (seedsVisible && !visible)
            {
                StopVisiblePulses();
            }

            seedsVisible = visible;
        }

        private void UpdateAnimations()
        {
            if (!spawned) return;

            // for (int i = 0; i < seedAnimations.Length; i++)
            // {
            //     if (seedAnimations[i].IsActive)
            //     {
            //         seedAnimations[i].UpdateAnimation(Time.deltaTime);
            //     }
            // }

            if (seedsVisible && intervalTimer.CheckTime())
            {
                DoPulse();
            }
        }

        public void SpawnSeeds(Vector3 centerPosition)
        {
            centerPosition.y = chunkLoader.GetInterpolatedGroundHeightAt(centerPosition);

            CreateDestinations(centerPosition);

            plantSeeds = new PlantSeed[seedCount];
            lines = new Vector3[seedCount][];
            intervalTimer = new Timer(pulseInterval, true);
            
            pulses = new ObjectPool<FollowLine>(() => Instantiate(pulsePrefab),
                actionOnGet: pulse => pulse.gameObject.SetActive(true),
                actionOnRelease: pulse => pulse.gameObject.SetActive(false),
                defaultCapacity: 4, maxSize: 12);

            // float angleStep = math.PI2 / seedCount;

            for (int i = 0; i < seedCount; i++)
            {
                //     Vector3 pos = Vector3.zero;
                //     math.sincos(angleStep * i, out pos.z, out pos.x);
                //     
                //     pos *= 1.5f;
                //     pos += centerPosition;
                //     pos.y = chunkLoader.GetInterpolatedGroundHeightAt(pos);
                //     
                plantSeeds[i] = Instantiate(prefab, destinations[i] + Vector3.up * 1.3f, Quaternion.identity);
                plantSeeds[i].SetInteractable(true);

                lines[i] = GenerateLine(centerPosition, destinations[i]);
            }

            spawned = true;
            SetSeedPathsVisible(true);

            // StartAnimations();
        }

        private Vector3[] GenerateLine(Vector3 start, Vector3 end)
        {
            var path = chunkLoader.TraceValleyPath(start, end, 30, refinementDepth: 3, initialAngle: 65);

            path = Math.SmoothPathChaikin(path, 2);
            path = Math.SubdividePath(path, 4);
            path = Math.ModifyPathLikeRoot(path, (uint)seedProvider.Seed ^ math.hash(end), chunkLoader.GetGroundHeightAtFastest, noiseAmplitude: 5);

            // var lr = Instantiate(linePrefab, Vector3.zero, Quaternion.Euler(90, 0, 0));
            // lr.positionCount = path.Length;
            // lr.SetPositions(path);

            return path;
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
                    chunkLoader.GetGroundHeightAtFastest
                );
            }
        }

        private void DoPulse()
        {
            for (int i = 0; i < seedCount; i++)
            {
                FollowLine pulse = pulses.Get();
                pulse.Activate(lines[i], p => pulses.Release(p));
            }
        }

        private void StopVisiblePulses()
        {
            // TODO fck this shiot man
            foreach (var pulse in FindObjectsByType<FollowLine>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Destroy(pulse.gameObject);
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