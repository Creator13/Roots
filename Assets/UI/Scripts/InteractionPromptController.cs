using Roots.Player;
using UnityEngine;

namespace Roots.UI
{
    public class InteractionPromptController : MonoBehaviour
    {
        [SerializeField] private RectTransform interactionPromptPanel;

        [Space]
        [SerializeField] private InteractionController interactionController;

        private void Start()
        {
            interactionPromptPanel.gameObject.SetActive(interactionController.HasInteractionTarget);
        }

        private void Update()
        {
            interactionPromptPanel.gameObject.SetActive(interactionController.HasInteractionTarget);
        }
    }
}