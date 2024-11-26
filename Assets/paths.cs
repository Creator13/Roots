using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class paths : MonoBehaviour
{
    public float frequencyModifier;
    
    public float worleyFrequency = 1;
    public float gradientFrequency = 1;
    public float gradientWeight = .75f;
    public float resolution = 1;
    public float multiplier = 1;

    public FastNoiseLite.CellularDistanceFunction CellularDistanceFunction;
    public FastNoiseLite.CellularReturnType CellularReturnType;
    [Range(-1, 1)] public float cellularJitter;
    public float smoothstepLevel = .76f;
    public float smoothstepWidth = 0.04f;

    bool isNoiseInitialized = false;
    float[,] noiseData;
    float noiseMin, noiseMax;

    private void GenerateNoise()
    {
        FastNoiseLite worleyGen = new FastNoiseLite();
        worleyGen.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        worleyGen.SetCellularDistanceFunction(CellularDistanceFunction);
        worleyGen.SetCellularJitter(cellularJitter);
        worleyGen.SetCellularReturnType(CellularReturnType);
        worleyGen.SetFrequency(worleyFrequency * frequencyModifier);

        FastNoiseLite gradientGen = new FastNoiseLite();
        gradientGen.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
        gradientGen.SetFrequency(gradientFrequency * frequencyModifier);

        const int n = 125;
        noiseData = new float[n, n];

        noiseMin = float.MaxValue;
        noiseMax = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                float gradientSample = math.remap(-1f, 1f, 0, 1f, gradientGen.GetNoise(i, j));
                float voronoiSample = math.remap(-1f, 1f, 0, 1f, worleyGen.GetNoise(i, j));
                float sample = gradientSample * gradientWeight + voronoiSample;
                sample *= multiplier;
                sample = math.smoothstep(smoothstepLevel, smoothstepLevel - smoothstepWidth, sample);
                noiseData[i, j] = sample;
                noiseMax = Mathf.Max(noiseMax, sample);
                noiseMin = Mathf.Min(noiseMin, sample);
            }
        }

        isNoiseInitialized = true;
    }

    [ContextMenu("Test")]
    private void Test()
    {
        Debug.Log($"max: {noiseMax}, min: {noiseMin}");
    }

    private void OnValidate()
    {
        GenerateNoise();
    }

    private void OnDrawGizmos()
    {
        if (isNoiseInitialized == false) GenerateNoise();

        const int n = 125;

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                float sample = noiseData[i, j];
                // sample = math.remap(noiseMin, noiseMax, 0, 1, sample);

                float x = i * resolution;
                float z = j * resolution;
                Gizmos.color = new Color(sample, sample, sample);
                Gizmos.DrawCube(new Vector3(x, 0, z), Vector3.one * resolution);
            }
        }
    }
}