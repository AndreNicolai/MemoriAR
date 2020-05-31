Shader "Unlit/WorldviewTransition"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NextTex ("Base (RGB)", 2D) = "white" {}
        _LerpFactor ("Lerp Factor", Range(0, 1.0)) = 0.3
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
            sampler2D _NextTex;
            float4 _MainTex_ST;
            float _LerpFactor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return lerp(tex2D(_MainTex, i.uv),tex2D(_NextTex, i.uv),_LerpFactor);
                if (i.uv.x >= 0)
                    return tex2D(_MainTex, i.uv);
                else
                    return tex2D(_NextTex, i.uv);
            }
            ENDCG
        }
    }
}
