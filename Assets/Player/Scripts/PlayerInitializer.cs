using Roots.World;
using Roots.World.Chunking;
using Unity.Mathematics;
using UnityEngine;

namespace Roots.Player
{
    public class PlayerInitializer : MonoBehaviour
    {
        [SerializeField] private ChunkLoader chunkLoader;
        [SerializeField] private FirstPersonController playerController;
        [SerializeField] private InteractionController interactionController;
        
        [Space]
        [SerializeField] private bool placeInValley;
        [SerializeField] private int maxRadius = 1;
        [SerializeField] private float lowestPointThreshold = 0.05f;

        private void Start()
        {
            chunkLoader.InitialChunksLoaded += InitializePlayer;

            interactionController.gameObject.SetActive(false);
            playerController.isEnabled = false;
        }

        private void InitializePlayer()
        {
            chunkLoader.InitialChunksLoaded -= InitializePlayer;
            
            if (placeInValley) PlacePlayerInValley();
            interactionController.gameObject.SetActive(true);
            
            playerController.isEnabled = true;
        }
        
        private void PlacePlayerInValley()
        {
            Vector3 lowestPoint = chunkLoader.FindLowestPointNearChunk(int2.zero, lowestPointThreshold, maxRadius);
            Debug.Log($"Placing player at low point: {lowestPoint}");
            playerController.ForceMove(lowestPoint);
        }
    }
}