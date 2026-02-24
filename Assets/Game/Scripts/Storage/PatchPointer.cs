using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Collections;

namespace Game.Storage
{
	
    internal readonly unsafe struct PatchPointer
    {
		private readonly SlotUnion slotsUnion;

		public readonly bool IsPartOfPatchSlotsGroup;

		public Pool<PatchSlot>.Slot Slot
		{
			get {
				if (IsPartOfPatchSlotsGroup) {
					throw new System.Exception("Invalid slot cast");
				}
				return slotsUnion.Slot;
			} 
		}

		public PatchesGroupSlotPart SlotsGroupPart
		{
			get {
				if (!IsPartOfPatchSlotsGroup) {
					throw new System.Exception("Invalid slot cast");
				}
				return slotsUnion.SlotsGroupPart;
			}
		}

		public PatchPointer(Pool<PatchSlot>.Slot slot)
		{
			if (slot.ItemPointer == null) {
				throw new System.Exception("PatchPointer: null slot is not supported");
			}
			slotsUnion = new SlotUnion(slot);
			IsPartOfPatchSlotsGroup = false;
		}

		public PatchPointer(PatchesGroupSlotPart patchesGroupSlotPart)
		{
			if (patchesGroupSlotPart.Group.ItemPointer == null) {
				throw new System.Exception("PatchPointer: null slot is not supported");
			}
			slotsUnion = new SlotUnion(patchesGroupSlotPart);
			IsPartOfPatchSlotsGroup = true;
		}

		/// <summary>
		/// Increases or decreases the number of references to the patch.
		/// </summary>
		public void AddReferenceCount(int count)
		{
			if (IsNull) {
				throw new System.Exception("Null PatchPointer");
			}
			if (IsPartOfPatchSlotsGroup) {
				PatchesGroupSlotPart part = slotsUnion.SlotsGroupPart;
				part.Group.ItemPointer->ReferenceCount += count;
			} else {
				Pool<PatchSlot>.Slot slot = slotsUnion.Slot;
				slot.ItemPointer->ReferenceCount += count;
			}
		}

		public ChunkPatch* TypedPointer
		{
			get {
				if (IsPartOfPatchSlotsGroup) {
					PatchesGroupSlotPart part = SlotsGroupPart;
					var itemPointer = part.Group.ItemPointer;
					if (itemPointer == null) {
						return null;
					}
					Repeat16<ChunkPatch>* slots = &itemPointer->Slots;
					return PointerArray.From(slots)[part.SlotIndexInGroup];
				} else {
					if (Slot.ItemPointer == null) {
						return null;
					}
					return &Slot.ItemPointer->Patch;
				}
			} 
		}

		public bool IsNull => TypedPointer == null;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator ChunkPatch*(PatchPointer patchPointer) => patchPointer.TypedPointer;

		[StructLayout(LayoutKind.Explicit)]
		private readonly struct SlotUnion
		{
			[FieldOffset(0)]
			public readonly Pool<PatchSlot>.Slot Slot;

			[FieldOffset(0)]
			public readonly PatchesGroupSlotPart SlotsGroupPart;

			public SlotUnion(Pool<PatchSlot>.Slot slot)
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
	}

	internal readonly struct PatchesGroupSlotPart
	{
		public readonly byte SlotIndexInGroup;
		public readonly Pool<PatchesGroup>.Slot Group;

		public PatchesGroupSlotPart(byte slotIndexInGroup, Pool<PatchesGroup>.Slot group)
		{
			SlotIndexInGroup = slotIndexInGroup;
			Group = group;
		}
	}

	internal struct PatchSlot
	{
		public int ReferenceCount;
		public ChunkPatch Patch;
	}

	internal struct PatchesGroup
	{
        public ushort SlotStatuses;
		public int ReferenceCount;
		public Repeat16<ChunkPatch> Slots;
	}
}
