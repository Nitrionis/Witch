using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Assets.Game.Scripts.Storage
{
	public readonly struct RegionKey : IEquatable<RegionKey>
	{
		public readonly int2 AxisIndices;

		public RegionKey(int2 axisIndices)
		{
			AxisIndices = axisIndices;
		}

		public override bool Equals(object obj) => obj is RegionKey other && Equals(other);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(RegionKey other) => AxisIndices.Equals(other.AxisIndices);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => AxisIndices.GetHashCode();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(RegionKey left, RegionKey right) => left.Equals(right);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(RegionKey left, RegionKey right) => !left.Equals(right);
	}
}

