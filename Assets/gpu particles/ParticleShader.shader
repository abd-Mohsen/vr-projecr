Shader "Unlit/ParticleShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            struct Particle
            {
                float3 position;
                float3 velocity;
            };

            StructuredBuffer<Particle> particles;
            sampler2D _MainTex;

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                Particle p = particles[v.vertexID];
                o.vertex = UnityObjectToClipPos(float4(p.position, 1.0));
                o.uv = float2(0.0, 0.0);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(1.0, 1.0, 1.0, 1.0); // White particles
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
