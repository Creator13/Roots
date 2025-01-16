using Roots.Util;
using Unity.Mathematics;
using UnityEngine;

namespace Roots.World
{
    public class Chunk : MonoBehaviour
    {
        private int x, z;
        public Vertex[] Vertices { get; private set; }
        public Vector3[] Points { get; private set; }
        public Vector3 CachedWorldPosition { get; private set; }
        public Vector3 LowestPoint { get; private set; }

        private MeshRenderer meshRenderer;

        public bool IsInitialized { get; private set; }
        
        public void InitAt(int x, int z, Vertex[] vertices, Vector3[] points)
        {
            this.x = x;
            this.z = z;
            
            this.Vertices = vertices;
            this.Points = points;

            if (vertices != null)
            {
                FindLowestPoint();
            }

            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            CachedWorldPosition = transform.position;
            IsInitialized = true;
        }

        private void FindLowestPoint()
        {
            LowestPoint = Vector3.positiveInfinity;
            foreach (Vertex vertex in Vertices)
            {
                if (vertex.position.y < LowestPoint.y)
                {
                    LowestPoint = vertex.position;
                }
            }
        }

        public void SetMeshRendererEnabled(bool enabled)
        {
            if (!IsInitialized) return;
            
            meshRenderer.enabled = enabled;
        }
    }
}