using System.Globalization;
using Roots.World;
using TMPro;
using UnityEngine;

namespace Roots.UI
{
    public class WorldStatisticsPanel : MonoBehaviour
    {
        [Header("UI elements")]
        [SerializeField] private TMP_Text objectCountText; 
        
        [Header("Data sources")]
        [SerializeField] private InstancedPointRenderer pointRenderer;
        
        private void Update()
        {
            if (pointRenderer.enabled)
            {
                objectCountText.fontStyle = FontStyles.Normal;
                objectCountText.SetText($"{pointRenderer.PointCount:N0} obj");
            }
            else
            {
                objectCountText.SetText($"0 obj");
                objectCountText.fontStyle = FontStyles.Strikethrough;
            }
        }
    }
}