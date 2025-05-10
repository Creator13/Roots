using UnityEngine;
using UnityEngine.Serialization;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    public class StarterAssetsInputs : MonoBehaviour
    {
        [Header("Character Input Values")]
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;

        [Header("Movement Settings")]
        public bool analogMovement;

        [FormerlySerializedAs("cursorLocked")]
        [Header("Mouse Cursor Settings")]
        public bool lockCursor = true;
        public bool cursorInputForLook = true;

        private bool cursorLockedRightNow = false;

        private void Start()
        {
            SetCursorLocked(true);
        }

#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }

        public void OnLook(InputValue value)
        {
            if (cursorInputForLook)
            {
                LookInput(value.Get<Vector2>());
            }
        }

        public void OnJump(InputValue value)
        {
            JumpInput(value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            SprintInput(value.isPressed);
        }
        
        public void OnUnlockCursor(InputValue value)
        {
            SetCursorLocked(!cursorLockedRightNow);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Inspector");
#endif
        }
#endif

        public void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
        }

        public void LookInput(Vector2 newLookDirection)
        {
            look = cursorLockedRightNow ? newLookDirection : Vector2.zero;
        }

        public void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        public void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            SetCursorLocked(hasFocus);
        }

        private void SetCursorLocked(bool newState)
        {
            cursorLockedRightNow = newState && lockCursor;
            Cursor.lockState = cursorLockedRightNow ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}