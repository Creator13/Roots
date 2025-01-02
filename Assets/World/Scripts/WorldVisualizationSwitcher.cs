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

        private WorldType currentWorldType = WorldType.Mesh;

        private void Start()
        {
            UpdateComponents();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                RotateWorldVisualization();
                UpdateComponents();
            }
        }

        private void UpdateComponents()
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
        
        private void RotateWorldVisualization()
        {
            currentWorldType = (WorldType)(((int)currentWorldType + 1) % Enum.GetValues(typeof(WorldType)).Length);
        }
    }
}