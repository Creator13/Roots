using System.Runtime.CompilerServices;

namespace Roots.World.Chunking
{
    public static class GridInfoMeshExtensions
    {
        public static int GetIndicesCount(this GridInfo info)
        {
            return (info.totalPoints - info.edgeCount) * 6;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexFromXZ(this GridInfo info, int x, int z)
        {
            return x * info.edgeCount + z;
        }
    }
}