using System.Collections.Generic;
using Roots.Util;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
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
        [SerializeField] private TerrainNoiseGenerator noiseGenerator;
        [SerializeField] private Material terrainMaterial;

        [Header("Detail")]
        [SerializeField] private int terrainMeshSubdivisions = 0; // Subsamples per unit
        [SerializeField] private int pointCloudStepSize = 0; // Subsample step size of mesh edge
        [SerializeField] private float uvScale = 1;
        
        private List<GenerationJobData> activeJobs = new();

        public Vector3 center;
        
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

            jobData.chunk.gameObject.AddComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
            MeshFilter meshFilter = jobData.chunk.gameObject.AddComponent<MeshFilter>();
            MeshCollider meshCollider = jobData.chunk.gameObject.AddComponent<MeshCollider>();
            meshCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | MeshColliderCookingOptions.UseFastMidphase;

            Mesh terrainMesh = TerrainMeshFromVertices(vertices);
            terrainMesh.name = $"Terrain Mesh ({jobData.chunkPosition.x}, {jobData.chunkPosition.y})";
            meshFilter.sharedMesh = terrainMesh;
            Mesh colliderMesh = ColliderMeshFromVertices(vertices);
            colliderMesh.name = $"Collider Mesh ({jobData.chunkPosition.x}, {jobData.chunkPosition.y})";
            meshCollider.sharedMesh = colliderMesh;

            jobData.chunk.InitAt(jobData.chunkPosition.x, jobData.chunkPosition.y, vertices, points);
        }

        public override Chunk CreateChunkAsync(Vector2Int chunkPosition, Transform parent = null)
        {
            Chunk chunk = new GameObject($"Chunk ({chunkPosition.x}, {chunkPosition.y})").AddComponent<Chunk>();
            chunk.transform.position = CalculateChunkOrigin(chunkPosition.x, chunkPosition.y);
            chunk.transform.localRotation = Quaternion.identity;
            if (parent)
            {
                chunk.transform.SetParent(parent, true);
            }

            int edgeVertexCount = ChunkEdgeVertexCount + 2; // Generate 1 extra vertex in each direction of the grid
            float stepSize = 1.0f / (terrainMeshSubdivisions + 1);

            int totalPointCount = edgeVertexCount * edgeVertexCount;

            GenerationJobData jobData = new()
            {
                chunkPosition = chunkPosition,
                heightData = new NativeArray<float>(totalPointCount, Allocator.Persistent),
                chunk = chunk
            };
            var job = noiseGenerator.CreateNoiseGenJob(edgeVertexCount, stepSize, (Vector2)chunkPosition * ChunkSize, jobData.heightData);
            jobData.jobHandle = job.Schedule(totalPointCount, 3);
            activeJobs.Add(jobData);

            return chunk;
        }

        public override Chunk CreateChunk(int x, int z, Transform parent = null)
        {
            throw new System.NotImplementedException();
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

        private Vertex[] GenerateVerticesFromHeightData(NativeArray<float> heights)
        {
            int edgeVertexCount = ChunkEdgeVertexCount;
            int edgeSampleCount = edgeVertexCount + 2; // There are two more samples on either axis/one more in each grid direction
            float stepSize = 1.0f / (terrainMeshSubdivisions + 1);

            Vertex[] vertices = new Vertex[edgeVertexCount * edgeVertexCount];
            for (int xi = 0, i = 0; xi < edgeVertexCount; xi++)
            {
                for (int zi = 0; zi < edgeVertexCount; zi++, i++)
                {
                    // Calculate the indexer into the heights array using larger width of the sample grid
                    int heightsIndexer = (xi + 1) * edgeSampleCount + zi + 1;

                    float xPos = xi * stepSize;
                    float zPos = zi * stepSize;

                    // Position
                    vertices[i].position = new Vector3(xPos, heights[heightsIndexer], zPos);

                    // Normal
                    float heightL = heights[heightsIndexer - edgeSampleCount]; // x - 1
                    float heightR = heights[heightsIndexer + edgeSampleCount]; // x + 1
                    float heightD = heights[heightsIndexer - 1]; // z - 1
                    float heightU = heights[heightsIndexer + 1]; // z + 1

                    Vector3 gradientX = new Vector3(1, heightR - heightL, 0);
                    Vector3 gradientZ = new Vector3(0, heightU - heightD, 1);

                    vertices[i].normal = Vector3.Cross(gradientZ, gradientX).normalized;

                    // Uv
                    vertices[i].uv = new Vector2(xPos, zPos) * uvScale;
                }
            }

            return vertices;
        }

        private Vector3[] GeneratePointCloudFromHeightData(NativeArray<float> heights)
        {
            int edgeVertexCount = ChunkEdgeVertexCount;
            int edgeSampleCount = edgeVertexCount + 2; // There are two more samples on either axis/one more in each grid direction
            // TODO there's an issue where the edge point count is not calculated correctly, when the point step size is set to 1. This shows up in the world as extra points drawn on top of each other at (0,0,0) of each chunk.
            int edgePointCount = ChunkEdgePointCount;
            float stepSize = 1.0f / (terrainMeshSubdivisions + 1);

            Vector3[] points = new Vector3[edgePointCount * edgePointCount];
            for (int xi = 0, j = 0; xi < edgeVertexCount; xi++)
            {
                for (int zi = 0; zi < edgeVertexCount; zi++)
                {
                    if (xi % pointCloudStepSize == 0 && xi != edgeVertexCount - 1 && zi % pointCloudStepSize == 0 && zi != edgeVertexCount - 1)
                    {
                        int heightsIndexer = (xi + 1) * edgeSampleCount + zi + 1;
                        points[j] = new Vector3(xi * stepSize, heights[heightsIndexer], zi * stepSize);
                        j++;
                    }
                }
            }

            return points;
        }

        private Mesh ColliderMeshFromVertices(Vertex[] vertices)
        {
            int edgeVertexCount = ChunkEdgeVertexCount;
            // TODO there's an issue where the edge point count is not calculated correctly, when the point step size is set to 1. This shows up in the world as extra points drawn on top of each other at (0,0,0) of each chunk.
            int colliderMeshEdgeVertexCount = (int)ChunkSize + 1;

            Vector3[] colliderVerts = new Vector3[colliderMeshEdgeVertexCount * colliderMeshEdgeVertexCount];
            for (int xi = 0, i = 0, j = 0; xi < edgeVertexCount; xi++)
            {
                for (int zi = 0; zi < edgeVertexCount; zi++, i++)
                {
                    if (xi % (terrainMeshSubdivisions + 1) == 0 && zi % (terrainMeshSubdivisions + 1) == 0)
                    {
                        colliderVerts[j] = vertices[i].position;
                        j++;
                    }
                }
            }

            var tris = new int[(colliderVerts.Length - colliderMeshEdgeVertexCount) * 6];
            for (int vertIndex = 0, triIndex = 0; vertIndex < colliderVerts.Length - colliderMeshEdgeVertexCount; vertIndex++, triIndex += 6)
            {
                if ((vertIndex + 1) % colliderMeshEdgeVertexCount == 0) continue;
                
                // tri 1
                tris[triIndex] = vertIndex;
                tris[triIndex + 1] = vertIndex + 1;
                tris[triIndex + 2] = vertIndex + colliderMeshEdgeVertexCount;
                // tri 2
                tris[triIndex + 3] = vertIndex + 1;
                tris[triIndex + 4] = vertIndex + colliderMeshEdgeVertexCount + 1;
                tris[triIndex + 5] = vertIndex + colliderMeshEdgeVertexCount;
            }

            var mesh = new Mesh();
            mesh.SetVertices(colliderVerts);
            mesh.SetTriangles(tris, 0);
            return mesh;
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

        public override float GetTerrainHeightAt(Vector3 worldPosition)
        {
            return noiseGenerator.GetNoise(worldPosition.x, worldPosition.z);
        }
    }
}