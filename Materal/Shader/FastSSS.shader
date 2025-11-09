Shader "Custom/FastSSS"
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
        _Gloss ("Gloss", Range(8.0, 256)) = 20
        _Fresnel ("Fresnel", Range(0.0, 1.0)) = 0.5

        // SSS properties
        [Space]
        [Toggle(_USE_THICKNESS_MAP)]_UseThicknessMap ("Use Thickness Map", Float) = 1.0
        _ThicknessMap ("Thickness Map", 2D) = "white" {}
        _SSSColor ("Subsurface Color", Color) = (1, 0.3, 0.2, 1)
        _SSSPower ("Subsurface Power", Range(0.1, 8.0)) = 1.0
        _SSSScale ("Subsurface Scale", Range(0.1, 10.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "UniversalMaterialType" = "Lit"
        }
        LOD 300
        // -- -- Base pass : simple textured (unlit) model -- --
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
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma shader_feature_local _USE_THICKNESS_MAP
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
                float3 fogFactor : TEXCOORD4;
                // float4 shadowCoord : TEXCOORD5;	// shadow receive
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            TEXTURE2D(_ThicknessMap);SAMPLER(sampler_ThicknessMap);

            half4 _SSSColor;
            half _SSSPower;
            half _SSSScale;
            
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _MainTex_ST;
            float4 _BumpMap_ST;
            float _BumpScale;
            float4 _Diffuse;
            float4 _Specular;
            float _Gloss;
            float _Fresnel;
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
                o.fogFactor.yz = v.texcoord.xy; // thickness map uv
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 worldPos = float3(i.TtoW0.w, i.TtoW1.w, i.TtoW2.w);
                float3 lightDir = normalize(GetMainLight().direction);
                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);

                // 法线
                float3 bump = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv.zw));//   (tex2D(_BumpMap, i.uv.zw));
                bump.xy *= _BumpScale; // Scale the normal map
                bump.z = sqrt(1.0 - saturate(dot(bump.xy, bump.xy))); // Ensure the normal is normalized
                bump = normalize(half3(dot(i.TtoW0.xyz, bump), dot(i.TtoW1.xyz, bump), dot(i.TtoW2.xyz, bump)));

                // 菲涅尔效果
                float fresnel = pow(1.0 - saturate(dot(bump, viewDir)), 5.0) * _Fresnel;

                // 纹理采样
                float3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color.rgb; // Sample the texture and apply color
                
                // 主光源阴影
                float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
                float shadow = MainLightRealtimeShadow(shadowCoord);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(worldPos));
                
                // 光照
                float3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w).xyz * albedo;
                float3 diffuse = mainLight.color * albedo * _Diffuse.rgb * max(0, dot(bump, lightDir)) * shadow + fresnel;
                // 反射方向
                float3 halfDir = normalize(lightDir + viewDir);
                float3 specular = mainLight.color * _Specular.rgb * pow(max(0, dot(bump, halfDir)), _Gloss) * shadow;

                // 次表面散射
                half thickness = 1;
                #ifdef _USE_THICKNESS_MAP
                thickness = 1 - SAMPLE_TEXTURE2D(_ThicknessMap, sampler_ThicknessMap, i.fogFactor.yz).r;
                #endif
                
                float3 h = normalize(bump - mainLight.direction);
                half backLight = saturate(dot(viewDir,  h));
                half sss = pow(backLight * thickness, _SSSPower) * _SSSScale ;
                half3 subsurface = _SSSColor.rgb * mainLight.color * sss * albedo;

#ifdef _ADDITIONAL_LIGHTS
                // 获取额外光源处理
                int pixelLightCount = GetAdditionalLightsCount();
                for(int index = 0; index < pixelLightCount; index++)
                {
                    Light light = GetAdditionalLight(index, worldPos, half4(1,1,1,1));
                    // 计算光照颜色和衰减
                    float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                    // 获取额外光源的阴影
                    // float addtionalShadow = AdditionalLightRealtimeShadow(light.shadowCoord, index);
                    // 计算漫反射和镜面反射
                    diffuse += LightingLambert(attenuatedLightColor, light.direction, bump); //* addtionalShadow;
			        specular += LightingSpecular(attenuatedLightColor, light.direction, bump, viewDir, _Specular, _Gloss); //* addtionalShadow;

                    // Additional light SSS
                    float3 addtionalH = normalize(bump - light.direction);
                    half addBackLight = saturate(dot(viewDir, addtionalH));
                    half addSss = pow(addBackLight * thickness, _SSSPower) * _SSSScale;
                    subsurface += _SSSColor.rgb * light.color * light.distanceAttenuation * addSss * albedo;
                }
#endif
                float4 col = float4(ambient + diffuse + specular + subsurface, 1.0);
                // 雾
                col.rgb = MixFog(col.rgb, i.fogFactor.x);
                return col;
            }

            ENDHLSL
        }
    }

    FallBack "Diffuse"
}