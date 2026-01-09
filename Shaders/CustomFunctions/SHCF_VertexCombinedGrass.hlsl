#ifndef VERTEXCOMBINEDGRASS_INCLUDED
#define VERTEXCOMBINEDGRASS_INCLUDED

void VertexCombinedGrass_float(float3 windOffsetVertex, float3 vertex, float3 interactionOffsetVertex, float interactionWeight, out float3 output)
{
    output = vertex + windOffsetVertex * (interactionWeight -1) + interactionOffsetVertex * interactionWeight;
}

#endif // VERTEXCOMBINEDGRASS_INCLUDED
