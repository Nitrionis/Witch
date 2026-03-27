using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Collections;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Storage
{
	internal unsafe struct RegionBase
    {
		public const int ChunkCountPerSide = 8;
		public const int ChunkCount = 64;

		/// <summary>
		/// The length of the region side in world units.
		/// </summary>
		public const int SideLength = ChunkSize.SideLength * ChunkCountPerSide;

		public static int BufferSizeForUnpack => sizeof(RegionBase);

		/// <remarks>Non-serializable field</remarks>
		public ulong PlayerUsageBits;

		/// <summary>
		/// 1 bit per player
		/// </summary>
		/// <remarks>Non-serializable field</remarks>
		public int ReferenceCount;

		/// <remarks>Non-serializable field</remarks>
		public RegionBaseLocation RegionBaseLocation;

		public Repeat64<LCG64> LCGs;

		/// <summary>
		/// Surface heights at intervals of 1 time per 4 world units.
		/// </summary>
		/// <remarks>
		/// <para>256 = <see cref="ChunkLocation.ChunkSideSize"/></para>
		/// <para>8 = <see cref="ChunkCountPerSide"/></para>
		/// <para>513 = 8 * (256 / 4) + 1</para>
		/// </remarks>
		public Repeat513x513<ushort> Heights;

		/// <summary>
		/// Biome data for each point in <see cref="Heights"/>.
		/// </summary>
		public Repeat513x513<byte> BiomeFlags;

		public static List<byte> PackRegion(RegionBase* region)
		{
			var buffer = new List<byte>(sizeof(RegionBase));

			for (int chunkIndex = 0; chunkIndex < ChunkCount; chunkIndex++) {
				var ptrToLCG = (byte*)PointerArray.From(&region->LCGs)[chunkIndex];
				for (int i = 0; i < sizeof(LCG64); i++) {
					buffer.Add(*(ptrToLCG + i));
				}
			}

			var terrainHeights = UnmanagedArray.From(&region->Heights);
			int currentHeight = 0;
			int previusHeight = terrainHeights[0];
			for (int i = 1; i < terrainHeights.Length; i++) {
				currentHeight = math.min(terrainHeights[i], 0x7FFF);
				int signedDelta = currentHeight - previusHeight;
				int unsignedDelta = math.abs(signedDelta);
				if (unsignedDelta <= 0x3F) {
					int b = unsignedDelta | (signedDelta < 0 ? 0x40 : 0);
					buffer.Add((byte)b);
				} else {
					int b = (currentHeight >> 8) | 0x80;
					buffer.Add((byte)b);
					b = currentHeight & 0xFF;
					buffer.Add((byte)b);
				}
				previusHeight = currentHeight;
			}

			var biomeFlags = UnmanagedArray.From(&region->BiomeFlags);
			byte repeatCount = 1;
			byte previusBiome = biomeFlags[0];
			for (int i = 1; i < biomeFlags.Length; i++) {
				byte currentBiome = biomeFlags[i];
				if (
					currentBiome != previusBiome ||
					repeatCount == byte.MaxValue
				) {
					buffer.Add(repeatCount);
					buffer.Add(previusBiome);
					previusBiome = currentBiome;
					repeatCount = 0;
				}
				repeatCount++;
			}
			if (repeatCount > 0) {
				buffer.Add(repeatCount);
				buffer.Add(previusBiome);
			}

			return buffer;
		}

		public static void UnpackRegion(RegionBase* region, UnmanagedArray<byte> buffer)
		{
			int bufferOffset = 0;
			for (int chunkIndex = 0; chunkIndex < ChunkCount; chunkIndex++) {
				var ptrToLCG = (byte*)PointerArray.From(&region->LCGs)[chunkIndex];
				for (int i = 0; i < sizeof(LCG64); i++) {
					*(ptrToLCG + i) = buffer[bufferOffset + i];
				}
				bufferOffset += sizeof(LCG64);
			}

			var terrainHeights = UnmanagedArray.From(&region->Heights);
			int currentHeight = 0;
			for (int i = 0; i < terrainHeights.Length; i++) {
				byte b = buffer[bufferOffset++];
				if ((b & 0x80) == 0) {
					currentHeight += (b & 0x3F) * ((b & 0x40) != 0 ? -1 : 1);
				} else {
					currentHeight = (b & 0x7F) << 8;
					b = buffer[bufferOffset++];
					currentHeight += b;
				}
				terrainHeights[i] = (ushort)currentHeight;
			}

			var biomeFlags = UnmanagedArray.From(&region->BiomeFlags);
			for (int i = 0; i < biomeFlags.Length;) {
				// TODO validate repeatCount
				byte repeatCount = buffer[bufferOffset++];
				byte biome = buffer[bufferOffset++];
				for (int ri = 0; ri < repeatCount; ri++) {
					biomeFlags[i] = biome;
				}
			}
		}

		public class LoadTask
		{
			private readonly byte* buffer = (byte*)Marshal.AllocHGlobal(BufferSizeForUnpack);

			private readonly FileManager fileManager;
			public readonly Func<LoadTask> ActionDelegate;

			public RegionBaseLocation RegionBaseLocation;
			public Pool<RegionBase>.Slot RegionBaseSlot;

			public LoadTask(FileManager fileManager)
			{
				this.fileManager = fileManager;
				ActionDelegate = Tick;
			}

			~LoadTask()
			{
				Marshal.FreeHGlobal((IntPtr)buffer);
			}

			private LoadTask Tick()
			{
				var filePath = fileManager.GetRegionBaseFilePath(RegionBaseLocation);
				var fileStream = new FileStream(
					filePath,
					FileMode.Open,
					FileAccess.Read,
					FileShare.Read,
					bufferSize: 0,  // Disable inner buffer
					FileOptions.SequentialScan
				);
				int byteCount = fileStream.Read(new Span<byte>(buffer, BufferSizeForUnpack));
				if (byteCount == BufferSizeForUnpack) {
					throw new Exception("Invalid file structure.");
				}
				var regionBase = RegionBaseSlot.ItemPointer;
				UnpackRegion(regionBase, new UnmanagedArray<byte>(buffer, BufferSizeForUnpack));
				regionBase->PlayerUsageBits = 0;
				regionBase->ReferenceCount = 0;
				regionBase->RegionBaseLocation = RegionBaseLocation;

				fileStream.Dispose();
				fileStream = null;
				return this;
			}
		}
	}

	public readonly struct RegionBaseLocation : IEquatable<RegionBaseLocation>
	{
		/// <summary>
		/// Region index for each axis of the world.
		/// </summary>
		public readonly ushort2 AxisIndices;

		public RegionBaseLocation(ushort2 axisIndices) => AxisIndices = axisIndices;

		public override bool Equals(object obj) =>
			obj is RegionBaseLocation other && Equals(other);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(RegionBaseLocation other) => AxisIndices.Equals(other.AxisIndices);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => AxisIndices.GetHashCode();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(RegionBaseLocation left, RegionBaseLocation right) => left.Equals(right);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(RegionBaseLocation left, RegionBaseLocation right) => !left.Equals(right);

		public static RegionBaseLocation From(ChunkLocation chunkLocation) =>
			new RegionBaseLocation(new ushort2(
				x: (ushort)(chunkLocation.AxisIndices.x / RegionBase.ChunkCountPerSide),
				y: (ushort)(chunkLocation.AxisIndices.y / RegionBase.ChunkCountPerSide)
			));
	}
}
