using System;
using System.Collections;
using Roots.World;
using Roots.World.Chunking;
using UnityEngine;
using UnityEngine.Pool;
using Math = Roots.Util.Math;

namespace Roots.Player
{
    public class StepPlantSpawner : MonoBehaviour
    {
        [SerializeField] private StepTracker stepTracker;
        [SerializeField] private ChunkLoader chunkLoader;

        [Header("Location parameters")]
        [SerializeField] private float distanceMin = 10;
        [SerializeField] private float distanceMax = 40;
        [SerializeField] private float distanceDeviation = 1;
        [SerializeField] private float angleOffsetMin = 35; // degrees
        [SerializeField] private float angleOffsetMax = 50; // degrees
        [SerializeField] private float angleDeviation = 1;

        [Header("Line")]
        [SerializeField] private VegetationRoot payload;
        [SerializeField] private GameObject particlePrefab;
        [SerializeField] private GameObject guidancePrefab;
        [SerializeField] private float particleSpeed;
        [SerializeField] private float jitter = .5f;
        [SerializeField] private float noiseScale = .5f;
        [SerializeField] private EasingFunction.Ease easingFunction = EasingFunction.Ease.EaseOutQuint;
        
        [Header("Guidance")]
        [SerializeField] private float timeToGuidanceParticle = 4;
        [SerializeField] private int guidanceParticleStepInterval = 5;

        private ObjectPool<Transform> particlePool;
        private ObjectPool<Transform> guidancePool;
        private Vector3 treePosition;
        
        private Vector3 latestPosition;

        private void Awake()
        {
            particlePool = new(
                createFunc: () => Instantiate(particlePrefab, Vector3.zero, Quaternion.identity).transform,
                actionOnGet: tr => tr.gameObject.SetActive(true),
                actionOnRelease: tr => tr.gameObject.SetActive(false),
                defaultCapacity: 4, maxSize: 10);

            guidancePool = new(
                createFunc: () => Instantiate(guidancePrefab, Vector3.zero, Quaternion.identity).transform,
                actionOnGet: tr => tr.gameObject.SetActive(true),
                actionOnRelease: tr => tr.gameObject.SetActive(false),
                defaultCapacity: 2, maxSize: 3);
        }

        private void Start()
        {
            stepTracker.Stepped += OnStep;
            TreeSpawner treeSpawner = FindFirstObjectByType<TreeSpawner>();
            treePosition = treeSpawner.TreePosition;
        }

        private void OnStep(StepTracker.StepInfo stepInfo)
        {
            latestPosition = stepInfo.position;
            
            if (ShouldSpawnGuidanceParticle(stepInfo))
            {
                SpawnGuidanceParticle(stepInfo.position);
            }
            else
            {
                SpawnPlantParticle(stepInfo.position, FindLocation(stepInfo));
            }
        }

        private bool ShouldSpawnGuidanceParticle(StepTracker.StepInfo stepInfo)
        {
            return stepInfo.movementTime > timeToGuidanceParticle 
                   && stepInfo.stepCountInSequence % guidanceParticleStepInterval == 0;
        }

        private Vector3 FindLocation(StepTracker.StepInfo stepInfo)
        {
            float randomDistance = Math.RandomNormalDistribution(distanceMin, distanceMax, distanceDeviation);
            float randomAngle = Math.RandomNormalDistribution(angleOffsetMin, angleOffsetMax, angleDeviation);
            randomAngle *= stepInfo.side;

            Vector3 angledDirection = Quaternion.AngleAxis(randomAngle, Vector3.up) * stepTracker.transform.forward;
            Vector3 pos = stepInfo.position + angledDirection * randomDistance;
            pos.y = chunkLoader.GetInterpolatedGroundHeightAt(pos);

            return pos;
        }

        private void SpawnGuidanceParticle(Vector3 from)
        {
            Debug.Log("Spawning guidance particle");

            Vector3 direction = (treePosition - from).normalized * Math.RandomNormalDistribution(distanceMin, distanceMax, distanceDeviation); 
            StartCoroutine(MoveParticle(guidancePool, from, from + direction, 1.5f));
        }

        private void SpawnPlantParticle(Vector3 from, Vector3 to)
        {
            StartCoroutine(MoveParticle(particlePool, from, to, .1f, SpawnPlantPayload));
        }

        private void SpawnPlantPayload(Transform particle, Vector3 endPos)
        {
            var root = Instantiate(payload, endPos, Quaternion.identity);
            root.Initialize(5);
        }

        private IEnumerator MoveParticle(ObjectPool<Transform> pool, Vector3 from, Vector3 to, float timeToDestroy = 0, Action<Transform, Vector3> onDestinationReached = null)
        {
            EasingFunction.Function ease = EasingFunction.GetEasingFunction(easingFunction);

            Transform particle = pool.Get();
            particle.position = from;
            particle.GetComponentInChildren<TrailRenderer>().Clear();

            float perlinSeed = Time.time;
            Vector3 perp = Vector3.Cross(to - from, Vector3.up);

            float totalTime = (to - from).magnitude / particleSpeed;
            float time = 0;
            while (time < totalTime)
            {
                float t = ease(0, 1, time / totalTime);

                Vector3 pos = Vector3.Lerp(from, to, t);

                float offset = (Mathf.PerlinNoise1D(t * noiseScale + perlinSeed) * 2 - 1) * jitter * (1 - t);
                pos += perp * offset;

                pos.y = chunkLoader.GetInterpolatedGroundHeightAt(pos) + .02f;

                particle.position = pos;

                time += Time.deltaTime;
                yield return null;
            }

            // snap
            to.y = chunkLoader.GetInterpolatedGroundHeightAt(to);
            particle.position = to;

            onDestinationReached?.Invoke(particle, to);

            yield return new WaitForSeconds(timeToDestroy);
            pool.Release(particle);
        }

        // private void OnDrawGizmos()
        // {
        //     Vector3 direction = (treePosition - latestPosition).normalized;
        //     Gizmos.color = Color.cyan;
        //     Gizmos.DrawRay(latestPosition, direction * 10);
        //     // Gizmos.color = Color.red;
        //     // Gizmos.DrawLine(latestPosition, treePosition);
        // }
    }
}