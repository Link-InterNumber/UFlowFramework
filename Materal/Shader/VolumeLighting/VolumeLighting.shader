Shader "PostEffect/VolumeLighting"
{
    Properties
    {
        _MainTex ("_MainTex", 2D) = "black" {}
        _MaxStep ("_MaxStep", float) = 200
        _MaxDistance ("_MaxDistance", float) = 200
        _LightIntensity ("_LightIntensity", Range(0, 10)) = 0.5
        _StepSize ("_StepSize", Range(0, 2)) = 0.1
        _LightColor ("_LightColor", Color) = (1, 1, 1, 1)
        _ShadowPower ("_ShadowPower", Range(0, 3)) = 1.0
        _MaxHeight ("_MaxHeight", float) = 1000.0
        _FadeDistance ("_FadeDistance", float) = 100.0

        [Toggle(_Noise)]_Noise ("_Noise", Float) = 1.0
        _NoiseScale ("_NoiseScale", Range(0, 3)) = 0.5
        _NoiseSpeed ("_NoiseSpeed", Range(0, 5)) = 1.0
        _NoiseIntensity ("_NoiseIntensity", Range(0, 3)) = 2.0
        _NoiseDirection ("_NoiseDirection (xyz)", Vector) = (1, 0, 0, 0)
        _NoiseOctaves ("_NoiseOctaves", Range(0, 5)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        // HLSLINCLUDE

        // // CBUFFER_START(UnityPerMaterial)

        // // CBUFFER_END
        // ENDHLSL

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            // 设置关键字
            // #pragma shader_feature_local _AdditionalLights

            
            // 接收阴影所需关键字
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            // // #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            // // #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma shader_feature_local _Noise
            // #pragma shader_feature _ _ALPHATEST_ON

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"


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

            // Noise params
            float _NoiseScale;
            float _NoiseSpeed;
            float _NoiseIntensity;
            float3 _NoiseDirection;
            int _NoiseOctaves;
            float _MaxHeight;
            float _FadeDistance;

            // --- Procedural noise (value noise + FBM) ---
            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);

                float n000 = Hash31(i + float3(0,0,0));
                float n100 = Hash31(i + float3(1,0,0));
                float n010 = Hash31(i + float3(0,1,0));
                float n110 = Hash31(i + float3(1,1,0));
                float n001 = Hash31(i + float3(0,0,1));
                float n101 = Hash31(i + float3(1,0,1));
                float n011 = Hash31(i + float3(0,1,1));
                float n111 = Hash31(i + float3(1,1,1));

                float nx00 = lerp(n000, n100, u.x);
                float nx10 = lerp(n010, n110, u.x);
                float nx01 = lerp(n001, n101, u.x);
                float nx11 = lerp(n011, n111, u.x);

                float nxy0 = lerp(nx00, nx10, u.y);
                float nxy1 = lerp(nx01, nx11, u.y);

                return lerp(nxy0, nxy1, u.z); // 0..1
            }

            float FBM3D(float3 p, int octaves)
            {
                float amp = 0.5;
                float sum = 0.0;
                for (int k = 0; k < octaves; k++)
                {
                    sum += ValueNoise3D(p) * amp;
                    p *= 2.0;
                    amp *= 0.5;
                }
                return sum; // ~0..1
            }

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
                    // 高度衰减
                    float heightFade = saturate((_MaxHeight - currentPos.y) / _FadeDistance);
                    if (heightFade <= 0) continue;

                    // 世界空间的动画噪声（固定在空间中，不随相机抖动）
                    #ifdef _Noise
                    float t = _TimeParameters.x * _NoiseSpeed;
                    float3 dir = _NoiseDirection / max(length(_NoiseDirection), 1e-5);
                    float3 noisePos = currentPos * _NoiseScale + dir * t;
                    float n = FBM3D(noisePos, _NoiseOctaves);
                    float density = saturate(pow(n , 3 - _NoiseIntensity));
                    #else
                    float density = 1.0;
                    #endif

                    float shadow = GetShadow(currentPos);
                    totalInt += _LightIntensity * shadow * density * heightFade;
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