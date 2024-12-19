using UnityEngine;

namespace Roots.World
{
    [CreateAssetMenu(fileName = "Terrain Chunk Generator", menuName = "Roots/Terrain Chunk Generator", order = 50)]
    public class TerrainChunkGenerator : ChunkGenerator
    {
        [SerializeField] private PathNoiseGenerator noiseGenerator;
        
        public override Chunk CreateChunk(int x, int z, Transform parent = null)
        {
            throw new System.NotImplementedException();
        }
    }
}