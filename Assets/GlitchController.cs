using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Roots
{
    public class GlitchController : MonoBehaviour
    {
        [SerializeField] private GameObject director;
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private Transform glitch;
        [SerializeField] private GlitchMovement glitchMovement;

        private void Start()
        {
            director.SetActive(false);
        }

        private Vector3 moveTargetPosition;
        private bool shouldMove = false;

        private void Update()
        {
            if (shouldMove)
            {
                transform.position = Vector3.Lerp(transform.position, moveTargetPosition, .04f);
                shouldMove = (transform.position - moveTargetPosition).sqrMagnitude > .001f;
            }

            director.SetActive(Mouse.current.leftButton.isPressed);

            if (Mouse.current.leftButton.isPressed)
            {
                director.transform.localPosition = glitch.localPosition;
                director.transform.localRotation = Quaternion.Euler(0, cameraRoot.localRotation.eulerAngles.y, 0);
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                MoveTo(transform.position + Quaternion.Euler(0, cameraRoot.localRotation.eulerAngles.y, 0) * Vector3.forward * 6);
            }
        }

        public void MoveTo(Vector3 targetPosition)
        {
            moveTargetPosition = targetPosition;
            shouldMove = true;
        }
    }
}