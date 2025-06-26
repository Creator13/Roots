using System;
using Roots.World;
using UnityEngine;

namespace Roots.Player
{
    public class DistanceToTreeChecker : MonoBehaviour
    {
        [SerializeField] private GameStateManager gameStateManager;

        [SerializeField] private Transform player;
        [SerializeField] private Transform tree;

        [SerializeField] private float triggerDistance = 15;

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

            if (currentDistance < triggerDistance)
            {
                gameStateManager.EndPlantSpawning();
            }
        }

        private bool CheckActive()
        {
            return gameStateManager.IsInPlantingStage;
        }
    }
}