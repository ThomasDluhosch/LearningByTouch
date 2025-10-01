Shader "Hidden/BlitAdd"
{
    Properties
    {
        _BaseTex("Base Texture", 2D) = "white" {}
        _BrushTex("Brush Texture", 2D) = "black" {}
        _EraseMode("Erase Mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _BaseTex;
            sampler2D _BrushTex;
            float    _EraseMode;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            v2f vert (appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 baseCol  = tex2D(_BaseTex, i.uv);
                fixed4 brushCol = tex2D(_BrushTex, i.uv);

                fixed shouldPaint = step(0.001, brushCol.a);
                fixed4 painted = fixed4(brushCol.rgb, 1.0);

                if (_EraseMode < 0.5)
                {
                    return lerp(baseCol, painted, shouldPaint);
                }
                else
                {
                    fixed4 erased = fixed4(baseCol.rgb, 0.0);
                    return lerp(baseCol, erased, shouldPaint);
                }
            }
            ENDHLSL
        }
    }
}
