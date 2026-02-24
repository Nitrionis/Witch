using System.Runtime.CompilerServices;

namespace Game
{
    internal readonly unsafe struct Pointer<T> where T : unmanaged
    {
		public readonly T* TypedPointer;
		public readonly T Value
		{
			get => *TypedPointer;
			set => *TypedPointer = value;
		}
        public Pointer(T* value) => TypedPointer = value;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator T*(Pointer<T> ptr) => ptr.TypedPointer;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Pointer<T>(T* ptr) => new Pointer<T>(ptr);
	}
}
