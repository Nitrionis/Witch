// The Core.hlsl file contains definitions of frequently used HLSL
// macros and functions, and also contains #include references to other
// HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct VertexInput
{
	float4 positionOS : POSITION;
	float3 normalOS : NORMAL;
};

struct VertexOutput
{
	float4 positionCS : SV_POSITION;
	float3 positionWS : TEXCOORD1;
	float eyeDepth : TEXCOORD0;
	float normalOSx : TEXCOORD2;
};

CBUFFER_START(UnityPerMaterial)
float3 _CapsulePointA;
float3 _CapsulePointB;
float _CapsuleRadius;
CBUFFER_END

VertexOutput vert(VertexInput i)
{
	VertexOutput OUT;
	OUT.positionCS = TransformObjectToHClip(i.positionOS.xyz);
	OUT.positionWS = TransformObjectToWorld(i.positionOS.xyz);
	OUT.eyeDepth = -TransformWorldToView(OUT.positionWS).z;
	OUT.normalOSx = i.normalOS.x;	
	return OUT;
}

struct ClosestPointOfApproach
{
	float3 pointA;
	float3 pointB;
	float relativeA;
	float relativeB;
};

ClosestPointOfApproach SegmentSegmentCPA(
	float3 a0, float3 a1, float3 b0, float3 b1
) {
	float3 r = b0 - a0;
	float3 u = a1 - a0;
	float3 v = b1 - b0;
	float ru = dot(r, u);
	float rv = dot(r, v);
	float uu = dot(u, u);
	float uv = dot(u, v);
	float vv = dot(v, v);
	float det = uu * vv - uv * uv;
	float s, t;
	float parallel = float(det < 1e-4f * uu * vv);
	s = lerp(clamp((ru * vv - rv * uv) / det, 0.0, 1.0), clamp(ru / uu, 0.0, 1.0), parallel);
	t = lerp(clamp((ru * uv - rv * uu) / det, 0.0, 1.0), 0.0, parallel);
	float S = clamp((t * uv + ru) / uu, 0.0, 1.0);
	float T = clamp((s * uv - rv) / vv, 0.0, 1.0);
	ClosestPointOfApproach cpa;
	cpa.pointA = a0 + S * u;
	cpa.pointB = b0 + T * v;
	cpa.relativeA = S;
	cpa.relativeB = T;
	return cpa;
}

half4 frag(VertexOutput IN) : SV_Target
{
	float3 rayStartPointWS = GetCameraPositionWS();
	//float3 rayEndPointWS = IN.positionWS;
	float3 rayEndPointWS = rayStartPointWS + normalize(IN.positionWS - rayStartPointWS) * 20.0;
	ClosestPointOfApproach cpa =
		SegmentSegmentCPA(rayStartPointWS, rayEndPointWS, _CapsulePointA, _CapsulePointB);
	float worldUnitsPerPixel = IN.eyeDepth * unity_CameraProjection._m11 / _ScreenParams.x;
	
	float capsuleRadius = _CapsuleRadius;
	capsuleRadius *= 0.6 + 0.4 * cpa.relativeB;
	
	float contactDistance = capsuleRadius + worldUnitsPerPixel;
	float fullContactDistance = max(
		capsuleRadius - 2.0 * worldUnitsPerPixel,
		worldUnitsPerPixel - 2.0 * capsuleRadius
	);
	float dist = distance(cpa.pointA, cpa.pointB);
	float intersectionFactor = 1.0 - smoothstep(fullContactDistance, contactDistance, dist);
	
	intersectionFactor *= 1.0 - min(1.0, (distance(_CapsulePointA, rayStartPointWS) / 20.0));
	
	intersectionFactor *= 1.0 - 0.4 * smoothstep(0.0, capsuleRadius, dist);
	float f = 0.1 + 0.9 * IN.normalOSx;
	
	intersectionFactor *= smoothstep(0.2, 0.8, IN.positionWS.y);
	
	intersectionFactor /= 5;
	
	return half4(intersectionFactor, intersectionFactor, intersectionFactor, 1.0);
}