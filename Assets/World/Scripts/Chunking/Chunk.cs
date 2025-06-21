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