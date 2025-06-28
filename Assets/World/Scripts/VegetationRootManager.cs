using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Roots.World.Chunking;
using Unity.Collections;
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
        private GrowthType currentGrowthType;
        private float currentRadius;
        private VegetationAsset currentReplacementVegetation;

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
            if (currentGrowthType == GrowthType.Circle)
            {
                instance.Initialize(currentRadius, currentKey, currentVegetationAsset, currentGrowthType);
            }
            else if (currentGrowthType == GrowthType.Tendrils)
            {
                var target = FindRootInRadiusByKey(10, position, 20); // key 10 is a root from stage 1, TODO plz don't hardcode this :( actually this is game logic and should be controlled by the gamemanager i suppose
                Action onReached = null;
                if (target && target.Key != currentKey)
                {
                    onReached = () => target.ReplaceVegetation(currentReplacementVegetation);
                }

                instance.Initialize(currentRadius, currentKey, currentVegetationAsset, currentGrowthType, target, onReached);
            }

            StoreRoot(instance, position);
        }

        private VegetationRoot FindRootInRadiusByKey(int key, Vector3 position, float radius)
        {
            float sqrRadius = radius * radius;

            VegetationRoot closest = null;
            float closestSqrDistance = float.MaxValue;
            int closestIndex = -1;
            for (int i = 0; i < rootsByKey[key].Count; i++)
            {
                VegetationRoot root = rootsByKey[key][i];
                // TODO remove bias towards first added
                float sqrDistance = (position - root.transform.position).sqrMagnitude;
                if (sqrDistance < sqrRadius)
                {
                    if (sqrDistance < closestSqrDistance)
                    {
                        closest = root;
                        closestSqrDistance = sqrDistance;
                        closestIndex = i;
                    }
                }
            }

            if (closest != null)
            {
                rootsByKey[key].RemoveAtSwapBack(closestIndex);
                EnsureKey(key + 1);
                rootsByKey[key + 1].Add(closest);
            }

            return closest;
        }

        public void SetCurrentVegetationAsset(VegetationAsset asset)
        {
            currentVegetationAsset = asset;
        }

        public void SetGrowthType(GrowthType growthType)
        {
            currentGrowthType = growthType;
        }

        public void SetRadius(float radius)
        {
            currentRadius = radius;
        }

        public void SetReplacementVegetationAsset(VegetationAsset vegetationAsset)
        {
            currentReplacementVegetation = vegetationAsset;
        }

        public void SetKey(int key)
        {
            Assert.IsFalse(key == 0, "0 is an invalid key.");

            EnsureKey(key);
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
            float sqrRadius = radius * radius;
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
            EnsureChunk(coords);
            rootsByChunk[coords].Add(instance);
        }

        private void EnsureKey(int key)
        {
            if (!rootsByKey.ContainsKey(key))
            {
                rootsByKey.Add(key, new List<VegetationRoot>(24));
            }
        }

        private void EnsureChunk(int2 coords)
        {
            if (!rootsByChunk.ContainsKey(coords))
            {
                rootsByChunk.Add(coords, new List<VegetationRoot>(24));
            }
        }
    }
}