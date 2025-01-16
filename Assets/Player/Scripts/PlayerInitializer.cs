using Roots.World;
using StarterAssets;
using UnityEngine;

namespace Roots.Player
{
    public class PlayerInitializer : MonoBehaviour
    {
        [SerializeField] private ChunkLoader chunkLoader;
        [SerializeField] private FirstPersonController playerController;

        private void Awake()
        {
            playerController.enabled = false;
        }

        private void Start()
        {
            chunkLoader.InitialChunksLoaded += PlacePlayerInValley;
        }

        private void PlacePlayerInValley()
        {
            chunkLoader.InitialChunksLoaded -= PlacePlayerInValley;

            Vector3 lowestPoint = chunkLoader.FindLowestPointNearChunk(Vector2Int.zero, maxRadius: 2);
            lowestPoint.y += 0.05f;
            Debug.Log($"Placing player at low point: {lowestPoint}");
            playerController.transform.position = lowestPoint;
            playerController.enabled = true;
        }
    }
}