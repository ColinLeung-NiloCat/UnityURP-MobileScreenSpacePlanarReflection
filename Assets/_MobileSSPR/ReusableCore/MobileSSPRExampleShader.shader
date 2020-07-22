//see README here: https://github.com/ColinLeung-NiloCat/UnityURP-MobileScreenSpacePlanarReflection

//just a simple example shader to show how to use SSPR's result texture
Shader "MobileSSPR/ExampleShader"
{
    Properties
    {
        [MainColor] _BaseColor("BaseColor", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("BaseMap", 2D) = "black" {}

        _Roughness("_Roughness", range(0,1)) = 0.25 
        [NoScaleOffset]_SSPR_UVNoiseTex("_SSPR_UVNoiseTex", 2D) = "gray" {}
        _SSPR_NoiseIntensity("_SSPR_NoiseIntensity", range(-0.2,0.2)) = 0.0

        _UV_MoveSpeed("_UV_MoveSpeed (xy only)(for things like water flow)", Vector) = (0,0,0,0)

        [NoScaleOffset]_ReflectionAreaTex("_ReflectionArea", 2D) = "white" {}
    }

    SubShader
    {
        Pass
        {
            //================================================================================================
            //if "LightMode"="MobileSSPR", this shader will only draw if MobileSSPRRendererFeature is on
            Tags { "LightMode"="MobileSSPR" }
            //================================================================================================

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            //================================================================================================
            #include "MobileSSPRInclude.hlsl"
            #pragma multi_compile _ _MobileSSPR
            //================================================================================================

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 screenPos    : TEXCOORD1;
                float3 posWS        : TEXCOORD2;
                float4 positionHCS  : SV_POSITION;
            };

            //textures
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            TEXTURE2D(_SSPR_UVNoiseTex);
            SAMPLER(sampler_SSPR_UVNoiseTex);
            TEXTURE2D(_ReflectionAreaTex);
            SAMPLER(sampler_ReflectionAreaTex);

            //cbuffer
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _SSPR_NoiseIntensity;
            float2 _UV_MoveSpeed;
            half _Roughness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap) + _Time.y*_UV_MoveSpeed;
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                OUT.posWS = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            { 
                //base color
                half3 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor.rgb;

                //noise texture
                float2 noise = SAMPLE_TEXTURE2D(_SSPR_UVNoiseTex,sampler_SSPR_UVNoiseTex, IN.uv);
                noise = noise *2-1;
                noise.y = -abs(noise); //hide missing data, only allow offset to valid location
                noise.x *= 0.25;
                noise *= _SSPR_NoiseIntensity;

                //================================================================================================
                //GetResultReflection from SSPR

                ReflectionInput reflectionData;
                reflectionData.posWS = IN.posWS;
                reflectionData.screenPos = IN.screenPos;
                reflectionData.screenSpaceNoise = noise;
                reflectionData.roughness = _Roughness;
                reflectionData.SSPR_Usage = _BaseColor.a;

                half3 resultReflection = GetResultReflection(reflectionData);
                //================================================================================================

                //decide show reflection area
                half reflectionArea = SAMPLE_TEXTURE2D(_ReflectionAreaTex,sampler_ReflectionAreaTex, IN.uv);

                half3 finalRGB = lerp(baseColor,resultReflection,reflectionArea);

                return half4(finalRGB,1);
            }

            ENDHLSL
        }
    }
}
