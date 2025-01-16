using Roots.World;
using UnityEngine;

namespace Roots.Player
{
    public class PlayerPositionInitializer : MonoBehaviour
    {
        [SerializeField] private ChunkLoader chunkLoader;
        [SerializeField] private Transform playerTransform;
        
        private void Start()
        {
            chunkLoader.InitialChunksLoaded += PlacePlayerInValley;
        }

        private void PlacePlayerInValley()
        {
            chunkLoader.InitialChunksLoaded -= PlacePlayerInValley;

            Vector3 lowestPoint = chunkLoader.FindLowestPointNearChunk(Vector2Int.zero, maxRadius: 2);
            Debug.Log($"Placing player at low point: {lowestPoint}");
            playerTransform.position = lowestPoint;
        }
    }
}