using Roots.World;
using Unity.Mathematics;
using UnityEngine;

namespace Roots.Player
{
    public class PlayerInitializer : MonoBehaviour
    {
        [SerializeField] private ChunkLoader chunkLoader;
        [SerializeField] private FirstPersonController playerController;

        private void Awake()
        {
            playerController.isEnabled = false;
        }

        private void Start()
        {
            chunkLoader.InitialChunksLoaded += PlacePlayerInValley;
        }

        private void PlacePlayerInValley()
        {
            chunkLoader.InitialChunksLoaded -= PlacePlayerInValley;

            Vector3 lowestPoint = chunkLoader.FindLowestPointNearChunk(int2.zero, maxRadius: 2);
            Debug.Log($"Placing player at low point: {lowestPoint}");
            playerController.ForceMove(lowestPoint);
            playerController.isEnabled = true;
        }
    }
}