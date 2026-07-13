Shader "Custom/CartoonUrpLitV2"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [Space]
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Bump Scale", Float) = 1.0
        [Space]
        _Diffuse ("Diffuse", Color) = (1, 1, 1, 1)
        _Specular ("Specular", Color) = (1, 1, 1, 1)
        _Gloss ("Gloss", Range(2.0, 256)) = 20
        _Fresnel ("Fresnel", Range(0.0, 1.0)) = 0.5

        _LightBoundary ("Light Boundary", Range(-0.5, 1.0)) = 0.0
        [Toggle(_SoftEdgeLight)]_SoftEdgeLight ("SoftEdgeLight", Float) = 1.0
        _EdgeLightSize ("Edge Light Size", Range(0.0, 1.0)) = 0.5
        _EdgeLightScale ("Edge Light Scale", Range(0.0, 10.0)) = 1.0
        _DiffuseScale ("Diffuse Scale", Range(0.0, 5.0)) = 1.0
        _DarkIntensity ("DarkIntensity", Range(0.0, 1.0)) = 1.0
        _LightPower ("Light Power", Range(0.01, 1)) = 0.5
        _DarkPower ("Dark Power", Range(1.0, 5.0)) = 1.0
        _SubShadow ("Sub Shadow", Range(0.0, 5.0)) = 1.0

        [Toggle(_ReceiveShadows)]_ReceiveShadows ("ReceiveShadows", Float) = 1.0
        [Toggle(_CalSubShadow)]_CalSubShadow ("CalSubShadow", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "UniversalMaterialType" = "Lit" }
        LOD 300
        // -- -- Base pass : simple textured (unlit) model -- --

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "BaseColor"
            Tags
            {
                "RenderType" = "Opaque"
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
            #pragma vertex vert
            #pragma fragment frag
            // 阴影
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            // 雾
            #pragma multi_compile_fog
            // 额外光源
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma shader_feature_local _ReceiveShadows
            #pragma shader_feature_local _CalSubShadow
            #pragma shader_feature_local _SoftEdgeLight
            // #pragma multi_compile _ _ADDITIONAL_LIGHT_CALCULATE_SHADOWS
            // GPU Instance
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct a2v
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD0;
                float4 TtoW0 : TEXCOORD1; // Tangent to World space rotatio
                float4 TtoW1 : TEXCOORD2; // Tangent to World space
                float4 TtoW2 : TEXCOORD3; // Tangent to World space
                float2 fogFactor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _MainTex_ST;
            float4 _BumpMap_ST;
            float _BumpScale;
            float4 _Diffuse;
            float4 _Specular;
            float _Gloss;
            float _Fresnel;
            float _LightBoundary;
            float _EdgeLightSize;
            float _EdgeLightScale;
            float _DiffuseScale;
            float _DarkIntensity;
            float _LightPower;
            float _DarkPower;
            float _SubShadow;
            CBUFFER_END

            v2f vert(a2v v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);

                o.pos = vertexInput.positionCS; // UnityObjectToClipPos(v.vertex); // Transform vertex position to clip space
                o.uv.xy = v.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw; // Adjust UVs based on texture scale and offset
                o.uv.zw = v.texcoord.xy * _BumpMap_ST.xy + _BumpMap_ST.zw; // Adjust UVs for normal map

                float3 worldPos = vertexInput.positionWS.xyz; // mul(unity_ObjectToWorld, v.vertex).xyz;

                float3 worldNormal = normalize(mul(unity_ObjectToWorld, v.normal));
                float3 worldTangent = normalize(mul(unity_ObjectToWorld, v.tangent.xyz));
                float3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;

                o.TtoW0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
                o.TtoW1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
                o.TtoW2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);

                o.fogFactor.x = ComputeFogFactor(vertexInput.positionCS.z);
#ifdef _CalSubShadow
                // 1. 获取世界空间的主光源方向
                float3 worldLightDir = GetMainLight().direction;
                // 2. 将光源方向从世界空间转换到模型空间
                float3 modelLightDir = TransformWorldToObjectDir(worldLightDir);
                o.fogFactor.y = dot(v.vertex.xyz, modelLightDir);
#endif
                return o;
            }

            float3 getEdgeLight(float dotDumpLight, float3 viewDir, float3 lightDir, float3 lightColor, float fresnelP5, float shadow, float edgeSize)
            {
                // 背光系数
                float backLight = max(0, dot(-viewDir, lightDir));
                // float3 edgeLight_al = _EdgeLightScale * light.distanceAttenuation * smoothstep(edgeSize, edgeSize, fresnelP5 * (dotProduct_al + backLight_al));
#ifdef _SoftEdgeLight
                float3 edgeLightBase = _EdgeLightScale * fresnelP5 * fresnelP5 * saturate(dotDumpLight + backLight) * edgeSize;
#else
                edgeSize = 1 - edgeSize;
                float3 edgeLightBase = _EdgeLightScale * fresnelP5 * smoothstep(edgeSize, edgeSize, fresnelP5 * (dotDumpLight + backLight));
#endif
                float3 edgeLight = shadow * edgeLightBase * lightColor;
                return edgeLight;
            }

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 worldPos = float3(i.TtoW0.w, i.TtoW1.w, i.TtoW2.w);
                float3 lightDir = GetMainLight().direction;
                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);

                // 法线
                float3 bump = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv.zw));//   (tex2D(_BumpMap, i.uv.zw));
                bump.xy *= _BumpScale; // Scale the normal map
                bump.z = sqrt(1.0 - saturate(dot(bump.xy, bump.xy))); // Ensure the normal is normalized
                bump = normalize(half3(dot(i.TtoW0.xyz, bump), dot(i.TtoW1.xyz, bump), dot(i.TtoW2.xyz, bump)));

                // 纹理采样
                float3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color.rgb; // Sample the texture and apply color

                // 菲涅尔效果
                float fresnelP1 = 1.0 - saturate(dot(bump, viewDir));
                float fresnelP5 = pow(fresnelP1, 5.0);
                float fresnel = fresnelP5 * _Fresnel;

                // 环境光
                float3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w).xyz * albedo;
                // 初始化光照结果
                float3 diffuse = half3(0,0,0);
                float3 specular = half3(0,0,0);
                float3 edgeLight = half3(0,0,0);
                float diffuseShadow = 0;
                
