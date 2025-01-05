using Roots.Util;
using UnityEngine;

namespace Roots.World
{
    public class Chunk : MonoBehaviour
    {
        private int x, z;
        public Vertex[] Vertices { get; private set; }
        public Vector3[] Points { get; private set; }
        public Vector3 cachedWorldPosition { get; private set; }

        private MeshRenderer meshRenderer;

        public void InitAt(int x, int z)
        {
            this.x = x;
            this.z = z;
            
            meshRenderer = GetComponent<MeshRenderer>();
            cachedWorldPosition = transform.position;
        }

        public void SetVertices(Vertex[] vertices, Vector3[] points)
        {
            this.Vertices = vertices;
            this.Points = points;
        }

        public void SetMeshRendererEnabled(bool enabled)
        {
            meshRenderer.enabled = enabled;
        }
    }
}