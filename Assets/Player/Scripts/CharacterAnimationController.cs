using UnityEngine;

namespace Roots
{
    public class CharacterAnimationController : MonoBehaviour
    {
        private static readonly int CPI_Sitting = Animator.StringToHash("Sitting");
        
        [SerializeField] private Animator animator;
        
        private enum AnimationState
        {
            Idle,
            KneelDown,
            KneelIdle,
            KneelUp
        }

        private AnimationState currentAnimationState = AnimationState.Idle;
        
        public bool IsKneeling => currentAnimationState != AnimationState.Idle;
        public bool IsStateChangeLocked => currentAnimationState is AnimationState.KneelDown or AnimationState.KneelUp;

        public void OnKneelDownStart() => currentAnimationState = AnimationState.KneelDown;
        public void OnKneelDownEnd() => currentAnimationState = AnimationState.KneelIdle;
        public void OnKneelUpStart() => currentAnimationState = AnimationState.KneelUp;
        public void OnKneelUpEnd() => currentAnimationState = AnimationState.Idle;

        public void StartKneeling()
        {
            animator.SetBool(CPI_Sitting, true);
        }

        public void StopKneeling()
        {
            animator.SetBool(CPI_Sitting, false);
        }
    }
}
