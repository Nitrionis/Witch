#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct appdata
{
	uint vertexID : SV_VertexID;
};

struct v2f
{
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
};

struct f2t
{
	half4 clor : SV_Target;
	float depth : SV_Depth;
};

CBUFFER_START(UnityPerMaterial)
	Texture2D _BlitTexture;
CBUFFER_END

v2f vert(appdata i)
{
	v2f o;
	o.positionCS = GetFullScreenTriangleVertexPosition(i.vertexID);
	o.uv = GetFullScreenTriangleTexCoord(i.vertexID);
	return o;
}

f2t frag(v2f i)
{
	f2t o;
	o.clor = half4(1.0, 0.0, 0.0, 1.0);
	o.depth = _BlitTexture.SampleLevel(sampler_LinearClamp, i.uv, 0).r;
	return o;
}