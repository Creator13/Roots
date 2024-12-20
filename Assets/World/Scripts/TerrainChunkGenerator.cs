using System;
using Roots.Util;
using Unity.Mathematics;
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
        [SerializeField] private float terrainPower = 1;
        [SerializeField] private float noiseHeightMultiplier = 1.25f;
        [SerializeField] private Material terrainMaterial;

        private int chunkVertexCount => Mathf.FloorToInt(chunkSize) * (terrainMeshResolution + 1) + 1;
        
        private void OnValidate()
        {
            Assert.IsTrue(terrainMeshResolution > 0);
            Assert.IsTrue(terrainPower > 0);
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
            
            Assert.IsTrue(terrainMeshResolution >= 0);
            
            Vertex[] points = GeneratePoints(x, z);
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

        private float GetTerrainModifiedNoise(float x, float z)
        {
            float noise = noiseGenerator.GetNoise(x, z);
            noise *= noiseHeightMultiplier;
            noise = math.pow(noise, terrainPower);
            return noise;
        }
        
        private Vertex[] GeneratePoints(int worldX, int worldZ)
        {
            int vertexCount = chunkVertexCount;
            float stepSize = 1.0f / (terrainMeshResolution + 1);
            
            Vertex[] vertices = new Vertex[vertexCount * vertexCount];
            for (int xi = 0, i = 0; xi < vertexCount; xi++)
            {
                for (int zi = 0; zi < vertexCount; zi++, i++)
                {
                    // Position
                    float x = xi * stepSize, z = zi * stepSize;
                    vertices[i].position = new Vector3(x, GetTerrainModifiedNoise(x + worldX * chunkSize, z + worldZ * chunkSize) * height, z);
                    
                    // Normal
                    float heightL = GetTerrainModifiedNoise(x - stepSize + worldX * chunkSize, z + worldZ * chunkSize) * height;
                    float heightR = GetTerrainModifiedNoise(x + stepSize + worldX * chunkSize, z + worldZ * chunkSize) * height;
                    float heightD = GetTerrainModifiedNoise(x + worldX * chunkSize, z - stepSize + worldZ * chunkSize) * height;
                    float heightU = GetTerrainModifiedNoise(x + worldX * chunkSize, z + stepSize + worldZ * chunkSize) * height;

                    Vector3 gradientX = new Vector3(1, heightR - heightL, 0);
                    Vector3 gradientZ = new Vector3(0, heightU - heightD, 1);

                    vertices[i].normal = Vector3.Cross(gradientZ, gradientX).normalized;
                    
                    // Uv
                    vertices[i].uv = new Vector2(xi * stepSize, zi * stepSize);
                }
            }
            return vertices;
        }

        private Mesh TerrainMeshFromPoints(Vertex[] vertices)
        {
            int vertexCount = chunkVertexCount;
            
            MeshBuilder mb = new MeshBuilder();
            for (int x = 0; x < vertexCount - 1; x++)
            {
                for (int z = 0; z < vertexCount - 1; z++)
                {
                    mb.AddQuadNew(vertices[z + vertexCount * x], vertices[z + 1 + vertexCount * x], vertices[z + 1 + vertexCount * (x + 1)], vertices[z + vertexCount * (x + 1)]);
                }
            }

            Mesh mesh = mb.GetMesh();
            return mesh;
        }
    }
}