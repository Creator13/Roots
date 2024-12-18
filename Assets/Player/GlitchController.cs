using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace Roots
{
    public class GlitchController : MonoBehaviour
    {
        [SerializeField] private float chargeTime = 1;

        [Header("References")]
        [SerializeField] private GameObject director;
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private Transform glitch;
        [SerializeField] private GlitchMovement glitchMovement;

        private Vector3 moveStartPosition;
        private Vector3 moveTargetPosition;
        private bool shouldMove = false;

        private float lastChargeStartTime = 0;
        private float lastMoveStartTime = 0;

        private void Start()
        {
            director.SetActive(false);
        }

        private void Update()
        {
            if (shouldMove)
            {
                transform.position = Vector3.Lerp(transform.position, moveTargetPosition, .04f);
                shouldMove = (transform.position - moveTargetPosition).sqrMagnitude > .001f;
                glitchMovement.Energy = Mathf.Sqrt(Mathf.Sqrt((transform.position - moveTargetPosition).sqrMagnitude / (moveStartPosition - moveTargetPosition).sqrMagnitude));
            }

            director.SetActive(Mouse.current.leftButton.isPressed);

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                lastChargeStartTime = Time.time;
            }

            if (Mouse.current.leftButton.isPressed)
            {
                float chargeProgress = GetCurrentChargeProgress();

                glitchMovement.Energy = chargeProgress;

                director.transform.localScale = new Vector3(1, 1, chargeProgress);
                director.transform.localPosition = glitch.localPosition;
                director.transform.localRotation = Quaternion.Euler(0, cameraRoot.localRotation.eulerAngles.y, 0);
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                float chargeProgress = GetCurrentChargeProgress();

                MoveTo(transform.position + Quaternion.Euler(0, cameraRoot.localRotation.eulerAngles.y, 0) * Vector3.forward * (chargeProgress * 6));
            }
        }

        private float GetCurrentChargeProgress()
        {
            Assert.IsTrue(chargeTime > 0);

            float currentChargeTime = Time.time - lastChargeStartTime;
            return Mathf.Clamp01(currentChargeTime / chargeTime);
        }

        public void MoveTo(Vector3 targetPosition)
        {
            moveStartPosition = transform.position;
            moveTargetPosition = targetPosition;
            shouldMove = true;
        }
    }
}