using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Roots.World
{
    public class WorldVisualizationSwitcher : MonoBehaviour
    {
        private enum WorldType { Mesh, Instanced }

        [SerializeField] private ChunkLoader chunkLoader;
        [SerializeField] private InstancedPointRenderer instancedRenderer;

        [SerializeField] private WorldType currentWorldType = WorldType.Mesh;

        private void OnEnable()
        {
            chunkLoader.LoadedChunksChanged += UpdateEnabledRenderers;
        }

        private void OnDisable()
        {
            chunkLoader.LoadedChunksChanged -= UpdateEnabledRenderers;
        }

        private void Start()
        {
            UpdateEnabledRenderers();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                RotateWorldVisualizationType();
                UpdateEnabledRenderers();
            }
        }

        private void UpdateEnabledRenderers()
        {
            Assert.IsNotNull(chunkLoader);
            Assert.IsNotNull(instancedRenderer);
            
            bool meshEnabled = currentWorldType == WorldType.Mesh;
            bool instancedRendererEnabled = currentWorldType == WorldType.Instanced;
            
            foreach (Chunk chunk in chunkLoader.GetChunkEnumarable())
            {
                chunk.SetMeshRendererEnabled(meshEnabled);
            }
            instancedRenderer.enabled = instancedRendererEnabled;
        }
        
        private void RotateWorldVisualizationType()
        {
            currentWorldType = (WorldType)(((int)currentWorldType + 1) % Enum.GetValues(typeof(WorldType)).Length);
        }
    }
}