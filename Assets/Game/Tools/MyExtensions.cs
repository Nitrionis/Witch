using Unity.Mathematics;
using UnityEngine;

namespace Assets.Game.Tools
{
	public static class MyExtensions
	{
		public static Vector4 WithW(this Vector3 v, float w) => new Vector4(v.x, v.y, v.z, w);
		public static float4 WithW(this float3 v, float w) => new Vector4(v.x, v.y, v.z, w);
	}
}
