using UnityEditor;

namespace Roots.World
{
    [CustomEditor(typeof(TerrainChunkGenerator))]
    public class TerrainChunkGeneratorInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            EditorGUILayout.Space();
            
            var chunkGenerator = (TerrainChunkGenerator)target;
            int verticesOnEdge = chunkGenerator.VertexGridInfo.edgeCount;
            int pointsOnEdge = chunkGenerator.PointGridInfo.edgeCount;
            EditorGUILayout.LabelField($"Terrain vertices: {verticesOnEdge}, point cloud vertices: {pointsOnEdge}");
            EditorGUILayout.LabelField($"Above must be equal or this must be one {(verticesOnEdge) % pointsOnEdge}");
        }
    }
}