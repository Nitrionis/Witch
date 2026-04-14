
using Unity.Collections;

namespace Game.Allocators
{
	internal readonly struct SessionRewindableAllocator
	{
		private readonly AllocatorHelper<RewindableAllocator> helper;

		public readonly ref RewindableAllocator Allocator => ref helper.Allocator;
		public readonly AllocatorManager.AllocatorHandle Handle => Allocator.Handle;

		public SessionRewindableAllocator(int initialBlockSize)
		{
			this = default;
			helper = new AllocatorHelper<RewindableAllocator>(
				Unity.Collections.Allocator.Persistent
			);
			Allocator.Initialize(initialBlockSize, enableBlockFree: false);
		}

		public void Dispose() => DisposeRewindableAllocator();

		// Sample code to use rewindable allocator to allocate containers
		public unsafe void UseRewindableAllocator(out NativeArray<int> nativeArray, out NativeList<int> nativeList, out byte* bytePtr)
		{
			// Use rewindable allocator to allocate a native array, no need to dispose the array manually
			// CollectionHelper is required to create/allocate native array from a custom allocator.
			nativeArray = CollectionHelper.CreateNativeArray<int, RewindableAllocator>(100, ref Allocator);
			nativeArray[0] = 0xFE;

			// Use rewindable allocator to allocate a native list, do not need to dispose the list manually
			nativeList = new NativeList<int>(Handle);
			for (int i = 0; i < 50; i++) {
				nativeList.Add(i);
			}

			// Use custom allocator to allocate a byte buffer.
			bytePtr = (byte*)AllocatorManager.Allocate(ref Allocator, sizeof(byte), sizeof(byte), items: 10);
			bytePtr[0] = 0xAB;
		}

		private void DisposeRewindableAllocator()
		{
			// Dispose all the memory blocks in the rewindable allocator
			Allocator.Dispose();
			// Unregister the rewindable allocator and dispose it
			helper.Dispose();
		}
	}
}
