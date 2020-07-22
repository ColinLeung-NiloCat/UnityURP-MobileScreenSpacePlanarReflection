//see README here: https://github.com/ColinLeung-NiloCat/UnityURP-MobileScreenSpacePlanarReflection
#ifndef MobileSSPRInclude
#define MobileSSPRInclude

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

//textures         
TEXTURE2D(_MobileSSPR_ColorRT);
sampler LinearClampSampler;

sampler2D _SSPR_UVNoiseTex;
sampler2D _ReflectionAreaTex;

half3 ApplySSPRAndReflectionProbe(half3 input) 
{ 
    //sample scene's reflection probe
    half3 viewWS = (IN.posWS - _WorldSpaceCameraPos);
    viewWS = normalize(viewWS);

    half3 reflectDirWS = viewWS * half3(1,-1,1);//reflect at horizontal plane

    //call this function in Lighting.hlsl-> half3 GlossyEnvironmentReflection(half3 reflectVector, half perceptualRoughness, half occlusion)
    half3 reflectionProbeResult = GlossyEnvironmentReflection(reflectDirWS,_Roughness,1);               
    half4 SSPRResult = 0;
#if _MobileSSPR
    //our screen space reflection
    half2 noise = tex2D(_SSPR_UVNoiseTex, IN.uv);
    noise = noise *2-1;
    noise.y = -abs(noise); //hide missing data, only allow offset to valid location
    noise.x *= 0.25;
    
    half2 screenUV = IN.screenPos.xy/IN.screenPos.w;
    SSPRResult = SAMPLE_TEXTURE2D(_MobileSSPR_ColorRT,LinearClampSampler, screenUV + noise * _SSPR_NoiseIntensity); //use LinearClampSampler to make it blurry
#endif
    //final reflection
    half3 finalReflection = lerp(reflectionProbeResult,SSPRResult.rgb, SSPRResult.a * _BaseColor.a);//combine reflection probe and SSPR
    
    //show reflection area
    half reflectionArea = tex2D(_ReflectionAreaTex,IN.uv);

    return half4(lerp(baseColor,finalReflection,reflectionArea),1);
}
#endif
