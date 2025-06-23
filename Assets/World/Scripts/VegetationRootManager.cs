using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Assertions;

namespace Roots.World
{
    public class VegetationRootManager : MonoBehaviour
    {
        [SerializeField] private VegetationRoot placePrefab;

        private Dictionary<int, List<VegetationRoot>> rootsKeyed = new(4);
        private List<VegetationRoot> roots = new();

        // -1 is global key, 0 is invalid key
        private int currentKey;
        private VegetationAsset currentVegetationAsset;
        private bool isVisible = true;

        private void Awake()
        {
            SetKey(-1);
        }

        public ReadOnlyCollection<VegetationRoot> GetAll()
        {
            return roots.AsReadOnly();
        }

        public void PlaceNew(Vector3 position, float radius = 3)
        {
            Assert.IsFalse(currentKey == 0, "Key cannot be 0.");

            if (!isVisible) return;

            VegetationRoot instance = Instantiate(placePrefab, position, Quaternion.identity);
            instance.Initialize(radius, currentVegetationAsset);
            rootsKeyed[currentKey].Add(instance);
            roots.Add(instance);
        }

        public void SetCurrentVegetationAsset(VegetationAsset asset)
        {
            currentVegetationAsset = asset;
        }

        public void SetKey(int key)
        {
            Assert.IsFalse(key == 0, "0 is an invalid key.");

            if (!rootsKeyed.ContainsKey(key))
            {
                rootsKeyed.Add(key, new List<VegetationRoot>(48));
            }

            currentKey = key;
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
            foreach (VegetationRoot root in roots)
            {
                root.gameObject.SetActive(visible);
            }
        }
    }
}