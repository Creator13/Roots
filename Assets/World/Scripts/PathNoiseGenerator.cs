using System;
using FastNoise;
using Unity.Mathematics;
using UnityEngine;

namespace Roots.World
{
    public class PathNoiseGenerator : MonoBehaviour
    {
        [SerializeField] private int seed;
        [SerializeField] private float frequencyModifier = 1;
        [SerializeField] private float worleyFrequency = .02f;
        [SerializeField] private float gradientFrequency = .05f;

        [Space]
        [SerializeField] private float gradientWeight = -.26f;
        [SerializeField] private float worleyStrengthMultiplier = 2.33f;

        [Space]
        [SerializeField] private float smoothstepLevel = .89f;
        [SerializeField] private float smoothstepWidth = 1.27f;

        private FastNoiseLite worleyGen;
        private FastNoiseLite gradientGen;

        private bool isInitialized = false;
        public bool IsInitialized => isInitialized && !(worleyGen == null || gradientGen == null);

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            worleyGen = new FastNoiseLite(seed);
            worleyGen.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            worleyGen.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Euclidean);
            worleyGen.SetCellularJitter(.88f);
            worleyGen.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance2Div);
            worleyGen.SetFrequency(worleyFrequency * frequencyModifier);

            gradientGen = new FastNoiseLite(seed);
            gradientGen.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
            gradientGen.SetFrequency(gradientFrequency * frequencyModifier);

            isInitialized = true;
        }

        public float GetNoise(float x, float z)
        {
            if (!IsInitialized) throw new InvalidOperationException("Noise generator is not initialized.");

            float gradientSample = math.remap(-1f, 1f, 0, 1f, gradientGen.GetNoise(x, z));
            float voronoiSample = math.remap(-1f, 1f, 0, 1f, worleyGen.GetNoise(x, z));
            float sample = gradientSample * gradientWeight + voronoiSample;
            sample *= worleyStrengthMultiplier;
            sample = math.smoothstep(smoothstepLevel, smoothstepLevel - smoothstepWidth, sample);
            return sample;
        }

        private void OnValidate()
        {
            Initialize();
        }

        private void OnDrawGizmosSelected()
        {
            if (IsInitialized == false) Initialize();

            const int n = 100;

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    float sample = GetNoise(i, j);
                    float x = i;
                    float z = j;
                    Gizmos.color = new Color(sample, sample, sample);
                    Gizmos.DrawCube(new Vector3(x, 0, z), Vector3.one);
                }
            }
        }
    }
}