using UnityEngine;

namespace Roots.Util
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshTester : MonoBehaviour
    {
        private MeshFilter meshFilter;
        [SerializeField] private Transform faceTransform;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        private void Start()
        {
            GenerateMesh();
        }

        [ContextMenu("Generate Mesh")]
        private void GenerateMesh()
        {
            MeshBuilder mb = new MeshBuilder();

            var dir = faceTransform.position - transform.position;
        
            mb.CreateCircle(new Vector3(1, 1, 0), 1.5f, 7, dir, true);

            meshFilter.sharedMesh = mb.GetMesh("Test mesh");
        }
    }
}