using System;
using System.Collections.Generic;
using Roots.Util;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Roots.World
{
    [BurstCompile]
    public struct CreateVerticesJob : IJobParallelFor
    {
        public float stepSize;
        public int edgeVertexCount;
        public int edgeSampleCount;
        public float uvScale;
        
        [ReadOnly] public NativeArray<float> heights;
        [WriteOnly] public NativeArray<Vertex> vertices;

        public void Execute(int index)
        {
            int xi = index / edgeVertexCount; // point x index
            int zi = index % edgeVertexCount; // point z index
            
            // Calculate the indexer into the heights array using larger width of the sample grid
            int heightsIndexer = (xi + 1) * edgeSampleCount + zi + 1;

            float xPos = xi * stepSize;
            float zPos = zi * stepSize;

            // Position
            float3 position = new float3(xPos, heights[heightsIndexer], zPos);

            // Normal
            float heightL = heights[heightsIndexer - edgeSampleCount]; // x - 1
            float heightR = heights[heightsIndexer + edgeSampleCount]; // x + 1
            float heightD = heights[heightsIndexer - 1]; // z - 1
            float heightU = heights[heightsIndexer + 1]; // z + 1

            float3 gradientX = new float3(1, heightR - heightL, 0);
            float3 gradientZ = new float3(0, heightU - heightD, 1);

            float3 normal = math.normalize(math.cross(gradientZ, gradientX));

            // Uv
            float2 uv = new float2(xPos, zPos) * uvScale;
            
            vertices[index] = new Vertex
            {
                position = position,
                normal = normal,
                uv = uv,
            };
        }
    }

    [BurstCompile]
    public struct CreateMeshJob : IJob
    {
        public int edgeVertexCount;
        
        [WriteOnly] public Mesh.MeshData meshData;
        
        [ReadOnly] public NativeArray<Vertex> vertices;
        
        public void Execute()
        {
            meshData.SetIndexBufferParams((vertices.Length - edgeVertexCount) * 6, IndexFormat.UInt32);
            
            var attributes = new NativeArray<VertexAttributeDescriptor>(3, Allocator.Temp);
            attributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, dimension: 3);
            attributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, dimension: 3);
            attributes[2] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 2);
            meshData.SetVertexBufferParams(vertices.Length, attributes);

            var vertexData = meshData.GetVertexData<Vertex>();
            vertexData.CopyFrom(vertices);
            
            var indexData = meshData.GetIndexData<uint>();
            for (int vertIndex = 0, triIndex = 0; vertIndex < vertices.Length - edgeVertexCount; vertIndex++, triIndex += 6)
            {
                if ((vertIndex + 1) % edgeVertexCount == 0) continue;

                // tri 1
                indexData[triIndex] = (uint)vertIndex;
                indexData[triIndex + 1] = (uint)(vertIndex + 1);
                indexData[triIndex + 2] = (uint)(vertIndex + edgeVertexCount);
                // tri 2
                indexData[triIndex + 3] = (uint)(vertIndex + 1);
                indexData[triIndex + 4] = (uint)(vertIndex + edgeVertexCount + 1);
                indexData[triIndex + 5] = (uint)(vertIndex + edgeVertexCount);
            }
        }
    }

    public class GenerationJobData
    {
        public int indicesCount;
        public Vector2Int chunkPosition;
        
        public JobHandle jobHandle;
        public NativeArray<float> heightData;
        public NativeArray<Vertex> vertexData;
        public Mesh.MeshDataArray meshData;
        public Chunk chunk;
    }

    [CreateAssetMenu(fileName = "Terrain Chunk Generator", menuName = "Roots/Terrain Chunk Generator", order = 50)]
    public class TerrainChunkGenerator : ChunkGenerator
    {
        private const MeshUpdateFlags NoCalcMeshUpdateFlags = 
            MeshUpdateFlags.DontRecalculateBounds 
            | MeshUpdateFlags.DontValidateIndices 
            | MeshUpdateFlags.DontNotifyMeshUsers 
            | MeshUpdateFlags.DontResetBoneBounds;
        
        [SerializeField] private TerrainNoiseGenerator noiseGenerator;
        [SerializeField] private Material terrainMaterial;

        [Header("Detail")]
        [SerializeField] private int terrainMeshSubdivisions = 0; // Subsamples per unit
        [SerializeField] private int pointCloudStepSize = 0; // Subsample step size of mesh edge
        [SerializeField] private float uvScale = 1;

        // Grids
        private GridInfo vertexGridInfo;
        private GridInfo pointGridDescriptor;
        public override GridInfo VertexGridInfo => vertexGridInfo;
        public override GridInfo PointGridDescriptor => pointGridDescriptor;
        
        // Jobs
        private List<GenerationJobData> activeJobs = new();
        public override int ActiveChunkGenJobCount => activeJobs.Count;

        private void OnValidate()
        {
            Assert.IsTrue(pointCloudStepSize > 0);
            Assert.IsTrue(ChunkSize > 0);
            
            CalculateGridDescriptors();
        }

        private void Awake()
        {
            CalculateGridDescriptors();
        }

        private void CalculateGridDescriptors()
        {
            vertexGridInfo = GridInfo.FromSubdivisionsPerUnit(Mathf.FloorToInt(ChunkSize), terrainMeshSubdivisions);
            pointGridDescriptor = GridInfo.FromEdgeCount(ChunkSize, vertexGridInfo.edgeCount / pointCloudStepSize);
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

        public override Chunk CreateChunkAsync(Vector2Int chunkPosition, Transform parent = null)
        {
            Chunk chunk = new GameObject($"Chunk ({chunkPosition.x}, {chunkPosition.y})").AddComponent<Chunk>();
            chunk.transform.position = CalculateChunkOrigin(chunkPosition.x, chunkPosition.y);
            chunk.transform.localRotation = Quaternion.identity;
            if (parent)
            {
                chunk.transform.SetParent(parent, true);
            }
            
            int edgeSamplePointCount = vertexGridInfo.edgeCount + 2; // Generate 1 extra noise sample in each direction of the grid
            int totalSamplePointCount = edgeSamplePointCount * edgeSamplePointCount;

            Vector2 chunkWorldPosition = ((Vector2)chunkPosition * ChunkSize) - Vector2.one * vertexGridInfo.stepSize;

            GenerationJobData jobData = new()
            {
                indicesCount = vertexGridInfo.GetIndicesCount(),
                chunkPosition = chunkPosition,
                heightData = new NativeArray<float>(totalSamplePointCount, Allocator.Persistent),
                vertexData = new NativeArray<Vertex>(vertexGridInfo.totalPoints, Allocator.Persistent), // TODO figure out if it makes more sense to allocate this in the chunk (it does)
                meshData = Mesh.AllocateWritableMeshData(1),
                chunk = chunk
            };
            
            // Noise gen job
            var noiseJobHandle = noiseGenerator
                .CreateNoiseGenJob(edgeSamplePointCount, chunkWorldPosition, vertexGridInfo.stepSize, jobData.heightData)
                .Schedule(totalSamplePointCount, 3);

            // Vertex position/normal/uv job
            var vertexJobHandle = new CreateVerticesJob
            {
                heights = jobData.heightData,
                vertices = jobData.vertexData,
                stepSize = vertexGridInfo.stepSize,
                edgeVertexCount = vertexGridInfo.edgeCount,
                edgeSampleCount = edgeSamplePointCount,
                uvScale = uvScale,
            }.Schedule(vertexGridInfo.totalPoints, 4, noiseJobHandle);

            // Mesh data job
            var meshJobHandle = new CreateMeshJob
            {
                meshData = jobData.meshData[0],
                vertices = jobData.vertexData,
                edgeVertexCount = vertexGridInfo.edgeCount,
            }.Schedule(vertexJobHandle);
            
            jobData.jobHandle = meshJobHandle;
            
            activeJobs.Add(jobData);

            return chunk;
        }

        private void FinalizeChunkJob(GenerationJobData jobData)
        {
            Vector3[] points = GeneratePointCloudFromHeightData(jobData.heightData);

            jobData.chunk.gameObject.AddComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
            MeshFilter meshFilter = jobData.chunk.gameObject.AddComponent<MeshFilter>();
            
            var terrainMeshData = jobData.meshData[0]; 
            terrainMeshData.subMeshCount = 1;
            terrainMeshData.SetSubMesh(0, new SubMeshDescriptor(0, jobData.indicesCount), NoCalcMeshUpdateFlags);
            
            var terrainMesh = new Mesh();
            terrainMesh.name = $"Terrain Mesh ({jobData.chunkPosition.x}, {jobData.chunkPosition.y})";
            terrainMesh.bounds = new Bounds(new Vector3(ChunkSize * .5f, noiseGenerator.height, ChunkSize * .5f), new Vector3(ChunkSize, noiseGenerator.height * 2, ChunkSize));
            Mesh.ApplyAndDisposeWritableMeshData(jobData.meshData, terrainMesh, NoCalcMeshUpdateFlags);
            meshFilter.mesh = terrainMesh;
            
            jobData.chunk.InitAt(jobData.chunkPosition.x, jobData.chunkPosition.y, vertexGridInfo, jobData.vertexData, points);
        }

        public override Chunk CreateChunk(int x, int z, Transform parent = null)
        {
            throw new System.NotImplementedException();
        }

        private Vector3[] GeneratePointCloudFromHeightData(NativeArray<float> heights)
        {
            int edgeVertexCount = vertexGridInfo.edgeCount;
            int edgeSampleCount = edgeVertexCount + 2; // There are two more samples on either axis/one more in each grid direction
            // TODO there's an issue where the edge point count is not calculated correctly, when the point step size is set to 1. This shows up in the world as extra points drawn on top of each other at (0,0,0) of each chunk.
            int edgePointCount = pointGridDescriptor.edgeCount;
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

        public override float GetTerrainHeightAt(Vector3 worldPosition)
        {
            return noiseGenerator.GetNoise(worldPosition.x, worldPosition.z);
        }
    }
}