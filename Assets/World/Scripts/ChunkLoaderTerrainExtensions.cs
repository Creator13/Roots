using Roots.Util;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Roots.World
{
    public static class ChunkLoaderTerrainExtensions
    {
        public static Vector3 RandomLowPointNormalDistribution(this ChunkLoader chunkLoader, Random random, float minDistance, float maxDistance, float deviationModifier, float maxAltitude)
        {
            float randomDistance = Math.RandomNormalDistribution(random, minDistance, maxDistance, deviationModifier);

            // TODO: Limit this to avoid a worst case scenario (store a best value, use that if a max # of attempts is reached)
            Vector2 randomXZ = random.NextFloat2Direction() * randomDistance;
            float altitude = chunkLoader.GetInterpolatedGroundHeightAt(new Vector3(randomXZ.x, 0, randomXZ.y));
            while (altitude > maxAltitude)
            {
                randomXZ = random.NextFloat2Direction() * randomDistance;
                altitude = chunkLoader.GetInterpolatedGroundHeightAt(new Vector3(randomXZ.x, 0, randomXZ.y));
            }
            
            return new Vector3(randomXZ.x, altitude, randomXZ.y);
        }
    }
}