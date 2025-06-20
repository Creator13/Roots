using Roots.Util;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Roots.World.Chunking
{
    public struct Chunk
    {
        public int2 coords;
        public Vector3 worldPos;
        public GridInfo grid;

        public NativeArray<Vertex> vertices;
        public NativeArray<Vector3> points;
        // public NativeArray<float> heights;
        public ChunkHeightmap heightmap;

        public void Dispose()
        {
            vertices.Dispose();
            points.Dispose();
            heightmap.Dispose();
        }

        public Vector3 FindLowestPoint()
        {
            Vector3 lowestPoint = Vector3.positiveInfinity;
            foreach (Vertex vertex in vertices)
            {
                if (vertex.position.y < lowestPoint.y)
                {
                    lowestPoint = vertex.position;
                }
            }

            return lowestPoint;
        }

        // Position is a v3 to use the transform.position directly but the y value is entirely ignored. 
        public float InterpolateHeightAtWorldPosition(Vector3 worldPos)
        {
            Vector3 position = worldPos - this.worldPos; // convert to local position

            Assert.IsTrue(position.x >= 0 && position.x <= grid.size && position.z >= 0 && position.z <= grid.size,
                "World position not inside chunk bounds (GetHeightAt called on incorrect chunk for world position)");
            
            // return InterpolateHeightAt(position);
            return heightmap.Interpolate(position);
        }

        // Position is a v3 to use the transform.position directly but the y value is entirely ignored. 
        // Position assumes a local chunk position (one that lies within the range [0, chunk size] inclusive). 
        // Uses simple bilinear interpolation, which results in points that lie on the plane defined by four vertices that lie on one plane
        // (note that not all four vertices of a terrain quad will lie on one plane, but this is an inaccuracy that doesn't matter much)
        public float InterpolateHeightAt(float3 localPos)
        {
            int xi_low = (int)math.floor(localPos.x / grid.stepSize);
            int zi_low = (int)math.floor(localPos.z / grid.stepSize);
            int lowestVertIndex = grid.GetIndexFromXZ(xi_low, zi_low);

            Vector3 posA = vertices[lowestVertIndex].position;
            Vector3 posB = vertices[lowestVertIndex + 1].position;
            Vector3 posC = vertices[lowestVertIndex + grid.edgeCount].position;
            Vector3 posD = vertices[lowestVertIndex + grid.edgeCount + 1].position;

            float tx = (localPos.x - posA.x) / grid.stepSize;
            float tz = (localPos.z - posA.z) / grid.stepSize;

            float h0 = math.lerp(posA.y, posB.y, tz);
            float h1 = math.lerp(posC.y, posD.y, tz);
            return math.lerp(h0, h1, tx);
        }

        public Bounds GetBounds(float height)
        {
            return new Bounds(CalculateChunkCenterPosition(coords, grid.size), new Vector3(grid.size, height, grid.size));
        }
        
        public static Vector3 CalculateChunkCenterPosition(int2 coords, float chunkSize)
        {
            return new Vector3(coords.x * chunkSize + .5f * chunkSize, 0, coords.y * chunkSize + .5f * chunkSize);
        }
    }
}