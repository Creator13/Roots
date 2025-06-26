using UnityEngine;

namespace Roots.World
{
    public class VegReplTest : MonoBehaviour
    {
        [SerializeField] private VegetationAsset a;
        [SerializeField] private VegetationAsset b;

        private VegetationRootManager vegetationRootManager;
        private int count;

        private void Awake()
        {
            vegetationRootManager = FindAnyObjectByType<VegetationRootManager>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                if (count == 0)
                {
                    vegetationRootManager.ReplaceAll(a);
                }
                else if (count == 1)
                {
                    vegetationRootManager.ReplaceAll(b);
                }

                count = (count + 1) % 2;
            }
        }
    }
}