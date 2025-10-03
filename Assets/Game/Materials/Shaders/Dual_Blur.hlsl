// The Core.hlsl file contains definitions of frequently used HLSL
// macros and functions, and also contains #include references to other
// HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct appdata
{
	uint vertexID : SV_VertexID;
};

sampler2D _BlitTexture;
float4 _BlitTexture_TexelSize;

struct downV2f
{
	float4 positionCS : SV_POSITION;
	float2 uv0 : TEXCOORD0;
	float2 uv1 : TEXCOORD1;
	float2 uv2 : TEXCOORD2;
	float2 uv3 : TEXCOORD3;
	float2 uv4 : TEXCOORD4;
};

downV2f downVert(appdata IN)
{
	const float2 halfTexel = _BlitTexture_TexelSize.xy * 0.5;
	float2 uv = GetFullScreenTriangleTexCoord(IN.vertexID);
	downV2f OUT;
	OUT.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
	OUT.uv0 = uv;
	OUT.uv1 = uv + float2(-halfTexel.x,-halfTexel.y);
	OUT.uv2 = uv + float2( halfTexel.x,-halfTexel.y);
	OUT.uv3 = uv + float2(-halfTexel.x, halfTexel.y);
	OUT.uv4 = uv + float2( halfTexel.x, halfTexel.y);
	return OUT;
}

half4 downFrag(downV2f IN) : SV_Target
{
	return tex2D(_BlitTexture, IN.uv0) * 0.5h +
           tex2D(_BlitTexture, IN.uv1) * (1.0h / 8.0h) +
           tex2D(_BlitTexture, IN.uv2) * (1.0h / 8.0h) +
           tex2D(_BlitTexture, IN.uv3) * (1.0h / 8.0h) +
           tex2D(_BlitTexture, IN.uv4) * (1.0h / 8.0h);
}

struct upV2f
{
	float4 positionCS : SV_POSITION;
	float2 uv0 : TEXCOORD0;
	float2 uv1 : TEXCOORD1;
	float2 uv2 : TEXCOORD2;
	float2 uv3 : TEXCOORD3;
	float2 uv4 : TEXCOORD4;
	float2 uv5 : TEXCOORD5;
	float2 uv6 : TEXCOORD6;
	float2 uv7 : TEXCOORD7;
};

upV2f upVert(appdata IN)
{
	const float2 halfTexel = _BlitTexture_TexelSize.xy * 0.5;
	float2 uv = GetFullScreenTriangleTexCoord(IN.vertexID);
	upV2f OUT;
	OUT.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
	OUT.uv0 = uv + float2(-halfTexel.x,-halfTexel.y) * 2.0f;
	OUT.uv1 = uv + float2( halfTexel.x,-halfTexel.y) * 2.0f;
	OUT.uv2 = uv + float2(-halfTexel.x, halfTexel.y) * 2.0f;
	OUT.uv3 = uv + float2( halfTexel.x, halfTexel.y) * 2.0f;
	OUT.uv4 = uv + float2( 0,-halfTexel.y) * 4.0f;
	OUT.uv5 = uv + float2(-halfTexel.x, 0) * 4.0f;
	OUT.uv6 = uv + float2( halfTexel.x, 0) * 4.0f;
	OUT.uv7 = uv + float2( 0, halfTexel.y) * 4.0f;
	return OUT;
}

half4 upFrag(upV2f i) : SV_Target
{
	return tex2D(_BlitTexture, i.uv0) * (1.0h / 6.0h) +
           tex2D(_BlitTexture, i.uv1) * (1.0h / 6.0h) +
           tex2D(_BlitTexture, i.uv2) * (1.0h / 6.0h) +
           tex2D(_BlitTexture, i.uv3) * (1.0h / 6.0h) +
           tex2D(_BlitTexture, i.uv4) * (1.0h / 12.0h) +
           tex2D(_BlitTexture, i.uv5) * (1.0h / 12.0h) +
           tex2D(_BlitTexture, i.uv6) * (1.0h / 12.0h) +
           tex2D(_BlitTexture, i.uv7) * (1.0h / 12.0h);
}




