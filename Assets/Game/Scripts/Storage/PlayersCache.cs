using System.Runtime.CompilerServices;
using Game.Allocators;
using Unity.Mathematics;

namespace Game.Storage
{
	internal readonly struct PlayersCache
	{
		public readonly UnmanagedArray<RegionChangesLocation> CurrentChangesRegionPerPlayer;
		public readonly UnmanagedArray<RegionBaseLocation> CurrentBasesRegionPerPlayer;
		private readonly int regionBasesCacheRadius;
		private readonly int regionChangesCacheRadius;

		public unsafe PlayersCache(
			in SessionRewindableAllocator poolsAllocator,
			int maxPlayerCount,
			int regionBasesCacheRadius = 4,
			int regionChangesCacheRadius = 3)
		{
			this.regionBasesCacheRadius = regionBasesCacheRadius;
			this.regionChangesCacheRadius = regionChangesCacheRadius;
			CurrentBasesRegionPerPlayer = new(
				poolsAllocator.AllocateArray<RegionBaseLocation>(maxPlayerCount),
				maxPlayerCount
			);
			CurrentBasesRegionPerPlayer.Fill(new(new ushort2(ushort.MaxValue, ushort.MaxValue)));
			CurrentChangesRegionPerPlayer = new(
				poolsAllocator.AllocateArray<RegionChangesLocation>(maxPlayerCount),
				maxPlayerCount
			);
			CurrentChangesRegionPerPlayer.Fill(new(new ushort2(ushort.MaxValue, ushort.MaxValue)));
		}

		/// <remarks>Call only in sync phase!</remarks>
		public void SetPlayerCurrentChunk(int playerIndex, ChunkLocation chunkLocation)
		{
			CurrentChangesRegionPerPlayer[playerIndex] = RegionChangesLocation.From(chunkLocation);
			CurrentBasesRegionPerPlayer[playerIndex] = RegionBaseLocation.From(chunkLocation);
		}

		public bool CanSaveChunks(RegionChangesLocation location) =>
			CanSaveChunks(location, CurrentChangesRegionPerPlayer, regionChangesCacheRadius);

		public bool CanSaveChunks(RegionBaseLocation location) =>
			CanSaveChunks(location, CurrentBasesRegionPerPlayer, regionBasesCacheRadius);

		public bool CanSaveChunks<T>(T location, UnmanagedArray<T> playerPositions, int radius)
			where T : unmanaged, ILoacation
		{
			bool any = false;
			for (int i = 0; i < playerPositions.Length; i++) {
				var position = (int2)playerPositions[i].AxisIndices;
				any |= IsInBounds(
					position: location.AxisIndices,
					offset: position - new int2(radius),
					size: 2 + 2 * radius // diameter(1 + 2 * radius) + 1 for border check
				);
			}
			return any;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsInBounds(int2 position, int2 offset, int size)
		{
			return (uint)(position.x - offset.x) < size
				&& (uint)(position.y - offset.y) < size;
		}
	}
}
