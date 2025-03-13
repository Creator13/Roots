using Roots.Player;
using TMPro;
using UnityEngine;

namespace Roots.UI
{
    public class TravelPanel : MonoBehaviour
    {
        [Header("UI elements")]
        [SerializeField] private TMP_Text distanceText;

        [Header("Data sources")]
        [SerializeField] private FirstPersonController playerController;
        
        private void Update()
        {
            if (playerController.TotalDistance < 1000)
            {
                distanceText.SetText($"{playerController.TotalDistance:F1}m");
            }
            else
            {
                distanceText.SetText($"{playerController.TotalDistance / 1000:F3}km");
            }
        }
    }
}