using FastNoise;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Math = Roots.Util.Math;

namespace Roots.World
{
    [BurstCompile]
    public struct GenerateTerrainNoisePointJob : IJobParallelFor
    {
        public Vector2 offset;
        public int edgePointCount;
        public float stepSize;
        public NativeArray<float> heightData;

        public FastNoiseLite worleyGen;
        public FastNoiseLite gradientGen;

        public float height;
        public float gradientWeight;
        public float worleyStrengthMultiplier;
        public float smoothstepLevel;
        public float smoothstepWidth;
        public float noisePremultiplier;

        public void Execute(int index)
        {
            int xi = index / edgePointCount; // point x index
            int zi = index % edgePointCount; // point z index
            
            // Offset x and z with 1x -stepsize to generate border of heights used for normal calculation
            float x = xi * stepSize + offset.x - stepSize; // point x world position
            float z = zi * stepSize + offset.y - stepSize; // point z world position

            float warpedX = x, warpedZ = z;
            gradientGen.DomainWarp(ref warpedX, ref warpedZ);
            float gradientSample = math.remap(-1f, 1f, 0, 1f, gradientGen.GetNoise(warpedX, warpedZ));
            float voronoiSample = math.remap(-1f, 1f, 0, 1f, worleyGen.GetNoise(x, z));
            float sample = gradientSample * gradientWeight + voronoiSample;

            sample *= worleyStrengthMultiplier;
            sample = math.smoothstep(smoothstepLevel, smoothstepLevel - smoothstepWidth, sample);

            sample *= noisePremultiplier;
            sample = Math.Smootherstep(sample);

            heightData[index] = sample * height;
        }
    }

    [CreateAssetMenu(menuName = "Roots/Noise Generator", fileName = "New Noise Generator", order = 0)]
    public class PathNoiseGenerator : ScriptableObject
    {
        [SerializeField] private SeedProvider seedProvider;
        
        [Space]
        [SerializeField] private float frequencyModifier = 1;
        [SerializeField] private float worleyFrequency = .02f;
        [SerializeField] private float gradientFrequency = .05f;

        [Space]
        [SerializeField] private float gradientWeight = -.26f;
        [SerializeField] private float worleyStrengthMultiplier = 2.33f;

        [Space]
        [SerializeField] private float smoothstepLevel = .89f;
        [SerializeField] private float smoothstepWidth = 1.27f;
        
        [Space]
        [SerializeField] private float noisePremultiplier = 1.1f;
        [SerializeField] private float height = 4f;

        private FastNoiseLite worleyGen;
        private FastNoiseLite gradientGen;

        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            worleyGen = new FastNoiseLite(seedProvider.Seed);
            worleyGen.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            worleyGen.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Euclidean);
            worleyGen.SetCellularJitter(.88f);
            worleyGen.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance2Div);
            worleyGen.SetFrequency(worleyFrequency * frequencyModifier);

            gradientGen = new FastNoiseLite(seedProvider.Seed);
            gradientGen.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
            gradientGen.SetFrequency(gradientFrequency * frequencyModifier);
            gradientGen.SetDomainWarpType(FastNoiseLite.DomainWarpType.BasicGrid);
            gradientGen.SetDomainWarpAmp(1f);
            gradientGen.SetFractalType(FastNoiseLite.FractalType.Ridged);
            gradientGen.SetFractalOctaves(3);
            gradientGen.SetFractalLacunarity(1.8f);

            isInitialized = true;

            Debug.Log("Noise generator initialized", this);
        }

        public float GetNoise(float x, float z)
        {
            Assert.IsTrue(IsInitialized, "Noise generator is not initialized.");

            float warpedX = x, warpedZ = z;
            gradientGen.DomainWarp(ref warpedX, ref warpedZ);
            float gradientSample = math.remap(-1f, 1f, 0, 1f, gradientGen.GetNoise(warpedX, warpedZ));
            float voronoiSample = math.remap(-1f, 1f, 0, 1f, worleyGen.GetNoise(x, z));
            float sample = gradientSample * gradientWeight + voronoiSample;
            sample *= worleyStrengthMultiplier;
            sample = math.smoothstep(smoothstepLevel, smoothstepLevel - smoothstepWidth, sample);
            return sample;
        }

        public GenerateTerrainNoisePointJob CreateNoiseGenJob(int edgePointCount, float stepSize, Vector2 chunkOffset, NativeArray<float> heightDataArray)
        {
            Assert.IsTrue(IsInitialized, "Noise generator is not initialized.");
            
            return new GenerateTerrainNoisePointJob
            {
                gradientGen = gradientGen,
                worleyGen = worleyGen,
                edgePointCount = edgePointCount,
                heightData = heightDataArray,
                stepSize = stepSize,
                offset = chunkOffset,
                
                gradientWeight = gradientWeight,
                worleyStrengthMultiplier = worleyStrengthMultiplier,
                smoothstepWidth = smoothstepWidth,
                smoothstepLevel = smoothstepLevel,
                noisePremultiplier = noisePremultiplier,
                height = height
            };
        }

        private void OnValidate()
        {
            Initialize();
        }
    }
}