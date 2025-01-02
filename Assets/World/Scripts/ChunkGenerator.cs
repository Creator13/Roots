using UnityEngine;

namespace Roots.World
{
    public abstract class ChunkGenerator : ScriptableObject
    {
        [field: SerializeField] public float chunkSize { get; private set; }
        
        public abstract int ChunkEdgeVertexCount { get; }

        public int ChunkVertexCount
        {
            get
            {
                int edgeCount = ChunkEdgeVertexCount;
                return edgeCount * edgeCount;
            }
        }

        public abstract Chunk CreateChunk(int x, int z, Transform parent = null);
        
        public Vector3 CalculateChunkCenter(int x, int z)
        {
            return new Vector3(x * chunkSize - .5f * chunkSize, 0, z * chunkSize - .5f * chunkSize);
        }
    }
}