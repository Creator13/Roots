using Roots.Player;
using Roots.Util;
using UnityEngine;

namespace Roots.World
{
    public class GameStateManager : MonoBehaviour
    {
        [SerializeField] private WorldVisualizationSwitcher worldVisSwitcher;
        [SerializeField] private SeedPointSpawner seedPointSpawner;
        [SerializeField] private StepPlantSpawner stepPlantSpawner;
        [SerializeField] private VegetationRootManager rootManager;

        [SerializeField] private VegetationAsset[] vegetationStages;
        
        private int seedsFound = 0;
        public bool IsInPlantingStage { get; private set; }

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
        }

        private void StartPlantSpawning(int stageIndex)
        {
            IsInPlantingStage = true;
            
            worldVisSwitcher.SetVisualizationType(WorldVisualizationSwitcher.WorldType.Mesh);
            stepPlantSpawner.enabled = true;
            
            rootManager.SetCurrentVegetationAsset(vegetationStages[stageIndex]);
            rootManager.SetKey(stageIndex + 1);
        }

        public void EndPlantSpawning()
        {            
            IsInPlantingStage = false;

            worldVisSwitcher.SetVisualizationType(WorldVisualizationSwitcher.WorldType.Isoline);
            stepPlantSpawner.enabled = false;
        }
    }
}