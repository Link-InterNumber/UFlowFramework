Shader "Custom/BaseUrpLit"
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
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
                float3 lightDirectionWS = _LightDirection;
#endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

#if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#else
                positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif

                return ApplyShadowClamping(positionCS);
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            // 雾
            #pragma multi_compile_fog
            // 额外光源
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
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
                half fogFactor : TEXCOORD4;
                half3 vertexLighting : TEXCOORD5;
                // float4 shadowCoord : TEXCOORD5;	// shadow receive
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

                float3 worldNormal = TransformObjectToWorldNormal(v.normal);
                float3 worldTangent = normalize(TransformObjectToWorldDir(v.tangent.xyz));
                float tangentSign = v.tangent.w * GetOddNegativeScale();
                float3 worldBinormal = normalize(cross(worldNormal, worldTangent) * tangentSign);

                o.TtoW0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
                o.TtoW1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
                o.TtoW2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);

                o.vertexLighting = VertexLighting(worldPos, worldNormal);
                o.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 worldPos = float3(i.TtoW0.w, i.TtoW1.w, i.TtoW2.w);
                float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);

                // 法线
                float3 bump = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv.zw));//   (tex2D(_BumpMap, i.uv.zw));
                bump.xy *= _BumpScale; // Scale the normal map
                bump.z = sqrt(1.0 - saturate(dot(bump.xy, bump.xy))); // Ensure the normal is normalized
                bump = normalize(half3(dot(i.TtoW0.xyz, bump), dot(i.TtoW1.xyz, bump), dot(i.TtoW2.xyz, bump)));

                // 菲涅尔效果
                float fresnelBase = 1.0 - saturate(dot(bump, viewDir));
                float fresnel = fresnelBase * fresnelBase * fresnelBase * fresnelBase * fresnelBase * _Fresnel;

                // 纹理采样
                float3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy) * _Color.rgb; // Sample the texture and apply color
                
                // 主光源阴影
                float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
                Light mainLight = GetMainLight(shadowCoord);
                float3 lightDir = mainLight.direction;

                // 光照
                float3 ambient = SampleSH(bump) * albedo;
                float3 diffuse = mainLight.color * albedo * _Diffuse.rgb * saturate(dot(bump, lightDir)) * mainLight.shadowAttenuation + fresnel;
                // 反射方向
                float3 halfDir = normalize(lightDir + viewDir);
                float3 specular = mainLight.color * _Specular.rgb * pow(saturate(dot(bump, halfDir)), _Gloss) * mainLight.shadowAttenuation;

#if defined(_ADDITIONAL_LIGHTS_VERTEX)
                diffuse += i.vertexLighting * albedo * _Diffuse.rgb;
#endif

#if defined(_ADDITIONAL_LIGHTS)
                // 获取额外光源处理
                int pixelLightCount = GetAdditionalLightsCount();
                for(int index = 0; index < pixelLightCount; index++)
                {
                    Light light = GetAdditionalLight(index, worldPos, half4(1,1,1,1));
                    // 计算光照颜色和衰减
                    float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                    // 获取额外光源的阴影
                    // float addtionalShadow = AdditionalLightRealtimeShadow(i.shadowCoord, index);
                    // 计算漫反射和镜面反射
                    diffuse += LightingLambert(attenuatedLightColor, light.direction, bump); //* addtionalShadow;
			        specular += LightingSpecular(attenuatedLightColor, light.direction, bump, viewDir, _Specular, _Gloss); //* addtionalShadow;
                }
#endif
                float4 col = float4(ambient + diffuse + specular, 1.0);
                // 雾
                col.rgb = MixFog(col.rgb, i.fogFactor);
                return col;
            }

            ENDHLSL
        }
    }

    FallBack "Diffuse"
}