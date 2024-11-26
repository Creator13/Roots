inline float2 randomOffset(float2 seed, float offset)
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    seed = frac(sin(mul(seed, m)) * 46839.32);
    return float2(sin(seed.y * offset) * 0.5 + 0.5, cos(seed.x * offset) * 0.5 + 0.5);
}

void CustomVoronoi_float(float2 UV, float AngleOffset, float CellDensity, out float DistFromCenter, out float DistFromEdge)
{
    int2 cell = floor(UV * CellDensity);
    float2 posInCell = frac(UV * CellDensity);

    DistFromCenter = 8.0f;
    float2 closestOffset;

    // Iterate through a larger neighborhood (to account for randomness)
    for (int y = -2; y <= 2; ++y)
    {
        for (int x = -2; x <= 2; ++x)
        {
            int2 neighborCell = cell + int2(x, y);

            // Generate a random position for the point in the neighboring cell
            float2 randomPoint = randomOffset(float2(neighborCell), AngleOffset);
            float2 cellOffset = float2(neighborCell - cell) + randomPoint - posInCell;

            float distToPoint = dot(cellOffset, cellOffset);

            if (distToPoint < DistFromCenter)
            {
                DistFromCenter = distToPoint;
                closestOffset = cellOffset;
            }
        }
    }

    DistFromCenter = sqrt(DistFromCenter);

    DistFromEdge = 8.0f;

    // Find the edge distance
    for (int y = -2; y <= 2; ++y)
    {
        for (int x = -2; x <= 2; ++x)
        {
            int2 neighborCell = cell + int2(x, y);

            // Generate a random position for the point in the neighboring cell
            float2 randomPoint = randomOffset(float2(neighborCell), AngleOffset);
            float2 cellOffset = float2(neighborCell - cell) + randomPoint - posInCell;

            float distToEdge = dot(0.5f * (closestOffset + cellOffset), normalize(cellOffset - closestOffset));

            DistFromEdge = min(DistFromEdge, distToEdge);
        }
    }
}
