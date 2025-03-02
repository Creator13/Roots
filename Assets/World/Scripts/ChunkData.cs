using Roots.Util;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Roots.World
{
    public struct Chunk
    {
        public int2 coords;
        public Vector3 worldPos;
        public NativeArray<Vertex> vertices;
        public GridInfo gridInfo;
        public NativeArray<Vector3> points;

        public void Dispose()
        {
            vertices.Dispose();
            points.Dispose();
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
        // Position assumes a WORLD position
        public float InterpolateHeightAtWorldPosition(Vector3 worldPosition)
        {
            Vector3 position = worldPosition - worldPos; // convert to local position
            return InterpolateHeightAt(position);
        }
        
        public float InterpolateHeightAt(float3 position)
        {
            Assert.IsTrue(position.x >= 0 && position.x <= gridInfo.size && position.z >= 0 && position.z <= gridInfo.size,
                "World position not inside chunk bounds (GetHeightAt called on incorrect chunk for world position)");

            int xi_low = (int)math.floor(position.x / gridInfo.stepSize);
            int zi_low = (int)math.floor(position.z / gridInfo.stepSize);
            int lowestVertIndex = gridInfo.GetIndexFromXZ(xi_low, zi_low);

            Vector3 posA = vertices[lowestVertIndex].position;
            Vector3 posB = vertices[lowestVertIndex + 1].position;
            Vector3 posC = vertices[lowestVertIndex + gridInfo.edgeCount].position;
            Vector3 posD = vertices[lowestVertIndex + gridInfo.edgeCount + 1].position;

            float tx = (position.x - posA.x) / gridInfo.stepSize;
            float tz = (position.z - posA.z) / gridInfo.stepSize;

            float h0 = math.lerp(posA.y, posB.y, tz);
            float h1 = math.lerp(posC.y, posD.y, tz);
            return math.lerp(h0, h1, tx);
        }
    }
}