#ifndef CURVEGRASS_INCLUDED
#define CURVEGRASS_INCLUDED

float3 CurveGrass(float y, float2 direction, float profile, float strength)
{
    float curve = pow(y, profile) * strength;
    return float3(direction.x * curve, 0.0, direction.y * curve);
}

#endif // CURVEGRASS_INCLUDED
