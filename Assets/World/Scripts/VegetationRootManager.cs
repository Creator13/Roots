using System.Collections.Generic;
using System.Collections.ObjectModel;
using Roots.World.Chunking;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Roots.World
{
    public class VegetationRootManager : MonoBehaviour
    {
        [SerializeField] private ChunkLoader chunkLoader;
        [SerializeField] private VegetationRoot placePrefab;

        private Dictionary<int2, List<VegetationRoot>> rootsByChunk = new(32);
        private Dictionary<int, List<VegetationRoot>> rootsByKey = new(4);
        private List<VegetationRoot> roots = new();

        // -1 is global key, 0 is invalid key
        private int currentKey;
        private VegetationAsset currentVegetationAsset;
        private VegetationRoot.GrowthType currentGrowthType;
        private float currentRadius;
        
        private bool isVegetationVisible = true;

        private void Awake()
        {
            SetKey(-1);
        }

        public ReadOnlyCollection<VegetationRoot> GetAll()
        {
            return roots.AsReadOnly();
        }

        public void PlaceNew(Vector3 position)
        {
            Assert.IsFalse(currentKey == 0, "Key cannot be 0.");
            Assert.IsFalse(currentRadius == 0, "Radius cannot be 0.");

            if (!isVegetationVisible) return;
            
            VegetationRoot instance = Instantiate(placePrefab, position, Quaternion.identity);
            instance.Initialize(currentRadius, currentVegetationAsset, currentGrowthType);
            StoreRoot(instance, position);
        }

        public void SetCurrentVegetationAsset(VegetationAsset asset)
        {
            currentVegetationAsset = asset;
        }

        public void SetGrowthType(VegetationRoot.GrowthType growthType)
        {
            currentGrowthType = growthType;
        }

        public void SetRadius(float radius)
        {
            currentRadius = radius;
        }

        public void SetKey(int key)
        {
            Assert.IsFalse(key == 0, "0 is an invalid key.");

            if (!rootsByKey.ContainsKey(key))
            {
                rootsByKey.Add(key, new List<VegetationRoot>(48));
            }

            currentKey = key;
        }

        public void SetVegetationVisible(bool visible)
        {
            isVegetationVisible = visible;
            foreach (VegetationRoot root in roots)
            {
                root.SetVisible(visible);
            }
        }

        public void ReplaceAll(VegetationAsset newVegetation)
        {
            for (int i = 0; i < roots.Count; i++)
            {
                roots[i].ReplaceVegetation(newVegetation);
            }
        }

        public void ReplaceAllByKey(int key, VegetationAsset newVegetation)
        {
            var set = rootsByKey[key];
            for (int i = 0; i < set.Count; i++)
            {
                set[i].ReplaceVegetation(newVegetation);
            }
        }

        public void ReplaceInRadius(Vector3 position, float radius, VegetationAsset newVegetation)
        {
            float sqrRadius =  radius * radius;
            position.y = 0;
            
            foreach (var root in roots)
            {
                Vector3 rootPos = root.transform.position;
                rootPos.y = 0;
                if ((rootPos - position).sqrMagnitude < sqrRadius)
                {
                    root.ReplaceVegetation(newVegetation);
                }
            }
        }

        private void StoreRoot(VegetationRoot instance, Vector3 position)
        {
            rootsByKey[currentKey].Add(instance);
            roots.Add(instance);

            int2 coords = chunkLoader.WorldPositionToWorldChunkCoordinates(position);
            if (!rootsByChunk.ContainsKey(coords))
            {
                rootsByChunk.Add(coords, new List<VegetationRoot>(24));
            }

            rootsByChunk[coords].Add(instance);
        }
    }
}