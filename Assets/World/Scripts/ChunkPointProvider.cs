using UnityEngine;

namespace Roots.World
{
    public class ChunkPointProvider : PointProvider
    {
        [SerializeField] private ChunkLoader chunkLoader;

        private void OnEnable()
        {
            chunkLoader.LoadedChunksChanged += OnPointDataChanged;
        }

        private void OnDisable()
        {
            chunkLoader.LoadedChunksChanged -= OnPointDataChanged;
        }
        
        public override Vector3[] GetPointData()
        {
            return chunkLoader.GetCombinedPointData();
        }

        public override Bounds GetBounds()
        {
            return chunkLoader.GetCurrentBounds();
        }
    }
}