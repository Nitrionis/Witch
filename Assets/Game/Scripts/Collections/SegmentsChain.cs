using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Game.Collections
{


	internal readonly unsafe struct Segment<TItems, TItem>
		where TItems : unmanaged, IRepeat<TItem>
		where TItem : unmanaged
	{
		private readonly Pool<DataChunk>.Slot slot;

		private Segment(Pool<DataChunk>.Slot slot) => this.slot = slot;

		public readonly bool IsNull
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => slot.IsNull;
		}

		public void ReleaseChain(Pool pool)
		{
			var segment = this;
			while (!segment.IsNull) {
				var next = segment.slot.ItemPointerUnchecked->Next;
				pool.Return(next);
				segment = next;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public EnumerableChain EnumerateChain() => new EnumerableChain(this);

		public struct EnumerableChain : IEnumerable<TItem>
		{
			public Segment<TItems, TItem> FirstSegment;
			public EnumerableChain(Segment<TItems, TItem> firstSegment) => FirstSegment = firstSegment;

			public Enumerator GetEnumerator() => new Enumerator(FirstSegment);
			IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TItem>)this).GetEnumerator();

			public struct Enumerator : IEnumerator<TItem>
			{
				private int itemIndex;
				private DataChunk* dataChunk;

				public TItem Current { get; private set; }
				object IEnumerator.Current => Current;

				public Enumerator(Segment<TItems, TItem> currentSegment) : this()
				{
					if (currentSegment.IsNull) {
						throw new System.Exception("Enumeration of an uninitialized segment");
					}
					dataChunk = currentSegment.slot.ItemPointerUnchecked;
				}

				public bool MoveNext()
				{
					if (itemIndex < dataChunk->ItemCount) {
						Current = ((TItem*)&dataChunk->Items)[itemIndex];
						itemIndex++;
						return true;
					}
					return TryGetItemInNextSegments();
				}

				[MethodImpl(MethodImplOptions.NoInlining)]
				private bool TryGetItemInNextSegments()
				{
					while (!dataChunk->Next.IsNull) {
						dataChunk = dataChunk->Next.slot.ItemPointerUnchecked;
						if (dataChunk->ItemCount > 0) {
							Current = ((TItem*)&dataChunk->Items)[itemIndex];
							itemIndex = 1;
							return true;
						}
					}
					return false;
				}

				public void Reset() { }
				public void Dispose() { }
			}
		}

		private struct DataChunk
		{
			public Segment<TItems, TItem> Next;
			public int ItemCount;
			public TItems Items;
		}

		public struct ChainBuilder
		{
			private Segment<TItems, TItem> first;
			private Segment<TItems, TItem> last;

			public Segment<TItems, TItem> First => first;

			/// <summary>
			/// Allows you to continue a chain from its last element.
			/// </summary>
			/// <remarks>The default constructor is also valid.</remarks>
			public ChainBuilder(Segment<TItems, TItem> chainStart)
			{
				first = chainStart;
				last = first;
				if (last.IsNull) {
					return;
				}
				Segment<TItems, TItem> next;
				while (!(next = last.slot.ItemPointerUnchecked->Next).IsNull) {
					last = next;
				}
			}

			public void Add(TItem item, Pool pool)
			{
				if (last.IsNull) {
					first = pool.Rent();
					last = first;
					last.slot.ItemPointerUnchecked->ItemCount = 0;
				}
				var dataChunk = last.slot.ItemPointerUnchecked;
				int itemCount = dataChunk->ItemCount;
				if (itemCount < dataChunk->Items.Length) {
					((TItem*)&dataChunk->Items)[itemCount] = item;
					dataChunk->ItemCount = itemCount + 1;
					return;
				}
				AddItemToNewSegment(item, pool);
			}

			public bool TryAddNoResize(TItem item)
			{
				if (last.IsNull) {
					return false;
				}
				var dataChunk = last.slot.ItemPointerUnchecked;
				int itemCount = dataChunk->ItemCount;
				if (itemCount < dataChunk->Items.Length) {
					((TItem*)&dataChunk->Items)[itemCount] = item;
					dataChunk->ItemCount = itemCount + 1;
					return true;
				}
				return false;
			}

			public void ConnectNextSegment(Segment<TItems, TItem> segment)
			{
				if (last.IsNull) {
					first = segment;
					last = first;
					last.slot.ItemPointerUnchecked->ItemCount = 0;
					return;
				}
				var dataChunk = last.slot.ItemPointerUnchecked;
				dataChunk->Next = segment;
				dataChunk->ItemCount = 0;
				last = segment;
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			private void AddItemToNewSegment(TItem item, Pool pool)
			{
				var segment = pool.Rent();
				last.slot.ItemPointerUnchecked->Next = segment;
				last = segment;
				var dataChunk = last.slot.ItemPointerUnchecked;
				int itemCount = dataChunk->ItemCount;
				((TItem*)&dataChunk->Items)[itemCount] = item;
				dataChunk->ItemCount = 1;
			}
		}

		public class Pool : IDisposable
		{
			private readonly Pool<DataChunk> innerPool;

			public Pool(int itemCountPerAllocation) => innerPool =
				new Pool<DataChunk>(itemCountPerAllocation);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Segment<TItems, TItem> Rent() => new Segment<TItems, TItem>(innerPool.Rent());

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Return(Segment<TItems, TItem> segment) => innerPool.Release(segment.slot);

			public void Dispose() => innerPool.Dispose();
		}
	}
}
