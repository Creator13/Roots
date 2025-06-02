using UnityEngine;

namespace Roots.Player
{
    public class StepSoundEffect : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private StepTracker stepTracker;

        private void OnEnable()
        {
            stepTracker.Stepped += PlaySoundEffect;
        }

        private void OnDisable()
        {
            stepTracker.Stepped -= PlaySoundEffect;
        }

        private void PlaySoundEffect(StepTracker.StepInfo stepInfo)
        {
            audioSource.panStereo = stepInfo.side * .5f;
            audioSource.Play();
        }
    }
}