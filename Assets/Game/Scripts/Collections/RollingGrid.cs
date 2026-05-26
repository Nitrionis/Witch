using System;
using System.Runtime.CompilerServices;
using Game.Allocators;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Game.Collections
{
	public interface IRollingGridItem
	{
		void Release();
	}

	public readonly unsafe struct RollingGrid<T>
		where T : unmanaged, IRollingGridItem
	{
		private readonly RollingGridData* data;

		public int2 Offset
		{
			get => data->offset;
			set
			{
				data->Shift(value);
				data->offset = value;
			}
		}

		/// <summary>
		/// Gets or sets the value associated with the specified key.
		/// </summary>
		public readonly T this[int2 key]
		{
			get => data->Get(key);
			set => data->Set(key, value);
		}

		internal RollingGrid(int sideLength, in SessionRewindableAllocator allocator)
		{
			data = (RollingGridData*)AllocatorManager.Allocate(
				ref allocator.Allocator,
				sizeOf: sizeof(RollingGridData),
				alignOf: UnsafeUtility.AlignOf<RollingGridData>(),
				items: 1
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool IsInBounds(int2 position) => data->IsInBounds(position);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool TryGet(int2 position, out T item) => data->TryGet(position, out item);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool TryReplace(int2 position, T item, out T originalItem) => data->TryReplace(position, item, out originalItem);

		private struct RollingGridData
		{
			private readonly UnmanagedArray<T> buffer;
			private readonly int sideLength;

			public int2 offset;

			public RollingGridData(int sideLength, in SessionRewindableAllocator allocator)
			{
				this.sideLength = sideLength;
				offset = 0;
				int length = sideLength * sideLength;
				var bufferDataPtr = (T*)AllocatorManager.Allocate(
					ref allocator.Allocator,
					sizeOf: sizeof(T),
					alignOf: UnsafeUtility.AlignOf<T>(),
					items: length
				);
				buffer = new UnmanagedArray<T>(bufferDataPtr, length);
			}

			public readonly bool TryReplace(int2 position, T item, out T originalItem)
			{
				originalItem = default;
				if (IsInBounds(position)) {
					int index = GetIndex(position.x, position.y);
					originalItem = buffer[index];
					buffer[index] = item;
					return true;
				}
				return false;
			}

			public readonly bool TryGet(int2 position, out T item)
			{
				item = default;
				if (IsInBounds(position)) {
					item = buffer[GetIndex(position.x, position.y)];
					return true;
				}
				return false;
			}

			public readonly T Get(int2 key)
			{
				if (!IsInBounds(key))
					throw new ArgumentOutOfRangeException($"Key {key} is outside the current grid bounds.");
				return buffer[GetIndex(key.x, key.y)];
			}
			
			public readonly void Set(int2 key, T value)
			{
				if (!IsInBounds(key))
					throw new ArgumentOutOfRangeException(nameof(key), $"Key {key} is outside the current grid bounds.");
				buffer[GetIndex(key.x, key.y)] = value;
			}

			public readonly void Shift(int2 newOffset)
			{
				int2 shift = newOffset - offset;
				if (math.cmax(math.abs(shift)) == 1) {
					if (shift.x != 0) {
						int colToDispose = offset.x + shift.x > 0 ? 0 : sideLength - 1;
						for (int y = offset.y; y < sideLength + offset.y; y++) {
							int index = GetIndex(colToDispose, y);
							buffer[index].Release();
							buffer[index] = default;
						}
					}
					if (shift.y != 0) {
						int rowToDispose = offset.y + shift.y > 0 ? 0 : sideLength - 1;
						for (int x = offset.x; x < sideLength + offset.x; x++) {
							int index = GetIndex(x, rowToDispose);
							buffer[index].Release();
							buffer[index] = default;
						}
					}
				} else {
					if (shift.Equals(int2.zero)) {
						return;
					}
					for (int x = 0; x < sideLength; x++) {
						for (int y = 0; y < sideLength; y++) {
							if (!IsInBounds(new int2(x, y))) {
								int index = GetIndex(x, y);
								buffer[index].Release();
								buffer[index] = default;
							}
						}
					}
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public readonly bool IsInBounds(int2 offset)
			{
				// Cache struct field in a local to avoid repeated field loads.
				// The JIT often does this anyway, but it's cheap insurance.
				int2 localOffset = this.offset;
				uint side = (uint)sideLength;

				return (uint)(offset.x - localOffset.x) < side
					&& (uint)(offset.y - localOffset.y) < side;
			}

			private readonly int GetIndex(int x, int y)
			{
				// Apply origin offsets (modulo arithmetic for ring buffer)
				x = (x + sideLength) % sideLength;
				y = (y + sideLength) % sideLength;
				return x + y * sideLength;
			}
		}
	}
}
