Shader "Custom/URP_IntersectionHighlight"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _IntersectionColor ("Intersection Color", Color) = (1,0,0,1)
        _IntersectionPower ("Intersection Power", Range(0.1, 10)) = 2.0
        _IntersectionDistance ("Intersection Distance", Range(0, 5)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _IntersectionColor;
                float _IntersectionPower;
                float _IntersectionDistance;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = positionInputs.positionCS;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.worldPos = positionInputs.positionWS;
                output.worldNormal = normalInputs.normalWS;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 获取屏幕空间UV
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                
                // 采样深度纹理
                float sceneDepth = SampleSceneDepth(screenUV);
                float sceneDepthEye = LinearEyeDepth(sceneDepth, _ZBufferParams);
                
                // 计算当前片元的眼空间深度
                float currentDepthEye = input.screenPos.w;
                
                // 计算深度差异
                float depthDifference = abs(sceneDepthEye - currentDepthEye);
                
                // 计算相交强度
                float intersectionMask = 1.0 - saturate(depthDifference / _IntersectionDistance);
                intersectionMask = pow(intersectionMask, _IntersectionPower);
                
                // 混合颜色
                float4 finalColor = lerp(_BaseColor, _IntersectionColor, intersectionMask);
                finalColor.a = _BaseColor.a + intersectionMask * _IntersectionColor.a;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}