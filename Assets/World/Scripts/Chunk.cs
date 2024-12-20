using Roots.Util;
using UnityEngine;

namespace Roots.World
{
    public class Chunk : MonoBehaviour
    {
        private int x, z;
        private Vertex[] points;

        public void LoadAt(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        public void SetPoints(Vertex[] points)
        {
            this.points = points;
        }
    }
}