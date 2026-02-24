using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Game.Collections
{
	using Allocator = Unity.Collections.Allocator;

	internal unsafe class Pool<T> where T : unmanaged
    {
		private readonly Queue<Pointer<byte>> allocatedBlocks = new();
		private readonly Queue<SlotInnerView> freeSlots = new();

		public Slot Acquire()
		{
			if (freeSlots.TryDequeue(out var slotInnerView)) {
				return slotInnerView;
			}
			const int ItemCountPerAllocation = 8;
			var memoryBlock = (InnerItem*)UnsafeUtility.Malloc(
				size: ItemCountPerAllocation * sizeof(InnerItem),
				alignment: UnsafeUtility.AlignOf<InnerItem>(),
				allocator: Allocator.Persistent
			);
			allocatedBlocks.Enqueue(memoryBlock);
			for (var i = 1; i < ItemCountPerAllocation; i++) {
				freeSlots.Enqueue(new SlotInnerView { Version = 0, InnerItem = memoryBlock + i });
			}
			return new SlotInnerView { Version = 0, InnerItem = memoryBlock };
		}

        public void Release(Slot slot)
        {
			SlotInnerView slotInnerView = slot;
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

			public T* ItemPointer => &InnerItem->Item;

			public static implicit operator T*(Slot slot) => &slot.InnerItem->Item;
        }
	}
}
