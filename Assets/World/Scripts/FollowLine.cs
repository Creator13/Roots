using System;
using UnityEngine;
using UnityEngine.Pool;

namespace Roots.World
{
    [RequireComponent(typeof(TrailRenderer))]
    public class FollowLine : MonoBehaviour
    {
        [SerializeField] private Vector3[] line;
        [SerializeField] private float speed = 2f;

        private int currentSegment = 0;
        private float segmentProgress = 0f;

        public bool IsReady { get; private set; }

        private TrailRenderer trailRenderer;
        private Action<FollowLine> onEndReachedCallback;

        private void Awake()
        {
            this.trailRenderer = this.GetComponent<TrailRenderer>();
        }

        private void Update()
        {
            if (!IsReady) return;

            Vector3 start = line[currentSegment];
            Vector3 end = line[currentSegment + 1];
            float segmentLength = Vector3.Distance(start, end);

            if (segmentLength == 0f)
            {
                currentSegment++;
                segmentProgress = 0f;
                return;
            }

            segmentProgress += (speed * Time.deltaTime) / segmentLength;

            if (segmentProgress >= 1f)
            {
                currentSegment++;
                segmentProgress = 0f;

                if (currentSegment >= line.Length - 1)
                {
                    transform.position = line[^1];
                    onEndReachedCallback(this);
                    IsReady = false;
                    return;
                }

                start = line[currentSegment];
                end = line[currentSegment + 1];
            }

            transform.position = Vector3.Lerp(start, end, segmentProgress);
        }
        
        public void Activate(Vector3[] line, Action<FollowLine> onEndReachedCallback = null)
        {
            this.line = line;
            this.onEndReachedCallback = onEndReachedCallback;

            transform.position = line[0];
            trailRenderer.Clear();
            currentSegment = 0;

            IsReady = true;
        }
    }
}