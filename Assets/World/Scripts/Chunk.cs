using Roots.Util;
using UnityEngine;

namespace Roots.World
{
    public class Chunk : MonoBehaviour
    {
        private MeshRenderer meshRenderer;
        
        private int x, z;
        public Vertex[] Points { get; private set; }
        public Vector3 cachedWorldPosition { get; private set; }

        public void InitAt(int x, int z)
        {
            this.x = x;
            this.z = z;
            
            meshRenderer = GetComponent<MeshRenderer>();
            cachedWorldPosition = transform.position;
        }

        public void SetPoints(Vertex[] points)
        {
            this.Points = points;
        }

        public void SetMeshRendererEnabled(bool enabled)
        {
            meshRenderer.enabled = enabled;
        }
    }
}