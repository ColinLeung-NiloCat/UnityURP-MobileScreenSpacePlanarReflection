Shader "MobileSSPR/DebugDrawer"
{
    Properties
    {
        [MainColor] _BaseColor("BaseColor", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("BaseMap", 2D) = "white" {}

        _SSPR_UVNoiseTex("_SSPR_UVNoiseTex", 2D) = "gray" {}
        _SSPR_NoiseIntensity("_SSPR_NoiseIntensity", range(-0.05,0.05)) = 0.02

        _UV_MoveSpeed("_UV_MoveSpeed", Float) = 0

        _ReflectionAreaTex("_ReflectionArea", 2D) = "gray" {}
    }

    SubShader
    {
        Pass
        {
            Tags { "LightMode"="MobileSSPR" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 screenPos    : TEXCOORD1;
                
                float4 positionHCS  : SV_POSITION;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _SSPR_NoiseIntensity;
            float _UV_MoveSpeed;
            CBUFFER_END

            TEXTURE2D(_MobileSSPR_RT);
            sampler LinearClampSampler;
            sampler2D _SSPR_UVNoiseTex;
            sampler2D _ReflectionAreaTex;



            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap)+ float2(0,_Time.y*_UV_MoveSpeed);
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            { 
                //base
                half3 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                //reflection
                half2 noise = tex2D(_SSPR_UVNoiseTex, IN.uv);
                half2 screenUV = IN.screenPos.xy/IN.screenPos.w;
                half4 r = SAMPLE_TEXTURE2D(_MobileSSPR_RT,LinearClampSampler, screenUV + (noise*2-1)* _SSPR_NoiseIntensity);

                //reflection area
                half reflectionArea = tex2D(_ReflectionAreaTex,IN.uv);
                return half4(lerp(c,r,reflectionArea),1);
            }
            ENDHLSL
        }
    }
}