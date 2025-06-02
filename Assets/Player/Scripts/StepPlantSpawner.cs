using NUnit.Framework;
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
        [SerializeField] private int lineResolution = 3; // sample points per unit of length
        [SerializeField] private LineRenderer lineRendererPrefab;

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

        private void DrawLine(Vector3 from, Vector3 to)
        {
            Assert.IsTrue(lineResolution > 0);
            
            float distance = Vector3.Distance(from, to);
            Vector3 direction = (to - from).normalized;
            
            int segments = Mathf.FloorToInt(distance * lineResolution);
            float segmentLength = distance / segments;
            
            Vector3[] points = new Vector3[segments + 1];
            
            for (int i = 0; i < segments + 1; i++)
            {
                points[i] = from + direction * segmentLength * i;
                points[i].y = chunkLoader.GetInterpolatedGroundHeightAt(points[i]);
            }

            LineRenderer line = Instantiate(lineRendererPrefab, from, Quaternion.identity);
            line.positionCount = points.Length;
            line.SetPositions(points);
        }
    }
}