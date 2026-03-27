using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Game.Collections
{
	using Allocator = Unity.Collections.Allocator;

	internal unsafe class Pool<T> where T : unmanaged
    {
		private readonly Queue<Pointer<byte>> allocatedBlocks = new();
		private readonly Queue<SlotInnerView> freeSlots = new();
		private readonly int itemCountPerAllocation;

		public Pool(int itemCountPerAllocation = 8)
		{
			if (itemCountPerAllocation <= 0) {
				throw new Exception($"Invalid itemCountPerAllocation {itemCountPerAllocation}");
			}
			this.itemCountPerAllocation = itemCountPerAllocation;
		}

		public Slot Rent()
		{
			if (freeSlots.TryDequeue(out var slotInnerView)) {
				return slotInnerView;
			}
			var memoryBlock = (InnerItem*)UnsafeUtility.Malloc(
				size: itemCountPerAllocation * sizeof(InnerItem),
				alignment: UnsafeUtility.AlignOf<InnerItem>(),
				allocator: Allocator.Persistent
			);
			allocatedBlocks.Enqueue((byte*)memoryBlock);
			for (var i = 1; i < itemCountPerAllocation; i++) {
				freeSlots.Enqueue(new SlotInnerView { Version = 0, InnerItem = memoryBlock + i });
			}
			return new SlotInnerView { Version = 0, InnerItem = memoryBlock };
		}

        public void Release(Slot slot)
        {
			SlotInnerView slotInnerView = slot;
			if (slotInnerView.InnerItem is null) {
				throw new Exception("Releasing an uninitialized slot");
			}
			ulong innerVersion = slotInnerView.InnerItem->Version;
			if (slotInnerView.Version != innerVersion) {
				throw new Exception("Invaled slot version");
			}
			slotInnerView.Version++;
			slotInnerView.InnerItem->Version = slotInnerView.Version;
			freeSlots.Enqueue(slotInnerView);
        }

        public void Destroy()
		{
			foreach (var block in allocatedBlocks) {
				UnsafeUtility.Free(memory: block, Allocator.Persistent);
			}
		}

		private struct InnerItem
		{
			public ulong Version;
			public T Item;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct SlotInnerView
		{
			public ulong Version;
			public InnerItem* InnerItem;
			
			public static implicit operator Slot(SlotInnerView slot) => *(Slot*)&slot;
			public static implicit operator SlotInnerView(Slot slot) => *(SlotInnerView*)&slot;
		}

		[StructLayout(LayoutKind.Sequential)]
		public readonly struct Slot
		{
			public readonly ulong Version;
			private readonly InnerItem* InnerItem;

			public readonly bool IsNull
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => InnerItem is null;
			}

			public T* ItemPointerUnchecked
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => &InnerItem->Item;
			}

			public T* ItemPointer => InnerItem is not null ? &InnerItem->Item : throw new Exception();

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static implicit operator T*(Slot slot) => slot.ItemPointer;
        }
	}
}
