using UnityEngine;

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

        public override int ChunkEdgeVertexCount => 100;

        public override Chunk CreateChunk(int x, int z, Transform parent = null)
        {
            Chunk chunk = Instantiate(chunkPrefab, CalculateChunkCenter(x, z), Quaternion.identity, parent);
            chunk.gameObject.name = $"Chunk ({x}, {z})";
            chunk.InitAt(x, z);
            SpawnTrees(chunk.transform);
            return chunk;
        }

        private void SpawnTrees(Transform chunkTransform)
        {
            float targetTreesPerChunk = chunkSize * chunkSize * treeDensity;
            for (int i = 0; i < targetTreesPerChunk; i++)
            {
                float x = Random.Range(0, chunkSize) - chunkSize * .5f;
                float z = Random.Range(0, chunkSize) - chunkSize * .5f;
                float noise = noiseGenerator.GetNoise(chunkTransform.position.x + x, chunkTransform.position.z + z);
                if (Random.value < noise)
                {
                    Transform treeInstance = Instantiate(treePrefab, chunkTransform, true);
                    treeInstance.SetLocalPositionAndRotation(new Vector3(x, 0, z), Quaternion.Euler(0, Random.Range(-180, 180), 0));
                }
            }
        }
    }
}