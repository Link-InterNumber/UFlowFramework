Shader "Custom/Gray2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Gray ("Gray", Range(0.0, 1.0)) = 1.0
        _GrayMask ("GrayMask", Color) = (0.299,0.587,0.114,1.0)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "BASE_COLOR"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Gray;
            float4 _GrayMask;
            CBUFFER_END

            struct app
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float4 vcolor : COLOR;
            };

            v2f vert(app v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                
                o.pos = vertexInput.positionCS;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.vcolor = v.color; 
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                // 混合顶点颜色和Tint
                float4 matColor = i.vcolor;

                float4 tex = tex2D(_MainTex, i.uv);

                float mask = tex.a;

                // 判断是否使用贴图，使用分量最大的颜色分量来判断（适配Text组件）
                float texPresence = max(max(tex.r, tex.g), tex.b);
                float useTex = step(0.001, texPresence); // 0 or 1, branchless

                float3 baseRGB = lerp(matColor.rgb * mask, (tex.rgb * matColor.rgb), useTex);
                float baseAlpha = mask * matColor.a;

                float3 outRGB = baseRGB.rgb * baseAlpha;
                outRGB = outRGB / max(baseAlpha, 1e-6);

                // 置灰
                float gray = dot(outRGB, _GrayMask.rgb);
                outRGB = lerp(outRGB, float3(gray, gray, gray), _Gray) * _Color;

                return float4(outRGB, baseAlpha);
            }
            ENDHLSL
        }
    }

    FallBack "Unlit/Transparent"
}