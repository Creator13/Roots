using System.Collections.Generic;
using Roots.Util;
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
        private List<FollowLine>[] activePulses;
        private bool[] collected;

        private bool spawned = false;
        private bool seedsVisible;
        private int activeSeedCount;
        private Timer intervalTimer;
        private ObjectPool<FollowLine> pulsePool;

        private void Update()
        {
            UpdateAnimations();
        }

        private void UpdateAnimations()
        {
            if (!spawned) return;

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
            collected = new bool[seedCount];
            activePulses = new List<FollowLine>[seedCount];
            for (int i = 0; i < seedCount; i++) activePulses[i] = new List<FollowLine>(4);
            intervalTimer = new Timer(pulseInterval, true);

            pulsePool = new ObjectPool<FollowLine>(() => Instantiate(pulsePrefab),
                actionOnGet: pulse => pulse.gameObject.SetActive(true),
                actionOnRelease: pulse => pulse.gameObject.SetActive(false),
                defaultCapacity: 4, maxSize: 12);

            for (int i = 0; i < seedCount; i++)
            {
                plantSeeds[i] = Instantiate(prefab, destinations[i] + Vector3.up * 1.3f, Quaternion.identity);
                plantSeeds[i].SetInteractable(true);
                plantSeeds[i].gameObject.AddComponent<OwnedIndexable>().Index = i;

                lines[i] = GenerateLine(centerPosition, destinations[i]);
            }

            spawned = true;
            SetSeedPathsVisible(true);
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

        public void SetPointCount(int count)
        {
            this.seedCount = count;
        }

        public void MarkCollected(int index)
        {
            collected[index] = true;
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

        private void DoPulse()
        {
            for (int i = 0; i < seedCount; i++)
            {
                if (collected[i]) continue; // do not show for collected points
                
                FollowLine pulse = pulsePool.Get();
                
                int iCopy = i;
                pulse.Activate(lines[i], p =>
                {
                    activePulses[iCopy].Remove(p);
                    pulsePool.Release(p);
                });

                activePulses[i].Add(pulse);
            }
        }

        private void StopVisiblePulses()
        {
            for (int i = 0; i < activePulses.Length; i++)
            {
                foreach (var pulse in activePulses[i])
                {
                    pulsePool.Release(pulse);
                }
                activePulses[i].Clear();
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

                if (IsTooCloseToOtherPoints(sqrMinDistance, position, i))
                {
                    continue;
                }

                destinations[i] = position;
                i++;
            }
        }

        private bool IsTooCloseToOtherPoints(float sqrMinDistance, Vector3 pointToCheck, int count)
        {
            for  (int i = 0; i < count; i++)
            {
                if ((destinations[i] - pointToCheck).sqrMagnitude < sqrMinDistance)
                {
                    return true;
                }
            }

            return false;
        }
    }
}