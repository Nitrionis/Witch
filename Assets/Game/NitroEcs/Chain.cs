using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Assets.Game.NitroEcs
{
	public readonly struct EntityDescriptorPair
	{
		public readonly ushort EntityIndex;
		public readonly IStandalonePool.Descriptor Descriptor;

		public EntityDescriptorPair(ushort entityIndex, IStandalonePool.Descriptor descriptor)
		{
			EntityIndex = entityIndex;
			Descriptor = descriptor;
		}
	}

	public class Chain
	{
		private Item[] items;
		private Segment[] freeSegments;
		private int freeSegmentCount;

		public int Capacity => items.Length;

		[IndexerName("TheItem")]
		public Item this[Segment segment, int localItemIndex]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => items[segment.Index * Segment.Length + localItemIndex];
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => items[segment.Index * Segment.Length + localItemIndex] = value;
		}

		[IndexerName("TheItem")]
		public Item this[Caret caret]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => items[caret.Head.Index * Segment.Length + caret.LocalItemIndex];
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => items[caret.Head.Index * Segment.Length + caret.LocalItemIndex] = value;
		}

		public Chain()
		{
			freeSegmentCount = 128;
			freeSegments = new Segment[freeSegmentCount];
			for (int i = 0; i < freeSegments.Length; i++) {
				freeSegments[i] = new Segment(index: (ushort)i);
			}
			items = new Item[freeSegments.Length * Segment.Length];
		}

		public Segment Rent()
		{
			if (freeSegmentCount == 0) {
				Resize();
			}
			return freeSegments[--freeSegmentCount];
		}

		public void Release(Segment segment)
		{
			freeSegments[freeSegmentCount++] = segment;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void Resize()
		{
			// required because it allows reading data from multiple threads
			Tools.ThreadSafeResize(ref items, 2 * items.Length);
			int length = freeSegments.Length;
			Array.Resize(ref freeSegments, 2 * freeSegments.Length);
			for (int i = 0; i < length; i++) {
				freeSegments[i] = new Segment(index: (ushort)(i + length));
			}
			freeSegmentCount = length;
		}

		public struct Caret
		{
			public Segment Head;
			public ushort LocalItemIndex;

			public bool IsLastItemInSegment => LocalItemIndex == Segment.Length;

			public void MoveToNextSegment(Segment nextSegment)
			{
				Head = nextSegment;
				LocalItemIndex = 0;
			}
		}

		public readonly struct Segment
		{
			public const int Length = 32;
			public const int LastItemIndex = Length - 1;

			public readonly ushort Index;

			public Segment(ushort index) => Index = index;
		}

		[StructLayout(LayoutKind.Explicit)]
		public readonly struct Item
		{
			[FieldOffset(0)]
			public readonly Entity AsEntity;

			[FieldOffset(0)]
			public readonly EntityDescriptorPair AsEntityDescriptorPair;

			[FieldOffset(0)]
			public readonly Segment AsSegment;

			[FieldOffset(0)]
			public readonly uint AsNumber;

			public Item(Entity entity) : this() => AsEntity = entity;
			public Item(EntityDescriptorPair pair) : this() => AsEntityDescriptorPair = pair;
			public Item(Segment segment) : this() => AsSegment = segment;
			private Item(uint number) : this() => AsNumber = number;

			public static implicit operator Item(Entity entity) => new Item(entity);
			public static implicit operator Item(EntityDescriptorPair pair) => new Item(pair);
			public static implicit operator Item(Segment segment) => new Item(segment);
			public static implicit operator Item(uint number) => new Item(number);
		}
	}
}
