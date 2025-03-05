using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Roots.World
{
    public class WorldVisualizationSwitcher : MonoBehaviour
    {
        private enum WorldType { Mesh, Instanced }

        [SerializeField] private WorldType currentWorldType = WorldType.Mesh;
        
        [Space]
        [SerializeField] private ChunkLoader chunkLoader;
        [SerializeField] private InstancedPointRenderer instancedRenderer;
        // [SerializeField] private Camera camera;
        [SerializeField] private Material skyBox;
        [SerializeField] private Material skyBox2;

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
            UpdateCameraSettings();
        }

        private void UpdateCameraSettings()
        {
            Material skyBoxMaterial = currentWorldType switch
            {
                WorldType.Mesh => skyBox,
                WorldType.Instanced => skyBox2,
                _ => throw new ArgumentOutOfRangeException()
            };
            RenderSettings.skybox = skyBoxMaterial;
            // fog night color : 23272F, pow 0.015
        }
        
        private void UpdateEnabledRenderers()
        {
            Assert.IsNotNull(chunkLoader);
            Assert.IsNotNull(instancedRenderer);
            
            bool meshEnabled = currentWorldType == WorldType.Mesh;
            bool instancedRendererEnabled = currentWorldType == WorldType.Instanced;

            chunkLoader.SetChunkMeshRenderersEnabled(meshEnabled);
            instancedRenderer.enabled = instancedRendererEnabled;
        }
        
        private void RotateWorldVisualizationType()
        {
            currentWorldType = (WorldType)(((int)currentWorldType + 1) % Enum.GetValues(typeof(WorldType)).Length);
        }
    }
}