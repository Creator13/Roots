using System.Collections;
using Roots.Util;
using Roots.World;
using UnityEngine;
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
        [SerializeField] private GameObject payload;
        [SerializeField] private GameObject particlePrefab;
        [SerializeField] private float particleSpeed;
        [SerializeField] private float jitter = .5f;
        [SerializeField] private float noiseScale = .5f;
        [SerializeField] private EasingFunction.Ease easingFunction = EasingFunction.Ease.EaseOutQuint;

        private StepTracker.StepInfo latestStepInfo;
        private Vector3 latestPosition;
        
        private void Start()
        {
            stepTracker.Stepped += OnStep;
        }

        private void Update()
        {
            if (!latestStepInfo.Equals(default(StepTracker.StepInfo)))
            {
                Debug.DrawRay(latestStepInfo.position, latestStepInfo.direction, Color.darkGoldenRod);
                Debug.DrawLine(latestStepInfo.position, latestPosition, Color.darkCyan);
            }
        }

        private void OnStep(StepTracker.StepInfo stepInfo)
        {
            this.latestStepInfo = stepInfo;
            latestPosition = FindLocation(stepInfo);
            DrawLine(stepInfo.position, latestPosition);
        }

        private Vector3 FindLocation(StepTracker.StepInfo stepInfo)
        {
            float randomDistance = Math.RandomNormalDistribution(distanceMin, distanceMax, distanceDeviation);
            float randomAngle = Math.RandomNormalDistribution(angleOffsetMin, angleOffsetMax, angleDeviation);
            randomAngle *= stepInfo.side;

            Vector3 angledDirection = Quaternion.AngleAxis(randomAngle, Vector3.up) * stepTracker.transform.forward;
            Vector3 pos = stepInfo.position + angledDirection * randomDistance;
            pos.y = chunkLoader.GetInterpolatedGroundHeightAt(pos);
            
            Debug.Log($"distance: {randomDistance}, angle: {randomAngle}, final pos {pos}");
            return pos;
        }

        // private void DrawLine(Vector3 from, Vector3 to)
        // {
        //     Assert.IsTrue(lineResolution > 0);
        //     
        //     float distance = Vector3.Distance(from, to);
        //     Vector3 direction = (to - from).normalized;
        //     
        //     int segments = Mathf.FloorToInt(distance * lineResolution);
        //     float segmentLength = distance / segments;
        //     
        //     Vector3[] points = new Vector3[segments + 1];
        //     
        //     for (int i = 0; i < segments + 1; i++)
        //     {
        //         points[i] = from + direction * segmentLength * i;
        //         points[i].y = chunkLoader.GetInterpolatedGroundHeightAt(points[i]);
        //     }
        //
        //     LineRenderer line = Instantiate(lineRendererPrefab, from, Quaternion.identity);
        //     line.positionCount = points.Length;
        //     line.SetPositions(points);
        // }

        private void DrawLine(Vector3 from, Vector3 to)
        {
            Transform particle = Instantiate(this.particlePrefab, from, Quaternion.identity).transform;
            StartCoroutine(MoveParticle(particle, from, to));
        }

        private IEnumerator MoveParticle(Transform particle, Vector3 from, Vector3 to)
        {
            EasingFunction.Function ease = EasingFunction.GetEasingFunction(easingFunction);
            
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

            // Vector3 direction = to - particle.position;
            // float originalDistanceSqr = direction.sqrMagnitude;
            // direction = direction.normalized;
            //
            // float distanceSqr = originalDistanceSqr;
            // while (originalDistanceSqr - distanceSqr > 0.01)
            // {
            //     particle.position += direction * (particleSpeed * Time.deltaTime);
            //     
            //     distanceSqr = (to - particle.position).sqrMagnitude;
            //     yield return null;
            // } 
            
            // snap
            particle.position = to;

            Instantiate(payload, to, Quaternion.identity);
            StartCoroutine(CoroutineHelper.ExecuteDelayed(.1f, () => Destroy(particle.gameObject)));
        }
    }
}