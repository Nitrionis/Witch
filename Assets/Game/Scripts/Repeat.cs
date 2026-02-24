
using System.Runtime.CompilerServices;

namespace Game
{
	internal struct UnmanagedArray
	{
		public static unsafe UnmanagedArray<T> From<T>(Repeat2<T>* array) where T : unmanaged => new((T*)array, 2);
		public static unsafe UnmanagedArray<T> From<T>(Repeat4<T>* array) where T : unmanaged => new((T*)array, 4);
		public static unsafe UnmanagedArray<T> From<T>(Repeat8<T>* array) where T : unmanaged => new((T*)array, 8);
		public static unsafe UnmanagedArray<T> From<T>(Repeat16<T>* array) where T : unmanaged => new((T*)array, 16);
		public static unsafe UnmanagedArray<T> From<T>(Repeat32<T>* array) where T : unmanaged => new((T*)array, 32);
		public static unsafe UnmanagedArray<T> From<T>(Repeat64<T>* array) where T : unmanaged => new((T*)array, 64);
		public static unsafe UnmanagedArray<T> From<T>(Repeat128<T>* array) where T : unmanaged => new((T*)array, 128);
		public static unsafe UnmanagedArray<T> From<T>(Repeat256<T>* array) where T : unmanaged => new((T*)array, 256);
		public static unsafe UnmanagedArray<T> From<T>(Repeat512<T>* array) where T : unmanaged => new((T*)array, 512);
		public static unsafe UnmanagedArray<T> From<T>(Repeat1024<T>* array) where T : unmanaged => new((T*)array, 1024);
		public static unsafe UnmanagedArray<T> From<T>(Repeat2048<T>* array) where T : unmanaged => new((T*)array, 2048);
		public static unsafe UnmanagedArray<T> From<T>(Repeat4096<T>* array) where T : unmanaged => new((T*)array, 4096);
		public static unsafe UnmanagedArray<T> From<T>(Repeat8192<T>* array) where T : unmanaged => new((T*)array, 8192);
		public static unsafe UnmanagedArray<T> From<T>(Repeat16384<T>* array) where T : unmanaged => new((T*)array, 16384);
		public static unsafe UnmanagedArray<T> From<T>(Repeat32768<T>* array) where T : unmanaged => new((T*)array, 32768);
		public static unsafe UnmanagedArray<T> From<T>(Repeat65536<T>* array) where T : unmanaged => new((T*)array, 65536);

		public struct SquareView
		{
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat4<T>* array) where T : unmanaged => new((T*)array, sideLength: 2);
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat16<T>* array) where T : unmanaged => new((T*)array, sideLength: 4);
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat64<T>* array) where T : unmanaged => new((T*)array, sideLength: 8);
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat256<T>* array) where T : unmanaged => new((T*)array, sideLength: 16);
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat1024<T>* array) where T : unmanaged => new((T*)array, sideLength: 32);
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat4096<T>* array) where T : unmanaged => new((T*)array, sideLength: 64);
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat16384<T>* array) where T : unmanaged => new((T*)array, sideLength: 128);
			public static unsafe UnmanagedArray<T>.SquareView From<T>(Repeat65536<T>* array) where T : unmanaged => new((T*)array, sideLength: 256);
		}
	}

	internal readonly unsafe struct UnmanagedArray<T> where T : unmanaged
	{
		private readonly T* pointer;
		public readonly int Length;
        public UnmanagedArray(T* pointer, int length)
        {
            this.pointer = pointer;
			Length = length;
        }

		private readonly T* GetSlot(int slotIndex)
		{
			if (slotIndex < 0 || slotIndex >= Length) {
				throw new System.Exception("UnmanagedArray slotIndex is out of range");
			}
			return pointer + slotIndex;
		}

        public readonly T this[int slotIndex]
        {
            get => *GetSlot(slotIndex);
			set => *GetSlot(slotIndex) = value;
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

			private readonly T* GetSlot(int slotIndex)
			{
				if (slotIndex < 0 || slotIndex >= Length) {
					throw new System.Exception("UnmanagedArray slotIndex is out of range");
				}
				return pointer + slotIndex;
			}

			public readonly T this[int slotIndex]
			{
				get => *GetSlot(slotIndex);
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

	internal struct Repeat2<T> where T : unmanaged
	{
		public T Value0;
		public T Value1;
	}

	internal struct Repeat4<T> where T : unmanaged
	{
		public T Value0;
		public T Value1;
		public T Value2;
		public T Value3;
	}

	internal struct Repeat8<T> where T : unmanaged
    {
		public Repeat4<T> Repeat_0_3;
		public Repeat4<T> Repeat_4_7;
	}

	internal struct Repeat16<T> where T : unmanaged
	{
		public Repeat8<T> Repeat_0_7;
		public Repeat8<T> Repeat_8_15;
	}

	internal struct Repeat32<T> where T : unmanaged
	{
		public Repeat16<T> Repeat_0_15;
		public Repeat16<T> Repeat_16_31;
	}

	internal struct Repeat64<T> where T : unmanaged
	{
		public Repeat32<T> Repeat_0_31;
		public Repeat32<T> Repeat_32_63;
	}

	internal struct Repeat128<T> where T : unmanaged
	{
		public Repeat64<T> Repeat_0_63;
		public Repeat64<T> Repeat_64_127;
	}

	internal struct Repeat256<T> where T : unmanaged
	{
		public Repeat128<T> Repeat_0_127;
		public Repeat128<T> Repeat_127_255;
	}

	internal struct Repeat512<T> where T : unmanaged
	{
		public Repeat256<T> Repeat_0_255;
		public Repeat256<T> Repeat_255_511;
	}

	internal struct Repeat1024<T> where T : unmanaged
	{
		public Repeat512<T> Repeat_0_511;
		public Repeat512<T> Repeat_512_1023;
	}

	internal struct Repeat2048<T> where T : unmanaged
	{
		public Repeat1024<T> Repeat_0_1023;
		public Repeat1024<T> Repeat_1024_2047;
	}

	internal struct Repeat4096<T> where T : unmanaged
	{
		public Repeat2048<T> Repeat_0_2047;
		public Repeat2048<T> Repeat_2048_4095;
	}

	internal struct Repeat8192<T> where T : unmanaged
	{
		public Repeat4096<T> Repeat_0_4095;
		public Repeat4096<T> Repeat_4096_8191;
	}

	internal struct Repeat16384<T> where T : unmanaged
	{
		public Repeat8192<T> Repeat_0_8191;
		public Repeat8192<T> Repeat_8192_16383;
	}

	internal struct Repeat32768<T> where T : unmanaged
	{
		public Repeat16384<T> Repeat_0_16383;
		public Repeat16384<T> Repeat_16384_32767;
	}

	internal struct Repeat65536<T> where T : unmanaged
	{
		public Repeat32768<T> Repeat_0_32767;
		public Repeat32768<T> Repeat_32768_65535;
	}
}
