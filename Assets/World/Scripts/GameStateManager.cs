using System;
using Roots.Player;
using Roots.Util;
using UnityEngine;

namespace Roots.World
{
    [Serializable]
    public struct VegetationStage
    {
        public VegetationAsset vegetationAsset;
        public GrowthType growthType;
        public float radius;
    }
    
    public class GameStateManager : MonoBehaviour
    {
        [SerializeField] private WorldVisualizationSwitcher worldVisSwitcher;
        [SerializeField] private SeedPointSpawner seedPointSpawner;
        [SerializeField] private StepPlantSpawner stepPlantSpawner;
        [SerializeField] private VegetationRootManager rootManager;
        [SerializeField] private DistanceToTreeChecker distanceToTreeChecker;
        [SerializeField] private Transform tree;

        [Header("Vegetation")]
        [SerializeField] private VegetationStage[] vegetationStages;
        [SerializeField] private VegetationAsset secondStageReplacementVegetation;
        [SerializeField] private VegetationAsset peakVegetation;
        
        private int seedsFound = 0;
        public bool IsInPlantingStage { get; private set; }

        private void OnEnable()
        {
            TreeSpawner.treeSpawned += OnTreeSpawned;
        }

        private void OnDisable()
        {
            TreeSpawner.treeSpawned -= OnTreeSpawned;
        }

        private void OnTreeSpawned(Transform obj)
        {
            tree = obj;
        }
        
        private void Start()
        {
            seedPointSpawner.SetPointCount(vegetationStages.Length);
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Alpha1))
            {
                CollectSeed(FindAnyObjectByType<PlantSeed>().gameObject);
            }
        }

        public void RecordTreeFall(Vector3 rootPointPosition)
        {
            worldVisSwitcher.SetVisualizationType(WorldVisualizationSwitcher.WorldType.Isoline);
            ActivateSeedPoints(rootPointPosition);
        }

        // position in world space
        private void ActivateSeedPoints(Vector3 rootPointPosition)
        {
            seedPointSpawner.SpawnSeeds(rootPointPosition);
        }

        public void CollectSeed(GameObject seedObject)
        {
            seedPointSpawner.MarkCollected(seedObject.GetComponent<OwnedIndexable>().Index);
            Destroy(seedObject);

            StartPlantSpawning(seedsFound);
            seedsFound++;

            // On the second stage we replace all vegetation of the first stage
            if (seedsFound == 2)
            {
                rootManager.ReplaceAllByKey(10, secondStageReplacementVegetation);
                rootManager.SetReplacementVegetationAsset(peakVegetation);
                distanceToTreeChecker.SetEnding(true);
                tree.gameObject.SetActive(false);
            }
        }

        private void StartPlantSpawning(int stageIndex)
        {
            IsInPlantingStage = true;
            
            worldVisSwitcher.SetVisualizationType(WorldVisualizationSwitcher.WorldType.Mesh);
            stepPlantSpawner.enabled = true;
            
            VegetationStage stage =  vegetationStages[stageIndex];
            rootManager.SetCurrentVegetationAsset(stage.vegetationAsset);
            rootManager.SetKey((stageIndex + 1) * 10);
            rootManager.SetGrowthType(stage.growthType);
            rootManager.SetRadius(stage.radius);
        }

        public void EndPlantSpawning()
        {            
            IsInPlantingStage = false;

            stepPlantSpawner.enabled = false;
            if (seedsFound == 2) return;
            worldVisSwitcher.SetVisualizationType(WorldVisualizationSwitcher.WorldType.Isoline);
        }
    }
}