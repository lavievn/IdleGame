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
            float4 _SkyMix;
            float _Aspect;

            half4 frag(v2f_img input) : SV_Target
            {
                float2 uv = input.uv;
                float3 direction = normalize(float3((uv.x - .5) * _Aspect * .95, .045 + uv.y * .92, 1));
                float3 clouds = texCUBE(_CloudySky, direction).rgb;
                float3 day = lerp(texCUBE(_ClearSky, direction).rgb, clouds, _SkyMix.z);
                float3 warm = texCUBE(_TwilightSky, direction).rgb;
                float3 night = clouds * float3(.085, .13, .26);
                float3 light = day * (1 - _SkyMix.x - _SkyMix.y) + warm * _SkyMix.x + night * _SkyMix.y;
                light = max(0, light * _SkyMix.w);
                return half4(1 - exp(-light), 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
