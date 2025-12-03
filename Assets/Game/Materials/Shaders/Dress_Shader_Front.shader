Shader "Unlit/Dress_Shader_Front"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _Shade ("Shade Color", Color) = (1,1,1,1)
        _GlareColor ("Glare Color", Color) = (1,1,1,1)
        _ViewDirOffset ("_ViewDirOffset", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "Front"
            Cull Back
            
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
                float3 positionWS : TEXCOORD1;
                float3 normalVS : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _Shade;
            float _ViewDirOffset;
            fixed4 _GlareColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // calculate the world space view dir to use for an offset direction
                o.positionWS = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                float3 offsetDir = normalize(o.positionWS - _WorldSpaceCameraPos.xyz);

                // alternatively use the camera's forward vector
                // this offset will actually result in a different screen XY position, but that doesn't matter since we only need the Z
                // float3 offsetDir = -UNITY_MATRIX_V[2].xyz;

                // offset world position by a constant amount
                o.positionWS += offsetDir * _ViewDirOffset;

                // offset clip space position
                float4 newClipPos = UnityWorldToClipPos(o.positionWS);

                // reproject the z from the offset clip space back into the original clip space
                o.vertex.z = newClipPos.z / newClipPos.w * o.vertex.w;

                o.normalWS = UnityObjectToWorldNormal(v.normalOS);
                o.normalVS = mul((float3x3)UNITY_MATRIX_V, o.normalWS);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 ddxPos = ddx(i.positionWS);
                float3 ddyPos = ddy(i.positionWS);

                // Calculate the normal using the cross product of the tangent vectors
                float3 normal = normalize(cross(ddyPos, ddxPos));
                normal = lerp(normal, i.normalWS, 0.2);

                float f = max(0.0, dot(normal, _WorldSpaceLightPos0.xyz));
                
                fixed4 color = lerp(_Shade, _Color, f * 1.4);


                float lf = max(0.0, 1.0 - i.normalVS.z);
                lf = max(lf - 0.3, 0.0);
                lf = 5.0 * lf / (1.0 + 5.0 * lf);
                color.rgb = lerp(color.rgb, _GlareColor, clamp(lf, 0.0, 1.0));

                return color;
            }
            ENDCG
        }
    }
}
