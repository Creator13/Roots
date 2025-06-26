using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Random = Unity.Mathematics.Random;

namespace Roots.Util
{
    public static class Math
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Smootherstep(float x)
        {
            return 6 * x * x * x * x * x - 15 * x * x * x * x + 10 * x * x * x;
        }

        // deviationMultiplier: Smaller values cluster the random distance towards the center of the spread.
        public static float RandomNormalDistribution(float min, float max, float deviationMultiplier = 1)
        {
            float center = (min + max) / 2f;
            float range = max - min;
            float standardDeviation = (range / 6f) * deviationMultiplier;

            // Box-muller
            float u1 = UnityEngine.Random.value;
            float u2 = UnityEngine.Random.value;
            float z = math.sqrt(-2.0f * math.log(u1)) * math.cos(2.0f * math.PI * u2);

            float distance = center + z * standardDeviation;
            return math.clamp(distance, min, max);
        }

        // deviationMultiplier: Smaller values cluster the random distance towards the center of the spread.
        public static float RandomNormalDistribution(Random random, float min, float max, float deviationMultiplier = 1)
        {
            float center = (min + max) / 2f;
            float range = max - min;
            float standardDeviation = (range / 6f) * deviationMultiplier;

            // Box-muller
            float u1 = random.NextFloat();
            float u2 = random.NextFloat();
            float z = math.sqrt(-2.0f * math.log(u1)) * math.cos(2.0f * math.PI * u2);

            float distance = center + z * standardDeviation;
            return math.clamp(distance, min, max);
        }

        public static Vector3[] SmoothPathChaikin(Vector3[] path, int iterations, bool keepEndpoints = true)
        {
            List<Vector3> points = new(path);

            for (int iter = 0; iter < iterations; iter++)
            {
                List<Vector3> newPoints = new();

                if (keepEndpoints && points.Count > 0)
                    newPoints.Add(points[0]);

                for (int i = 0; i < points.Count - 1; i++)
                {
                    Vector3 p0 = points[i];
                    Vector3 p1 = points[i + 1];

                    Vector3 Q = Vector3.Lerp(p0, p1, 0.25f);
                    Vector3 R = Vector3.Lerp(p0, p1, 0.75f);

                    newPoints.Add(Q);
                    newPoints.Add(R);
                }

                if (keepEndpoints && points.Count > 1)
                    newPoints.Add(points[^1]);

                points = newPoints;
            }

            return points.ToArray();
        }

        public static Vector3[] AverageSmoothPath(Vector3[] path)
        {
            Vector3[] result = new Vector3[path.Length];
            result[0] = path[0];
            result[^1] = path[^1];

            for (int i = 1; i < path.Length - 1; i++)
                result[i] = (path[i - 1] + path[i] + path[i + 1]) / 3f;

            return result;
        }

        public static Vector3[] ApplyNoiseToPath(Vector3[] input, int seed, float noiseAmplitude = 1.0f, float noiseFrequency = 0.1f)
        {
            Vector3[] output = new Vector3[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                Vector3 point = input[i];

                float x = point.x * noiseFrequency + seed * 0.001f;
                float z = point.z * noiseFrequency + seed * 0.002f;

                float offsetX = Mathf.PerlinNoise(x, z) - 0.5f;
                float offsetZ = Mathf.PerlinNoise(x + 10f, z + 10f) - 0.5f;

                Vector3 offset = new Vector3(offsetX, 0f, offsetZ) * noiseAmplitude;

                output[i] = point + offset;
            }

            return output;
        }

        public static Vector3[] ModifyPathLikeRoot(Vector3[] input, uint seed, Func<Vector3, float> getGroundHeight,
            float noiseAmplitude = 1f, float noiseFrequency = 0.1f)
        {
            if (input == null || input.Length < 2)
            {
                return input;
            }

            Assert.IsNotNull(getGroundHeight, "Must supply terrain height sampling function.");

            Vector3[] output = new Vector3[input.Length];

            Random rand = new Random(seed);
            float offset = rand.NextFloat(1000);

            output[0] = input[0];
            output[^1] = input[^1];

            for (int i = 1; i < input.Length - 1; i++)
            {
                float t = i / ((float)input.Length - 1);

                Vector3 prev = input[i - 1];
                Vector3 next = input[i + 1];
                Vector3 forward = (next - prev).normalized;

                Vector3 right = Vector3.Cross(Vector3.up, forward);

                Vector3 pos = input[i];
                float noiseX = Mathf.PerlinNoise(pos.x * noiseFrequency + offset, pos.z * noiseFrequency + offset);

                float offsetX = (noiseX - 0.5f) * 2f * noiseAmplitude * EdgeFalloff(t, .2f);

                Vector3 bend = right * offsetX;
                Vector3 bentPos = pos + bend;

                float terrainY = getGroundHeight(bentPos);
                output[i] = new Vector3(bentPos.x, terrainY, bentPos.z);
            }

            return output;
        }


        public static Vector3[] SubdividePath(Vector3[] path, int subdivisionsPerSegment)
        {
            if (path == null || path.Length < 2 || subdivisionsPerSegment < 1)
                return path;

            int newPointCount = (path.Length - 1) * subdivisionsPerSegment + 1;
            Vector3[] output = new Vector3[newPointCount];

            int index = 0;

            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector3 a = path[i];
                Vector3 b = path[i + 1];

                for (int j = 0; j < subdivisionsPerSegment; j++)
                {
                    float t = j / (float)subdivisionsPerSegment;
                    output[index++] = Vector3.Lerp(a, b, t);
                }
            }

            output[index] = path[^1]; // last point
            return output;
        }

        public static float EdgeFalloff(float t, float range = 0.1f)
        {
            range = math.clamp(range, 0f, 0.5f);

            float distToEdge = math.min(t, 1f - t);

            if (distToEdge < range)
            {
                return 1 - (range - distToEdge) / range;
            }

            return 1f;
        }

        public static float GetLineLength(this Vector3[] line)
        {
            float length = 0f;
            for (int i = 0; i < line.Length - 1; i++)
            {
                length += Vector3.Distance(line[i], line[i + 1]);
            }

            return length;
        } 
    }
}

