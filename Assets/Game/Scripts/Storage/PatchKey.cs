using System;
using System.Runtime.CompilerServices;

namespace Game.Storage
{
	internal readonly struct PatchKey : IEquatable<PatchKey>
	{
		public readonly ChunkLocation ChunkLocation;
		public readonly ushort IndexInChein;
		public readonly ulong Version;

		public PatchKey(ChunkLocation chunkLocation, ushort indexInChein, ulong version)
		{
			ChunkLocation = chunkLocation;
			IndexInChein = indexInChein;
			Version = version;
		}

		public override bool Equals(object obj) => obj is PatchKey key && Equals(key);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(PatchKey other)
		{
			return ChunkLocation.Equals(other.ChunkLocation) &&
				   IndexInChein == other.IndexInChein &&
				   Version == other.Version;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => HashCode.Combine(ChunkLocation, IndexInChein, Version);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(PatchKey left, PatchKey right) => left.Equals(right);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(PatchKey left, PatchKey right) => !(left == right);
	}
}
