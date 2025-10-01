Shader "Custom/BrushShader"
{
    Properties
    {
        _BrushSize ("Brush Size", Float) = 0.05
        _BrushUV ("Brush UV", Vector) = (0,0,0,0)
        _UseHardBrush ("Use Hard Brush", Float) = 0
        _BrushColor ("Brush Color", Color) = (0, 0.9529, 1, 1) 
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

            float _BrushSize;
            float4 _BrushUV;
            float _UseHardBrush;
            fixed4 _BrushColor; 

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 delta = uv - _BrushUV.xy;
                float dist = length(delta);
            
                float alpha;
                if (_UseHardBrush > 0.5)
                    alpha = dist < _BrushSize ? 1.0 : 0.0;
                else
                    alpha = smoothstep(_BrushSize, _BrushSize * 0.8, dist);
            
                fixed4 color = _BrushColor;
                color.a *= alpha;
                return color;
            }
            ENDHLSL
        }
    }
}
