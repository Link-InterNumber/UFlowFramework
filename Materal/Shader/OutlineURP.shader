Shader "Custom/URP_InvertedHull_Outline"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Diffuse ("Diffuse", Color) = (1, 1, 1, 1)
        [Toggle(_USE_SMOOTH_NORMAL)] _UseSmoothNormal("Use SmoothNormal Map", Float) = 0
        _SmoothNormal ("SmoothNormal", 2D) = "bump" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Bump Scale", Float) = 1.0
        _Specular ("Specular", Color) = (1, 1, 1, 1)
        _Gloss ("Gloss", Range(8.0, 256)) = 20
        _Fresnel ("Fresnel", Range(0.0, 1.0)) = 0.5

        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Width (object units)", Range(0.0, 50.0)) = 1.0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

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
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            // 雾
            #pragma multi_compile_fog
            // 额外光源
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            // #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS _ADDITIONAL_LIGHT_CALCULATE_SHADOWS
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
                half fogFactor : TEXCOORD4;
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
            float _OutlineWidth;
            float4 _OutlineColor;
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

                o.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
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

                // 纹理采样
                float3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color.rgb; // Sample the texture and apply color
                
                // 主光源阴影
                float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
                float shadow = MainLightRealtimeShadow(shadowCoord);
                
                // 菲涅尔效果
                float fresnel = pow(1.0 - saturate(dot(bump, viewDir)), 5.0) * _Fresnel;

                // 光照
                float3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w).xyz * albedo;
                float3 diffuse = _MainLightColor.rgb * albedo * _Diffuse.rgb * max(0, dot(bump, lightDir)) * shadow + fresnel;
                // 反射方向
                float3 halfDir = normalize(lightDir + viewDir);
                float3 specular = _MainLightColor.rgb * _Specular.rgb * pow(max(0, dot(bump, halfDir)), _Gloss) * shadow;

#if defined(_ADDITIONAL_LIGHTS)
                // 获取额外光源处理
                int pixelLightCount = GetAdditionalLightsCount();
                for(int index = 0; index < pixelLightCount; index++)
                {
                    Light light = GetAdditionalLight(index, worldPos);
                    // 计算光照颜色和衰减
                    float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                    // 获取额外光源的阴影
                    float addtionalShadow = AdditionalLightRealtimeShadow(index, worldPos);
                    // 计算漫反射和镜面反射
                    diffuse += LightingLambert(attenuatedLightColor, light.direction, bump) * addtionalShadow;
			        specular += LightingSpecular(attenuatedLightColor, light.direction, bump, viewDir, _Specular, _Gloss) * addtionalShadow;
                }
#endif
                float4 col = float4(ambient + diffuse + specular, 1.0);
                // 雾
                col.rgb = MixFog(col.rgb, i.fogFactor);
                return col;
            }

            ENDHLSL
        }

        // -- -- Outline pass : draw backfaces expanded along normal -- --
        Pass
        {
            Name "OUTLINE"
            // Tags { "LightMode" = "Always" }
            Cull Front            // 反向剔除，显示外挂面
            // ZWrite Off            // 不写深度
            // ZTest LEqual
            Blend Off

            HLSLPROGRAM
            #pragma vertex VertOutline
            #pragma fragment FragOutline
            #pragma multi_compile_fog
            #pragma shader_feature_local _USE_SMOOTH_NORMAL

            // gpu instance
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            TEXTURE2D(_SmoothNormal); 
            SAMPLER(sampler_SmoothNormal);
            // float4 _SmoothNormal_ST;

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _MainTex_ST;
            float4 _BumpMap_ST;
            float _BumpScale;
            float4 _Diffuse;
            float4 _Specular;
            float _Gloss;
            float _Fresnel;
            float _OutlineWidth;
            float4 _OutlineColor;
            CBUFFER_END

            struct appdataOutline
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 vcolor : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                half fogFactor: TEXCOORD1;
                float4 vcolor : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f VertOutline(appdataOutline v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                // 不随摄像机远近变化的描边宽度（屏幕空间像素）
                // float3 posOffset = v.vertex.xyz + v.normal * _OutlineWidth;
                // o.pos = TransformObjectToHClip(float4(posOffset, 1.0));

                float3 smoothNormal = v.normal;
                #ifdef _USE_SMOOTH_NORMAL
                {
                    // 使用平滑法线贴图
                    float2 uv = v.texcoord.xy; // * _SmoothNormal.xy + _SmoothNormal.zw;
                    smoothNormal = UnpackNormal(SAMPLE_TEXTURE2D_LOD(_SmoothNormal, sampler_SmoothNormal, uv, 0));
                }
                #endif

                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                
                // 恒定宽度
                // Transform vertex to clip space
                float4 clipPos = vertexInput.positionCS;
                // 把法线变换到裁剪空间 (w=0)，再转换到 NDC 方向（除以 clip.w）
                float2 clipNormalXY = mul(UNITY_MATRIX_MVP, float4(smoothNormal, 0.0)).xy;
                float2 ndcDir = clipNormalXY / clipPos.w;

                // 安全归一化
                float len = max(length(ndcDir), 1e-6);
                ndcDir /= len;

                // 每轴的像素->NDC 缩放（一个像素在 NDC 中的大小）
                float2 pixelToNDC = float2(2.0 / _ScreenParams.x, 2.0 / _ScreenParams.y);

                // 在 NDC 空间按像素偏移，然后乘回 clip.w 应用到 clipPos.xy
                float2 offsetNDC = ndcDir * (_OutlineWidth * pixelToNDC);
                clipPos.xy += offsetNDC * clipPos.w;

                o.pos = clipPos;
                o.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                o.vcolor = v.vcolor;
                return o;
            }

            float4 FragOutline(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 col = MixFog(_OutlineColor, i.fogFactor) * i.vcolor.rgb;
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Diffuse"
}