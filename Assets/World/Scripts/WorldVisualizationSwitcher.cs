using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Roots.World
{
    public class WorldVisualizationSwitcher : MonoBehaviour
    {
        [Serializable]
        private struct WorldTypeSettings
        {
            public Material skybox;
            public Color fogColor;
        }
        
        private enum WorldType { Mesh, Instanced }

        [SerializeField] private WorldType currentWorldType = WorldType.Mesh;
        
        [Space]
        [SerializeField] private ChunkLoader chunkLoader;
        [SerializeField] private InstancedPointRenderer instancedRenderer;
        
        [Space]
        [SerializeField] private WorldTypeSettings meshSettings;
        [SerializeField] private WorldTypeSettings instancedSettings;

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
            UpdateAll();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                RotateWorldVisualizationType();
                UpdateAll();
            }
        }

        private void UpdateAll()
        {
            UpdateEnabledRenderers();
            UpdateRenderingSettings();
        }

        private void UpdateRenderingSettings()
        {
            WorldTypeSettings settings = currentWorldType switch
            {
                WorldType.Mesh => meshSettings,
                WorldType.Instanced => instancedSettings,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            RenderSettings.skybox = settings.skybox;
            RenderSettings.fogColor = settings.fogColor;
        }
        
        private void UpdateEnabledRenderers()
        {
            Assert.IsNotNull(chunkLoader);
            Assert.IsNotNull(instancedRenderer);
            
            bool meshEnabled = currentWorldType == WorldType.Mesh;
            bool instancedRendererEnabled = currentWorldType == WorldType.Instanced;
            
            foreach (Chunk chunk in chunkLoader.GetChunkEnumerable())
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