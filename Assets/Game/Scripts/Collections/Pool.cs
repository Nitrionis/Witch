using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Game.Allocators;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Game.Collections
{
	internal unsafe readonly struct Pool<T> where T : unmanaged
	{
		private readonly SessionRewindableAllocator poolsAllocator;
		private readonly int itemCountPerAllocation;
		private readonly NativeQueue<SlotInnerView> freeSlots;

		public Pool(
			DisposeList disposeList, in SessionRewindableAllocator poolsAllocator, int itemCountPerAllocation = 8)
		{
			if (itemCountPerAllocation <= 0) {
				throw new Exception($"Invalid itemCountPerAllocation {itemCountPerAllocation}");
			}
			this.itemCountPerAllocation = itemCountPerAllocation;
			this.poolsAllocator = poolsAllocator;
			freeSlots = new NativeQueue<SlotInnerView>(Allocator.Persistent);
			disposeList.Add(freeSlots);
		}

		public Slot Rent()
		{
			if (freeSlots.TryDequeue(out var slotInnerView)) {
				return slotInnerView;
			}
			var memoryBlock = (InnerItem*)AllocatorManager.Allocate(
				ref poolsAllocator.Allocator,
				sizeOf: sizeof(T),
				alignOf: UnsafeUtility.AlignOf<T>(),
				items: itemCountPerAllocation
			);
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
