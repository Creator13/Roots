using Roots.World;
using Roots.World.Chunking;
using UnityEngine;
using UnityEngine.Assertions;

namespace Roots
{
    public struct SeedAnimation
    {
        private readonly PlantSeed plantSeed;
        private readonly Transform transform;
        private readonly Vector3 destination;
        private readonly ChunkLoader.TerrainHeightFunction terrainHeightFunction;
        private readonly float floatyHeight;

        private readonly float riseSpeed;
        private readonly float timeToDestination;
        private int stage;

        public bool IsActive => !hasAnimationEnded;

        private float timeSinceStart;
        private Vector3 startPosition;
        private bool hasAnimationEnded;

        public SeedAnimation(PlantSeed plantSeed, Vector3 destination, float floatyHeight, ChunkLoader.TerrainHeightFunction terrainHeightFunction)
        {
            const float riseTime = 2.4f;

            this.plantSeed = plantSeed;
            this.transform = plantSeed.transform;
            this.destination = destination;
            this.terrainHeightFunction = terrainHeightFunction;
            this.floatyHeight = floatyHeight;

            riseSpeed = floatyHeight / riseTime;
            stage = 0;

            const float destinationSpeed = 8;

            timeSinceStart = 0;
            startPosition = transform.position;
            startPosition.y = 0;

            Vector3 distance = destination - startPosition;
            timeToDestination = distance.magnitude / destinationSpeed;

            hasAnimationEnded = false;
        }

        public void UpdateAnimation(float deltaTime)
        {
            switch (stage)
            {
                case 0:
                    Stage1(deltaTime);
                    break;
                case 1:
                    Stage2(deltaTime);
                    break;
                case 2:
                    Stage3(deltaTime);
                    break;
            }

            if (stage == -1 && !hasAnimationEnded)
            {
                plantSeed.SetInteractable(true);
                hasAnimationEnded = true;
            }
        }

        private void Stage1(float deltaTime)
        {
            Assert.AreEqual(0, stage, "Stage1 logic can only be executed in stage 0 state");

            if (transform.position.y < floatyHeight)
            {
                transform.position += Vector3.up * (riseSpeed * deltaTime);
            }
            else
            {
                stage = 1;
                plantSeed.ActivateGlitchThings(true);
            }
        }

        private void Stage2(float deltaTime)
        {
            Assert.AreEqual(1, stage, "Stage2 logic can only be executed in stage 1 state");

            timeSinceStart += deltaTime;
            if (timeSinceStart >= .6f)
            {
                stage = 2;
                timeSinceStart = 0;
                plantSeed.ActivateGlitchThings(false);
            }
        }

        private void Stage3(float deltaTime)
        {
            Assert.AreEqual(2, stage, "Stage3 logic can only be executed in stage 2 state");
            timeSinceStart += deltaTime;

            float progress = timeSinceStart / timeToDestination;
            if (progress < 1)
            {
                Vector3 pos = Vector3.Lerp(startPosition, destination, EasingFunction.EaseOutCubic(0, 1, progress));

                pos.y = terrainHeightFunction(pos) + floatyHeight;

                transform.position = pos;
            }
            else
            {
                stage = -1;
            }
        }
    }
}