// The Core.hlsl file contains definitions of frequently used HLSL
// macros and functions, and also contains #include references to other
// HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//
//struct appdata
//{
//	uint vertexID : SV_VertexID;
//};
//
//CBUFFER_START(UnityPerMaterial)  
//sampler2D _BlitTexture;
//float4 _BlitTexture_TexelSize;
//CBUFFER_END
//
//struct downV2f
//{
//	float4 positionCS : SV_POSITION;
//	float2 uv0 : TEXCOORD0;
//	float2 uv1 : TEXCOORD1;
//	float2 uv2 : TEXCOORD2;
//	float2 uv3 : TEXCOORD3;
//	float2 uv4 : TEXCOORD4;
//};
//
//downV2f downVert(appdata IN)
//{
//	const float2 halfTexel = _BlitTexture_TexelSize.xy * 0.5;
//	float2 uv = GetFullScreenTriangleTexCoord(IN.vertexID);
//	downV2f OUT;
//	OUT.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
//	OUT.uv0 = uv;
//	OUT.uv1 = uv + float2(-halfTexel.x, -halfTexel.y);
//	OUT.uv2 = uv + float2(halfTexel.x, -halfTexel.y);
//	OUT.uv3 = uv + float2(-halfTexel.x, halfTexel.y);
//	OUT.uv4 = uv + float2(halfTexel.x, halfTexel.y);
//	return OUT;
//}
//
//half4 downFrag(downV2f IN) : SV_Target
//{
//	return tex2D(_BlitTexture, IN.uv0) * 0.5h +
//           tex2D(_BlitTexture, IN.uv1) * (1.0h / 8.0h) +
//           tex2D(_BlitTexture, IN.uv2) * (1.0h / 8.0h) +
//           tex2D(_BlitTexture, IN.uv3) * (1.0h / 8.0h) +
//           tex2D(_BlitTexture, IN.uv4) * (1.0h / 8.0h);
//}
//
//struct upV2f
//{
//	float4 positionCS : SV_POSITION;
//	float2 uv0 : TEXCOORD0;
//	float2 uv1 : TEXCOORD1;
//	float2 uv2 : TEXCOORD2;
//	float2 uv3 : TEXCOORD3;
//	float2 uv4 : TEXCOORD4;
//	float2 uv5 : TEXCOORD5;
//	float2 uv6 : TEXCOORD6;
//	float2 uv7 : TEXCOORD7;
//};
//
//upV2f upVert(appdata IN)
//{
//	const float2 halfTexel = _BlitTexture_TexelSize.xy * 0.5;
//	float2 uv = GetFullScreenTriangleTexCoord(IN.vertexID);
//	upV2f OUT;
//	OUT.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
//	OUT.uv0 = uv + float2(-halfTexel.x, -halfTexel.y) * 2.0f;
//	OUT.uv1 = uv + float2(halfTexel.x, -halfTexel.y) * 2.0f;
//	OUT.uv2 = uv + float2(-halfTexel.x, halfTexel.y) * 2.0f;
//	OUT.uv3 = uv + float2(halfTexel.x, halfTexel.y) * 2.0f;
//	OUT.uv4 = uv + float2(0, -halfTexel.y) * 4.0f;
//	OUT.uv5 = uv + float2(-halfTexel.x, 0) * 4.0f;
//	OUT.uv6 = uv + float2(halfTexel.x, 0) * 4.0f;
//	OUT.uv7 = uv + float2(0, halfTexel.y) * 4.0f;
//	return OUT;
//}
//
//half4 upFrag(upV2f i) : SV_Target
//{
//	return tex2D(_BlitTexture, i.uv0) * (1.0h / 6.0h) +
//           tex2D(_BlitTexture, i.uv1) * (1.0h / 6.0h) +
//           tex2D(_BlitTexture, i.uv2) * (1.0h / 6.0h) +
//           tex2D(_BlitTexture, i.uv3) * (1.0h / 6.0h) +
//           tex2D(_BlitTexture, i.uv4) * (1.0h / 12.0h) +
//           tex2D(_BlitTexture, i.uv5) * (1.0h / 12.0h) +
//           tex2D(_BlitTexture, i.uv6) * (1.0h / 12.0h) +
//           tex2D(_BlitTexture, i.uv7) * (1.0h / 12.0h);
//}