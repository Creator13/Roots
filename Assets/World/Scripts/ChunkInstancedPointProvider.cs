using UnityEngine;

namespace Roots.World
{
    public class ChunkInstancedPointProvider : InstancedPointProvider
    {
        [SerializeField] private ChunkLoader chunkLoader;

        private void OnEnable()
        {
            chunkLoader.LoadedChunksChanged += OnLoadedChunksChanged;
        }

        private void OnDisable()
        {
            chunkLoader.LoadedChunksChanged -= OnLoadedChunksChanged;
        }

        private void OnLoadedChunksChanged()
        {
            OnPointDataChanged();
        }
        
        public override Vector3[] GetPointData()
        { 
            return chunkLoader.GetCombinedPointData();
        }
    }
}