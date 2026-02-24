using System;
using System.Runtime.CompilerServices;

namespace Game.Storage
{
	internal readonly struct ChunkLocation : IEquatable<ChunkLocation>
	{
		/// <summary>
		/// Chunk index for each axis of the world.
		/// </summary>
		public readonly ushort2 AxisIndices;

		public ChunkLocation(ushort2 axisIndices) => AxisIndices = axisIndices;

		public override bool Equals(object obj) =>
			obj is ChunkLocation other && Equals(other);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(ChunkLocation other) => AxisIndices.Equals(other.AxisIndices);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => AxisIndices.GetHashCode();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(ChunkLocation left, ChunkLocation right) => left.Equals(right);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(ChunkLocation left, ChunkLocation right) => !left.Equals(right);
	}
}
