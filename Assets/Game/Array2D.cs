using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Assets.Game
{
	internal struct NativeArray2D
	{
		public struct WithChunk4x4<T> where T : unmanaged
		{
			public const int ItemCountPerBlock = 16;

			private int blockCountPerRow;
			private NativeArray<T> data;
			public NativeArray<T> Data => data;

			public T this[int x, int y]
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => data[GetIndex(x, y)];
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				set => data[GetIndex(x, y)] = value;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private int GetIndex(int x, int y)
			{
				int indexInBlock = (y & 3) * 4 + (x & 3);
				int blockIndex = (y >> 2) * blockCountPerRow + (x >> 2);
				return blockIndex * ItemCountPerBlock + indexInBlock;
			}

			public WithChunk4x4(NativeArray<T> data, int itemCountPerRow)
			{
				this.data = data;
				if (itemCountPerRow % 4 != 0) {
					throw new System.ArgumentException("Invalid row length");
				}
				if (data.Length % itemCountPerRow != 0) {
					throw new System.ArgumentException("Invalid row count");
				}
				blockCountPerRow = itemCountPerRow / 4;
			}
		}
	}

	internal struct Array2D
	{
		public struct WithChunk4x4<T>
		{
			private int blockCountPerRow;
			private T[] data;
			public T[] Data => data;

			public T this[int x, int y]
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => data[GetIndex(x, y)];
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				set => data[GetIndex(x, y)] = value;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private int GetIndex(int x, int y)
			{
				int indexInBlock = (x & 3) + (y & 3) * 4;
				int blockIndex = (x >> 2) + (y >> 2) * blockCountPerRow;
				return blockIndex * 16 + indexInBlock;
			}

			public WithChunk4x4(T[] data, int itemCountPerRow)
			{
				this.data = data;
				if (itemCountPerRow % 4 != 0) {
					throw new System.ArgumentException("Invalid row length");
				}
				if (data.Length % itemCountPerRow != 0) {
					throw new System.ArgumentException("Invalid row count");
				}
				blockCountPerRow = itemCountPerRow / 4;
			}
		}
	}
}
