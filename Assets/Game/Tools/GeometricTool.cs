using System;
using Unity.Mathematics;
using UnityEngine;

namespace Game
{
	public class GeometricTool
	{
		public static Vector3 CirclePoint(double angele01, Vector3 right, Vector3 up)
		{
			float angle_0_2p = (float)(angele01 * 2.0 * Math.PI);
			return right * (float)Math.Cos(angle_0_2p) + up * (float)Math.Sin(angle_0_2p);
		}
		
		public static float DistancePointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
		{
			return Vector3.Distance(p, ClosestPointTriangle(p, a, b, c));
		}

		public static Vector3 ClosestPointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
		{
			Vector3 ab = b - a;
			Vector3 ac = c - a;
			Vector3 ap = p - a;

			float d1 = Vector3.Dot(ab, ap);
			float d2 = Vector3.Dot(ac, ap);
			if (d1 <= .0f && d2 <= .0f) return a; //#1

			Vector3 bp = p - b;
			float d3 = Vector3.Dot(ab, bp);
			float d4 = Vector3.Dot(ac, bp);
			if (d3 >= .0f && d4 <= d3) return b; //#2

			Vector3 cp = p - c;
			float d5 = Vector3.Dot(ab, cp);
			float d6 = Vector3.Dot(ac, cp);
			if (d6 >= .0f && d5 <= d6) return c; //#3

			float vc = d1 * d4 - d3 * d2;
			if (vc <= .0f && d1 >= .0f && d3 <= .0f) {
				float v = d1 / (d1 - d3);
				return a + v * ab; //#4
			}

			float vb = d5 * d2 - d1 * d6;
			if (vb <= .0f && d2 >= .0f && d6 <= .0f) {
				float v = d2 / (d2 - d6);
				return a + v * ac; //#5
			}

			float va = d3 * d6 - d5 * d4;
			if (va <= .0f && (d4 - d3) >= .0f && (d5 - d6) >= .0f) {
				float v = (d4 - d3) / ((d4 - d3) + (d5 - d6));
				return b + v * (c - b); //#6
			}

			{
				float denom = .1f / (va + vb + vc);
				float v = vb * denom;
				float w = vc * denom;
				return a + v * ab + w * ac; //#0
			}
		}
		public struct ClosestPointOfApproach
		{
			public float3 pointA;
			public float3 pointB;
			public float relativeA;
			public float relativeB;
		};

		public ClosestPointOfApproach SegmentSegmentCPA(
			float3 a0, float3 a1, float3 b0, float3 b1
		) {
			float3 r = b0 - a0;
			float3 u = a1 - a0;
			float3 v = b1 - b0;
			float ru = math.dot(r, u);
			float rv = math.dot(r, v);
			float uu = math.dot(u, u);
			float uv = math.dot(u, v);
			float vv = math.dot(v, v);
			float det = uu * vv - uv * uv;
			float s, t;
			if (det < 1e-4f * uu * vv) { // parallel
				s = math.clamp(ru / uu, lowerBound: 0f, upperBound: 1f);
				t = 0f;
			} else { // not parallel
				s = math.clamp((ru * vv - rv * uv) / det, lowerBound: 0f, upperBound: 1f);
				t = math.clamp((ru * uv - rv * uu) / det, lowerBound: 0f, upperBound: 1f);
			}
			float S = math.clamp((t * uv + ru) / uu, lowerBound: 0f, upperBound: 1f);
			float T = math.clamp((s * uv - rv) / vv, lowerBound: 0f, upperBound: 1f);
			ClosestPointOfApproach cpa;
			cpa.pointA = a0 + S * u;
			cpa.pointB = b0 + T * v;
			cpa.relativeA = S;
			cpa.relativeB = T;
			return cpa;
		}

		public static float PointToLineDistanceSq(float2 a, float2 b, float2 p)
		{
			float l2 = math.distancesq(a, b);
			if (l2 <= float.Epsilon) {
				return math.distancesq(p, a);
			}
			float t = math.clamp(math.dot(p - a, b - a) / l2, lowerBound: 0, upperBound: 1);
			float2 projection = a + t * (b - a);  // Projection falls on the segment
			return math.distancesq(p, projection);
		}
	}
}