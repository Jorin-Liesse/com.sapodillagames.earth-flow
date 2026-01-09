#ifndef VERTEXWINDGRASS_INCLUDED
#define VERTEXWINDGRASS_INCLUDED

#include "SHCF_CurveGrass.hlsl"

void VertexWindGrass_float(float3 vertex, float windStrength, float windFrequency, float windSpeed, float2 windDirection, out float3 output)
{
    float2 windDirNormalized = normalize(windDirection);

    float projectedPos = dot(float2(vertex.x, vertex.z), windDirNormalized);

    float curveIntensity = (sin(projectedPos * windFrequency + _Time.y * windSpeed) * 0.5 + 0.5);

    float3 windOffset = CurveGrass(vertex.y, windDirNormalized, 1.5, (windStrength * curveIntensity)) * windStrength;

    float curve = pow(vertex.y, 1.5) * (windStrength * curveIntensity);

    output =  windOffset;
}

#endif // VERTEXWINDGRASS_INCLUDED
