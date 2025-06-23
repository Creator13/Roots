using System.Runtime.CompilerServices;

// Grids in the project are defined with x being the column axis and z the row axis. Implications for index calculations:
//    x: movement: ±grid width;   x index = i / grid width;   outer loop
//    z: movement: ±1;            z index = i % grid width;   inner loop
// i = x * width + z;

namespace Roots.World.Chunking
{
    public readonly struct GridInfo
    {
        public readonly float size;
        public readonly float stepSize;
        public readonly float invStepSize;
        public readonly int edgeCount;
        public readonly int totalPoints;
        
        private GridInfo(float size, float stepSize, int edgeCount)
        {
            this.size = size;
            this.stepSize = stepSize;
            this.edgeCount = edgeCount;
            
            this.invStepSize = 1.0f / stepSize;
            this.totalPoints = edgeCount * edgeCount;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GridInfo FromEdgePointCount(float size, int edgeCount)
        {
            float stepSize = size / (edgeCount - 1);

            return new GridInfo(size, stepSize, edgeCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GridInfo FromSubdivisionsPerUnit(int size, int subdivisions)
        {
            int edgeCount = size * (subdivisions + 1) + 1;
            float stepSize = 1.0f / (subdivisions + 1);
            return new GridInfo(size, stepSize, edgeCount);
        }
    }
}