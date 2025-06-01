using System;
using UnityEngine;

namespace Roots.Player
{
    public class MovementStepTracker : MonoBehaviour
    {
        public struct StepInfo
        {
            public Vector3 position;
            public int stepCountInSequence;
            public float movementTime;
        }

        [SerializeField] private FirstPersonController movementController;
        [Header("Steps")]
        [SerializeField] private float minDistance = .6f; // Triggers step only if time since last step is greater than minTime
        [SerializeField] private float maxDistance = 1.2f; // If travelled farther than max distance, trigger a step always, overriding minTime
        [SerializeField] private float minTime = .8f; // Triggers step only if distance is greater than minDistance

        private bool wasMovingLastFrame;
        private float moveStartTime;
        private float lastStepTime;
        private Vector3 lastStepPosition;
        private int currentMovementStepCount;

        public Action<StepInfo> Stepped;

        public int StepCount { get; private set; }

        private void Update()
        {
            CheckForMovementStartedOrEnded();
            CheckForStep();
        }

        private void CheckForMovementStartedOrEnded()
        {
            if (movementController.IsMoving && !wasMovingLastFrame) // move started
            {
                moveStartTime = Time.time;
                lastStepTime = moveStartTime;
                lastStepPosition = transform.position;
                currentMovementStepCount = 0;
            }
            else if (!movementController.IsMoving && wasMovingLastFrame) // move ended
            {
                TriggerStep(Time.time, transform.position);
            }
            
            wasMovingLastFrame = movementController.IsMoving;
        }

        private void CheckForStep()
        {
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

        private void TriggerStep(float time, Vector3 position)
        {
            lastStepTime = time;
            lastStepPosition = position;

            StepCount++;

            Stepped?.Invoke(new StepInfo
            {
                position = position,
                stepCountInSequence = currentMovementStepCount,
                movementTime = moveStartTime - time
            });
            currentMovementStepCount++;
        }
    }
}