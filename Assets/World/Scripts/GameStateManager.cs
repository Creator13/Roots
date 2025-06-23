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

        [SerializeField] private VegetationAsset[] vegetationStages;
        
        private int seedsFound = 0;
        
        public int StageCount => vegetationStages.Length;
        
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
            StartPlantSpawning();
            seedPointSpawner.MarkCollected(seedObject.GetComponent<OwnedIndexable>().Index);
            Destroy(seedObject);
            seedsFound++;
        }

        private void StartPlantSpawning()
        {
            worldVisSwitcher.SetVisualizationType(WorldVisualizationSwitcher.WorldType.Mesh);
            stepPlantSpawner.enabled = true;
        }

        public void EndPlantSpawning()
        {            
            worldVisSwitcher.SetVisualizationType(WorldVisualizationSwitcher.WorldType.Isoline);
            stepPlantSpawner.enabled = false;
        }
    }
}