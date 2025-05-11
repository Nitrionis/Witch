using System;
using System.Runtime.CompilerServices;
using static Assets.Game.NitroEcs.IStandalonePool;

namespace Assets.Game.NitroEcs
{
	public interface IStandalonePool
	{
		void Release(Descriptor descriptor);

		public readonly struct Descriptor
		{
			public readonly ushort Value;
			public Descriptor(ushort value) => Value = value;
		}
	}

	public class StandalonePool<T> : IStandalonePool where T : struct
	{
		private T[] slots;

		private ushort[] freeSlots;
		private int head;
		private int tail;
		private int size;

		public StandalonePool()
		{
			slots = new T[128];
			freeSlots = new ushort[slots.Length];
			for (int i = 0; i < slots.Length; i++) {
				freeSlots[i] = (ushort)i;
			}
			head = 0;
			tail = 0;
			size = freeSlots.Length;
		}

		public ref T Rent(out Descriptor descriptor)
		{
			descriptor = new Descriptor(Dequeue());
			return ref slots[descriptor.Value];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ushort Dequeue()
		{
			if (size == 0) {
				ResizeQueueAndSlots();
			}
			ushort removed = freeSlots[head];
			head = (head + 1) % freeSlots.Length;
			size--;
			return removed;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref T Get(Descriptor descriptor) => ref slots[descriptor.Value];

		public void Release(Descriptor descriptor)
		{
			Enqueue(descriptor.Value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Enqueue(ushort item)
		{
			freeSlots[tail] = item;
			tail = (tail + 1) % freeSlots.Length;
			size++;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ResizeQueueAndSlots()
		{
			Tools.ThreadSafeResize(ref slots, 2 * slots.Length);
			size = freeSlots.Length;
			Array.Resize(ref freeSlots, 2 * freeSlots.Length);
			head = 0;
			tail = size;
			for (int i = 0; i < size; i++) {
				freeSlots[i] = (ushort)(size + i);
			}
		}
	}
}
