using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Roots.Util
{
    public static class CartesianMath
    {
        /// <summary>
        /// Calculate whether a point 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInSquareRadius(int2 point, int radius)
        {
            return point.x < radius && point.x > -radius && point.y < radius && point.y > -radius;
        }

        /// <summary>
        /// Calculate whether a point on a grid of discrete values is 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInSquareRadius(int2 point, int2 center, int radius)
        {
            point -= center;
            return point.x < radius && point.x > -radius && point.y < radius && point.y > -radius;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ManhattanDistance(int2 a, int2 b)
        {
            return math.abs(a.x - b.x) + math.abs(a.y - b.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVector2(this int2 a)
        {
            return new Vector2(a.x, a.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Int ToVector2Int(this int2 a)
        {
            return new Vector2Int(a.x, a.y);
        }
    }
}