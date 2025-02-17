using Roots.World;
using Unity.Mathematics;
using UnityEngine;

namespace Roots.Player
{
    public class PlayerInitializer : MonoBehaviour
    {
        [SerializeField] private ChunkLoader chunkLoader;
        [SerializeField] private FirstPersonController playerController;
        [SerializeField] private int maxRadius = 1;

        private void Start()
        {
            chunkLoader.InitialChunksLoaded += PlacePlayerInValley;
            playerController.isEnabled = false;
        }

        private void PlacePlayerInValley()
        {
            chunkLoader.InitialChunksLoaded -= PlacePlayerInValley;

            // Vector3 lowestPoint = chunkLoader.FindLowestPointNearChunk(int2.zero, maxRadius);
            // Debug.Log($"Placing player at low point: {lowestPoint}");
            // playerController.ForceMove(lowestPoint);
            playerController.isEnabled = true;
        }
    }
}