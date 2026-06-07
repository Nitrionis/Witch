using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Game.Allocators;

namespace Game.Collections
{
	internal readonly unsafe struct ChainSegment<TItem> where TItem : unmanaged
	{
		private readonly Pool<DataChunk>.Slot slot;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ChainSegment(Pool<DataChunk>.Slot slot) => this.slot = slot;

		public readonly bool IsNull
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => slot.IsNull;
		}

		public readonly bool IsNotNull
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => !slot.IsNull;
		}

		public void ReleaseChain(Pool pool)
		{
			var segment = this;
			while (!segment.IsNull) {
				var next = segment.slot.PointerUnchecked->Next;
				pool.Return(next);
				segment = next;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public EnumerableChain EnumerateChain() => new EnumerableChain(this);

		public struct EnumerableChain : IEnumerable<TItem>
		{
			public ChainSegment<TItem> FirstSegment;
			public EnumerableChain(ChainSegment<TItem> firstSegment) => FirstSegment = firstSegment;

			public Enumerator GetEnumerator() => new Enumerator(FirstSegment);
			IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TItem>)this).GetEnumerator();

			public struct Enumerator : IEnumerator<TItem>
			{
				private int itemIndex;
				private DataChunk* dataChunk;

				public TItem Current { get; private set; }
				object IEnumerator.Current => Current;

				public Enumerator(ChainSegment<TItem> currentSegment) : this()
				{
					if (currentSegment.IsNull) {
						throw new System.Exception("Enumeration of an uninitialized segment");
					}
					dataChunk = currentSegment.slot.PointerUnchecked;
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
						dataChunk = dataChunk->Next.slot.PointerUnchecked;
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
			public ChainSegment<TItem> Next;
			public int ItemCount;
			public Repeat8<TItem> Items;
		}

		public struct ChainBuilder
		{
			private ChainSegment<TItem> first;
			private ChainSegment<TItem> last;

			public ChainSegment<TItem> First => first;

			/// <summary>
			/// Allows you to continue a chain from its last element.
			/// </summary>
			/// <remarks>The default constructor is also valid.</remarks>
			public ChainBuilder(ChainSegment<TItem> chainStart)
			{
				first = chainStart;
				last = first;
				if (last.IsNull) {
					return;
				}
				ChainSegment<TItem> next;
				while (!(next = last.slot.PointerUnchecked->Next).IsNull) {
					last = next;
				}
			}

			public void Add(TItem item, in Pool pool)
			{
				if (last.IsNull) {
					first = pool.Rent();
					last = first;
					last.slot.PointerUnchecked->ItemCount = 0;
				}
				var dataChunk = last.slot.PointerUnchecked;
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
				var dataChunk = last.slot.PointerUnchecked;
				int itemCount = dataChunk->ItemCount;
				if (itemCount < dataChunk->Items.Length) {
					((TItem*)&dataChunk->Items)[itemCount] = item;
					dataChunk->ItemCount = itemCount + 1;
					return true;
				}
				return false;
			}

			public void ConnectNextSegment(ChainSegment<TItem> segment)
			{
				if (last.IsNull) {
					first = segment;
					last = first;
					last.slot.PointerUnchecked->ItemCount = 0;
					return;
				}
				var dataChunk = last.slot.PointerUnchecked;
				dataChunk->Next = segment;
				dataChunk->ItemCount = 0;
				last = segment;
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			private void AddItemToNewSegment(TItem item, Pool pool)
			{
				var segment = pool.Rent();
				last.slot.PointerUnchecked->Next = segment;
				last = segment;
				var dataChunk = last.slot.PointerUnchecked;
				int itemCount = dataChunk->ItemCount;
				((TItem*)&dataChunk->Items)[itemCount] = item;
				dataChunk->ItemCount = 1;
			}
		}

		public readonly struct Pool
		{
			private readonly Pool<DataChunk> pool;

			public Pool(DisposeList disposeList, in SessionRewindableAllocator poolsAllocator, int itemCountPerAllocation) =>
				pool = new Pool<DataChunk>(disposeList, in poolsAllocator, itemCountPerAllocation);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ChainSegment<TItem> Rent() => new ChainSegment<TItem>(pool.Rent());

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Return(ChainSegment<TItem> segment) => pool.Release(segment.slot);
		}
	}
}
