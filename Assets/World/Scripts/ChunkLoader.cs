using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace Roots.World
{
    public class ChunkLoader : MonoBehaviour
    {
        private static readonly Vector2Int[] NeighborDirections =
        {
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1),
            new(1, 1),
            new(1, -1),
            new(-1, 1),
            new(-1, -1)
        };

        [SerializeField] private ChunkGenerator chunkGenerator;
        [SerializeField] private int loadRadius = 3;

        [Space]
        [SerializeField] private Transform player;
        [SerializeField] private int playerChunkX;
        [SerializeField] private int playerChunkZ;

        private Vector3 playerPosition;
        private Dictionary<Vector2Int, Chunk> loadedChunks = new();

        public int ChunkCount => (loadRadius * 2 + 1) * (loadRadius * 2 + 1);
        public int InitializedChunkCount => loadedChunks.Values.Count(chunk => chunk.IsInitialized);
        public int ActiveChunkGenJobCount => chunkGenerator.ActiveChunkGenJobCount;

        private bool playerChunkChangedInFrame;
        private bool initialChunksLoaded;
        public event Action LoadedChunksChanged;
        public event Action InitialChunksLoaded;

        private void Start()
        {
            UpdateVisibleChunks();
            // This assertion verifies that the UpdateVisibleChunks method orders all the chunks it needs to
            // (this won't assert false because the jobs can't complete until at least one UpdateChunkGenerationJobs() has been called)
            Assert.IsTrue(chunkGenerator.ActiveChunkGenJobCount == ChunkCount); 
            initialChunksLoaded = false;
        }

        private void Update()
        {
            if (Keyboard.current[Key.G].wasPressedThisFrame)
            {
                RegenerateChunks();
            }

            int finishedJobs = chunkGenerator.UpdateChunkGenerationJobs();

            if (finishedJobs > 0)
            {
                LoadedChunksChanged?.Invoke();

                if (!initialChunksLoaded)
                {
                    if (chunkGenerator.ActiveChunkGenJobCount == 0)
                    {
                        initialChunksLoaded = true;
                        InitialChunksLoaded?.Invoke();
                    }
                }
            }

            playerChunkChangedInFrame = false;
            playerPosition = player.transform.position;

            UpdateCurrentChunk();

            if (playerChunkChangedInFrame)
            {
                UpdateVisibleChunks();
            }
        }

        private void UpdateVisibleChunks()
        {
            Dictionary<Vector2Int, Chunk> newChunks = new(loadedChunks.Count);

            // Create new chunks
            for (int xRel = -loadRadius; xRel < loadRadius + 1; xRel++)
            {
                for (int zRel = -loadRadius; zRel < loadRadius + 1; zRel++)
                {
                    Vector2Int key = new Vector2Int(xRel + playerChunkX, zRel + playerChunkZ);
                    if (loadedChunks.TryGetValue(key, out var chunk))
                    {
                        newChunks.Add(key, chunk);
                    }
                    else
                    {
                        Chunk newChunk = chunkGenerator.CreateChunkAsync(key, transform);
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

            Vector2Int currentChunk = chunkGenerator.WorldPositionToChunkCoordinates(playerPosition);
            playerChunkX = currentChunk.x;
            playerChunkZ = currentChunk.y;

            if (playerChunkX != originalChunkX || playerChunkZ != originalChunkZ)
            {
                playerChunkChangedInFrame = true;
            }
        }

        public IEnumerable<Chunk> GetChunkEnumerable()
        {
            return loadedChunks.Values;
        }

        public Vector3[] GetCombinedPointData()
        {
            int chunkCount = InitializedChunkCount;
            int chunkPointCount = chunkGenerator.ChunkPointCount;

            // TODO this can definitely be parallelized (copy each chunk to the array in a separate job; see NativeSlices)
            Vector3[] points = new Vector3[chunkCount * chunkPointCount];
            int iChunk = 0;
            foreach (Chunk chunk in loadedChunks.Values)
            {
                if (!chunk.IsInitialized) continue;

                for (int iPoint = 0; iPoint < chunkPointCount; iPoint++)
                {
                    points[chunkPointCount * iChunk + iPoint] = chunk.Points[iPoint] + chunk.CachedWorldPosition;
                }

                iChunk++;
            }

            return points;
        }

        public Bounds GetCurrentBounds()
        {
            // TODO fix this method (and bound calculation in general)
            float edgeSize = (loadRadius * 2 + 3) * chunkGenerator.ChunkSize;

            return new Bounds(Vector3.zero, new Vector3(edgeSize, 50, edgeSize));
        }

        public float GetGroundHeightAt(Vector3 position)
        {
            return chunkGenerator.GetTerrainHeightAt(position);
        }

        public Vector3 FindLowestPointNearChunk(Vector2Int startChunkPos, float threshold = 0.05f, int maxRadius = 1)
        {
            Assert.IsTrue(loadedChunks.TryGetValue(startChunkPos, out var startChunk) && startChunk.IsInitialized, 
                $"Called on a start chunk that is not loaded or initialized: {startChunkPos}.");
            
            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            Vector3 bestPoint = Vector3.zero;
            bestPoint.y = float.MaxValue;

            frontier.Enqueue(startChunkPos);
            visited.Add(startChunkPos);

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                if (!loadedChunks.TryGetValue(current, out var currentChunk)) continue;

                Vector3 chunkLowestPoint = currentChunk.LowestPoint + currentChunk.CachedWorldPosition;
                
                if (chunkLowestPoint.y < threshold)
                {
                    return chunkLowestPoint;
                }

                if (chunkLowestPoint.y < bestPoint.y)
                {
                    bestPoint = chunkLowestPoint;
                }

                foreach (var dir in NeighborDirections)
                {
                    Vector2Int neighbor = current + dir;
                    if (!visited.Contains(neighbor) && math.abs(startChunkPos.x - neighbor.x) <= maxRadius && math.abs(startChunkPos.y - neighbor.y) <= maxRadius)
                    {
                        frontier.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
            }
            
            return bestPoint;
        }

        [ContextMenu("Regenerate all")]
        private void RegenerateChunks()
        {
            // TODO fix this method (it wonks out when run and not at chunk 0,0)
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) return;
#endif

            var newChunks = new Dictionary<Vector2Int, Chunk>(loadedChunks.Count);
            foreach (var (chunkPos, chunk) in loadedChunks)
            {
                Chunk newChunk = chunkGenerator.CreateChunk(chunkPos.x + playerChunkX, chunkPos.y + playerChunkZ, transform);
                Destroy(chunk.gameObject);
                newChunks.Add(chunkPos, newChunk);
            }

            loadedChunks = newChunks;
            LoadedChunksChanged?.Invoke();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(chunkGenerator.CalculateChunkCenterPosition(playerChunkX, playerChunkZ), Vector3.one * chunkGenerator.ChunkSize + Vector3.up * chunkGenerator.ChunkSize * 2);

            if (loadedChunks != null)
            {
                Gizmos.color = Color.green;
                foreach (var pos in loadedChunks.Keys)
                {
                    if (pos.x == playerChunkX && pos.y == playerChunkZ) continue;

                    Gizmos.DrawWireCube(chunkGenerator.CalculateChunkCenterPosition(pos.x, pos.y), Vector3.one * chunkGenerator.ChunkSize + Vector3.up * chunkGenerator.ChunkSize);
                }
            }
        }
    }
}