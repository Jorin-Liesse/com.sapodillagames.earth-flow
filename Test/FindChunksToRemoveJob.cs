using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;

// [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
[BurstCompile]
public struct FindChunksToRemoveJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int> UsedChunks;
    [ReadOnly] public NativeArray<int2> ChunksGridCoord;

    [ReadOnly] public int2 PlayerGrid;
    [ReadOnly] public int RadiusSqr;

    [WriteOnly] public NativeList<int>.ParallelWriter RemoveIndices;

    public void Execute(int index)
    {
        int chunkIndex = UsedChunks[index];
        int2 coord = ChunksGridCoord[chunkIndex];

        int2 delta = coord - PlayerGrid;
        int distSqr = delta.x * delta.x + delta.y * delta.y;

        if (distSqr >= RadiusSqr)
            RemoveIndices.AddNoResize(index);
    }
}
