using System.Runtime.CompilerServices;

namespace Roots.World
{
    public readonly struct GridInfo
    {
        public readonly float size;
        public readonly float stepSize;
        public readonly int edgeCount;
        public readonly int totalPoints;
        
        private GridInfo(float size, float stepSize, int edgeCount, int totalPoints)
        {
            this.size = size;
            this.stepSize = stepSize;
            this.edgeCount = edgeCount;
            this.totalPoints = totalPoints;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GridInfo FromEdgeCount(float size, int edgeCount)
        {
            float stepSize = size / (edgeCount - 1);
            int totalPoints = edgeCount * edgeCount;

            return new GridInfo(size, stepSize, edgeCount, totalPoints);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GridInfo FromSubdivisionsPerUnit(int size, int subdivisions)
        {
            int edgeCount = size * (subdivisions + 1) + 1;
            float stepSize = 1.0f / (subdivisions + 1);
            int totalPoints = edgeCount * edgeCount;
            return new GridInfo(size, stepSize, edgeCount, totalPoints);
        }
    }
}