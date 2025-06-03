using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Roots.Player
{
    public class StepTracker : MonoBehaviour
    {
        public struct StepInfo
        {
            public Vector3 position;
            public Vector3 direction;
            public int stepCountInSequence;
            public float movementTime;
            public int side;
        }

        [SerializeField] private FirstPersonController movementController;
        [Header("Steps")]
        [SerializeField] private float minDistance = .6f; // Triggers step only if time since last step is greater than minTime
        [SerializeField] private float maxDistance = 1.2f; // If travelled farther than max distance, trigger a step always, overriding minTime
        [SerializeField] private float minTime = .8f; // Triggers step only if distance is greater than minDistance
        [SerializeField] private float firstStepDelay = .3f;
        [SerializeField] private Vector2 footOffset;

        private bool wasMovingLastFrame;
        private bool isStarting;

        private float moveStartTime;
        private float lastStepTime;
        private Vector3 lastStepPosition;
        private int currentMovementStepCount;
        private int stepSide = -1; // Used to track left vs right. -1 is left, 1 is right

        public Action<StepInfo> Stepped;

        public int StepCount { get; private set; }

        private void Update()
        {
            CheckForMovementStartedOrEnded();
            CheckForStep();

            wasMovingLastFrame = movementController.IsMoving;
        }

        private void CheckForMovementStartedOrEnded()
        {
            if (isStarting) return;

            if (movementController.IsMoving && !wasMovingLastFrame) // move started
            {
                StartCoroutine(StartStepSequence());
            }
            else if (!movementController.IsMoving && wasMovingLastFrame) // move ended
            {
                if ((transform.position - lastStepPosition).magnitude > minDistance)
                {
                    TriggerStep(Time.time, transform.position);
                }
            }
        }

        private void CheckForStep()
        {
            if (isStarting) return;

            float time = Time.time;
            Vector3 position = transform.position;

            if (movementController.IsMoving)
            {
                float timeSinceLastStep = time - lastStepTime;
                float distanceSinceLastStep = (position - lastStepPosition).magnitude;

                if (distanceSinceLastStep > maxDistance || timeSinceLastStep > minTime && distanceSinceLastStep > minDistance)
                {
                    TriggerStep(time, position);
                }
            }
        }

        private IEnumerator StartStepSequence()
        {
            isStarting = true;
            yield return new WaitForSeconds(firstStepDelay);
            isStarting = false;

            if (!movementController.IsMoving) yield break;

            moveStartTime = Time.time;
            lastStepTime = moveStartTime;
            lastStepPosition = transform.position;
            currentMovementStepCount = 0;
            stepSide = Random.Range(0, 1) * 2 - 1;

            TriggerStep(moveStartTime, lastStepPosition);
        }

        private void TriggerStep(float time, Vector3 position)
        {
            lastStepPosition.y = position.y; // flatten the plane in which the direction calculation happens; laststep gets overwritten by position *AFTER* this so we can safely do this; this is quicker than setting both y to 0 and having to copy.
            Vector3 direction = (position - lastStepPosition).normalized;
            
            
            Stepped?.Invoke(new StepInfo
            {
                position = position + transform.forward * footOffset.x + transform.right * (footOffset.y * stepSide),
                stepCountInSequence = currentMovementStepCount,
                movementTime = moveStartTime - time,
                direction = direction,
                side = stepSide
            });

            lastStepTime = time;
            lastStepPosition = position;
            StepCount++;
            currentMovementStepCount++;
            stepSide *= -1;
        }

        private void OnDrawGizmos()
        {
            Vector3 offset = transform.forward * footOffset.x + transform.right * (footOffset.y * stepSide);
            Gizmos.color = Color.orangeRed;
            Gizmos.DrawLine(transform.position, transform.position + offset);
        }
    }
}