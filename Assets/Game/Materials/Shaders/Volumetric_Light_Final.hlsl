#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// The DeclareDepthTexture.hlsl file contains utilities for sampling the
// Camera depth texture.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

struct appdata
{
	uint vertexID : SV_VertexID;
};

struct VertexOutput
{
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
	float2 uv0 : TEXCOORD1;
	float2 uv1 : TEXCOORD2;
	float2 uv2 : TEXCOORD3;
	float2 uv3 : TEXCOORD4;
	float2 uv4 : TEXCOORD5;
	float2 uv5 : TEXCOORD6;
	float2 uv6 : TEXCOORD7;
	float2 uv7 : TEXCOORD8;
};

Texture2D _VolumetricLightAndGlowTexture;
float4 _VolumetricLightAndGlowTexture_TexelSize;
float4 _FarFogColor;
float4 _NearFogColor;
float4 _FarFogColorTop;
float4 _NearFogColorTop;

VertexOutput vert(appdata IN)
{
	VertexOutput OUT;
	OUT.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
	OUT.uv = GetFullScreenTriangleTexCoord(IN.vertexID);
	
	const float2 halfTexel = _VolumetricLightAndGlowTexture_TexelSize.xy * 0.5;
	OUT.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
	OUT.uv0 = OUT.uv + float2(-halfTexel.x, -halfTexel.y) * 2.0f;
	OUT.uv1 = OUT.uv + float2(halfTexel.x, -halfTexel.y) * 2.0f;
	OUT.uv2 = OUT.uv + float2(-halfTexel.x, halfTexel.y) * 2.0f;
	OUT.uv3 = OUT.uv + float2(halfTexel.x, halfTexel.y) * 2.0f;
	OUT.uv4 = OUT.uv + float2(0, -halfTexel.y) * 4.0f;
	OUT.uv5 = OUT.uv + float2(-halfTexel.x, 0) * 4.0f;
	OUT.uv6 = OUT.uv + float2(halfTexel.x, 0) * 4.0f;
	OUT.uv7 = OUT.uv + float2(0, halfTexel.y) * 4.0f;
	
	return OUT;
}

half4 frag(VertexOutput IN) : SV_Target
{
	float4 volumetricLightGlow =
		_VolumetricLightAndGlowTexture.SampleLevel(sampler_LinearClamp, IN.uv, 0);
		//_VolumetricLightAndGlowTexture.SampleLevel(sampler_LinearClamp, IN.uv0, 0) * (1.0h / 6.0h) +
		//_VolumetricLightAndGlowTexture.SampleLevel(sampler_LinearClamp, IN.uv1, 0) * (1.0h / 6.0h) +
		//_VolumetricLightAndGlowTexture.SampleLevel(sampler_LinearClamp, IN.uv2, 0) * (1.0h / 6.0h) +
		//_VolumetricLightAndGlowTexture.SampleLevel(sampler_LinearClamp, IN.uv3, 0) * (1.0h / 6.0h) +
		//_VolumetricLightAndGlowTexture.SampleLevel(sampler_LinearClamp, IN.uv4, 0) * (1.0h / 12.0h) +
		//_VolumetricLightAndGlowTexture.SampleLevel(sampler_LinearClamp, IN.uv5, 0) * (1.0h / 12.0h) +
		//_VolumetricLightAndGlowTexture.SampleLevel(sampler_LinearClamp, IN.uv6, 0) * (1.0h / 12.0h) +
		//_VolumetricLightAndGlowTexture.SampleLevel(sampler_LinearClamp, IN.uv7, 0) * (1.0h / 12.0h);
	
	float sceneDepth =
		_CameraDepthTexture.SampleLevel(sampler_LinearClamp, IN.uv, 0).r;
	float depth01 = Linear01Depth(sceneDepth, _ZBufferParams);
	
	// Sample the depth from the Camera depth texture.
#if !UNITY_REVERSED_Z
    // Adjust Z to match NDC for OpenGL ([-1, 1])
	sceneDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, sceneDepth);
#endif
	
	// To calculate the UV coordinates for sampling the depth buffer,
    // divide the pixel location by the render target resolution
    // _ScaledScreenParams.
	float2 screenPosUV = IN.positionCS.xy / _ScaledScreenParams.xy;
	
	// Reconstruct the world space positions.
	float3 worldPos = ComputeWorldSpacePosition(screenPosUV, sceneDepth, UNITY_MATRIX_I_VP);
	float factor = clamp(worldPos.y, 0, 400) / 400.0;
	float3 nearColor = lerp(_NearFogColor.rgb, _NearFogColorTop.rgb, factor);
	float3 farColor = lerp(_FarFogColor.rgb, _FarFogColorTop.rgb, factor);
	
	float3 fogColor = lerp(farColor, nearColor, min(1.0, 5.0 * volumetricLightGlow.r));
	
	float fogFactor = smoothstep(0.0, 20.0 / _ProjectionParams.z, depth01);
	fogFactor = clamp(fogFactor - 0.99 * volumetricLightGlow.r, 0.0, 0.8);
	
	//_ProjectionParams.z;
	//return float4(_VolumetricLightAndGlowTexture.SampleLevel(sampler_LinearClamp, IN.uv, 0).rrr, 1.0);
	//return float4(worldPos.yyy / 20.0, 1.0);
	//return float4(farColor, 1.0);
	return float4(fogColor, fogFactor);
}