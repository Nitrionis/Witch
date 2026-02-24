using System;
using System.Runtime.CompilerServices;

namespace Game.Storage
{
    internal struct ObjectId : IEquatable<ObjectId>
	{
        public uint Value;

		public ObjectId(uint value) => Value = value;

		public override bool Equals(object obj) => obj is ObjectId id && Equals(id);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(ObjectId other) => Value == other.Value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => HashCode.Combine(Value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(ObjectId left, ObjectId right) => left.Equals(right);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(ObjectId left, ObjectId right) => !(left == right);
	}
}
