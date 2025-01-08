using Roots.World;
using TMPro;
using UnityEngine;

namespace Roots.UI
{
    public class WorldStatisticsPanel : MonoBehaviour
    {
        [Header("UI elements")]
        [SerializeField] private TMP_Text objectCountText;
        [SerializeField] private TMP_Text chunkLoaderText;
        
        [Header("Data sources")]
        [SerializeField] private InstancedPointRenderer pointRenderer;
        [SerializeField] private ChunkLoader chunkLoader;
        
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

            chunkLoaderText.text = $"{chunkLoader.ActiveChunkGenJobCount} active jobs";
        }
    }
}