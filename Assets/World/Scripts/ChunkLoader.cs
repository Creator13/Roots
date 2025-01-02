using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

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

        public int ChunkCount => (loadRadius * 2 + 1) * (loadRadius * 2 + 1);

        private bool playerChunkChanged;
        public event Action LoadedChunksChanged;

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
            if (Keyboard.current[Key.G].wasPressedThisFrame)
            {
                RegenerateChunks();
            }

            playerChunkChanged = false;
            playerPosition = player.transform.position;

            UpdateCurrentChunk();

            if (playerChunkChanged)
            {
                // Loading new chunks *ALWAYS* needs to happen before the event invocation
                UpdateVisibleChunks();
                LoadedChunksChanged?.Invoke();
            }
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

        public IEnumerable<Chunk> GetChunkEnumarable()
        {
            return loadedChunks.Values;
        }

        public Vector3[] GetCombinedPointData()
        {
            int chunkCount = ChunkCount;
            int chunkVertexCount = chunkGenerator.ChunkVertexCount;
            
            // TODO this can definitely be parallelized (copy each chunk to the array in a separate job; see NativeSlices)
            Vector3[] points = new Vector3[chunkCount * chunkVertexCount];
            int iChunk = 0;
            foreach (Chunk chunk in loadedChunks.Values)
            {
                for (int iPoint = 0; iPoint < chunkVertexCount; iPoint++)
                {
                    points[chunkVertexCount * iChunk + iPoint] = chunk.Points[iPoint].position + chunk.cachedWorldPosition;
                }

                iChunk++;
            }

            return points;
        }

        [ContextMenu("Regenerate all")]
        private void RegenerateChunks()
        {
            // TODO fix this method (it wonks out)
            if (!EditorApplication.isPlaying) return;

            var newChunks = new Dictionary<Vector2Int, Chunk>(loadedChunks.Count);
            foreach (var (chunkPos, chunk) in loadedChunks)
            {
                Chunk newChunk = chunkGenerator.CreateChunk(chunkPos.x + playerChunkX, chunkPos.y + playerChunkZ, transform);
                Destroy(chunk.gameObject);
                newChunks.Add(chunkPos, newChunk);
            }

            loadedChunks = newChunks;
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