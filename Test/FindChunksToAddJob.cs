using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;

// [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
[BurstCompile]
public struct FindChunksToAddJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int2> CircularCoords;
    [ReadOnly] public NativeArray<byte> ChunkStamp;

    [ReadOnly] public int2 PlayerGrid;
    [ReadOnly] public int RadiusSqr;

    [ReadOnly] public int2 Min;
    [ReadOnly] public int2 Max;
    [ReadOnly] public int GridWidth;

    [WriteOnly] public NativeList<int2>.ParallelWriter AddCoords;

    readonly int ToIndex(int2 coord)
    {
        int x = coord.x + GridWidth / 2;
        int y = coord.y + GridWidth / 2;
        return x + y * GridWidth;
    }

    public void Execute(int index)
    {
        int2 coord = CircularCoords[index] + PlayerGrid;

        if (coord.x < Min.x || coord.x >= Max.x ||
            coord.y < Min.y || coord.y >= Max.y)
            return;

        int2 delta = coord - PlayerGrid;
        int distSqr = delta.x * delta.x + delta.y * delta.y;

        if (distSqr >= RadiusSqr) return;
        if (ChunkStamp[ToIndex(coord)] == 1) return;

        AddCoords.AddNoResize(coord);
    }
}
