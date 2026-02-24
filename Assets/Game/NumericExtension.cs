using System;
using System.Runtime.CompilerServices;

namespace Game
{
    internal struct byte2 : IEquatable<byte2>
	{
		public const int Size = 2 * sizeof(byte);

        public byte x;
        public byte y;

		public byte2(byte x, byte y)
		{
			this.x = x;
			this.y = y;
		}

		public override bool Equals(object obj) => obj is byte2 @byte && Equals(@byte);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(byte2 other)
		{
			return x == other.x &&
				   y == other.y;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => HashCode.Combine(x, y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(byte2 left, byte2 right) => left.Equals(right);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(byte2 left, byte2 right) => !(left == right);
	}

	internal struct byte3 : IEquatable<byte3>
	{
		public const int Size = 3 * sizeof(byte);

		public byte x;
		public byte y;
		public byte z;

		public byte3(byte x, byte y, byte z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public override bool Equals(object obj)
		{
			return obj is byte3 @byte && Equals(@byte);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(byte3 other)
		{
			return x == other.x &&
				   y == other.y &&
				   z == other.z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => HashCode.Combine(x, y, z);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(byte3 left, byte3 right) => left.Equals(right);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(byte3 left, byte3 right) => !(left == right);
	}

	internal struct byte4 : IEquatable<byte4>
	{
		public const int Size = 4 * sizeof(byte);

		public byte x;
		public byte y;
		public byte z;
		public byte w;

		public byte4(byte x, byte y, byte z, byte w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		public override bool Equals(object obj) => obj is byte4 @byte && Equals(@byte);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(byte4 other)
		{
			return x == other.x &&
				   y == other.y &&
				   z == other.z &&
				   w == other.w;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => HashCode.Combine(x, y, z, w);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(byte4 left, byte4 right) => left.Equals(right);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(byte4 left, byte4 right) => !(left == right);
	}

	internal struct ushort2 : IEquatable<ushort2>
	{
		public const int Size = 2 * sizeof(ushort);

		public ushort x;
		public ushort y;

		public ushort2(ushort x, ushort y)
		{
			this.x = x;
			this.y = y;
		}

		public override bool Equals(object obj) =>
			obj is ushort2 other && Equals(other);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(ushort2 other) => (x == other.x) & (y == other.y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => (x & (y << 16)).GetHashCode();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(ushort2 left, ushort2 right) => left.Equals(right);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(ushort2 left, ushort2 right) => !left.Equals(right);
	}
}
