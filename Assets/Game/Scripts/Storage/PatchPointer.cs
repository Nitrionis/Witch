using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Collections;

namespace Game.Storage
{
	
    internal readonly unsafe struct PatchPointer
    {
		private readonly SlotUnion slotsUnion;

		public readonly bool IsPartOfPatchSlotsGroup;

		public PatchPointer(Pool<SinglePatch>.Slot slot)
		{
			slotsUnion = new SlotUnion(slot);
			IsPartOfPatchSlotsGroup = false;
		}

		public PatchPointer(PatchesGroupSlotPart patchesGroupSlotPart)
		{
			slotsUnion = new SlotUnion(patchesGroupSlotPart);
			IsPartOfPatchSlotsGroup = true;
		}

		public void Release(IPoolsHolder poolsHolder)
		{
			if (IsPartOfPatchSlotsGroup) {
				poolsHolder.PatchGroupsPool.Release(slotsUnion.SlotsGroupPart.Group);
			} else {
				poolsHolder.PatchesPool.Release(slotsUnion.Slot);
			}
		}

		/// <summary>
		/// Increases or decreases the number of references to the patch.
		/// </summary>
		public int AddReferenceCount(int count)
		{
			if (IsPartOfPatchSlotsGroup) {
				PatchesGroupSlotPart part = slotsUnion.SlotsGroupPart;
				return part.Group.ItemPointer->ReferenceCount += count;
			} else {
				Pool<SinglePatch>.Slot slot = slotsUnion.Slot;
				return slot.ItemPointer->ReferenceCount += count;
			}
		}

		public int IncrementReferenceCount() => AddReferenceCount(1);
		public int DecrementReferenceCount () => AddReferenceCount(-1);

		public ChunkPatch* TypedPointer
		{
			get {
				if (IsPartOfPatchSlotsGroup) {
					PatchesGroupSlotPart part = slotsUnion.SlotsGroupPart;
					Repeat16<ChunkPatch>* slots = &part.Group.ItemPointer->Slots;
					return PointerArray.From(slots)[part.SlotIndexInGroup];
				} else {
					return &slotsUnion.Slot.ItemPointer->Patch;
				}
			} 
		}

		public bool IsNull
		{
			get => IsPartOfPatchSlotsGroup ? slotsUnion.SlotsGroupPart.Group.IsNull : slotsUnion.Slot.IsNull;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator ChunkPatch*(PatchPointer patchPointer) => patchPointer.TypedPointer;

		[StructLayout(LayoutKind.Explicit)]
		private readonly struct SlotUnion
		{
			[FieldOffset(0)]
			public readonly Pool<SinglePatch>.Slot Slot;

			[FieldOffset(0)]
			public readonly PatchesGroupSlotPart SlotsGroupPart;

			public SlotUnion(Pool<SinglePatch>.Slot slot)
			{
				SlotsGroupPart = default;
				Slot = slot;
			}

			public SlotUnion(PatchesGroupSlotPart slotsGroupPart)
			{
				Slot = default;
				SlotsGroupPart = slotsGroupPart;
			}
		}

		public readonly struct PatchesGroupSlotPart
		{
			public readonly byte SlotIndexInGroup;
			public readonly Pool<PatchesGroup>.Slot Group;

			public PatchesGroupSlotPart(byte slotIndexInGroup, Pool<PatchesGroup>.Slot group)
			{
				SlotIndexInGroup = slotIndexInGroup;
				Group = group;
			}
		}

		public struct SinglePatch
		{
			public int ReferenceCount;
			public ChunkPatch Patch;
		}

		public struct PatchesGroup
		{
			public const int PatchCountPerSide = 4;

			public ushort SlotStatuses;
			public int ReferenceCount;
			public Repeat16<ChunkPatch> Slots;
		}

		public interface IPoolsHolder
		{
			Pool<SinglePatch> PatchesPool { get; }
			Pool<PatchesGroup> PatchGroupsPool { get; }
		}
	}
}
