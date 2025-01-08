using System.Collections.Generic;
using Roots.Util;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;

namespace Roots.World
{
    public class GenerationJobData
    {
        public Vector2Int chunkPosition;
        public JobHandle jobHandle;
        public NativeArray<float> heightData;
        public Chunk chunk;
    } 

    [CreateAssetMenu(fileName = "Terrain Chunk Generator", menuName = "Roots/Terrain Chunk Generator", order = 50)]
    public class TerrainChunkGenerator : ChunkGenerator
    {
        [SerializeField] private PathNoiseGenerator noiseGenerator;
        [SerializeField] private Material terrainMaterial;
        
        [Header("Detail")]
        [SerializeField] private int terrainMeshSubdivisions = 0; // Subsamples per unit
        [SerializeField] private int pointCloudStepSize = 0; // Subsample step size of mesh edge
        
        [Header("Terrain settings")]
        // [SerializeField] private float height = 4f;
        // [SerializeField] private float noisePremultiplier = 1.25f;
        
        private List<GenerationJobData> activeJobs = new();
        
        public override int ChunkEdgeVertexCount => Mathf.FloorToInt(ChunkSize) * (terrainMeshSubdivisions + 1) + 1;
        public override int ChunkEdgePointCount => ChunkEdgeVertexCount / pointCloudStepSize;

        public override int ActiveChunkGenJobCount => activeJobs.Count;

        private void OnValidate()
        {
            Assert.IsTrue(pointCloudStepSize > 0);
            Assert.IsTrue(ChunkSize > 0);
        }

        public override int UpdateChunkGenerationJobs()
        {
            if (ActiveChunkGenJobCount == 0) return 0;

            List<GenerationJobData> toRemove = new List<GenerationJobData>(activeJobs.Count);
            
            foreach (GenerationJobData jobData in activeJobs)
            {
                if (jobData.jobHandle.IsCompleted)
                {
                    jobData.jobHandle.Complete();
                    FinalizeChunkJob(jobData);
                    jobData.heightData.Dispose();
                    toRemove.Add(jobData);
                }
            }

            foreach (var jobData in toRemove)
            {
                activeJobs.Remove(jobData);
            }
            
            return toRemove.Count;
        }

        private void FinalizeChunkJob(GenerationJobData jobData)
        {
            Vertex[] vertices = GenerateVerticesFromHeightData(jobData.heightData);
            Vector3[] points = GeneratePointCloudFromHeightData(jobData.heightData);
            jobData.chunk.SetVertices(vertices, points);

            jobData.chunk.gameObject.AddComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
            MeshFilter meshFilter = jobData.chunk.gameObject.AddComponent<MeshFilter>();
            MeshCollider meshCollider = jobData.chunk.gameObject.AddComponent<MeshCollider>();

            Mesh terrainMesh = TerrainMeshFromVertices(vertices);
            terrainMesh.name = $"Terrain Mesh ({jobData.chunkPosition.x}, {jobData.chunkPosition.y})";
            meshFilter.sharedMesh = terrainMesh;
            meshCollider.sharedMesh = terrainMesh;

            jobData.chunk.InitAt(jobData.chunkPosition.x, jobData.chunkPosition.y);
        }

        // public List<GenerationJobData> ScheduleChunkGenerationJobs(Vector2Int[] positions, Transform parent = null)
        // {
        //     List<GenerationJobData> generationJobData =  new List<GenerationJobData>(positions.Length);
        //     
        //     int edgeVertexCount = ChunkEdgeVertexCount;
        //     float stepSize = 1.0f / (terrainMeshSubdivisions + 1);
        //     
        //     for (int i = 0; i < positions.Length; i++)
        //     {
        //         GenerationJobData jobData = new()
        //         {
        //             chunkPosition = positions[i],
        //             heightData = new NativeArray<float>(edgeVertexCount * edgeVertexCount, Allocator.TempJob),
        //             chunk = new GameObject($"Chunk ({positions[i].x}, {positions[i].y})").AddComponent<Chunk>()
        //         };
        //         var job = noiseGenerator.CreateNoiseGenJob(edgeVertexCount, stepSize, positions[i].x, positions[i].y, jobData.heightData);
        //         jobData.jobHandle = job.Schedule(edgeVertexCount * edgeVertexCount, 6);
        //         generationJobData[i] = jobData;
        //     }
        //
        //     return generationJobData;
        // }

        public override Chunk CreateChunkAsync(Vector2Int chunkPosition, Transform parent = null)
        {
            Chunk chunk = new GameObject($"Chunk ({chunkPosition.x}, {chunkPosition.y})").AddComponent<Chunk>();
            chunk.transform.position = CalculateChunkCenterPosition(chunkPosition.x, chunkPosition.y);
            chunk.transform.localRotation = Quaternion.identity;
            if (parent)
            {
                chunk.transform.SetParent(parent, true);
            }
            
            int edgeVertexCount = ChunkEdgeVertexCount;
            float stepSize = 1.0f / (terrainMeshSubdivisions + 1);
            
            GenerationJobData jobData = new()
            {
                chunkPosition = chunkPosition,
                heightData = new NativeArray<float>(edgeVertexCount * edgeVertexCount, Allocator.Persistent),
                chunk = chunk
            };
            var job = noiseGenerator.CreateNoiseGenJob(edgeVertexCount, stepSize, (Vector2)chunkPosition * ChunkSize, jobData.heightData);
            jobData.jobHandle = job.Schedule(edgeVertexCount * edgeVertexCount, 3);
            activeJobs.Add(jobData);

            return chunk;
        }

