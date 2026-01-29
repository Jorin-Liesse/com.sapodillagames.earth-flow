using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;

// [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
[BurstCompile]
public struct CullingJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<AABB> ChunksBounds;
    [ReadOnly] public NativeArray<int2> ChunksGridCoord;
    [ReadOnly] public int PlayerChunkX;
    [ReadOnly] public int PlayerChunkY;
    [ReadOnly] public int ViewDistanceSqr;
    [ReadOnly] public NativeArray<float4> FrustumPlanes;

    [WriteOnly] public NativeArray<bool> ChunksIsVisible;

    public void Execute(int index)
    {
        AABB bounds = ChunksBounds[index];
        for (int i = 0; i < 6; i++)
        {
            float4 plane = FrustumPlanes[i];
            float3 normal = plane.xyz;
            float3 absNormal = math.abs(normal);
            float r = math.dot(bounds.Extents, absNormal);
            float s = math.dot(bounds.Center, normal) + plane.w;
            if (s + r < 0)
            {
                ChunksIsVisible[index] = false;
                return;
            }
        }

        ChunksIsVisible[index] = true;
    }
}
