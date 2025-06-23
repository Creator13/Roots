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
        // public GameObject prefab;
        public GameObject prefab;
        public Mesh mesh;
        public Material material;
        public float weight;
    }

    [CreateAssetMenu(fileName = "New Vegetation Asset", menuName = "Roots/Vegetation Asset", order = 0)]
    public class VegetationAsset : ScriptableObject
    {
        private const int LUT_SIZE = 256;
        private int[] samplingLUT = new int[LUT_SIZE];

        [SerializeField] private List<VegetationType> plantTypes = new();
        [field: SerializeField] public float density { get; private set; }

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

            float cum = 0;
            Span<float> cumulativeWeights = stackalloc float[plantTypes.Count];
            for (int i = 0; i < plantTypes.Count; i++)
            {
                cum += plantTypes[i].weight;
                cumulativeWeights[i] = cum;
            }

            for (int i = 0; i < LUT_SIZE; i++)
            {
                float sampleValue = (i / (float)LUT_SIZE) * totalWeight;
                for (int j = 0; j < cumulativeWeights.Length; j++)
                {
                    if (sampleValue < cumulativeWeights[j])
                    {
                        samplingLUT[i] = j;
                        break;
                    }
                }
            }
        }

        public VegetationType GetAssetFromRoll(float roll)
        {
            Assert.IsNotNull(plantTypes);
            Assert.IsTrue(plantTypes.Count > 0);

            int lutIndex = (int)(roll * samplingLUT.Length);
            int typeIndex = samplingLUT[lutIndex];
            return plantTypes[typeIndex];
        }

        public VegetationType GetPlantType(ref Random random)
        {
            float roll = random.NextFloat();
            return GetAssetFromRoll(roll);
        }
    }
}