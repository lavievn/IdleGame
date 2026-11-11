Shader "Hidden/IdleGame/WorldClimateSkyProjection"
{
    Properties
    {
        _ClearSky ("Day", Cube) = "" {}
        _CloudySky ("Cloudy", Cube) = "" {}
        _TwilightSky ("Twilight", Cube) = "" {}
        _SkyMix ("Twilight, Night, Cloud, Exposure", Vector) = (0,0,0,1)
        _Aspect ("Aspect", Float) = 1.7778
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _ClearSky, _CloudySky, _TwilightSky;
            float4 _SkyMix, _Storm;
            float _Aspect, _SkyScroll;

            half4 frag(v2f_img input) : SV_Target
            {
                float2 uv = input.uv;
                // Double the angular span to halve apparent sky feature size.
                float horizontal = min(3.05, 4 * atan(_Aspect * .475));
                float longitude = (uv.x - .5 - _SkyScroll) * horizontal;
                float elevation = .045 + uv.y * 1.44;
                float3 direction = float3(sin(longitude) * cos(elevation),
                    sin(elevation), cos(longitude) * cos(elevation));
                float3 clouds = texCUBE(_CloudySky, direction).rgb;
                float3 day = lerp(texCUBE(_ClearSky, direction).rgb, clouds, _SkyMix.z);
                float3 warm = texCUBE(_TwilightSky, direction).rgb;
                float3 night = clouds * float3(.085, .13, .26);
                float3 light = day * (1 - _SkyMix.x - _SkyMix.y) + warm * _SkyMix.x + night * _SkyMix.y;
                light = max(0, light * _SkyMix.w);
                // A small, distant sky-only pulse and bolt. Terrain and HUD are separate.
                float y = (uv.y - .54) * 3.85;
                float zigzag = (abs(frac(y * 3) - .5) - .25) * .032;
                float boltX = _Storm.y + zigzag + y * .034;
                float bolt = (1 - smoothstep(.001, .0035, abs(uv.x - boltX))) * step(0, y) * step(y, 1);
                float glow = exp(-14 * abs(uv.x - _Storm.y)) * smoothstep(.42, .85, uv.y);
                light += _Storm.x * (glow * .58 + bolt * 1.8) * float3(.74, .86, 1);
                return half4(1 - exp(-light), 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
