using Roots.Util;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Roots.World
{
    public class Chunk : MonoBehaviour
    {
        private int x, z;
        
        public NativeArray<Vertex> Vertices { get; private set; }
        public Vector3[] Points { get; private set; }
        public Vector3 CachedWorldPosition { get; private set; }
        public Vector3 LowestPoint { get; private set; }

        private GridInfo gridInfo;
        
        private MeshRenderer meshRenderer;

        public bool IsInitialized { get; private set; }

        private void OnDestroy()
        {
            Vertices.Dispose();
        }

        public void InitAt(int x, int z, GridInfo gridInfo, NativeArray<Vertex> vertices, Vector3[] points)
        {
            this.x = x;
            this.z = z;
            this.gridInfo = gridInfo;
            
            this.Vertices = vertices;
            this.Points = points;

            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            CachedWorldPosition = transform.position;
            IsInitialized = true;
        }

        public Vector3 FindLowestPoint()
        {
            Vector3 lowestPoint = Vector3.positiveInfinity;
            foreach (Vertex vertex in Vertices)
            {
                if (vertex.position.y < lowestPoint.y)
                {
                    lowestPoint = vertex.position;
                }
            }

            return lowestPoint;
        }

        public void SetMeshRendererEnabled(bool enabled)
        {
            if (!IsInitialized) return;
            
            meshRenderer.enabled = enabled;
        }

        // Position is a v3 to use the transform.position directly but the y value is entirely ignored. 
        // Position assumes a WORLD position
        public float GetHeightAt(Vector3 position)
        {
            if (!IsInitialized) return 0;
            
            position -= CachedWorldPosition; // convert to local position
            
            int xi_low = (int)math.floor(position.x / gridInfo.stepSize);
            int zi_low = (int)math.floor(position.z / gridInfo.stepSize);
            int lowestVertIndex = gridInfo.GetIndexFromXZ(xi_low, zi_low);
            
            Vector3 posA = Vertices[lowestVertIndex].position;
            Vector3 posB = Vertices[lowestVertIndex + 1].position;
            Vector3 posC = Vertices[lowestVertIndex + gridInfo.edgeCount].position;
            Vector3 posD = Vertices[lowestVertIndex + gridInfo.edgeCount + 1].position;

            float tx = (position.x - posA.x) / gridInfo.stepSize;
            float tz = (position.z - posA.z) / gridInfo.stepSize;

            float h0 = math.lerp(posA.y, posB.y, tz);
            float h1 = math.lerp(posC.y, posD.y, tz);
            return math.lerp(h0 ,h1, tx);
        }
    }
}