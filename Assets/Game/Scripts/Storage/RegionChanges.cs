
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Collections;
using Unity.Collections.LowLevel.Unsafe;
using static Game.Storage.PatchPointer;

namespace Game.Storage
{
	internal struct ChunkPatches : IEnumerable<PatchView>
	{
		public ulong Version; // TODO must be saved
		public UsageBits UsageState;
		public ChainSegment<PatchView> PatchesChainStart;

		public ChainSegment<PatchView>.EnumerableChain.Enumerator GetEnumerator() =>
			new ChainSegment<PatchView>.EnumerableChain.Enumerator(PatchesChainStart);
		IEnumerator<PatchView> IEnumerable<PatchView>.GetEnumerator() => GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<PatchView>)this).GetEnumerator();

		[Flags]
		public enum UsageBits : uint
		{
			None = 0,
			Cached = 1 << 0,
			MeshBuilder = 1 << 1,
			ObjectPlacer = 1 << 2,
			NavmeshBuilder = 1 << 3,
			ChunkVariantBuilder = 1 << 4,
		}
	}

	internal static class ChunkPatchesUsageBitsExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasBits(this ChunkPatches.UsageBits srcBits, ChunkPatches.UsageBits targetBits) =>
			(srcBits & targetBits) == targetBits;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SetBits(this ref ChunkPatches.UsageBits srcBits, ChunkPatches.UsageBits targetBits) =>
			srcBits |= targetBits;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void UnsetBits(this ref ChunkPatches.UsageBits srcBits, ChunkPatches.UsageBits targetBits) =>
			srcBits &= ~targetBits;
	}

	internal struct RegionChanges
	{
		public const int ChunkCount = 16;
		public const int ChunkCountPerSide = 4;

		public bool IsModified;
		public int UnloadTaskCount;
		public RegionChangesLocation Location;
		public Repeat16<Pool<ChunkPatches>.Slot> Chunks;

		public static int GetChunkIndexInsideRegion(ChunkLocation chunkLocation)
		{
			int x = chunkLocation.AxisIndices.x % ChunkCountPerSide;
			int y = chunkLocation.AxisIndices.y % ChunkCountPerSide;
			return x + y * ChunkCountPerSide;
		}

		public static ChunkLocation GetChunkLocation(RegionChangesLocation regionLocation, int localChunkIndex)
		{
			return new ChunkLocation(axisIndices: new ushort2(
				x: (ushort)(regionLocation.AxisIndices.x * ChunkCountPerSide + localChunkIndex % ChunkCountPerSide),
				y: (ushort)(regionLocation.AxisIndices.y * ChunkCountPerSide + localChunkIndex / ChunkCountPerSide)
			));
		}

		private const int MetadataBufferLength = 4096;

		[StructLayout(LayoutKind.Sequential)]
		private struct RegionInfo
		{
			public int TotalPatchCount;
			public int PatchesOffsetInFile;
			public Repeat16<int> PatchCountPefChunk;
			public Repeat16<ulong> ChunkVersions;
		}

		public unsafe class UnloadTask
		{
			private readonly byte* metadataBuffer = (byte*)Marshal.AllocHGlobal(MetadataBufferLength);

			public RegionChangesLocation RegionChangesLocation;
			public readonly Repeat16<Pool<ChunkPatches>.Slot>* Chunks =
				(Repeat16<Pool<ChunkPatches>.Slot>*)Marshal.AllocHGlobal(MetadataBufferLength);

			public readonly Func<UnloadTask> ActionDelegate;

			private readonly FileManager fileManager;

			public UnloadTask(FileManager fileManager)
			{
				ActionDelegate = SaveToFile;
				this.fileManager = fileManager;
			}

			private UnloadTask SaveToFile()
			{
				var regionFullPath = fileManager.GetRegionChangesFilePath(RegionChangesLocation);
				var fileStream = new FileStream(regionFullPath, FileMode.OpenOrCreate);
				fileStream.Seek(0, SeekOrigin.Begin);

				var chunks = UnmanagedArray.From(Chunks);
				var regionInfo = stackalloc RegionInfo[1];
				var patchCountPefChunk = UnmanagedArray.From(&regionInfo->PatchCountPefChunk);
				var chunkVersions = UnmanagedArray.From(&regionInfo->ChunkVersions);
				for (int i = 0; i < chunks.Length; i++) {
					int patchCount = 0;
					foreach (var patch in *chunks[i].Pointer) {
						patchCount++;
					}
					patchCountPefChunk[i] = patchCount;
					chunkVersions[i] = chunks[i].Pointer->Version;
					regionInfo->TotalPatchCount += patchCount;
				}
				int localPosition = sizeof(RegionInfo);
				int padding = 0;
				int metadataAligment = UnsafeUtility.AlignOf<ChunkPatch.Metadata>();
				if (fileStream.Position % metadataAligment != 0) {
					padding = (int)(metadataAligment - fileStream.Position % metadataAligment);
					localPosition += padding;
				}
				{
					int buffersCount = 1;
					int patchCount = regionInfo->TotalPatchCount;
					patchCount -= (MetadataBufferLength - localPosition) / sizeof(RegionInfo);
					while (patchCount > 0) {
						patchCount -= MetadataBufferLength / sizeof(RegionInfo);
						buffersCount++;
					}
					regionInfo->PatchesOffsetInFile = MetadataBufferLength * buffersCount;
				}
				fileStream.Write(new Span<byte>(regionInfo, sizeof(RegionInfo)));
				if (padding != 0) {
					fileStream.Write(new Span<byte>(metadataBuffer, padding));
				}
				for (int i = 0; i < chunks.Length; i++) {
					foreach (var patch in *chunks[i].Pointer) {
						int itemSize = sizeof(ChunkPatch.Metadata);
						if (localPosition + itemSize > MetadataBufferLength) {
							padding = MetadataBufferLength - localPosition;
							fileStream.Write(new Span<byte>(metadataBuffer, padding));
							localPosition = 0;
						}
						fileStream.Write(new Span<byte>(&patch.Metadata, itemSize));
					}
				}
				if (fileStream.Position % MetadataBufferLength != 0) {
					padding = (int)(MetadataBufferLength - fileStream.Position % MetadataBufferLength);
					fileStream.Write(new Span<byte>(metadataBuffer, padding));
				}
				for (int i = 0; i < chunks.Length; i++) {
					foreach (var patchView in *chunks[i].Pointer) {
						fileStream.Write(new Span<byte>(patchView.Patch.TypedPointer, sizeof(ChunkPatch)));
					}
				}
				fileStream.Dispose();
				return this;
			}

			~UnloadTask()
			{
				Marshal.FreeHGlobal((IntPtr)metadataBuffer);
				Marshal.FreeHGlobal((IntPtr)Chunks);
			}
		}

		public unsafe class LoadTask
		{
			private readonly byte* metadataBuffer = (byte*)Marshal.AllocHGlobal(MetadataBufferLength);
			private int metadataOffset;

			private readonly Queue<Pool<PatchesGroup>.Slot> freePatchGroups = new();
			private readonly Queue<ChainSegment<PatchView>> freeSegments = new();

			public bool IsCompleted { get; private set; } = true;

			public RegionChangesLocation RegionChangesLocation;
			public Pool<RegionChanges>.Slot RegionChangesSlot
			{
				get => regionChangesSlot;
				set
				{
					regionChangesSlot = value;
					var chunkSlots = UnmanagedArray.From(&value.Pointer->Chunks);
					foreach (var chunkSlot in chunkSlots) {
						if (chunkSlot.Pointer->PatchesChainStart.IsNotNull) {
							throw new Exception("Chunk slot patches is not null");
						}
					}
				}
			}

			public readonly Func<LoadTask> ActionDelegate;

			private readonly FileManager fileManager;
			private FileStream fileStream;
			private int metadataStreamOffset;
			private int patchesStreamOffset;

			private bool isFirstTick = true;
			private int currentChunkIndex;
			private int currentPatchIndexInChunk;
			private RegionInfo regionInfo;
			private Pool<PatchesGroup>.Slot currentGroupSlot;
			private int patchCountInSlot;
			private int currentPatchIndexInSlot;
			private Pool<RegionChanges>.Slot regionChangesSlot;

			public LoadTask(FileManager fileManager)
			{
				this.fileManager = fileManager;
				ActionDelegate = Tick;
			}

			~LoadTask()
			{
				Marshal.FreeHGlobal((IntPtr)metadataBuffer);
			}

			private void ReadMetadataBuffer()
			{
				if (fileStream.Position != metadataStreamOffset) {
					fileStream.Position = metadataStreamOffset;
				}
				int byteCount = fileStream.Read(new Span<byte>(metadataBuffer, MetadataBufferLength));
				if (byteCount != MetadataBufferLength) {
					throw new Exception("Invalid file structure.");
				}
				metadataStreamOffset += byteCount;
				metadataOffset = 0;
			}

			private LoadTask Tick()
			{
				var chunkSlots = UnmanagedArray.From(&RegionChangesSlot.Pointer->Chunks);
				RegionInfo regionInfoCopyOnStack;
				if (isFirstTick) {
					isFirstTick = false;
					var filePath = fileManager.GetRegionChangesFilePath(RegionChangesLocation);
					if (!File.Exists(filePath)) {
						IsCompleted = true;
						return this;
					}
					fileStream = new FileStream(
						filePath,
						FileMode.Open,
						FileAccess.Read,
						FileShare.Read,
						bufferSize: 0,  // Disable inner buffer
						FileOptions.SequentialScan
					);
					metadataStreamOffset = 0;
					ReadMetadataBuffer();
					regionInfo = *(RegionInfo*)metadataBuffer;
					regionInfoCopyOnStack = regionInfo;
					metadataOffset = sizeof(RegionInfo);
					int metadataAligment = UnsafeUtility.AlignOf<ChunkPatch.Metadata>();
					if (metadataOffset % metadataAligment != 0) {
						int padding = metadataAligment - metadataOffset % metadataAligment;
						metadataOffset += padding;
					}
					currentChunkIndex = 0;
					currentPatchIndexInChunk = 0;
					patchesStreamOffset = regionInfo.PatchesOffsetInFile;
					currentPatchIndexInSlot = 0;
					patchCountInSlot = 0;

					var chunkVersions = UnmanagedArray.From(&regionInfoCopyOnStack.ChunkVersions);
					for (int i = 0; i < chunkSlots.Length; i++) {
						chunkSlots[currentChunkIndex].Pointer->Version = chunkVersions[currentChunkIndex];
					}
				}

				regionInfoCopyOnStack = regionInfo;
				for (; currentChunkIndex < chunkSlots.Length; currentChunkIndex++) {
					var chunkPatches = chunkSlots[currentChunkIndex];
					int patchCount = UnmanagedArray.From(&regionInfoCopyOnStack.PatchCountPefChunk)[currentChunkIndex];
					if (currentPatchIndexInChunk == 0) {
						*chunkPatches.Pointer = default;
					}
					var chainBuilder = new ChainSegment<PatchView>.ChainBuilder(
						chunkPatches.Pointer->PatchesChainStart
					);
					for (; currentPatchIndexInChunk < patchCount; currentPatchIndexInChunk++) {
						if (currentPatchIndexInSlot >= patchCountInSlot) {
							if (!freePatchGroups.TryDequeue(out currentGroupSlot)) {
								return this;
							}
							if (fileStream.Position != patchesStreamOffset) {
								fileStream.Position = patchesStreamOffset;
							}
							int byteCount = fileStream.Read(new Span<byte>(
								&currentGroupSlot.Pointer->Slots,
								sizeof(Repeat16<ChunkPatch>)
							));
							if (byteCount % sizeof(ChunkPatch) != 0) {
								throw new Exception("Invalid file structure.");
							}
							patchesStreamOffset += byteCount;
							patchCountInSlot = byteCount / sizeof(ChunkPatch);
							currentPatchIndexInSlot = 0;
						}
						if (metadataOffset + sizeof(ChunkPatch.Metadata) > MetadataBufferLength) {
							ReadMetadataBuffer();
						}
						var metadata = *(ChunkPatch.Metadata*)(metadataBuffer + metadataOffset);
						var currentPatchPointer = new PatchPointer(new PatchesGroupSlotPart(
							slotIndexInGroup: (byte)currentPatchIndexInSlot,
							group: currentGroupSlot
						));
						var patchView = new PatchView(patch: currentPatchPointer, metadata);
						if (!chainBuilder.TryAddNoResize(patchView)) {
							if (!freeSegments.TryPeek(out var nextSegment)) {
								return this;
							}
							chainBuilder.ConnectNextSegment(nextSegment);
							chainBuilder.TryAddNoResize(patchView);
						}
						metadataOffset += sizeof(ChunkPatch.Metadata);
					}
					currentPatchIndexInChunk = 0;
				}

				fileStream.Dispose();
				fileStream = null;

				IsCompleted = true;
				return this;
			}

			public void FillBuffers<TPoolsHolder>(TPoolsHolder* poolsHolder)
				where TPoolsHolder : unmanaged, IPoolsHolder
			{
				while (freePatchGroups.Count < 128) {
					freePatchGroups.Enqueue(poolsHolder->PatchGroupsPool.Rent());
				}
				while (freeSegments.Count < 256) {
					freeSegments.Enqueue(poolsHolder->SegmentsPool.Rent());
				}
			}

			public void ResetForReuse<TPoolsHolder>(TPoolsHolder* poolsHolder)
				where TPoolsHolder : unmanaged, IPoolsHolder
			{
				IsCompleted = false;
				isFirstTick = true;
				FillBuffers(poolsHolder);
				fileStream = null;
			}

			public interface IPoolsHolder
			{
				Pool<PatchesGroup> PatchGroupsPool { get; }
				ChainSegment<PatchView>.Pool SegmentsPool { get; }
			}
		}
	}

	public readonly struct RegionChangesLocation : IEquatable<RegionChangesLocation>, ILoacation
	{
		/// <summary>
		/// Region index for each axis of the world.
		/// </summary>
		public readonly ushort2 AxisIndices;

		ushort2 ILoacation.AxisIndices => AxisIndices;

		public RegionChangesLocation(ushort2 axisIndices) => AxisIndices = axisIndices;

		public override bool Equals(object obj) =>
			obj is RegionChangesLocation other && Equals(other);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(RegionChangesLocation other) => AxisIndices.Equals(other.AxisIndices);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => AxisIndices.GetHashCode();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(RegionChangesLocation left, RegionChangesLocation right) => left.Equals(right);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(RegionChangesLocation left, RegionChangesLocation right) => !left.Equals(right);

		public static RegionChangesLocation From(ChunkLocation chunkLocation) =>
			new RegionChangesLocation(new ushort2(
				x: (ushort)(chunkLocation.AxisIndices.x / RegionChanges.ChunkCountPerSide),
				y: (ushort)(chunkLocation.AxisIndices.y / RegionChanges.ChunkCountPerSide)
			));
	}
}
