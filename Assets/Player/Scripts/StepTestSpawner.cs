using UnityEngine;

namespace Roots.Player
{
    public class StepTestSpawner : MonoBehaviour
    {
        [SerializeField] private StepTracker stepTracker;
        [SerializeField] private GameObject prefab;

        private void OnEnable()
        {
            stepTracker.Stepped += SpawnStep;
        }

        private void OnDisable()
        {
            stepTracker.Stepped -= SpawnStep;
        }

        private void SpawnStep(StepTracker.StepInfo stepInfo)
        {
            if (stepInfo.stepCountInSequence < 3) return;
            
            Instantiate(prefab, stepInfo.position, Quaternion.identity);
        }
    }
}