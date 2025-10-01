Shader "Custom/CharacterShader"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _FinalTex ("Final Texture", 2D) = "black" {}
        _PreviewTex ("Preview Texture", 2D) = "black" {}

        _FeedbackTex("Feedback Texture", 2D) = "black" {}
        _ShowFeedback("Show Feedback (0: false, 1: true)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            sampler2D _MainTex;
            sampler2D _FinalTex;
            sampler2D _PreviewTex;
            sampler2D _FeedbackTex;
            float _ShowFeedback;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                fixed4 baseColor = tex2D(_MainTex, i.uv);
                fixed4 previewColor = tex2D(_PreviewTex, i.uv);
                fixed4 finalColor = tex2D(_FinalTex, i.uv);
                fixed4 feedbackColor = tex2D(_FeedbackTex, i.uv);

                fixed4 output = baseColor;

                if(_ShowFeedback > 0.5)
                {
                    fixed4 whiteFinal;
                    whiteFinal.rgb = 1.0;
                    whiteFinal.a = 0.3 * finalColor.a;

                    output.rgb = lerp(output.rgb, feedbackColor.rgb, feedbackColor.a);
                    output.rgb = lerp(output.rgb, whiteFinal.rgb, whiteFinal.a);
                }
                else
                {
                    output.rgb = lerp(output.rgb, finalColor.rgb, finalColor.a);
                    output.rgb = lerp(output.rgb, previewColor.rgb, previewColor.a);
                }

                return output;
            }
            ENDHLSL
        }
    }
}
