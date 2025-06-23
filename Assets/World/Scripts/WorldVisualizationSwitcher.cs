using System;
using Roots.World.Chunking;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;

namespace Roots.World
{
    public class WorldVisualizationSwitcher : MonoBehaviour
    {
        public enum WorldType { Mesh, Isoline, Instanced }

        [SerializeField] private WorldType currentWorldType = WorldType.Mesh;
        
        [Space]
        [SerializeField] private ChunkLoader chunkLoader;
        [SerializeField] private InstancedPointRenderer instancedRenderer;
        // [SerializeField] private Camera camera;
        [SerializeField] private AudioSource footstepAudioSource;
        [SerializeField] private SeedPointSpawner seedPointSpawner;
        [SerializeField] private VegetationRootManager rootManager;
        
        [Space]
        [SerializeField] private Material skyBox;
        [SerializeField] private Material skyBox2;
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Material isolineMaterial;
        [SerializeField] private AudioResource overworldFootsteps;
        [SerializeField] private AudioResource underworldFootsteps;
            
        private void OnEnable()
        {
            chunkLoader.InitialChunksLoaded += UpdateAll;
            chunkLoader.LoadedChunksChanged += UpdateEnabledRenderers;
        }

        private void OnDisable()
        {
            chunkLoader.LoadedChunksChanged -= UpdateEnabledRenderers;            
            chunkLoader.InitialChunksLoaded -= UpdateAll;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                RotateWorldVisualizationType();
                UpdateAll();
            }
        }

        private void Start()
        {
            UpdateAll();
        }

        public void SetVisualizationType(WorldType worldType)
        {
            currentWorldType = worldType;
            UpdateAll();
        }

        private void UpdateAll()
        {
            UpdateEnabledRenderers();
            UpdateCameraSettings();
            UpdateAudioSettings();
            
            seedPointSpawner.SetSeedPathsVisible(currentWorldType is WorldType.Isoline or WorldType.Instanced);
            rootManager.SetVisible(currentWorldType is WorldType.Mesh);
        }

        private void UpdateCameraSettings()
        {
            Material skyBoxMaterial = currentWorldType switch
            {
                WorldType.Mesh => skyBox,
                WorldType.Isoline or WorldType.Instanced => skyBox2,
                _ => throw new ArgumentOutOfRangeException()
            };
            RenderSettings.skybox = skyBoxMaterial;
            // fog night color : 23272F, pow 0.015
        }

        private void UpdateAudioSettings()
        {
            AudioResource footsteps = currentWorldType switch
            {
                WorldType.Mesh => overworldFootsteps,
                WorldType.Isoline => underworldFootsteps,
                WorldType.Instanced => underworldFootsteps,
                _ => throw new ArgumentOutOfRangeException()
            };
            footstepAudioSource.resource = footsteps;
        }
        
        private void UpdateEnabledRenderers()
        {
            Assert.IsNotNull(chunkLoader);
            Assert.IsNotNull(instancedRenderer);
            
            bool meshEnabled = currentWorldType is WorldType.Mesh or WorldType.Isoline;
            bool instancedRendererEnabled = currentWorldType == WorldType.Instanced;

            chunkLoader.SetChunkMeshRenderersEnabled(meshEnabled);
            instancedRenderer.enabled = instancedRendererEnabled;
            
            UpdateMaterials();
        }

        private void UpdateMaterials()
        {
            if (currentWorldType == WorldType.Instanced) return;
            
            Material newMaterial = currentWorldType switch
            {
                WorldType.Mesh => groundMaterial,
                WorldType.Isoline => isolineMaterial,
            };
            chunkLoader.SetTerrainMaterial(newMaterial);
        }
        
        private void RotateWorldVisualizationType()
        {
            currentWorldType = (WorldType)(((int)currentWorldType + 1) % Enum.GetValues(typeof(WorldType)).Length);
        }
    }
}