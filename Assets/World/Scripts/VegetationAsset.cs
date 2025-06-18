using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Random = Unity.Mathematics.Random;

namespace Roots.World
{
    [Serializable]
    public struct VegetationType
    {
        public GameObject prefab;
        public float weight;
    }
    
    [CreateAssetMenu(fileName = "New Vegetation Asset", menuName = "Roots/Vegetation Asset", order = 0)]
    public class VegetationAsset : ScriptableObject
    {
        [SerializeField] private List<VegetationType> plantTypes = new();

        private float totalWeight = 0;
        
        private void Awake()
        {
            Recalculate();
        }

        private void OnValidate()
        {
            Recalculate();
        }

        private void Recalculate()
        {
            if (plantTypes == null || plantTypes.Count == 0)
            {
                totalWeight = 0;
                return;
            }

            totalWeight = 0;
            for (int i = 0; i < plantTypes.Count; i++)
            {
                totalWeight += plantTypes[i].weight;
            }
        }

        public GameObject GetPlantType(ref Random random)
        {
            Assert.IsNotNull(plantTypes);
            Assert.IsTrue(plantTypes.Count > 0);

            float roll = random.NextFloat(totalWeight);

            float cum = 0;
            for (int i = 0; i < plantTypes.Count; i++)
            {
                cum +=  plantTypes[i].weight;
                if (roll < cum)
                {
                    return plantTypes[i].prefab;
                }
            }

            return plantTypes[^1].prefab;
        }
    }
}