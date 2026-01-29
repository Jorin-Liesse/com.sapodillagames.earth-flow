using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;

[BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
public struct CullingJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<int> UsedChunks;
    [ReadOnly] public NativeArray<int2> ChunksGridCoord;
    [ReadOnly] public NativeArray<AABB> ChunksBounds;
    [ReadOnly] public NativeArray<float4> FrustumPlanes;

    [ReadOnly] public int2 PlayerGrid;
    [ReadOnly] public int ViewDistanceSqr;

    [WriteOnly] public NativeArray<bool> UsedVisibility;

    public void Execute(int index)
    {
        if (DistanceCulling(index))
        {
            UsedVisibility[index] = false;
            return;
        }

        if (FrustumCulling(index))
        {
            UsedVisibility[index] = false;
            return;
        }

        UsedVisibility[index] = true;
    }

    bool DistanceCulling(int index)
    {
        int chunkIndex = UsedChunks[index];
        int2 coord = ChunksGridCoord[chunkIndex];

        int2 delta = coord - PlayerGrid;
        int distSqr = delta.x * delta.x + delta.y * delta.y;

        return distSqr >= ViewDistanceSqr;
    }

    bool FrustumCulling(int index)
    {
        int chunkIndex = UsedChunks[index];
        AABB bounds = ChunksBounds[chunkIndex];

        for (int i = 0; i < 6; i++)
        {
            float4 plane = FrustumPlanes[i];
            float3 n = plane.xyz;

            float r = math.dot(bounds.Extents, math.abs(n));
            float s = math.dot(bounds.Center, n) + plane.w;

            if (s + r < 0)
                return true;
        }

        return false;
    }
}
