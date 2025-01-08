using System.Runtime.CompilerServices;

namespace Roots.Util
{
    public static class Math
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Smootherstep(float x)
        {
            return 6 * x * x * x * x * x - 15 * x * x * x * x + 10 * x * x * x;
        }
    }
}