Shader "Unlit/Dress_Shader_Back"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _Shade ("Shade Color", Color) = (1,1,1,1)
        _ViewDirOffset ("_ViewDirOffset", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Pass
        {
            Name "Back"
            Cull Front
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normalOS : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half3 normalWS : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _Shade;
            float _ViewDirOffset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normalWS = UnityObjectToWorldNormal(v.normalOS);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float f = max(0.0, dot(i.normalWS, _WorldSpaceLightPos0.xyz));
                float _Threshold = 0.618;
                /*f = lerp(
                    lerp(0.0, 0.25, step(_Threshold * 0.5, f)),
                    lerp(0.5, 1.0, step((1.0 + _Threshold) * 0.5, f)),
                    step(_Threshold, f)
                );*/
                fixed4 color = lerp(_Shade, _Color, f * 1.4);
                return color;
            }
            ENDCG
        }
    }
}
