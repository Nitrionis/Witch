using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Game.Collections
{
	public unsafe class NativeMemoryChain : IDisposable
	{
		private const int RealSectionSize = 4096;

		public static int SectionSize => RealSectionSize - sizeof(void*);

		private bool disposedValue;

		private int sectionCount;
		private IntPtr sections = IntPtr.Zero;

		private readonly Queue<IntPtr> freeSections = new();

		public void* Tail {  get; private set; }
		public void* Head { get; private set; }
		public int ChainLength { get; private set; }

		public NativeMemoryChain()
		{
			ResizeSections();
		}

		/// <summary>
		/// Tail (old section) -|-|-|-|- Head (new section)
		/// </summary>
		public void ConnectNextSection()
		{
			if (freeSections.Count == 0) {
				ResizeSections();
			}
			var nextSection = freeSections.Dequeue();
			if (nextSection == null) {
				throw new Exception("null from freeSections.Dequeue");
			}
			SetNextSection(nextSection, nextSection: IntPtr.Zero);
			if (ChainLength == 0) {
				Tail = (void*)nextSection;
			} else {
				SetNextSection((IntPtr)Head, nextSection);
			}
			Head = (void*)nextSection;
			ChainLength++;
			if (Head == null || Tail == null) {
				throw new Exception("Head == null || Tail == null");
			}
		}

		/// <summary>
		/// Tail (old section) -|-|-|-|- Head (new section)
		/// </summary>
		public void ReleaseTailSection()
		{
			if (ChainLength == 0) {
				throw new Exception("No section to release");
			}
			freeSections.Enqueue((IntPtr)Tail);
			Tail = GetNextSection((IntPtr)Tail);
			ChainLength--;
			if (ChainLength == 0) {
				Head = null;
				if (Tail != null) {
					throw new Exception("Invalid chain");
				}
			}
		}

		private static void* GetNextSection(IntPtr section) =>
			(void*)Unsafe.ReadUnaligned<IntPtr>(GetNextSectionSlot(section));

		private static void SetNextSection(IntPtr section, IntPtr nextSection) =>
			Unsafe.WriteUnaligned(GetNextSectionSlot(section), nextSection);

		private static void* GetNextSectionSlot(IntPtr section) => (byte*)section + SectionSize;

		private void ResizeSections()
		{
			int newSectionCount = Math.Max(4, 2 * sectionCount);
			var newSections = Marshal.AllocHGlobal(newSectionCount * sizeof(IntPtr));
			for (int i = 0; i < sectionCount; i++) {
				IntPtr section = Unsafe.ReadUnaligned<IntPtr>(source: (IntPtr*)sections + i);
				Unsafe.WriteUnaligned(destination: (IntPtr*)newSections + i, value: section);
			}
			for (int i = sectionCount; i < newSectionCount; i++) {
				var section = Marshal.AllocHGlobal(RealSectionSize);
				Unsafe.InitBlockUnaligned((void*)section, value: 0, RealSectionSize);
				Unsafe.WriteUnaligned(destination: (IntPtr*)newSections + i, value: section);
				freeSections.Enqueue(section);
			}
			if (sections != null) {
				Marshal.FreeHGlobal(sections);
			}
			sections = newSections;
			sectionCount = newSectionCount;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue) {
				disposedValue = true;
				for (int i = 0; i < sectionCount; i++) {
					Marshal.FreeHGlobal(Unsafe.ReadUnaligned<IntPtr>((IntPtr*)sections + i));
				}
				Marshal.FreeHGlobal(sections);
			}
		}

		~NativeMemoryChain()
		{
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
