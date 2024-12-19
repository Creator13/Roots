using System.Collections.Generic;
using UnityEngine;

namespace Roots.World
{
    public class ChunkLoader : MonoBehaviour
    {
        [SerializeField] private ChunkGenerator chunkGenerator;
        [SerializeField] private int loadRadius = 3;

        [Space]
        [SerializeField] private Transform player;
        [SerializeField] private int playerChunkX;
        [SerializeField] private int playerChunkZ;

        private Vector3 playerPosition;
        private Dictionary<Vector2Int, Chunk> loadedChunks;

        private bool playerChunkChanged;

        private void Awake()
        {
            loadedChunks = new Dictionary<Vector2Int, Chunk>();
        }

        private void Start()
        {
            UpdateVisibleChunks();
        }

        private void Update()
        {
            playerChunkChanged = false;
            playerPosition = player.transform.position;

            UpdateCurrentChunk();

            if (playerChunkChanged) UpdateVisibleChunks();
        }

        private void UpdateVisibleChunks()
        {
            Dictionary<Vector2Int, Chunk> newChunks = new();

            // Create new chunks
            for (int x = -loadRadius; x < loadRadius + 1; x++)
            {
                for (int z = -loadRadius; z < loadRadius + 1; z++)
                {
                    Vector2Int key = new Vector2Int(x + playerChunkX, z + playerChunkZ);
                    if (loadedChunks.TryGetValue(key, out var chunk))
                    {
                        newChunks.Add(key, chunk);
                    }
                    else
                    {
                        Chunk newChunk = chunkGenerator.CreateChunk(x + playerChunkX, z + playerChunkZ, transform);
                        newChunks.Add(key, newChunk);
                    }
                }
            }

            // Invalidate and remove old chunks
            foreach (var (key, chunk) in loadedChunks)
            {
                if (!newChunks.ContainsKey(key))
                {
                    Destroy(chunk.gameObject);
                }
            }

            // Update loaded chunks
            loadedChunks = newChunks;
        }

        private void UpdateCurrentChunk()
        {
            int originalChunkX = playerChunkX;
            int originalChunkZ = playerChunkZ;

            playerChunkX = Mathf.FloorToInt(playerPosition.x / chunkGenerator.chunkSize);
            playerChunkZ = Mathf.FloorToInt(playerPosition.z / chunkGenerator.chunkSize);

            if (playerChunkX != originalChunkX || playerChunkZ != originalChunkZ)
            {
                playerChunkChanged = true;
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(chunkGenerator.CalculateChunkCenter(playerChunkX, playerChunkZ), Vector3.one * chunkGenerator.chunkSize);

            if (loadedChunks != null)
            {
                Gizmos.color = Color.green;
                foreach (var pos in loadedChunks.Keys)
                {
                    if (pos.x == playerChunkX && pos.y == playerChunkZ) continue;

                    Gizmos.DrawWireCube(chunkGenerator.CalculateChunkCenter(pos.x, pos.y), Vector3.one * chunkGenerator.chunkSize);
                }
            }
        }
    }
}