        public override Chunk CreateChunk(int x, int z, Transform parent = null)
        {
            Assert.IsTrue(terrainMeshSubdivisions >= 0);
            Chunk chunk = new GameObject($"Chunk ({x}, {z})").AddComponent<Chunk>();
            chunk.transform.position = CalculateChunkCenterPosition(x, z);
            chunk.transform.localRotation = Quaternion.identity;
            if (parent)
            {
                chunk.transform.SetParent(parent, true);
            }

            Vertex[] vertices = GenerateVertices(x, z);
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
            // TODO there's an issue where the edge point count is not calculated correctly, when the point step size is set to 1. This shows up in the world as extra points drawn on top of each other at (0,0,0) of each chunk.
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

        // private float GetTerrainModifiedNoise(float x, float z)
        // {
        //     float noise = noiseGenerator.GetNoise(x, z);
        //     return noise;
        // }
        //
        // private static float Smootherstep(float x)
        // {
        //     return 6 * x * x * x * x * x - 15 * x * x * x * x + 10 * x * x * x;
        // }

        private Vertex[] GenerateVertices(int worldX, int worldZ)
        {
            int edgeVertexCount = ChunkEdgeVertexCount;
            float stepSize = 1.0f / (terrainMeshSubdivisions + 1);

            Vertex[] vertices = new Vertex[edgeVertexCount * edgeVertexCount];
            // for (int xi = 0, i = 0; xi < edgeVertexCount; xi++)
            // {
            //     for (int zi = 0; zi < edgeVertexCount; zi++, i++)
            //     {
            //         // Position
            //         float x = xi * stepSize, z = zi * stepSize;
            //         vertices[i].position = new Vector3(x, GetTerrainModifiedNoise(x + worldX * ChunkSize, z + worldZ * ChunkSize) * height, z) - new Vector3(ChunkSize * 0.5f, 0, ChunkSize * 0.5f);
            //
            //         // TODO: optimization- cache noise samples in a structure that can be sampled similarly to the noise generator itself (save nearly 80% of the noise samples).
            //         // Normal
            //         float heightL = GetTerrainModifiedNoise(x - stepSize + worldX * ChunkSize, z + worldZ * ChunkSize) * height;
            //         float heightR = GetTerrainModifiedNoise(x + stepSize + worldX * ChunkSize, z + worldZ * ChunkSize) * height;
            //         float heightD = GetTerrainModifiedNoise(x + worldX * ChunkSize, z - stepSize + worldZ * ChunkSize) * height;
            //         float heightU = GetTerrainModifiedNoise(x + worldX * ChunkSize, z + stepSize + worldZ * ChunkSize) * height;
            //
            //         Vector3 gradientX = new Vector3(1, heightR - heightL, 0);
            //         Vector3 gradientZ = new Vector3(0, heightU - heightD, 1);
            //
            //         vertices[i].normal = Vector3.Cross(gradientZ, gradientX).normalized;
            //
            //         // Uv
            //         vertices[i].uv = new Vector2(xi * stepSize, zi * stepSize);
            //     }
            // }

            return vertices;
        }

        private Vertex[] GenerateVerticesFromHeightData(NativeArray<float> heights)
        {
            int edgeVertexCount = ChunkEdgeVertexCount;
            float stepSize = 1.0f / (terrainMeshSubdivisions + 1);
            
            Vertex[] vertices = new Vertex[heights.Length];
            for (int xi = 0, i = 0; xi < edgeVertexCount; xi++)
            {
                for (int zi = 0; zi < edgeVertexCount; zi++, i++)
                {
                    // Position
                    vertices[i].position = new Vector3(xi * stepSize, heights[i], zi * stepSize);
                    
                    // Normal
                    // TODO: CALC NORMAL
                    vertices[i].normal = Vector3.up;
                    
                    // Uv
                    vertices[i].uv = new Vector2(xi * stepSize, zi * stepSize);
                }
            }

            return vertices;
        }        
        
        private Vector3[] GeneratePointCloudFromHeightData(NativeArray<float> heights)
        {
            int edgeVertexCount = ChunkEdgeVertexCount;
            // TODO there's an issue where the edge point count is not calculated correctly, when the point step size is set to 1. This shows up in the world as extra points drawn on top of each other at (0,0,0) of each chunk.
            int edgePointCount = ChunkEdgePointCount;
            float stepSize = 1.0f / (terrainMeshSubdivisions + 1);
            
            Vector3[] points = new Vector3[edgePointCount * edgePointCount];
            for (int xi = 0, i = 0, j = 0; xi < edgeVertexCount; xi++)
            {
                for (int zi = 0; zi < edgeVertexCount; zi++, i++)
                {
                    if (xi % pointCloudStepSize == 0 && xi != edgeVertexCount - 1 && zi % pointCloudStepSize == 0 && zi != edgeVertexCount - 1)
                    {
                        points[j] = new Vector3(xi * stepSize, heights[i], zi * stepSize);
                        j++;
                    }
                }
            }
            
            return points;
        }

        private Mesh TerrainMeshFromVertices(Vertex[] vertices)
        {
            int vertexCount = ChunkEdgeVertexCount;

            MeshBuilder mb = new MeshBuilder(vertices.Length * 4, vertices.Length * 6);
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