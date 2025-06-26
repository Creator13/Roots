using Roots.World;
using UnityEngine;

namespace Roots.Player
{
    public class DistanceToTreeChecker : MonoBehaviour
    {
        [SerializeField] private float speedModifier = 1f;
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private WorldVisualizationSwitcher worldVisSwitcher;

        [SerializeField] private Transform player;
        [SerializeField] private Transform tree;

        [SerializeField] private float triggerDistanceMin = 15;
        [SerializeField] private float triggerDistanceMax = 25;

        private void OnEnable()
        {
            TreeSpawner.treeSpawned += OnTreeSpawned;
        }

        private void OnDisable()
        {
            TreeSpawner.treeSpawned -= OnTreeSpawned;
        }

        private void OnTreeSpawned(Transform obj)
        {
            tree = obj;
        }

        private void Update()
        {
            if (!CheckActive() || !tree) return;
            
            float currentDistance = Vector3.Distance(player.position, tree.position);
            
            if (currentDistance < triggerDistanceMin)
            {
                gameStateManager.EndPlantSpawning();
            }

            if (currentDistance > triggerDistanceMax) return; // we don't have to do anything until the player gets within range
            
            float t = (currentDistance - triggerDistanceMin) / (triggerDistanceMax - triggerDistanceMin);

            float sample = Mathf.PerlinNoise1D(Time.time * speedModifier);
            if (sample > t)
            {
                worldVisSwitcher.SetVisualizationType(WorldVisualizationSwitcher.WorldType.Isoline);
            }
            else
            {
                worldVisSwitcher.SetVisualizationType(WorldVisualizationSwitcher.WorldType.Mesh);
            }
        }

        private bool CheckActive()
        {
            return gameStateManager.IsInPlantingStage;
        }
    }
}