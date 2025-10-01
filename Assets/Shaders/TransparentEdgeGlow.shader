Shader "Custom/TransparentEdgeGlowURP"
{
    Properties
    {
        _EdgeColor      ("Edge Color",       Color)            = (0, 0.65, 1, 1)
        _EdgeWidth      ("Edge Thickness",   Range(0, 0.1))    = 0.015
        _InteriorAlpha  ("Interior Alpha",   Range(0, 1))      = 0.0
        _HalfSize       ("Half Size (OS)",   Float)            = 0.5   
        _DrawCenterAxes ("Draw Center Axes", Float)            = 1.0   
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline"
               "RenderType"    = "Transparent"
               "Queue"         = "Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off                       
            HLSLPROGRAM

            #pragma multi_compile_instancing
            #pragma multi_compile _ _STEREO_INSTANCING_ON

            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 posOS      : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _EdgeColor;
            float  _EdgeWidth;
            float  _InteriorAlpha;
            float  _HalfSize;
            float  _DrawCenterAxes;

            Varyings vert (Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.posOS      = IN.positionOS;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 p = IN.posOS;

                float dx = _HalfSize - abs(p.x);
                float dy = _HalfSize - abs(p.y);
                float dz = _HalfSize - abs(p.z);

                float ex = step(dx, _EdgeWidth);
                float ey = step(dy, _EdgeWidth);
                float ez = step(dz, _EdgeWidth);

                float outer = saturate(ex*ey + ey*ez + ex*ez);

                float cx = step(abs(p.x), _EdgeWidth);
                float cy = step(abs(p.y), _EdgeWidth);
                float cz = step(abs(p.z), _EdgeWidth);
                float inner = saturate(cx*cy + cy*cz + cx*cz) * _DrawCenterAxes;

                float edgeMask = saturate(outer + inner);

                half  alpha = lerp(_InteriorAlpha, 1.0, edgeMask);
                half3 emit  = _EdgeColor.rgb * edgeMask;

                return half4(emit, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
