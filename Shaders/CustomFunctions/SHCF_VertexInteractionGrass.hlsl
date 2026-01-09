#ifndef VERTEXINTERACTIONGRASS_INCLUDED
#define VERTEXINTERACTIONGRASS_INCLUDED

#include "SHCF_CurveGrass.hlsl"

StructuredBuffer<float4> _InteractionPoints;

void VertexInteractionGrass_float(float3 vertex, float3 boundCenter, int amountPoints, float strength, float falloff, out float3 output, out float weight)
{
    output = float3(0, 0, 0);
    weight = 0.0;

    if (amountPoints <= 0) return;
    
    float3 totalOffset = float3(0, 0, 0);
    float totalWeight = 0.0;

    [loop]
    for (int i = 0; i < amountPoints; i ++)
    {
        float3 ipos = _InteractionPoints[i].xyz;
        float radius = _InteractionPoints[i].w;

        float3 diff = vertex + boundCenter - ipos;
        float dist = length(diff);

        if (dist < radius)
        {
            diff.y = 1.5;
            float3 dir = normalize(diff + 0.0001);

            float normalizedW = saturate(dist / radius);
            float w = pow(1.0 - normalizedW, 0.5 + falloff);

            float3 offset = CurveGrass(vertex.y, dir.xz, strength, w);

            totalOffset += offset;
            totalWeight += w;
        }
    }

    output = clamp(totalOffset, float3(-1.5, 0, -1.5), float3(1.5, 2.0, 1.5));
    weight = clamp(totalWeight, 0.0, 1.0);
}

#endif // VERTEXINTERACTIONGRASS_INCLUDED
