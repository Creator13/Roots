using UnityEngine;

namespace Roots.World
{
    public abstract class ChunkGenerator : ScriptableObject
    {
        [field: SerializeField] public float ChunkSize { get; private set; }

        // public abstract int ChunkEdgeVertexCount { get; }

        // public abstract int ChunkEdgePointCount { get; }

        // public int ChunkPointCount
        // {
        //     get
        //     {
        //         int edgeCount = ChunkEdgePointCount;
        //         return edgeCount * edgeCount;
        //     }
        // }
        
        public abstract GridInfo PointGridDescriptor { get; }
        public abstract GridInfo VertexGridInfo { get; }

        public abstract int ActiveChunkGenJobCount { get; }

        public abstract Chunk CreateChunk(int x, int z, Transform parent = null);
        public abstract Chunk CreateChunkAsync(Vector2Int position, Transform transform);
        public abstract int UpdateChunkGenerationJobs();

        public abstract float GetTerrainHeightAt(Vector3 worldPosition);

        public Vector3 CalculateChunkOrigin(int x, int z)
        {
            return new Vector3(x * ChunkSize, 0, z * ChunkSize);
        }
        
        public Vector3 CalculateChunkCenterPosition(int x, int z)
        {
            return new Vector3(x * ChunkSize + .5f * ChunkSize, 0, z * ChunkSize + .5f * ChunkSize);
        }

        public Vector2Int WorldPositionToChunkCoordinates(Vector3 worldPosition)
        {
            int chunkX = Mathf.FloorToInt(worldPosition.x / ChunkSize);
            int chunkZ = Mathf.FloorToInt(worldPosition.z / ChunkSize);
            
            return new Vector2Int(chunkX, chunkZ);
        }
    }
}