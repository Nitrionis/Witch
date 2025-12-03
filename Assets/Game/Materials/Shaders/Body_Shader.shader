Shader "Unlit/Body_Shader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        _ShadeColor ("Shade Color", Color) = (0,0,0,1)
        _GlareColor ("Glare Color", Color) = (1,1,1,1)
        _Threshold ("_Threshold", Float) = 0.5
        _Toon ("_Toon", Range(0.0, 1.0)) = 0.5
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normalWS : NORMAL;
                float3 normalVS : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _ShadeColor;
            fixed4 _GlareColor;
            float _Threshold;
            float _Toon;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normalWS = UnityObjectToWorldNormal(v.normalOS);
                o.normalVS = mul((float3x3)UNITY_MATRIX_V, o.normalWS);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir = -UNITY_MATRIX_V[2].xyz;
                // sample the texture
                fixed4 color = _Color * tex2D(_MainTex, i.uv);
                float lf = max(0, dot(i.normalWS, _WorldSpaceLightPos0.xyz));
                float toon = lerp(0.0, lerp(0.5, 1.0, step((1.0 + _Threshold) * 0.5, lf)), step(_Threshold, lf));
                lf = lerp(toon, lf, _Toon);
                color.rgb = lerp(_ShadeColor.rgb, _Color.rgb, lf);

                lf = max(0.0, 1.0 - i.normalVS.z);
                lf = max(lf - 0.0, 0.0);
                lf = 1.0 * lf / (1.0 + 1.0 * lf);
                color.rgb = lerp(color.rgb, _GlareColor, clamp(lf, 0.0, 1.0));
                return color;
            }
            ENDCG
        }
    }
}
