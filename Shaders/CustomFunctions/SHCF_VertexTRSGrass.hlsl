#ifndef VERTEXTRSGRASS_INCLUDED  // VERTEXTRSGRASS_INCLUDED
#define VERTEXTRSGRASS_INCLUDED  // VERTEXTRSGRASS_INCLUDED

struct GrassInstance
{
    float3 position;
    uint packedData;
};

StructuredBuffer<GrassInstance> _InstanceBuffer;
StructuredBuffer<uint> _VisibleIndicesBuffer;

float3 RotateY(float3 vertex, float sinAngle, float cosAngle)
{
    return float3(
        vertex.x * cosAngle + vertex.z * sinAngle,
        vertex.y,
        -vertex.x * sinAngle + vertex.z * cosAngle
    );
}

void VertexTRSGrass_float(float3 vertex, int instanceID, float2 rangeScale, float3 boundsExtents, float3 boundsCenter, out float3 output)
{
    uint actualInstanceIndex = _VisibleIndicesBuffer[instanceID];
    GrassInstance instance = _InstanceBuffer[actualInstanceIndex];

    uint rot16 = instance.packedData >> 16;
    uint scale16 = instance.packedData & 0xFFFF;
    
    float rot01 = rot16 * 0.0000152590218967;
    float scale01 = scale16 * 0.0000152590218967;

    float angleRadians = rot01 * 6.28318530718;
    
    float sinAngle, cosAngle;
    sincos(angleRadians, sinAngle, cosAngle);

    float scale = rangeScale.x + (rangeScale.y - rangeScale.x) * scale01;

    output = RotateY((vertex - boundsCenter) * scale, sinAngle, cosAngle) + (instance.position - boundsCenter);
}

#endif // VERTEXTRSGRASS_INCLUDED
