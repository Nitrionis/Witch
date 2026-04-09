using System;
using System.Runtime.InteropServices;

namespace Game.Collections
{
	internal unsafe class DisposableMemoryBlocks : IDisposable
	{
		private int allocatedBlockCount;
		private int allocatedBlocksCapacity;
		private IntPtr* allocatedBlocks;

		public DisposableMemoryBlocks()
		{
			allocatedBlocksCapacity = 128;
			allocatedBlocks = (IntPtr*)Marshal.AllocHGlobal(
				allocatedBlocksCapacity * sizeof(IntPtr)
			);
		}

		~DisposableMemoryBlocks() => Dispose(disposing: false);

		public void Dispose() => Dispose(disposing: true);

		private void Dispose(bool disposing)
		{
			if (disposing) {
				GC.SuppressFinalize(this);
			}
			if (allocatedBlocks == null) {
				return;
			}
			for (int i = 0; i < allocatedBlockCount; i++) {
				Marshal.FreeHGlobal(allocatedBlocks[i]);
			}
			Marshal.FreeHGlobal((IntPtr)allocatedBlocks);
			allocatedBlocks = null;
		}

		public void Add(IntPtr memoryBlock)
		{
			if (allocatedBlockCount >= allocatedBlocksCapacity) {
				ResizeAllocatedBlocks();
			}
			allocatedBlocks[allocatedBlockCount++] = memoryBlock;
		}

		private void ResizeAllocatedBlocks()
		{
			allocatedBlocksCapacity *= 2;
			var tmp = (IntPtr*)Marshal.AllocHGlobal(allocatedBlocksCapacity * sizeof(IntPtr));
			for (int i = 0; i < allocatedBlockCount; i++) {
				tmp[i] = allocatedBlocks[i];
			}
			Marshal.FreeHGlobal((IntPtr)allocatedBlocks);
			allocatedBlocks = tmp;
		}
	}
}