#ifdef _CalSubShadow
                float subShadow = saturate(i.fogFactor.y * _SubShadow * 0.5 + 0.5);
#else
                float subShadow = 1.0;
#endif

#ifdef _ReceiveShadows
                // 主光源阴影
                float3 offsetPos = worldPos + bump * 0.05;
                float4 shadowCoord = TransformWorldToShadowCoord(offsetPos);
                float shadow = MainLightRealtimeShadow(shadowCoord) * subShadow;
#else
                float shadow = subShadow;
#endif

#if defined(_ADDITIONAL_LIGHTS) || defined(USE_CLUSTER_LIGHT_LOOP) || defined(USE_FORWARD_PLUS)
                // 获取额外光源处理
                // Unity 6 / Unity 2022 的 Forward+ 光源循环宏可能会读取 inputData.normalizedScreenSpaceUV 和 inputData.positionWS。
                // 当前 Shader 没有使用 URP Lit 的完整 InputData，这里提供宏需要的最小字段。
                struct CartoonInputData
                {
                    float3 positionWS;
                    float2 normalizedScreenSpaceUV;
                };

                CartoonInputData inputData;
                inputData.positionWS = worldPos;
                inputData.normalizedScreenSpaceUV = i.pos.xy / GetScaledScreenParams().xy;

                int pixelLightCount = GetAdditionalLightsCount();
                float3 offsetPosAddtionalLight = worldPos + bump * 0.05;
#if defined(LIGHT_LOOP_BEGIN)
LIGHT_LOOP_BEGIN(pixelLightCount)
                    uint index = lightIndex;
#else
                for(int index = 0; index < pixelLightCount; index++)
                {
#endif
                    Light light = GetAdditionalLight(index, offsetPosAddtionalLight, half4(1,1,1,1));
                    float dotProduct_al = dot(bump, light.direction);
                    float distanceAttenuation = min(3, light.distanceAttenuation);
                    diffuseShadow += max(0, dotProduct_al * distanceAttenuation);
                    float shaow_al = (distanceAttenuation * light.shadowAttenuation);
                    shadow += shaow_al;

                    edgeLight += getEdgeLight(dotProduct_al, viewDir, light.direction, light.color * distanceAttenuation, fresnelP5, shaow_al, edgeSize);
                    float3 lightColor = (light.color + albedo) * 0.5;
                    diffuse += smoothstep(-_LightBoundary, 1.0 + _LightBoundary, dotProduct_al * distanceAttenuation) * light.shadowAttenuation * lightColor * _Diffuse.rgb;
                    float3 halfDir_al = normalize(light.direction + viewDir);
                    specular += light.color * _Specular.rgb * pow(max(0, dot(bump, halfDir_al)), _Gloss) * shaow_al;
#if defined(LIGHT_LOOP_END)
LIGHT_LOOP_END
#else
                }
#endif
                shadow = saturate(shadow);
#endif

                // 光照
                float dotProduct = dot(bump, lightDir);
                // 计算边缘光
                edgeLight += getEdgeLight(dotProduct, viewDir, lightDir, _MainLightColor.rgb, fresnelP5, shadow, _EdgeLightSize);
                diffuseShadow = smoothstep(-_LightBoundary, 1.0 + _LightBoundary, dotProduct + diffuseShadow);
                diffuseShadow = saturate(diffuseShadow);

                // 计算漫反射，使用颜色替换原本阴影
                float shadowFinal = shadow * diffuseShadow;
                shadowFinal = max( 1.0 -_DarkIntensity, shadowFinal);
                float3 lightColor = pow(albedo, lerp(_DarkPower, _LightPower, shadowFinal));
                diffuse += _MainLightColor.rgb * _Diffuse.rgb * lightColor + fresnel * albedo;
                diffuse = saturate(diffuse * _DiffuseScale);
                
                // 反射方向
                float3 halfDir = normalize(lightDir + viewDir);
                float3 lerpAlbedo = (lightColor * 0.75 + 0.25);
                specular += _MainLightColor.rgb * _Specular.rgb * pow(max(0, dot(bump, halfDir)), _Gloss) * lerpAlbedo * shadowFinal;

                // 最终颜色合成
                float4 col = float4(ambient + diffuse + specular + edgeLight, 1.0); 
                // float4 col = float4(shadowFinal, shadowFinal, shadowFinal, 1.0);
                // 雾
                col.rgb = MixFog(col.rgb, i.fogFactor.x);
                return col;
            }

            ENDHLSL
        }
    }

    FallBack "Diffuse"
}