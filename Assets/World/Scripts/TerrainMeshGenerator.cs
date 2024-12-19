using Roots.Util;
using UnityEngine;
using UnityEngine.Assertions;

namespace Roots.World
{
    [CreateAssetMenu(fileName = "Terrain Chunk Generator", menuName = "Roots/Terrain Chunk Generator", order = 50)]
    public class TerrainChunkGenerator : ChunkGenerator
    {
        [SerializeField] private PathNoiseGenerator noiseGenerator;
        [SerializeField] private int terrainMeshResolution = 0; // Subsamples per unit
        [SerializeField] private float height = 4f;
        [SerializeField] private Material terrainMaterial;

        private int chunkVertexCount => (Mathf.FloorToInt(chunkSize) + 1) * (terrainMeshResolution + 1);

        public override Chunk CreateChunk(int x, int z, Transform parent = null)
        {
            Chunk chunk = new GameObject($"Chunk ({x}, {z})").AddComponent<Chunk>();
            chunk.transform.position = CalculateChunkCenter(x, z);
            chunk.transform.localRotation = Quaternion.identity;
            if (parent)
            {
                chunk.transform.SetParent(parent, true);
            }
            
            Assert.IsTrue(terrainMeshResolution >= 0);
            
            Vector3[] points = GeneratePoints(x, z);
            chunk.SetPoints(points);

            chunk.gameObject.AddComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
            MeshFilter meshFilter = chunk.gameObject.AddComponent<MeshFilter>();
            MeshCollider meshCollider = chunk.gameObject.AddComponent<MeshCollider>();
            
            Mesh terrainMesh = TerrainMeshFromPoints(points);
            terrainMesh.name = $"Terrain Mesh ({x}, {z})";
            meshFilter.sharedMesh = terrainMesh;
            meshCollider.sharedMesh = terrainMesh;
            
            chunk.LoadAt(x, z);
            return chunk;
        }

        private Vector3[] GeneratePoints(int worldX, int worldZ)
        {
            int size = chunkVertexCount;

            // TODO: scale to fit to a float chunk size (floors down)
            Vector3[] points = new Vector3[size * size];
            for (int x = 0, i = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++, i++)
                {
                    points[i] = new Vector3(x, noiseGenerator.GetNoise(x + worldX, z + worldZ) * height, z);
                }
            }
            return points;
        }

        private Mesh TerrainMeshFromPoints(Vector3[] points)
        {
            int size = chunkVertexCount;
            
            MeshBuilder mb = new MeshBuilder();
            for (int x = 0; x < size - 1; x++)
            {
                for (int z = 0; z < size - 1; z++)
                {
                    mb.AddQuadNew(points[z + size * x], points[z + 1 + size * x], points[z + 1 + size * (x + 1)], points[z + size * (x + 1)]);
                }
            }

            Mesh mesh = mb.GetMesh();
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}