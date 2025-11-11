Shader "PostEffect/VolumeLighting"
{
    Properties
    {

        _MainTex ("_MainTex", 2D) = "black" {}
        _MaxStep ("_MaxStep", float) = 200
        _MaxDistance ("_MaxDistance", float) = 1000
        _LightIntensity ("_LightIntensity", float) = 0.01
        _StepSize ("_StepSize", float) = 0.1
        _LightColor ("_LightColor", Color) = (1, 1, 1, 1)
        _ShadowPower ("_ShadowPower", float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // CBUFFER_START(UnityPerMaterial)

        // CBUFFER_END
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            // 设置关键字
            // #pragma shader_feature_local _AdditionalLights

            
            // 接收阴影所需关键字
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            // #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            // #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            // #pragma shader_feature _ _ALPHATEST_ON

            #pragma vertex vert
            #pragma fragment frag

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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                float2 uv : TEXCOORD4;
            };

            TEXTURE2D_X_FLOAT(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float _MaxDistance;
            float _MaxStep;
            float _StepSize;
            float _LightIntensity;
            half4 _LightColor;
            float _ShadowPower;

            float4 GetTheWorldPos(float2 ScreenUV, float Depth)
            {
                //获取像素的屏幕空间位置
                float3 ScreenPos = float3(ScreenUV, Depth);
                float4 normalScreenPos = float4(ScreenPos * 2.0 - 1.0, 1.0);
                //得到ndc空间下像素位置
                float4 ndcPos = mul(unity_CameraInvProjection, normalScreenPos);
                ndcPos = float4(ndcPos.xyz / ndcPos.w, 1.0);
                //获取世界空间下像素位置
                float4 sencePos = mul(unity_CameraToWorld, ndcPos * float4(1, 1, - 1, 1));
                sencePos = float4(sencePos.xyz, 1.0);
                return sencePos;
            }

            
            float GetShadow(float3 posWorld)
            {
                float4 shadowCoord = TransformWorldToShadowCoord(posWorld);
                float shadow = MainLightRealtimeShadow(shadowCoord);
// #if defined(_ADDITIONAL_LIGHTS)
//                 // 获取额外光源处理
//                 int pixelLightCount = GetAdditionalLightsCount();
//                 for(int index = 0; index < pixelLightCount; index++)
//                 {
//                     Light light = GetAdditionalLight(index, posWorld, half4(1,1,1,1));
//                     // 计算光照颜色和衰减
//                     float attenuatedLightShadow = (light.distanceAttenuation * light.shadowAttenuation);
//                     // float addtionalShadow = AdditionalLightRealtimeShadow(index, posWorld, light.direction);
//                     shadow +=  attenuatedLightShadow;
//                 }
// #endif
                return shadow;
            }


            Varyings vert(Attributes v)
            {
                Varyings o;
                // 获取不同空间下坐标信息
                VertexPositionInputs positionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = positionInputs.positionCS;
                o.uv = v.uv;
                return o;
            }


            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                depth = 1 - depth;
                float3 ro = _WorldSpaceCameraPos.xyz;
                float3 worldPos = GetTheWorldPos(uv, depth).xyz;
                float3 rd = normalize(worldPos - ro);
                float3 currentPos = ro;
                float maxLightValue = min(_MaxStep, _MaxDistance / _StepSize);
                float m_length = min(length(worldPos - ro), _MaxDistance);
                float delta = _StepSize;
                float3 addLength = delta * rd;
                float totalInt = 0;
                float d = 0;
                for(int j = 0; j < _MaxStep; j ++)
                {
                    d += delta;
                    if(d > m_length) break;
                    currentPos += addLength;
                    float shadow = GetShadow(currentPos);
                    totalInt += _LightIntensity * shadow;
                }
                half3 lightCol = pow(totalInt / maxLightValue, _ShadowPower) * _LightColor.rgb * _MainLightColor.rgb;
                half3 oCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
                half3 dCol = lightCol + oCol;
                return real4(dCol, 1);

            }

            ENDHLSL

        }
        //下面计算阴影的Pass可以直接通过使用URP内置的Pass计算
        //UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        // or
        // 计算阴影的Pass
        // Pass
        // {
        //     Name "ShadowCaster"
        //     Tags { "LightMode" = "ShadowCaster" }
        //     Cull Off
        //     ZWrite On
        //     ZTest LEqual

        //     HLSLPROGRAM

        //     // 设置关键字
        //     #pragma shader_feature _ALPHATEST_ON

        //     #pragma vertex vert
        //     #pragma fragment frag

        //     #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
        //     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        //     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

        //     float3 _LightDirection;

        //     struct Attributes
        //     {
        //         float4 positionOS : POSITION;
        //         float3 normalOS : NORMAL;
        //     };

        //     struct Varyings
        //     {
        //         float4 positionCS : SV_POSITION;
        //     };

        //     // 获取裁剪空间下的阴影坐标
        //     float4 GetShadowPositionHClips(Attributes input)
        //     {
        //         float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
        //         float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
        //         // 获取阴影专用裁剪空间下的坐标
        //         float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

        //         // 判断是否是在DirectX平台翻转过坐标
        //         #if UNITY_REVERSED_Z
        //         positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
        //         #else
        //         positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
        //         #endif

        //         return positionCS;
        //     }

        //     Varyings vert(Attributes input)
        //     {
        //         Varyings output;
        //         output.positionCS = GetShadowPositionHClips(input);
        //         return output;
        //     }


        //     half4 frag(Varyings input) : SV_TARGET
        //     {
        //         return half4(0, 0, 0, 1);
        //     }

        //     ENDHLSL

        // }
    }
    FallBack "Packages/com.unity.render-pipelines.universal/FallbackError"
}