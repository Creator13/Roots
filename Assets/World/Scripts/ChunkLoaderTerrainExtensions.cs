using Roots.Util;
using Roots.World.Chunking;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Roots.World
{
    public static class ChunkLoaderTerrainExtensions
    {
        public static Vector3 RandomLowPointNormalDistribution(this ChunkLoader chunkLoader, ref Random random, Vector3 positionOffset, float minDistance, float maxDistance, float deviationModifier, float maxAltitude)
        {
            float randomDistance = Math.RandomNormalDistribution(random, minDistance, maxDistance, deviationModifier);

            // TODO: Limit this to avoid a worst case scenario (store a best value, use that if a max # of attempts is reached)
            // TODO: make a function that batch requests a few heights 
            float2 randomXZ = random.NextFloat2Direction() * randomDistance;
            float altitude = chunkLoader.GetExactGroundHeightAt(new Vector3(randomXZ.x, 0, randomXZ.y) + positionOffset);
            while (altitude > maxAltitude)
            {
                randomXZ = random.NextFloat2Direction() * randomDistance;
                altitude = chunkLoader.GetExactGroundHeightAt(new Vector3(randomXZ.x, 0, randomXZ.y) + positionOffset);
            }
            
            return new Vector3(randomXZ.x, altitude, randomXZ.y);
        }
    }
}