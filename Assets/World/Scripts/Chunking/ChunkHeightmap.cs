using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Roots.World.Chunking
{
    public struct ChunkHeightmap
    {
        public NativeArray<float> heights;
        
        private readonly GridInfo gridDesc;
        private readonly int borderWidth;
        private readonly int edgePointCount;

        public bool IsCreated => heights.IsCreated;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Interpolate(float3 localPos)
        {
            int xi_low = (int)math.floor(localPos.x / gridDesc.stepSize) + borderWidth;
            int zi_low = (int)math.floor(localPos.z / gridDesc.stepSize) + borderWidth;
            int lowestVertIndex = xi_low * edgePointCount + zi_low;

            float posA = heights[lowestVertIndex];
            float posB = heights[lowestVertIndex + 1];
            float posC = heights[lowestVertIndex + edgePointCount];
            float posD = heights[lowestVertIndex + edgePointCount + 1];

            float tx = (localPos.x - xi_low * gridDesc.stepSize) / gridDesc.stepSize;
            float tz = (localPos.z - zi_low * gridDesc.stepSize) / gridDesc.stepSize;

            float h0 = math.lerp(posA, posB, tz);
            float h1 = math.lerp(posC, posD, tz);
            return math.lerp(h0, h1, tx);
        }

        public void Dispose()
        {
            heights.Dispose();
        }

        private ChunkHeightmap(NativeArray<float> heights, GridInfo gridDesc,  int borderWidth, int edgePointCount)
        {
            this.heights = heights;
            this.gridDesc = gridDesc;
            this.borderWidth = borderWidth;
            this.edgePointCount = edgePointCount;
        }

        public static ChunkHeightmap Create(GridInfo chunkGrid, int edgeExtension = 1)
        {
            int edgePointCount = chunkGrid.edgeCount + 2 * edgeExtension;
            int totalSamplePointCount = edgePointCount * edgePointCount;

            return new ChunkHeightmap(
                new NativeArray<float>(totalSamplePointCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
                chunkGrid,
                edgeExtension,
                edgePointCount
            );
        }
    }
}