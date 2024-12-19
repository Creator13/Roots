using UnityEngine;

namespace Roots.World
{
    public class Chunk : MonoBehaviour
    {
        private int x, z;
        private Vector3[] points;

        public void LoadAt(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        public void SetPoints(Vector3[] points)
        {
            this.points = points;
        }
    }
}