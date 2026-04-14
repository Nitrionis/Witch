
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Game
{
	internal struct UnmanagedArray
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat2<T>* array) where T : unmanaged => new((T*)array, 2);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat4<T>* array) where T : unmanaged => new((T*)array, 4);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat8<T>* array) where T : unmanaged => new((T*)array, 8);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat16<T>* array) where T : unmanaged => new((T*)array, 16);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat32<T>* array) where T : unmanaged => new((T*)array, 32);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat64<T>* array) where T : unmanaged => new((T*)array, 64);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat128<T>* array) where T : unmanaged => new((T*)array, 128);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat256<T>* array) where T : unmanaged => new((T*)array, 256);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat512<T>* array) where T : unmanaged => new((T*)array, 512);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat1024<T>* array) where T : unmanaged => new((T*)array, 1024);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat2048<T>* array) where T : unmanaged => new((T*)array, 2048);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat4096<T>* array) where T : unmanaged => new((T*)array, 4096);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat8192<T>* array) where T : unmanaged => new((T*)array, 8192);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat16384<T>* array) where T : unmanaged => new((T*)array, 16384);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat32768<T>* array) where T : unmanaged => new((T*)array, 32768);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat65536<T>* array) where T : unmanaged => new((T*)array, 65536);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe UnmanagedArray<T> From<T>(Repeat513x513<T>* array) where T : unmanaged => new((T*)array, 513 * 513);

		public struct SquareView
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat4<T>* array) where T : unmanaged => new((T*)array, sideLength: 2);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat16<T>* array) where T : unmanaged => new((T*)array, sideLength: 4);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat64<T>* array) where T : unmanaged => new((T*)array, sideLength: 8);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat256<T>* array) where T : unmanaged => new((T*)array, sideLength: 16);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat1024<T>* array) where T : unmanaged => new((T*)array, sideLength: 32);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat4096<T>* array) where T : unmanaged => new((T*)array, sideLength: 64);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat16384<T>* array) where T : unmanaged => new((T*)array, sideLength: 128);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat65536<T>* array) where T : unmanaged => new((T*)array, sideLength: 256);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat513x513<T>* array) where T : unmanaged => new((T*)array, sideLength: 513);
		}
	}

	internal readonly unsafe struct UnmanagedArray<T> : IEnumerable<T> where T : unmanaged
	{
		private readonly T* pointer;
		public readonly int Length;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public UnmanagedArray(T* pointer, int length)
        {
            this.pointer = pointer;
			Length = length;
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private readonly T* GetSlot(int slotIndex)
		{
			if (slotIndex < 0 || slotIndex >= Length) {
				throw new System.Exception("UnmanagedArray slotIndex is out of range");
			}
			return pointer + slotIndex;
		}

		public readonly T this[int slotIndex]
        {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => *GetSlot(slotIndex);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => *GetSlot(slotIndex) = value;
		}

		public Enumerator GetEnumerator() => new Enumerator(this);
		IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => new Enumerator(this);

		internal struct Enumerator : IEnumerator<T>
		{
			public T Current { get; private set; }

			object IEnumerator.Current => Current;

			private UnmanagedArray<T> array;
			private int index;

			public Enumerator(UnmanagedArray<T> array)
			{
				this.array = array;
				index = -1;
				Current = default;
			}

			public bool MoveNext()
			{
				if (index >= array.Length) {
					return false;
				}
				Current = array[index];
				index++;
				return true;
			}

			public void Dispose() { }

			public void Reset() { }
		}

		public readonly struct SquareView
		{
			private readonly T* pointer;
			public readonly int Length;
			public readonly int SideLength;
			public SquareView(T* pointer, int sideLength)
			{
				this.pointer = pointer;
				SideLength = sideLength;
				Length = sideLength * sideLength;
			}


			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private readonly T* GetSlot(int slotIndex)
			{
				if (slotIndex < 0 || slotIndex >= Length) {
					throw new System.Exception("UnmanagedArray slotIndex is out of range");
				}
				return pointer + slotIndex;
			}

			public readonly T this[int slotIndex]
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => *GetSlot(slotIndex);
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				set => *GetSlot(slotIndex) = value;
			}

			public readonly T this[int x, int y]
            {
				get => this[x + SideLength * y];
				set => this[x + SideLength * y] = value;
			}
        }
	}

	internal struct PointerArray
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat2<T>* array) where T : unmanaged => new((T*)array, 2);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat4<T>* array) where T : unmanaged => new((T*)array, 4);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat8<T>* array) where T : unmanaged => new((T*)array, 8);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat16<T>* array) where T : unmanaged => new((T*)array, 16);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat32<T>* array) where T : unmanaged => new((T*)array, 32);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat64<T>* array) where T : unmanaged => new((T*)array, 64);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat128<T>* array) where T : unmanaged => new((T*)array, 128);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat256<T>* array) where T : unmanaged => new((T*)array, 256);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat512<T>* array) where T : unmanaged => new((T*)array, 512);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat1024<T>* array) where T : unmanaged => new((T*)array, 1024);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat2048<T>* array) where T : unmanaged => new((T*)array, 2048);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat4096<T>* array) where T : unmanaged => new((T*)array, 4096);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat8192<T>* array) where T : unmanaged => new((T*)array, 8192);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat16384<T>* array) where T : unmanaged => new((T*)array, 16384);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat32768<T>* array) where T : unmanaged => new((T*)array, 32768);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe PointerArray<T> From<T>(Repeat65536<T>* array) where T : unmanaged => new((T*)array, 65536);

		public struct SquareView
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe PointerArray<T>.SquareView From<T>(Repeat4<T>* array) where T : unmanaged => new((T*)array, sideLength: 2);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe PointerArray<T>.SquareView From<T>(Repeat16<T>* array) where T : unmanaged => new((T*)array, sideLength: 4);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe PointerArray<T>.SquareView From<T>(Repeat64<T>* array) where T : unmanaged => new((T*)array, sideLength: 8);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe PointerArray<T>.SquareView From<T>(Repeat256<T>* array) where T : unmanaged => new((T*)array, sideLength: 16);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe PointerArray<T>.SquareView From<T>(Repeat1024<T>* array) where T : unmanaged => new((T*)array, sideLength: 32);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe PointerArray<T>.SquareView From<T>(Repeat4096<T>* array) where T : unmanaged => new((T*)array, sideLength: 64);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe PointerArray<T>.SquareView From<T>(Repeat16384<T>* array) where T : unmanaged => new((T*)array, sideLength: 128);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe PointerArray<T>.SquareView From<T>(Repeat65536<T>* array) where T : unmanaged => new((T*)array, sideLength: 256);
		}
	}

	internal readonly unsafe struct PointerArray<T> where T : unmanaged
	{
		private readonly T* pointer;
		public readonly int Length;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointerArray(T* pointer, int length)
		{
			this.pointer = pointer;
			Length = length;
		}

		public readonly T* this[int slotIndex]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (slotIndex < 0 || slotIndex >= Length) {
					throw new System.Exception("UnmanagedArray slotIndex is out of range");
				}
				return pointer + slotIndex;
			}
		}

		public readonly struct SquareView
		{
			private readonly T* pointer;
			public readonly int Length;
			public readonly int SideLength;
			public SquareView(T* pointer, int sideLength)
			{
				this.pointer = pointer;
				SideLength = sideLength;
				Length = sideLength * sideLength;
			}

			public readonly T* this[int slotIndex]
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get {
					if (slotIndex < 0 || slotIndex >= Length) {
						throw new System.Exception("UnmanagedArray slotIndex is out of range");
					}
					return pointer + slotIndex;
				}
			}

			public readonly T* this[int x, int y] => this[x + SideLength * y];
		}
	}

	/// <summary>
	/// This interface marks a contiguous block of memory whose size is equal
	/// to the number of elements equal to <see cref="Length"/> for a given type.
	/// </summary>
	internal interface IRepeat<T> where T : unmanaged
	{
		int Length { get; }
	}

	internal struct Repeat2<T> : IRepeat<T> where T : unmanaged
	{
		public T Value0;
		public T Value1;

		public readonly int Length => 2;
	}

	internal struct Repeat4<T> : IRepeat<T> where T : unmanaged
	{
		public T Value0;
		public T Value1;
		public T Value2;
		public T Value3;

		public readonly int Length => 4;
	}

	internal struct Repeat8<T> : IRepeat<T> where T : unmanaged
    {
		public Repeat4<T> Repeat_0_3;
		public Repeat4<T> Repeat_4_7;

		public readonly int Length => 8;
	}

	internal struct Repeat16<T> : IRepeat<T> where T : unmanaged
	{
		public Repeat8<T> Repeat_0_7;
		public Repeat8<T> Repeat_8_15;

		public readonly int Length => 16;
	}

	internal struct Repeat32<T> : IRepeat<T> where T : unmanaged
	{
		public Repeat16<T> Repeat_0_15;
		public Repeat16<T> Repeat_16_31;

		public readonly int Length => 32;
	}

	internal struct Repeat64<T> : IRepeat<T> where T : unmanaged
	{
		public Repeat32<T> Repeat_0_31;
		public Repeat32<T> Repeat_32_63;

		public readonly	int Length => 64;
	}

	internal struct Repeat128<T> : IRepeat<T> where T : unmanaged
	{
		public Repeat64<T> Repeat_0_63;
		public Repeat64<T> Repeat_64_127;

		public readonly int Length => 128;
	}

	internal struct Repeat256<T> : IRepeat<T> where T : unmanaged
	{
		public Repeat128<T> Repeat_0_127;
		public Repeat128<T> Repeat_127_255;

		public readonly int Length => 256;
	}

	internal struct Repeat512<T> : IRepeat<T> where T : unmanaged
	{
		public Repeat256<T> Repeat_0_255;
		public Repeat256<T> Repeat_255_511;

		public readonly int Length => 512;
	}

	internal struct Repeat1024<T> : IRepeat<T> where T : unmanaged
	{
		public Repeat512<T> Repeat_0_511;
		public Repeat512<T> Repeat_512_1023;

		public readonly int Length => 1024;
	}

	internal struct Repeat2048<T> : IRepeat<T> where T : unmanaged
	{
		public Repeat1024<T> Repeat_0_1023;
		public Repeat1024<T> Repeat_1024_2047;

		public readonly int Length => 2048;
	}

	internal struct Repeat4096<T> : IRepeat<T> where T : unmanaged
	{
		public Repeat2048<T> Repeat_0_2047;
		public Repeat2048<T> Repeat_2048_4095;

		public readonly int Length => 4096;
	}

	internal struct Repeat8192<T> : IRepeat<T> where T : unmanaged
	{
		public Repeat4096<T> Repeat_0_4095;
		public Repeat4096<T> Repeat_4096_8191;

		public readonly int Length => 8192;
	}

	internal struct Repeat16384<T> : IRepeat<T> where T : unmanaged
	{
		public Repeat8192<T> Repeat_0_8191;
		public Repeat8192<T> Repeat_8192_16383;

		public readonly int Length => 16384;
	}

	internal struct Repeat32768<T> : IRepeat<T> where T : unmanaged
	{
		public Repeat16384<T> Repeat_0_16383;
		public Repeat16384<T> Repeat_16384_32767;

		public readonly int Length => 32768;
	}

	internal struct Repeat65536<T> : IRepeat<T> where T : unmanaged
	{
		public Repeat32768<T> Repeat_0_32767;
		public Repeat32768<T> Repeat_32768_65535;

		public readonly int Length => 65536;
	}

	internal struct Repeat513x513<T> : IRepeat<T> where T : unmanaged
	{
		public Repeat65536<T> P0;
		public Repeat65536<T> P1;
		public Repeat65536<T> P2;
		public Repeat65536<T> P3;
		public Repeat1024<T> P4;
		public T P5;

		public readonly int Length => 65536;
	}
}
