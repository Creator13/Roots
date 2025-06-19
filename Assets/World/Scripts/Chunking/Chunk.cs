using System;
using Roots.Util;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Roots.World.Chunking
{
    public struct Chunk : IDisposable
    {
        public int2 coords;
        public Vector3 worldPos;
        public GridInfo gridInfo;
        
        public NativeArray<Vertex> vertices;
        public NativeArray<Vector3> points;
        public NativeArray<float> heights;

        public void Dispose()
        {
            vertices.Dispose();
            points.Dispose();
            heights.Dispose();
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
            return InterpolateHeightAt(position);
        }

        // Position is a v3 to use the transform.position directly but the y value is entirely ignored. 
        // Position assumes a local chunk position (one that lies within the range [0, chunk size] inclusive). 
        // Uses simple bilinear interpolation, which results in points that lie on the plane defined by four vertices that lie on one plane
        // (note that not all four vertices of a terrain quad will lie on one plane, but this is an inaccuracy that doesn't matter much)
        public float InterpolateHeightAt(float3 localPos)
        {
            Assert.IsTrue(localPos.x >= 0 && localPos.x <= gridInfo.size && localPos.z >= 0 && localPos.z <= gridInfo.size,
                "World position not inside chunk bounds (GetHeightAt called on incorrect chunk for world position)");

            int xi_low = (int)math.floor(localPos.x / gridInfo.stepSize);
            int zi_low = (int)math.floor(localPos.z / gridInfo.stepSize);
            int lowestVertIndex = gridInfo.GetIndexFromXZ(xi_low, zi_low);

            Vector3 posA = vertices[lowestVertIndex].position;
            Vector3 posB = vertices[lowestVertIndex + 1].position;
            Vector3 posC = vertices[lowestVertIndex + gridInfo.edgeCount].position;
            Vector3 posD = vertices[lowestVertIndex + gridInfo.edgeCount + 1].position;

            float tx = (localPos.x - posA.x) / gridInfo.stepSize;
            float tz = (localPos.z - posA.z) / gridInfo.stepSize;

            float h0 = math.lerp(posA.y, posB.y, tz);
            float h1 = math.lerp(posC.y, posD.y, tz);
            return math.lerp(h0, h1, tx);
        }
    }
}