Shader "Custom/URP_Anisotropic"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Float) = 1.0
        
        [Header(Anisotropic)]
        _Anisotropy ("Anisotropy", Range(-1, 1)) = 0.8
        _AnisotropicDirection ("Anisotropic Direction", Vector) = (1,0,0,0)
        _Roughness ("Roughness", Range(0.01, 1)) = 0.1
        _Metallic ("Metallic", Range(0, 1)) = 0.8
        
        [Header(Highlight)]
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularIntensity ("Specular Intensity", Range(0, 5)) = 2.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _NormalScale;
                float _Anisotropy;
                float4 _AnisotropicDirection;
                float _Roughness;
                float _Metallic;
                float4 _SpecularColor;
                float _SpecularIntensity;
            CBUFFER_END

            // 各向异性高光计算
            float AnisotropicSpecular(float3 H, float3 T, float3 B, float anisotropy, float roughness)
            {
                float TdotH = dot(T, H);
                float BdotH = dot(B, H);
                
                float roughnessX = roughness * (1.0 + anisotropy);
                float roughnessY = roughness * (1.0 - anisotropy);
                
                float ax = roughnessX * roughnessX;
                float ay = roughnessY * roughnessY;
                
                float denominator = TdotH * TdotH / ax + BdotH * BdotH / ay + H.z * H.z;
                return 1.0 / (PI * ax * ay * denominator * denominator);
            }

            // 各向异性Fresnel
            float3 AnisotropicFresnel(float3 F0, float VdotH, float anisotropy)
            {
                float fresnel = pow(1.0 - VdotH, 5.0);
                return F0 + (1.0 - F0) * fresnel * (1.0 + anisotropy * 0.5);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 采样纹理
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = baseMap.rgb * _BaseColor.rgb;
                
                // 法线映射
                half4 normalMap = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv);
                half3 normalTS = UnpackNormalScale(normalMap, _NormalScale);
                
                // 构建TBN矩阵
                half3 normalWS = normalize(input.normalWS);
                half3 tangentWS = normalize(input.tangentWS);
                half3 bitangentWS = normalize(input.bitangentWS);
                half3x3 TBN = half3x3(tangentWS, bitangentWS, normalWS);
                normalWS = normalize(mul(normalTS, TBN));
                
                // 调整各向异性方向
                half3 anisotropicTangent = normalize(tangentWS + _AnisotropicDirection.xyz * _AnisotropicDirection.w);
                half3 anisotropicBitangent = normalize(cross(normalWS, anisotropicTangent));
                
                // 光照计算
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                half3 halfDir = normalize(lightDir + viewDir);
                
                // 各向异性高光
                float anisotropicSpec = AnisotropicSpecular(halfDir, anisotropicTangent, anisotropicBitangent, 
                                                          _Anisotropy, _Roughness);
                
                // Fresnel
                half3 F0 = lerp(0.04, albedo, _Metallic);
                half VdotH = saturate(dot(viewDir, halfDir));
                half3 fresnel = AnisotropicFresnel(F0, VdotH, _Anisotropy);
                
                // 组合最终颜色
                half NdotL = saturate(dot(normalWS, lightDir));
                half3 specular = anisotropicSpec * fresnel * _SpecularColor.rgb * _SpecularIntensity;
                half3 diffuse = albedo * (1.0 - _Metallic);
                
                half3 finalColor = (diffuse + specular) * mainLight.color * NdotL;
                finalColor += albedo * 0.1; // 环境光
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}