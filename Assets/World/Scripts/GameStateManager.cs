using UnityEngine;

namespace Roots.World
{
    public class GameStateManager : MonoBehaviour
    {
        [SerializeField] private WorldVisualizationSwitcher worldVisSwitcher;
        [SerializeField] private SeedPointSpawner seedPointSpawner;
        
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
    }
}