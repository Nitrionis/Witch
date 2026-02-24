using System;
using System.IO;
using Unity.Mathematics;

namespace Game.Storage
{
	internal struct RegionPatches
    {
		public const int ChunckCount = 256;
		public const int ChunckCountPerGroup = 16;

		/// <summary>
		/// Chunks form a square region in world space.
		/// </summary>
		public const int ChunckCountBySide = 16;

		/// <summary>
		/// Represents one of 256 files.
		/// </summary>
		public struct File
		{
			public const int PatchCount = 256;
			public const int PatchCountPerGroup = 16;
			public const int PatchCountPerGroupSide = 4;

			public ushort RegionIndex;
			public byte FileIndex;
			public Repeat16<Repeat16<PatchPointer>> ChunkPatches;

			public static unsafe Pointer<Repeat16<PatchPointer>> GetGroup(GroupAddress address, File* file)
			{
				return PointerArray.From(&file->ChunkPatches)[address.GroupIndexInFile];
			}

			public static unsafe void LoadGroup(GroupAddress address, File* file, FileStream fileStream)
			{
				if (address.RegionIndex != file->RegionIndex) {
					throw new Exception("Invalid RegionIndex");
				}
				if (address.FileIndex != file->FileIndex) {
					throw new Exception("Invalid FileIndex");
				}
				UnmanagedArray<PatchPointer> group = UnmanagedArray.From(GetGroup(address, file).TypedPointer);

				bool isContiguousMemoryRegion = true;
				Repeat16<ChunkPatch>* patchGroupSlots = null;
				for (int i = 0; i < group.Length; i++) {
					isContiguousMemoryRegion &= group[i].IsPartOfPatchSlotsGroup;
					Repeat16<ChunkPatch>* slots = &group[i].SlotsGroupPart.Group.ItemPointer->Slots;
					if (patchGroupSlots == null) {
						patchGroupSlots = slots;
					} else if (patchGroupSlots != slots) {
						isContiguousMemoryRegion = false;
						break;
					}
				}
				if (!isContiguousMemoryRegion) {
					throw new System.Exception("!isContiguousMemoryRegion");
				}

				fileStream.Seek(offset: address.GroupIndexInFile * sizeof(Repeat16<ChunkPatch>), SeekOrigin.Begin);
				fileStream.Read(new Span<byte>(patchGroupSlots, length: sizeof(Repeat16<ChunkPatch>)));
			}

			public static unsafe void SaveGroup(Metadata* metadata, GroupAddress address, File* file, FileStream fileStream)
			{
				if (
					address.RegionIndex != file->RegionIndex ||
					metadata->RegionIndex != file->RegionIndex
				) {
					throw new Exception("Invalid RegionIndex");
				}
				if (address.FileIndex != file->FileIndex) {
					throw new Exception("Invalid FileIndex");
				}

				var group = UnmanagedArray.From(GetGroup(address, file).TypedPointer);
				var changedPatchesInFile = PointerArray.From(&metadata->ChangedPatchSlots)[address.FileIndex];
				ushort groupChanges = UnmanagedArray.From(changedPatchesInFile)[address.GroupIndexInFile];
				for (int chunkIndexInGroup = 0; chunkIndexInGroup < group.Length; chunkIndexInGroup++) {
					if ((groupChanges & (1 << chunkIndexInGroup)) == 0) {
						continue;
					}
					ChunkPatch* patch = group[chunkIndexInGroup];
					int offset = address.GroupIndexInFile * sizeof(Repeat16<ChunkPatch>) + chunkIndexInGroup * sizeof(ChunkPatch);
					fileStream.Seek(offset, SeekOrigin.Begin);
					fileStream.Write(new Span<byte>(patch, length: sizeof(ChunkPatch)));
				}
				// reset flags of changed patches
				UnmanagedArray.From(changedPatchesInFile)[address.GroupIndexInFile] = 0;
			}

			public readonly struct GroupAddress : IEquatable<GroupAddress>
			{
				/// <summary>
				/// Global Region Index.
				/// </summary>
				/// <remarks>The map is a square with 256 regions on each axis.</remarks>
				public readonly ushort RegionIndex;

				/// <summary>
				/// Each region can have up to 255 files.
				/// </summary>
				/// <remarks>File indexing starts from 1 to filter out default values.</remarks>
				public readonly byte FileIndex;

				/// <summary>
				/// Patches group index within region file.
				/// </summary>
				public readonly byte GroupIndexInFile;

				public bool IsDefault => this == default;

				public GroupAddress(ushort regionIndex, byte regionFileIndex, byte groupIndexInFile)
				{
					RegionIndex = regionIndex;
					FileIndex = regionFileIndex;
					GroupIndexInFile = groupIndexInFile;
				}

				public override bool Equals(object obj) =>
					obj is GroupAddress other && Equals(other);

				public bool Equals(GroupAddress other) =>
					RegionIndex == other.RegionIndex &&
					FileIndex == other.FileIndex &&
					GroupIndexInFile == other.GroupIndexInFile;

				public override int GetHashCode() =>
					HashCode.Combine(RegionIndex, FileIndex, GroupIndexInFile);

				public static bool operator ==(GroupAddress left, GroupAddress right) => left.Equals(right);
				public static bool operator !=(GroupAddress left, GroupAddress right) => !left.Equals(right);
			}
		}

