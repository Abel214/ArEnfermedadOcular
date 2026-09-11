Shader "Custom/ChromaKeyWhite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Threshold ("Threshold", Range(0, 1)) = 0.9
        _Softness ("Softness", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Threshold;
            float _Softness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
    
                // Detectar fondo gris/blanco
                float r = col.r;
                float g = col.g;
                float b = col.b;

                float maxVal = max(r, max(g, b));
                float minVal = min(r, min(g, b));
                float diff = maxVal - minVal;
                float brightness = (r + g + b) / 3.0;

                float isGray = step(diff, _Softness);
                float inRange = smoothstep(
                    _Threshold - 0.15, 
                    _Threshold + 0.15, 
                    brightness);
    
                float alpha = 1.0 - (isGray * inRange);
                col.a = alpha;
                return col;
            }
            ENDCG
        }
    }
}
