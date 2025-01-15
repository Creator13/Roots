using FastNoise;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
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
        public FastNoiseLite ridgeGen;
        public FastNoiseLite fbmGen;

        public float height;
        public float gradientWeight;
        public float worleyStrengthMultiplier;
        public float fbmGenStrength;
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

            float ridgeWarpedX = x, ridgeWarpedZ = z;
            ridgeGen.DomainWarp(ref ridgeWarpedX, ref ridgeWarpedZ);
            float gradientSample = math.remap(-1f, 1f, 0, 1f, ridgeGen.GetNoise(ridgeWarpedX, ridgeWarpedZ));
            
            float worleyWarpedX = x, worleyWarpedZ = z;
            worleyGen.DomainWarp(ref worleyWarpedX, ref worleyWarpedZ, 1.86f);
            float voronoiSample = math.remap(-1f, 1f, 0, 1f, worleyGen.GetNoise(worleyWarpedX, worleyWarpedZ));
            
            float sample = gradientSample * gradientWeight + voronoiSample;
            
            sample *= worleyStrengthMultiplier;

            sample += math.remap(-1, 1, 0, 1, fbmGen.GetNoise(x, z)) * fbmGenStrength;
            sample *= .5f;
            
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
        [SerializeField] private float fbmFrequency = .1f;

        [Space]
        [SerializeField] private float gradientWeight = -.26f;
        [SerializeField] private float worleyStrengthMultiplier = 2.33f;
        [FormerlySerializedAs("fbmStrength")] [SerializeField] private float fbmGenStrength = .2f;

        [Space]
        [SerializeField] private float smoothstepLevel = .89f;
        [SerializeField] private float smoothstepWidth = 1.27f;
        
        [Space]
        [SerializeField] private float noisePremultiplier = 1.1f;
        [SerializeField] private float height = 4f;

        private FastNoiseLite worleyGen;
        private FastNoiseLite ridgeGen;
        private FastNoiseLite fbmGen;

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
            worleyGen.SetDomainWarpType(FastNoiseLite.DomainWarpType.BasicGrid);
            worleyGen.SetDomainWarpAmp(.36f);
            worleyGen.SetFrequency(worleyFrequency * frequencyModifier);

            ridgeGen = new FastNoiseLite(seedProvider.Seed);
            ridgeGen.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
            ridgeGen.SetFrequency(gradientFrequency * frequencyModifier);
            ridgeGen.SetDomainWarpType(FastNoiseLite.DomainWarpType.BasicGrid);
            ridgeGen.SetDomainWarpAmp(1f);
            ridgeGen.SetFractalType(FastNoiseLite.FractalType.Ridged);
            ridgeGen.SetFractalOctaves(3);
            ridgeGen.SetFractalLacunarity(1.8f);
            
            fbmGen = new FastNoiseLite(seedProvider.Seed);
            fbmGen.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            fbmGen.SetFrequency(fbmFrequency * frequencyModifier);
            fbmGen.SetFractalType(FastNoiseLite.FractalType.FBm);
            fbmGen.SetFractalOctaves(6);
            fbmGen.SetFractalWeightedStrength(0.8f);
            fbmGen.SetFractalGain(0.5f);
            fbmGen.SetFractalLacunarity(2.2f);

            isInitialized = true;

            Debug.Log("Noise generator initialized", this);
        }

        public float GetNoise(float x, float z)
        {
            Assert.IsTrue(IsInitialized, "Noise generator is not initialized.");

            float ridgeWarpedX = x, ridgeWarpedZ = z;
            ridgeGen.DomainWarp(ref ridgeWarpedX, ref ridgeWarpedZ);
            float gradientSample = math.remap(-1f, 1f, 0, 1f, ridgeGen.GetNoise(ridgeWarpedX, ridgeWarpedZ));
            
            float worleyWarpedX = x, worleyWarpedZ = z;
            worleyGen.DomainWarp(ref worleyWarpedX, ref worleyWarpedZ, 1.86f);
            float voronoiSample = math.remap(-1f, 1f, 0, 1f, worleyGen.GetNoise(worleyWarpedX, worleyWarpedZ));
            
            float sample = gradientSample * gradientWeight + voronoiSample;
            
            sample *= worleyStrengthMultiplier;

            sample += math.remap(-1, 1, 0, 1, fbmGen.GetNoise(x, z)) * fbmGenStrength;
            sample *= .5f;
            
            sample = math.smoothstep(smoothstepLevel, smoothstepLevel - smoothstepWidth, sample);
            
            sample *= noisePremultiplier;
            sample = Math.Smootherstep(sample);
            
            return sample * height;
        }

        public GenerateTerrainNoisePointJob CreateNoiseGenJob(int edgePointCount, float stepSize, Vector2 chunkOffset, NativeArray<float> heightDataArray)
        {
            Assert.IsTrue(IsInitialized, "Noise generator is not initialized.");
            
            return new GenerateTerrainNoisePointJob
            {
                ridgeGen = ridgeGen,
                worleyGen = worleyGen,
                fbmGen = fbmGen,
                edgePointCount = edgePointCount,
                heightData = heightDataArray,
                stepSize = stepSize,
                offset = chunkOffset,
                
                gradientWeight = gradientWeight,
                worleyStrengthMultiplier = worleyStrengthMultiplier,
                fbmGenStrength = fbmGenStrength,
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