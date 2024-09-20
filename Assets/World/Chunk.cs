using UnityEngine;

namespace World
{
    public class Chunk : MonoBehaviour
    {
        private int x, z;

        public void LoadAt(int x, int z)
        {
            this.x = x;
            this.z = z;
        }
    }
}