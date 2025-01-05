using Roots.Util;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Roots.World
{
    [CreateAssetMenu(fileName = "Terrain Chunk Generator", menuName = "Roots/Terrain Chunk Generator", order = 50)]
    public class TerrainChunkGenerator : ChunkGenerator
    {
        [SerializeField] private PathNoiseGenerator noiseGenerator;
        [SerializeField] private int terrainMeshSubdivisions = 0; // Subsamples per unit
        [SerializeField] private int pointCloudStepSize = 0;
        [SerializeField] private float height = 4f;
        [SerializeField] private float noisePremultiplier = 1.25f;
        [SerializeField] private Material terrainMaterial;

        public override int ChunkEdgeVertexCount => Mathf.FloorToInt(ChunkSize) * (terrainMeshSubdivisions + 1) + 1;
        public override int ChunkEdgePointCount => ChunkEdgeVertexCount / pointCloudStepSize;

        private void OnValidate()
        {
            Assert.IsTrue(pointCloudStepSize > 0);
            Assert.IsTrue(terrainMeshSubdivisions > 0);
            Assert.IsTrue(ChunkSize > 0);
        }

        public override Chunk CreateChunk(int x, int z, Transform parent = null)
        {
            Chunk chunk = new GameObject($"Chunk ({x}, {z})").AddComponent<Chunk>();
            chunk.transform.position = CalculateChunkCenter(x, z);
            chunk.transform.localRotation = Quaternion.identity;
            if (parent)
            {
                chunk.transform.SetParent(parent, true);
            }

            Assert.IsTrue(terrainMeshSubdivisions >= 0);

            Vertex[] vertices = GeneratePoints(x, z);
            Vector3[] points = GeneratePointCloudFromVertices(vertices);
            chunk.SetVertices(vertices, points);

            chunk.gameObject.AddComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
            MeshFilter meshFilter = chunk.gameObject.AddComponent<MeshFilter>();
            MeshCollider meshCollider = chunk.gameObject.AddComponent<MeshCollider>();

            Mesh terrainMesh = TerrainMeshFromVertices(vertices);
            terrainMesh.name = $"Terrain Mesh ({x}, {z})";
            meshFilter.sharedMesh = terrainMesh;
            meshCollider.sharedMesh = terrainMesh;

            chunk.InitAt(x, z);
            return chunk;
        }

        private Vector3[] GeneratePointCloudFromVertices(Vertex[] vertices)
        {
            int edgeVertexCount = ChunkEdgeVertexCount;
            int edgePointCount = ChunkEdgePointCount;
            
            Vector3[] points = new Vector3[edgePointCount * edgePointCount];
            for (int xi = 0, i = 0, j = 0; xi < edgeVertexCount; xi++)
            {
                for (int zi = 0; zi < edgeVertexCount; zi++, i++)
                {
                    if (xi % pointCloudStepSize == 0 && xi != edgeVertexCount - 1 && zi % pointCloudStepSize == 0 && zi != edgeVertexCount - 1)
                    {
                        points[j] = vertices[i].position;
                        j++;
                    }
                }
            }
            return points;
        }

        private float GetTerrainModifiedNoise(float x, float z)
        {
            float noise = noiseGenerator.GetNoise(x, z);
            noise *= noisePremultiplier;
            noise = Smootherstep(noise);
            return noise;
        }

        private static float Smootherstep(float x)
        {
            return 6 * x * x * x * x * x - 15 * x * x * x * x + 10 * x * x * x;
        }

        private Vertex[] GeneratePoints(int worldX, int worldZ)
        {
            int edgeVertexCount = ChunkEdgeVertexCount;
            float stepSize = 1.0f / (terrainMeshSubdivisions + 1);

            Vertex[] vertices = new Vertex[edgeVertexCount * edgeVertexCount];
            for (int xi = 0, i = 0; xi < edgeVertexCount; xi++)
            {
                for (int zi = 0; zi < edgeVertexCount; zi++, i++)
                {
                    // Position
                    float x = xi * stepSize, z = zi * stepSize;
                    vertices[i].position = new Vector3(x, GetTerrainModifiedNoise(x + worldX * ChunkSize, z + worldZ * ChunkSize) * height, z) - new Vector3(ChunkSize * 0.5f, 0, ChunkSize * 0.5f);

                    // TODO: optimization- cache noise samples in a structure that can be sampled similarly to the noise generator itself (save nearly 80% of the noise samples).
                    // Normal
                    float heightL = GetTerrainModifiedNoise(x - stepSize + worldX * ChunkSize, z + worldZ * ChunkSize) * height;
                    float heightR = GetTerrainModifiedNoise(x + stepSize + worldX * ChunkSize, z + worldZ * ChunkSize) * height;
                    float heightD = GetTerrainModifiedNoise(x + worldX * ChunkSize, z - stepSize + worldZ * ChunkSize) * height;
                    float heightU = GetTerrainModifiedNoise(x + worldX * ChunkSize, z + stepSize + worldZ * ChunkSize) * height;

                    Vector3 gradientX = new Vector3(1, heightR - heightL, 0);
                    Vector3 gradientZ = new Vector3(0, heightU - heightD, 1);

                    vertices[i].normal = Vector3.Cross(gradientZ, gradientX).normalized;

                    // Uv
                    vertices[i].uv = new Vector2(xi * stepSize, zi * stepSize);
                }
            }

            return vertices;
        }

        private Mesh TerrainMeshFromVertices(Vertex[] vertices)
        {
            int vertexCount = ChunkEdgeVertexCount;

            MeshBuilder mb = new MeshBuilder();
            for (int x = 0; x < vertexCount - 1; x++)
            {
                for (int z = 0; z < vertexCount - 1; z++)
                {
                    mb.AddQuadNew(
                        vertices[z + vertexCount * x],
                        vertices[z + 1 + vertexCount * x],
                        vertices[z + 1 + vertexCount * (x + 1)],
                        vertices[z + vertexCount * (x + 1)]
                    );
                }
            }

            Mesh mesh = mb.GetMesh();
            return mesh;
        }
    }
}