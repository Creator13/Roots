using UnityEngine;

namespace Roots.World
{
    public abstract class ChunkGenerator : ScriptableObject
    {
        [field: SerializeField] public float ChunkSize { get; private set; }
        
        public abstract int ChunkEdgeVertexCount { get; }

        public int ChunkVertexCount
        {
            get
            {
                int edgeCount = ChunkEdgeVertexCount;
                return edgeCount * edgeCount;
            }
        }
        
        public abstract int ChunkEdgePointCount { get; }

        public int ChunkPointCount
        {
            get
            {
                int edgeCount = ChunkEdgePointCount;
                return edgeCount * edgeCount;
            }
        }

        public abstract Chunk CreateChunk(int x, int z, Transform parent = null);
        
        public Vector3 CalculateChunkCenter(int x, int z)
        {
            return new Vector3(x * ChunkSize + .5f * ChunkSize, 0, z * ChunkSize + .5f * ChunkSize);
        }
    }
}