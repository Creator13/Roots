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
        public class ChunkContainer
        {
            public Chunk chunkData;

            public bool isLoaded;

            public GameObject gameObject;
            public Transform transform;
            public MeshRenderer meshRenderer;
            public MeshFilter meshFilter;
            public bool hasVegetation;
            public ChunkVegetationManager vegetation;
        }

        [SerializeField] private ChunkGenerator chunkGenerator;
        [SerializeField] private int loadRadius = 3;

        [Space]
        [SerializeField] private Transform player;
        [SerializeField] private int2 playerChunk;

        [Header("temp veg")]
        [SerializeField] private bool useVegetation;
        [SerializeField] private GameObject vegetationPrefab;
        [SerializeField] private GrowthParameters growthParams;
        [SerializeField] private float vegetationLoadRadius;

        private Vector3 playerPosition;
        private ChunkContainer[] chunks;

        private int Diameter => loadRadius * 2 + 1;
        private int PlayerChunkIndexOffset => Diameter * loadRadius + loadRadius;

        public int ChunkCount => Diameter * Diameter;
        public int InitializedChunkCount => chunks?.Count(chunkData => chunkData.isLoaded) ?? 0;
        public int ActiveChunkGenJobCount => chunkGenerator.ActiveChunkGenJobCount;
        public bool AllChunksLoaded => chunkGenerator.ActiveChunkGenJobCount == 0;
        public float2 HalfChunkOffset => new(chunkGenerator.ChunkSize * .5f, chunkGenerator.ChunkSize * .5f);

        private bool initialChunksLoaded;
        public event Action LoadedChunksChanged;
        public event Action InitialChunksLoaded;

        private void Start()
        {
            playerPosition = player.position;
            playerChunk = WorldPositionToWorldChunkCoordinates(playerPosition);

            InitializeChunks(playerChunk);
            // This assertion verifies that the UpdateVisibleChunks method orders all the chunks it needs to
            // (this won't ever assert false because the jobs can't complete until at least one UpdateChunkGenerationJobs() has been called, which should ONLY happen from the chunk loader)
            Assert.IsTrue(chunkGenerator.ActiveChunkGenJobCount == ChunkCount);
            initialChunksLoaded = false;
        }

        private void Update()
        {
            UpdateGenerationJobs();

            playerPosition = player.position;
            int2 currentChunk = WorldPositionToWorldChunkCoordinates(playerPosition);
            int2 delta = currentChunk - playerChunk;
            playerChunk = currentChunk;
            if (!delta.Equals(int2.zero))
            {
                UpdateVisibleChunks(delta);
            }
        }

        private void OnDestroy()
        {
            foreach (ChunkContainer chunk in chunks)
            {
                chunk.chunkData.Dispose();
            }
        }

        private void UpdateGenerationJobs()
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
        }

        #region Loading and updating of visible chunks

        private void InitializeChunks(int2 center)
        {
            chunks = new ChunkContainer[ChunkCount];

            for (int xRel = -loadRadius, i = 0; xRel < loadRadius + 1; xRel++)
            {
                for (int zRel = -loadRadius; zRel < loadRadius + 1; zRel++, i++)
                {
                    var container = new ChunkContainer();
                    container.gameObject = new GameObject($"Chunk x{xRel} z{zRel}");
                    container.transform = container.gameObject.transform;
                    container.meshFilter = container.gameObject.AddComponent<MeshFilter>();
                    container.meshRenderer = container.gameObject.AddComponent<MeshRenderer>();
                    
                    container.hasVegetation = useVegetation;
                    if (useVegetation)
                    {
                        container.vegetation = container.gameObject.AddComponent<ChunkVegetationManager>();
                        container.vegetation.SetPrefab(vegetationPrefab, growthParams);
                        container.vegetation.Initialize((int)(chunkGenerator.ChunkSize * chunkGenerator.ChunkSize));
                    }

                    container.transform.SetParent(transform, true);

                    chunks[i] = container;
                    chunkGenerator.CreateChunkAsync(new int2(xRel, zRel) + center, container);
                }
            }

            if (useVegetation) UpdateVisibleVegetation();
        }

        private void UpdateVisibleChunks(int2 movementDelta)
        {
            int2 abs = math.abs(movementDelta);
            for (int x = 0; x < abs.x; x++)
            {
                if (movementDelta.x > 0) ShiftGridPositiveX();
                if (movementDelta.x < 0) ShiftGridNegativeX();
            }

            for (int y = 0; y < abs.y; y++)
            {
                if (movementDelta.y > 0) ShiftGridPositiveZ();
                if (movementDelta.y < 0) ShiftGridNegativeZ();
            }

            if (useVegetation) UpdateVisibleVegetation();
        }

        private void UpdateVisibleVegetation()
        {
            Vector3 playerChunkCenter = chunkGenerator.CalculateChunkCenterPosition(playerChunk);
            float sqrVegRadius = vegetationLoadRadius * vegetationLoadRadius * chunkGenerator.ChunkSize * chunkGenerator.ChunkSize;
            foreach (ChunkContainer chunk in chunks)
            {
                Vector3 chunkCenter = chunkGenerator.CalculateChunkCenterPosition(chunk.chunkData.coords);
                chunk.vegetation.enabled = !((chunkCenter - playerChunkCenter).sqrMagnitude >= sqrVegRadius);
            }
        }

        #endregion

        #region Grid shifting

        private void ShiftGridNegativeX()
        {
            // Cache width
            int width = Diameter;

            // local coordinates to index function
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int _GetIndex(int x, int z) => x * width + z;

            for (int z = 0; z < width; z++)
            {
                var temp = chunks[_GetIndex(width - 1, z)]; // cache last element in row
                for (int x = width - 1; x > 0; x--) // iterate last to second element (backwards), set each item to the previous in the list (= next in backwards iteration), overwrites last element keeps first 
                {
                    chunks[_GetIndex(x, z)] = chunks[_GetIndex(x - 1, z)];
                }

                // Order a new chunk to be loaded into the rotating object
                int2 newChunkCoords = new int2(temp.chunkData.coords.x - width, temp.chunkData.coords.y);
                temp.chunkData.Dispose(); // Invalidate the old chunkData
                chunkGenerator.CreateChunkAsync(newChunkCoords, temp);
                chunks[_GetIndex(0, z)] = temp;
            }
        }

        private void ShiftGridPositiveX()
        {
            // Cache width
            int width = Diameter;

            // local coordinates to index function
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int _GetIndex(int x, int z) => x * width + z;

            for (int z = 0; z < width; z++)
            {
                var temp = chunks[_GetIndex(0, z)]; // cache first
                for (int x = 0; x < width - 1; x++) // iterate first to second last element, set each item to the next in the list (overwrites first element, keeps last)
                {
                    chunks[_GetIndex(x, z)] = chunks[_GetIndex(x + 1, z)];
                }

                // Order a new chunk to be loaded into the rotating object
                int2 newChunkCoords = new int2(temp.chunkData.coords.x + width, temp.chunkData.coords.y);
                temp.chunkData.Dispose(); // Invalidate the old chunkData
                chunkGenerator.CreateChunkAsync(newChunkCoords, temp);
                chunks[_GetIndex(width - 1, z)] = temp; // set last element to cached first element (rotate)
            }
        }

        private void ShiftGridNegativeZ()
        {
            // Cache width
            int width = Diameter;

            // local coordinates to index function
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int _GetIndex(int x, int z) => x * width + z;

            for (int x = 0; x < width; x++)
            {
                var temp = chunks[_GetIndex(x, width - 1)];
                for (int z = width - 2; z >= 0; z--)
                {
                    chunks[_GetIndex(x, z + 1)] = chunks[_GetIndex(x, z)];
                }

                // Order a new chunk to be loaded into the rotating object
                int2 newChunkCoords = new int2(temp.chunkData.coords.x, temp.chunkData.coords.y - width);
                temp.chunkData.Dispose(); // Invalidate the old chunkData
                chunkGenerator.CreateChunkAsync(newChunkCoords, temp);
                chunks[_GetIndex(x, 0)] = temp;
            }
        }

        private void ShiftGridPositiveZ()
        {
            // Cache width
            int width = Diameter;

            // local coordinates to index function
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int _GetIndex(int x, int z) => x * width + z;

            for (int x = 0; x < width; x++)
            {
                var temp = chunks[_GetIndex(x, 0)];
                for (int z = 0; z < width - 1; z++)
                {
                    chunks[_GetIndex(x, z)] = chunks[_GetIndex(x, z + 1)];
                }

                // Order a new chunk to be loaded into the rotating object
                int2 newChunkCoords = new int2(temp.chunkData.coords.x, temp.chunkData.coords.y + width);
                temp.chunkData.Dispose(); // Invalidate the old chunkData
                chunkGenerator.CreateChunkAsync(newChunkCoords, temp);
                chunks[_GetIndex(x, width - 1)] = temp;
            }
        }

        #endregion

        #region the getting data from chunks region

        public Vector3[] GetCombinedPointData()
        {
            int chunkCount = InitializedChunkCount;
            if (chunkCount == 0) return Array.Empty<Vector3>();
            int chunkPointCount = chunkGenerator.PointGridInfo.totalPoints;

            // TODO this can definitely be parallelized (copy each chunk to the array in a separate job; see NativeSlices)
            Vector3[] points = new Vector3[chunkCount * chunkPointCount];
            int iChunk = 0;
            foreach (ChunkContainer chunk in chunks)
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
            ChunkContainer cc = ChunkDataAt(position);
            Assert.IsTrue(cc.isLoaded, "Invalid call to get terrain height on a chunk that is still being loaded.");
            return cc.chunkData.InterpolateHeightAtWorldPosition(position);
        }

        public Vector3 FindLowestPointNearChunk(int2 coords, float threshold, int maxRadius)
        {
            Assert.IsTrue(CartesianMath.IsInSquareRadius(coords, playerChunk, loadRadius),
                $"Called on a chunk that is outside the current load radius. Start chunk: {coords}, center: {playerChunk}, radius: {loadRadius}");
            Assert.IsTrue(ChunkDataAt(coords).isLoaded,
                $"Called on a start chunk that is not initialized: {coords}.");

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

                Chunk currentChunk = ChunkAt(current + coords);
                Vector3 chunkLowestPoint = currentChunk.FindLowestPoint() + currentChunk.worldPos;

                if (chunkLowestPoint.y < threshold)
                {
                    return chunkLowestPoint;
                }

                if (chunkLowestPoint.y < bestPoint.y)
                {
                    bestPoint = chunkLowestPoint;
                }

                foreach (var dir in CartesianMath.NeighborDirections)
                {
                    int2 neighbor = current + dir;
                    if (!visited.Contains(neighbor) && math.abs(coords.x - neighbor.x) <= maxRadius && math.abs(coords.y - neighbor.y) <= maxRadius)
                    {
                        frontier.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
            }

            return bestPoint;
        }

        #endregion

        public void SetChunkMeshRenderersEnabled(bool enabled)
        {
            foreach (ChunkContainer chunk in chunks)
            {
                chunk.meshRenderer.enabled = enabled;
            }
        }

        #region Indexing shorthands

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ChunkContainer ChunkDataAt(int2 chunkCoordinates)
        {
            return chunks[WorldChunkCoordinatesToLocalChunkIndex(chunkCoordinates)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ChunkContainer ChunkDataAt(Vector3 worldPosition)
        {
            return chunks[WorldPositionToLocalChunkIndex(worldPosition)];
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
        public int2 LocalChunkIndexToWorldChunkCoordinates(int index)
        {
            index -= PlayerChunkIndexOffset;
            int2 localCoords = new int2
            {
                x = index / Diameter,
                y = index % Diameter,
            };
            return localCoords + playerChunk;
        }

        #endregion

        #region Gizmos/debug

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(chunkGenerator.CalculateChunkCenterPosition(playerChunk), Vector3.one * chunkGenerator.ChunkSize + Vector3.up * chunkGenerator.ChunkSize * 2);

            if (chunks != null)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < ChunkCount; i++)
                {
                    if (i == PlayerChunkIndexOffset) continue;

                    int2 chunkCoords = chunks[i].chunkData.coords;
                    Gizmos.DrawWireCube(chunkGenerator.CalculateChunkCenterPosition(chunkCoords), Vector3.one * chunkGenerator.ChunkSize + Vector3.up * chunkGenerator.ChunkSize);
                }
            }
        }

        #endregion
    }
}