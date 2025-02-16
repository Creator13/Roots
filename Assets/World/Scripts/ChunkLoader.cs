using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Roots.Util;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Roots.World
{
    public class ChunkLoader : MonoBehaviour
    {
        public class LoaderChunkData
        {
            public Chunk chunkData;

            public bool isLoaded;

            public GameObject gameObject;
            public Transform transform;
            public MeshRenderer meshRenderer;
            public MeshFilter meshFilter;
        }

        private static readonly int2[] NeighborDirections =
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
        [SerializeField] private int2 playerChunk;

        private Vector3 playerPosition;
        private LoaderChunkData[] loadedChunks;

        private int Diameter => loadRadius * 2 + 1;
        private int PlayerChunkIndexOffset => Diameter * loadRadius + loadRadius;

        public int ChunkCount => Diameter * Diameter;
        public int InitializedChunkCount => loadedChunks?.Count(chunkData => chunkData.isLoaded) ?? 0;
        public int ActiveChunkGenJobCount => chunkGenerator.ActiveChunkGenJobCount;

        private bool initialChunksLoaded;
        public event Action LoadedChunksChanged;
        public event Action InitialChunksLoaded;
        
        private void Start()
        {
            playerPosition = player.position;
            playerChunk = WorldPositionToWorldChunkCoordinates(playerPosition);
            
            InitializeChunks();
            // This assertion verifies that the UpdateVisibleChunks method orders all the chunks it needs to
            // (this won't assert false because the jobs can't complete until at least one UpdateChunkGenerationJobs() has been called)
            Assert.IsTrue(chunkGenerator.ActiveChunkGenJobCount == ChunkCount);
            initialChunksLoaded = false;
        }

        private void Update()
        {
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

            playerPosition = player.position;
            int2 currentChunk = WorldPositionToWorldChunkCoordinates(playerPosition);
            int2 delta = currentChunk - playerChunk;
            playerChunk = currentChunk;

            if (!delta.Equals(int2.zero))
            {
                int2 sign = math.sign(delta);
                int2 abs = math.abs(delta);
                for (int x = 0; x < abs.x; x++)
                {
                    UpdateVisibleChunks(new int2(sign.x, 0));
                }
                for (int y = 0; y < abs.y; y++)
                {
                    UpdateVisibleChunks(new int2(sign.y, 0));
                }
            }
        }

        private void OnDestroy()
        {
            foreach (LoaderChunkData chunk in loadedChunks)
            {
                chunk.chunkData.Dispose();
            }
        }

        private void InitializeChunks()
        {
            loadedChunks = new LoaderChunkData[ChunkCount];

            for (int xRel = -loadRadius, i = 0; xRel < loadRadius + 1; xRel++)
            {
                for (int zRel = -loadRadius; zRel < loadRadius + 1; zRel++, i++)
                {
                    var data = new LoaderChunkData();
                    data.gameObject = new GameObject($"Chunk x{xRel} z{zRel}");
                    data.transform = data.gameObject.transform;
                    data.meshFilter = data.gameObject.AddComponent<MeshFilter>();
                    data.meshRenderer = data.gameObject.AddComponent<MeshRenderer>();

                    data.transform.SetParent(transform, true);
                    loadedChunks[i] = data;
                    chunkGenerator.CreateChunkAsync(new int2(xRel, zRel) + playerChunk, data);
                }
            }
        }

        private void UpdateVisibleChunks(int2 movementDelta)
        {
            // Ensure the delta is never larger than 1 step in any direction and also not zero
            Assert.IsTrue(CartesianMath.ManhattanDistance(int2.zero, movementDelta) == 1);

            // Cache width
            int width = Diameter;
            
            // local coordinates to index function
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int _GetIndex(int x, int z) => x * width + z;
            
            // Player moves x + 1
            if (movementDelta.x > 0)
            {
                for (int z = 0; z < width; z++)
                {
                    var temp = loadedChunks[_GetIndex(0, z)]; // cache first
                    for (int x = 0; x < width - 1; x++) // iterate first to second last element, set each item to the next in the list (overwrites first element, keeps last)
                    {
                        loadedChunks[_GetIndex(x, z)] = loadedChunks[_GetIndex(x + 1, z)];
                    }
                    
                    // Order a new chunk to be loaded into the rotating object
                    int2 newChunkCoords =  + movementDelta;
                    temp.chunkData.Dispose(); // Invalidate the old chunkData
                    chunkGenerator.CreateChunkAsync(newChunkCoords, temp);
                    loadedChunks[_GetIndex(width - 1, z)] = temp; // set last element to cached first element (rotate)
                }
            }

            // Player moves z - 1
            if (movementDelta.x < 0)
            {
                for (int z = 0; z < width; z++)
                {
                    var temp = loadedChunks[_GetIndex(width - 1, z)]; // cache last element in row
                    for (int x = width - 1; x > 0; x--) // iterate last to second element (backwards), set each item to the previous in the list (= next in backwards iteration), overwrites last element keeps first 
                    {
                        loadedChunks[_GetIndex(x, z)] = loadedChunks[_GetIndex(x - 1 , z)];
                    }
                    loadedChunks[_GetIndex(0, z)] = temp;
                }
            }
        
            // Player moves z + 1
            if (movementDelta.y > 0)
            {
                for (int x = 0; x < width; x++)
                {
                    var temp = loadedChunks[_GetIndex(x, 0)];
                    for (int z = 0; z < width - 1; z++)
                    {
                        loadedChunks[_GetIndex(x, z)] = loadedChunks[_GetIndex(x, z + 1)];
                    }
                    loadedChunks[_GetIndex(x, width - 1)] = temp;
                }
            }
        
            // PLayer moves z - 1
            if (movementDelta.y < 0)
            {
                for (int x = 0; x < width; x++)
                {
                    var temp = loadedChunks[_GetIndex(x, width - 1)];
                    for (int z = width - 2; z >= 0; z--)
                    {
                        loadedChunks[_GetIndex(x, z + 1)] = loadedChunks[_GetIndex(x, z)];
                    }
                    loadedChunks[_GetIndex(x, 0)] = temp;
                }
            }
        }
        
        public Vector3[] GetCombinedPointData()
        {
            int chunkCount = InitializedChunkCount;
            if (chunkCount == 0) return Array.Empty<Vector3>();
            int chunkPointCount = chunkGenerator.PointGridInfo.totalPoints;

            // TODO this can definitely be parallelized (copy each chunk to the array in a separate job; see NativeSlices)
            Vector3[] points = new Vector3[chunkCount * chunkPointCount];
            int iChunk = 0;
            foreach (LoaderChunkData chunk in loadedChunks)
            {
                if (!chunk.isLoaded) continue;

                for (int iPoint = 0; iPoint < chunkPointCount; iPoint++)
                {
                    points[chunkPointCount * iChunk + iPoint] = chunk.chunkData.points[iPoint] + chunk.chunkData.worldPos;
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

        public float GetExactGroundHeightAt(Vector3 position)
        {
            return chunkGenerator.GetTerrainHeightAt(position);
        }

        public float GetInterpolatedGroundHeightAt(Vector3 position)
        {
            return ChunkAt(position).GetHeightAt(position);
        }

        public Vector3 FindLowestPointNearChunk(int2 chunkOffset, float threshold = 0.05f, int maxRadius = 1)
        {
            Assert.IsTrue(CartesianMath.IsInSquareRadius(chunkOffset, playerChunk, loadRadius),
                $"Called on a chunk that is outside the current load radius. Start chunk: {chunkOffset}, center: {playerChunk}, radius: {loadRadius}");
            Assert.IsTrue(loadedChunks[WorldChunkCoordinatesToLocalChunkIndex(chunkOffset)].isLoaded,
                $"Called on a start chunk that is not initialized: {chunkOffset}.");

            Queue<int2> frontier = new Queue<int2>();
            HashSet<int2> visited = new HashSet<int2>();
            Vector3 bestPoint = Vector3.zero;
            bestPoint.y = float.MaxValue;

            // Work with relative positions, apply chunk offset only where the actual chunk data is retrieved
            int2 startPos = int2.zero;
            frontier.Enqueue(startPos);
            visited.Add(startPos);

            while (frontier.Count > 0)
            {
                int2 current = frontier.Dequeue();
                if (!CartesianMath.IsInSquareRadius(current, loadRadius)) continue;

                Chunk currentChunk = ChunkAt(current + chunkOffset);
                Vector3 chunkLowestPoint = currentChunk.FindLowestPoint() + currentChunk.worldPos;

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
                    int2 neighbor = current + dir;
                    if (!visited.Contains(neighbor) && math.abs(chunkOffset.x - neighbor.x) <= maxRadius && math.abs(chunkOffset.y - neighbor.y) <= maxRadius)
                    {
                        frontier.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
            }

            return bestPoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LoaderChunkData ChunkDataAt(int2 chunkCoordinates)
        {
            return loadedChunks[WorldChunkCoordinatesToLocalChunkIndex(chunkCoordinates)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private LoaderChunkData ChunkDataAt(Vector3 worldPosition)
        {
            return loadedChunks[WorldPositionToLocalChunkIndex(worldPosition)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Chunk ChunkAt(int2 chunkCoordinates)
        {
            return ChunkDataAt(chunkCoordinates).chunkData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Chunk ChunkAt(Vector3 worldPosition)
        {
            return ChunkDataAt(worldPosition).chunkData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int WorldPositionToLocalChunkIndex(Vector3 worldPosition)
        {
            return WorldChunkCoordinatesToLocalChunkIndex(WorldPositionToWorldChunkCoordinates(worldPosition));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int WorldChunkCoordinatesToLocalChunkIndex(int2 coordinates)
        {
            coordinates -= playerChunk;
            return coordinates.x * Diameter + coordinates.y + PlayerChunkIndexOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int2 WorldPositionToWorldChunkCoordinates(Vector3 worldPosition)
        {
            return new int2
            {
                x = (int)math.floor(worldPosition.x / chunkGenerator.ChunkSize),
                y = (int)math.floor(worldPosition.z / chunkGenerator.ChunkSize),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int2 LocalChunkIndexToLocalChunkCoordinates(int index)
        {
            int2 localCoords = new int2
            {
                x = index / Diameter,
                y = index % Diameter,
            };
            return localCoords + playerChunk;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(chunkGenerator.CalculateChunkCenterPosition(playerChunk.x, playerChunk.y), Vector3.one * chunkGenerator.ChunkSize + Vector3.up * chunkGenerator.ChunkSize * 2);

            if (loadedChunks != null)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < ChunkCount; i++)
                {
                    if (i == PlayerChunkIndexOffset) continue;

                    int2 chunkCoords = loadedChunks[i].chunkData.coords;
                    Gizmos.DrawWireCube(chunkGenerator.CalculateChunkCenterPosition(chunkCoords.x, chunkCoords.y), Vector3.one * chunkGenerator.ChunkSize + Vector3.up * chunkGenerator.ChunkSize);
                }
            }
        }
    }
}