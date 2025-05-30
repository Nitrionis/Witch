using System;
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
	}
}