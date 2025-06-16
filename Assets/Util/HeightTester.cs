using Roots.World.Chunking;
using UnityEngine;

namespace Roots.Util
{
    public class HeightTester : MonoBehaviour
    {
        [SerializeField] private ChunkLoader chunkLoader;

        [SerializeField] private Vector3 position;
        [SerializeField] private float height;

        private void Update()
        {
            position = transform.position;
            height = chunkLoader.GetInterpolatedGroundHeightAt(position);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(position.x, 0, position.z), new Vector3(position.x, 10, position.z));
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(position.x - .5f, height, position.z), new Vector3(position.x + .5f, height, position.z));
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(new Vector3(position.x, height, position.z - .5f), new Vector3(position.x, height, position.z + .5f));
        }
    }
}