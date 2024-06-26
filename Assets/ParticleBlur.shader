Shader "Custom/BlurShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0, 30)) = 15
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
            float _BlurSize;
            fixed4 _TintColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                fixed4 col = tex2D(_MainTex, uv);

                // Apply blur effect
                fixed4 blur = col;
                blur += tex2D(_MainTex, uv + float2(_BlurSize, 0));
                blur += tex2D(_MainTex, uv - float2(_BlurSize, 0));
                blur += tex2D(_MainTex, uv + float2(0, _BlurSize));
                blur += tex2D(_MainTex, uv - float2(0, _BlurSize));
                blur /= 5.0; // average the colors

                // Apply tint color
                blur *= _TintColor;

                return blur;
            }
            ENDCG
        }
    }
}