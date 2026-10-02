#ifndef LINGGUANG_WATER_RIPPLE_INCLUDED
#define LINGGUANG_WATER_RIPPLE_INCLUDED

// Uniforms are declared by each consumer. Geometry = lifetime, speed, k, width.
// Analytic damped wave packet: signed heights/slopes sum BEFORE lighting.
// Opposing waves therefore cancel, unlike additive rings or particle sweeps.
void WaterSurface(float2 p, out float height, out float2 slope)
{
    height = 0;
    slope = 0;
    [unroll] for (int k = 0; k < 8; k++)
    {
        float age = _WaveClock - _Waves[k].z;
        if (_Waves[k].w <= 0 || age <= 0 || age >= _WaveGeometry.x) continue;
        float2 delta = p - _Waves[k].xy;
        float r = length(delta);
        float q = r - age * _WaveGeometry.y;
        float width2 = _WaveGeometry.w * _WaveGeometry.w;
        float envelope = exp(-q * q / width2);
        float fade = smoothstep(0, 0.22, age) * (1 - smoothstep(0.5, _WaveGeometry.x, age));
        float attenuation = 1 / (1 + r * 0.22);
        float amplitude = 0.16 * _Waves[k].w * fade * attenuation;
        float sn, cs;
        sincos(q * _WaveGeometry.z, sn, cs);
        height += sn * envelope * amplitude;
        float derivative = envelope * amplitude *
            (_WaveGeometry.z * cs - sn * (2 * q / width2 + 0.22 * attenuation));
        // Smooth the radial singularity at the impact point, never divide by zero.
        slope += delta / max(r, 0.12) * derivative;
    }
}

float WaterLobeMask(float2 p, float4 lobe)
{
    float2 q = abs((p - lobe.xy) / max(lobe.zw, 0.01));
    // Matches the superellipse used by NeuralBackdrop (cos^.76, sin^.84).
    float d = pow(q.x, 2.0 / 0.76) + pow(q.y, 2.0 / 0.84);
    return 1 - smoothstep(0.72, 1, d);
}

float WaterMask(float2 p)
{
    return max(WaterLobeMask(p, _WaterLobe0), WaterLobeMask(p, _WaterLobe1));
}
#endif
