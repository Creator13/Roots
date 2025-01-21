using UnityEngine;
using Random = UnityEngine.Random;

namespace Roots.World
{
    [CreateAssetMenu(fileName = "Forest Chunk Generator", menuName = "Roots/Forest Chunk Generator", order = 50)]
    public class ForestChunkGenerator : ChunkGenerator
    {
        [SerializeField] private Chunk chunkPrefab;
        [SerializeField] private Transform treePrefab;
        
        [Space]
        [SerializeField] private float treeDensity;
        [SerializeField] private PathNoiseGenerator noiseGenerator;

        public override int ChunkEdgeVertexCount => 11;
        public override int ChunkEdgePointCount => 11;

        public override int ActiveChunkGenJobCount => throw new System.NotImplementedException();

        public override Chunk CreateChunk(int x, int z, Transform parent = null)
        {
            Chunk chunk = Instantiate(chunkPrefab, CalculateChunkOrigin(x, z), Quaternion.identity, parent);
            chunk.gameObject.name = $"Chunk ({x}, {z})";
            chunk.InitAt(x, z, null, null);
            SpawnTrees(chunk.transform);
            return chunk;
        }

        public override int UpdateChunkGenerationJobs()
        {
            throw new System.NotImplementedException();
        }
        public override Chunk CreateChunkAsync(Vector2Int position, Transform transform)
        {
            throw new System.NotImplementedException();
        }

        private void SpawnTrees(Transform chunkTransform)
        {
            float targetTreesPerChunk = ChunkSize * ChunkSize * treeDensity;
            for (int i = 0; i < targetTreesPerChunk; i++)
            {
                float x = Random.Range(0, ChunkSize) - ChunkSize * .5f;
                float z = Random.Range(0, ChunkSize) - ChunkSize * .5f;
                float noise = noiseGenerator.GetNoise(chunkTransform.position.x + x, chunkTransform.position.z + z);
                if (Random.value < noise)
                {
                    Transform treeInstance = Instantiate(treePrefab, chunkTransform, true);
                    treeInstance.SetLocalPositionAndRotation(new Vector3(x, 0, z), Quaternion.Euler(0, Random.Range(-180, 180), 0));
                }
            }
        }

        public override float GetTerrainHeightAt(Vector3 worldPosition)
        {
            return 0;
        }
    }
}