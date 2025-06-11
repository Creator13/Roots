using Roots.World;
using Roots.World.Chunking;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Roots.UI
{
    public class WorldStatisticsPanel : MonoBehaviour
    {
        [SerializeField] private bool showStatistics = true;
        
        [Header("UI elements")]
        [SerializeField] private RectTransform statsRootPanel;
        [SerializeField] private TMP_Text objectCountText;
        [SerializeField] private TMP_Text chunkLoaderText;
        
        [Header("Data sources")]
        [SerializeField] private InstancedPointRenderer pointRenderer;
        [SerializeField] private ChunkLoader chunkLoader;
        
        private void Update()
        {
            if (Keyboard.current.ctrlKey.isPressed && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                showStatistics = !showStatistics;
                UpdateVisibility();
            }
            
            if (showStatistics)
            {
                UpdateValues();
            }
        }

        private void UpdateVisibility()
        {
            statsRootPanel.gameObject.SetActive(showStatistics);
        }
        
        private void UpdateValues()
        {
            if (pointRenderer.enabled)
            {
                objectCountText.fontStyle = FontStyles.Normal;
                objectCountText.SetText($"{pointRenderer.PointCount:N0} obj");
            }
            else
            {
                objectCountText.fontStyle = FontStyles.Strikethrough;
                objectCountText.SetText("0 obj");
            }

            chunkLoaderText.text = $"{chunkLoader.ActiveChunkGenJobCount} active jobs";
        }

        private void OnValidate()
        {
            UpdateVisibility();
        }
    }
}