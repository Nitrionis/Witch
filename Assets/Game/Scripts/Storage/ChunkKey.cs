using System;
using System.Runtime.CompilerServices;

namespace Game.Storage
{
	/// <summary>
	/// It is used to track different versions of chunks.
	/// The field values of this structure cannot be serialized.
	/// </summary>
	internal readonly struct ChunkKey : IEquatable<ChunkKey>
	{
		public readonly ChunkLocation ChunkLocation;

		/// <summary>
		/// The value of this field is incremented with each modification of the chunk.
		/// The value of this field is reset at each startup.
		/// </summary>
		public readonly ulong Version;

		public ChunkKey(ChunkLocation chunkLocation, ulong version)
		{
			ChunkLocation = chunkLocation;
			Version = version;
		}

		public override bool Equals(object obj) => obj is ChunkKey key && Equals(key);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(ChunkKey other)
		{
			return ChunkLocation.Equals(other.ChunkLocation) &&
				   Version == other.Version;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => HashCode.Combine(ChunkLocation, Version);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(ChunkKey left, ChunkKey right) => left.Equals(right);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(ChunkKey left, ChunkKey right) => !(left == right);
	}
}