		/// <summary>
		/// Stores metadata for all chunks in all region files.
		/// </summary>
		public struct Metadata
		{
			/// <summary>
			/// Global region index.Transformed directly from the region position.
			/// </summary>
			public ushort RegionIndex;
			
			/// <summary>
			/// Each of the 16 bits is responsible for one group within one of the region files.
			/// <para>0 - means a group with no free slots for patches</para>
			/// <para>1 - means a group with one or more free slots for patches</para>
			/// </summary>
			/// <remarks>Serializable data.</remarks>
			public Repeat256<ushort> PatchGroupStatuses;

			/// <summary>
			/// Each of the 16 bits is responsible for one of the 16 patch slots within the group.
			/// <para>1 - means free slot for patch</para>
			/// <para>0 - means obtained slot for patch</para>
			/// </summary>
			/// <remarks>Serializable data.</remarks>
			public Repeat256<Repeat16<ushort>> PatchSlotStatuses;

			/// <summary>
			/// Metadata for all patches for all files in one region.
			/// </summary>
			/// <remarks>Serializable data.</remarks>
			public Repeat256<Repeat16<ChunkPatch.Metadata>> ChunkPatchesMetadata;

			/// <summary>
			/// Each bit in ChangedPatchSlots corresponds to a corresponding bit in PatchSlotStatuses.
			/// <para>0 - means a slot with unchenged data</para>
			/// <para>1 - means a slot with modified data</para>
			/// </summary>
			public Repeat256<Repeat16<ushort>> ChangedPatchSlots;

			public static unsafe bool TryAcquirePatchSlot(Metadata* metadata, out ChunkPatch.Address address)
			{
				address = default;
				UnmanagedArray<ushort> groupsInAllFiles = UnmanagedArray.From(&metadata->PatchGroupStatuses);
				int fileIndex = -1;
				ushort fileGroups = 0;
				for (int i = 0; i < groupsInAllFiles.Length; i++) {
					fileGroups = groupsInAllFiles[i];
					if (fileGroups != 0) {
						fileIndex = i;
						break;
					}
				}
				if (fileIndex < 0)
					return false;
				int groupIndex = GetMsbIndex(fileGroups);
				var groupAddress = new File.GroupAddress(metadata->RegionIndex, (byte)fileIndex, (byte)groupIndex);
				groupsInAllFiles[fileIndex] = (ushort)(fileGroups & ~(1 << groupIndex));

				Pointer<ushort> groupSlotStatuses = GetGroupStatusesOrChanges(
					&metadata->PatchSlotStatuses, groupAddress
				);
				int slotIndexInGroup = GetMsbIndex(groupSlotStatuses.Value);
				groupSlotStatuses.Value &= (ushort)~(1 << slotIndexInGroup);

				Pointer<ushort> groupSlotChanges = GetGroupStatusesOrChanges(
					&metadata->ChangedPatchSlots, groupAddress
				);
				groupSlotChanges.Value |= (ushort)(1 << slotIndexInGroup);

				address = new ChunkPatch.Address(metadata->RegionIndex, (byte)fileIndex, (byte)groupIndex, (byte)slotIndexInGroup);
				return true;
			}

            private static int GetMsbIndex(int value) => 31 - math.lzcnt((uint)value);

            public static unsafe void ReleasePatchSlot(Metadata* metadata, ChunkPatch.Address address)
			{
				var groupAddress = address.GroupAddress;
				if (groupAddress.RegionIndex != metadata->RegionIndex) {
					throw new System.Exception("Invalid region");
				}

				Pointer<ushort> groupPatchSlotChanges = GetGroupStatusesOrChanges(
					&metadata->ChangedPatchSlots, groupAddress
				);
				groupPatchSlotChanges.Value &= (ushort)~(1 << address.SlotIndexInGroup);

				Pointer<ushort> groupPatchSlotStatuses = GetGroupStatusesOrChanges(
					&metadata->PatchSlotStatuses, groupAddress
				);
				groupPatchSlotStatuses.Value |= (ushort)(1 << address.SlotIndexInGroup);

				UnmanagedArray<ushort> groupsInAllFiles = UnmanagedArray.From(&metadata->PatchGroupStatuses);
				groupsInAllFiles[groupAddress.FileIndex] |= (ushort)(1 << groupAddress.GroupIndexInFile);
			}

			private static unsafe Pointer<ushort> GetGroupStatusesOrChanges(
				Repeat256<Repeat16<ushort>>* data, File.GroupAddress address
			) {
				PointerArray<Repeat16<ushort>> patchSlotStatusesOrChangesPerFile = PointerArray.From(data);
				return PointerArray.From(patchSlotStatusesOrChangesPerFile[address.FileIndex])[address.GroupIndexInFile];
			}
		}
	}
}
