using Unity.Mathematics;
using UnityEngine;

namespace Roots.World
{
    public abstract class ChunkGenerator : ScriptableObject
    {
        [field: SerializeField] public float ChunkSize { get; private set; }
        
        public abstract GridInfo PointGridInfo { get; }
        public abstract GridInfo VertexGridInfo { get; }

        public abstract int ActiveChunkGenJobCount { get; }

        public abstract Chunk CreateChunkAsync(int2 chunkPosition, Transform transform);
        public abstract void CreateChunkAsync(int2 chunkPosition, ChunkLoader.ChunkContainer container);
        public abstract int UpdateChunkGenerationJobs();

        public abstract float GetTerrainHeightAt(Vector3 worldPosition);

        public Vector3 CalculateChunkOrigin(int x, int z)
        {
            return new Vector3(x * ChunkSize, 0, z * ChunkSize);
        }
        public Vector3 CalculateChunkOrigin(int2 coords)
        {
            return new Vector3(coords.x * ChunkSize, 0, coords.y * ChunkSize);
        }
        
        public Vector3 CalculateChunkCenterPosition(int2 coords)
        {
            return new Vector3(coords.x * ChunkSize + .5f * ChunkSize, 0, coords.y * ChunkSize + .5f * ChunkSize);
        }
    }
}