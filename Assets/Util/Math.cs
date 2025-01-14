using System.Runtime.CompilerServices;
using Unity.Mathematics;

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
    }
}