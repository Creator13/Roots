using UnityEngine;

namespace Roots.Player
{
    public class StepSpawner : MonoBehaviour
    {
        [SerializeField] private MovementStepTracker stepTracker;
        [SerializeField] private GameObject prefab;

        private void OnEnable()
        {
            stepTracker.Stepped += SpawnStep;
        }

        private void OnDisable()
        {
            stepTracker.Stepped -= SpawnStep;
        }

        private void SpawnStep(MovementStepTracker.StepInfo stepInfo)
        {
            if (stepInfo.stepCountInSequence < 3) return;
            
            Instantiate(prefab, stepInfo.position, Quaternion.identity);
        }
    }
}