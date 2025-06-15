using Roots.Player;
using UnityEngine;

namespace Roots.World
{
    public class PlantSeed : MonoBehaviour
    {
        [SerializeField] private GlitchMovement glitch;
        [SerializeField] private Collider collider;
        
        private bool interactable;
        
        public void SetInteractable(bool interactable)
        {
            this.interactable = interactable;

            UpdateComponents();
        }

        public void ActivateGlitchThings(bool active)
        {
            glitch.enabled = active;
        }

        private void UpdateComponents()
        {
            glitch.enabled = interactable;
            collider.enabled = interactable;
        }
    }
}