Shader "Custom/Outline2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width (px)", Range(0,32)) = 2
        _OutlineSoftness ("Softness (px)", Range(0.1,16)) = 2.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "GPU_2D_Outline"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize; // x = 1/width, y = 1/height

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float4 _OutlineColor;
            float _OutlineWidth;    // in pixels
            float _OutlineSoftness; // in pixels
            CBUFFER_END

            struct app
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float4 vcolor : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
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

            // 8-neighbor offsets (N, S, E, W, NE, NW, SE, SW)
            static const float2 OFFS[8] = {
                float2( 1,  0),
                float2(-1,  0),
                float2( 0,  1),
                float2( 0, -1),
                float2( 1,  1),
                float2(-1,  1),
                float2( 1, -1),
                float2(-1, -1)
            };

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 matColor = _Color * i.vcolor;

                // sample texture once
                float4 tex = tex2D(_MainTex, i.uv);

                // mask / alpha used for both sprites and text
                float mask = tex.a;

                // determine if texture RGB carries color (for sprites) or is empty (for font atlases)
                float texPresence = max(max(tex.r, tex.g), tex.b);
                float useTex = step(0.001, texPresence); // 0 or 1, branchless

                // base color:
                // - if texture has real RGB: use tex.rgb * material color
                // - otherwise (font atlas): use material color modulated by mask
                float3 baseRGB = lerp(matColor.rgb * mask, (tex.rgb * matColor.rgb), useTex);
                float baseAlpha = mask * matColor.a;

                // convert outline width and softness from px to UV
                float2 radiusUV = _OutlineWidth * _MainTex_TexelSize.xy;
                float softnessUV = max(_OutlineSoftness * (_MainTex_TexelSize.x + _MainTex_TexelSize.y) * 0.5, 1e-6);

                // sample neighbors to compute max alpha (dilate)
                float maxNeighbor = 0.0;
                // unrolled loop for performance (GPU-friendly, no branches)
                // multiply OFFS by radiusUV per component
                float2 off;

                off = OFFS[0] * radiusUV; maxNeighbor = max(maxNeighbor, tex2D(_MainTex, i.uv + off).a);
                off = OFFS[1] * radiusUV; maxNeighbor = max(maxNeighbor, tex2D(_MainTex, i.uv + off).a);
                off = OFFS[2] * radiusUV; maxNeighbor = max(maxNeighbor, tex2D(_MainTex, i.uv + off).a);
                off = OFFS[3] * radiusUV; maxNeighbor = max(maxNeighbor, tex2D(_MainTex, i.uv + off).a);
                off = OFFS[4] * radiusUV; maxNeighbor = max(maxNeighbor, tex2D(_MainTex, i.uv + off).a);
                off = OFFS[5] * radiusUV; maxNeighbor = max(maxNeighbor, tex2D(_MainTex, i.uv + off).a);
                off = OFFS[6] * radiusUV; maxNeighbor = max(maxNeighbor, tex2D(_MainTex, i.uv + off).a);
                off = OFFS[7] * radiusUV; maxNeighbor = max(maxNeighbor, tex2D(_MainTex, i.uv + off).a);

                // compute outline strength from difference (no branching)
                // larger maxNeighbor - baseAlpha => stronger outline
                float raw = saturate((maxNeighbor - baseAlpha) / max(_OutlineSoftness * 0.01, 1e-6)); // normalized by softness
                // softstep for smoother gradation (still no branching)
                float outlineMask = smoothstep(0.0, 1.0, raw);

                // compose result: draw base where baseAlpha>0, otherwise outline where outlineMask>0.
                // compute final alpha and color in a stable (no-branch) way
                float outAlpha = saturate(baseAlpha + outlineMask * (1.0 - baseAlpha));
                // blend colors so base dominates where opaque, outline fills transparent areas
                float3 outRGB = (baseRGB.rgb * baseAlpha + _OutlineColor.rgb * outlineMask * (1.0 - baseAlpha));
                // normalize to non-premultiplied color
                outRGB = outRGB / max(outAlpha, 1e-6);

                return float4(outRGB, outAlpha);
            }
            ENDHLSL
        }
    }

    FallBack "Unlit/Transparent"
}