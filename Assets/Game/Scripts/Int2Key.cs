using System;

namespace Game
{
    internal readonly struct Int2Key
    {
        public readonly int X;
		public readonly int Y;

		public Int2Key(int x, int y) : this()
        {
            X = x;
            Y = y;
        }

		public override bool Equals(object obj) => obj is Int2Key other && Equals(other);

		public bool Equals(Int2Key other) => X == other.X && Y == other.Y;

		public override int GetHashCode() => HashCode.Combine(X, Y);

		public static bool operator ==(Int2Key left, Int2Key right) => left.Equals(right);
		public static bool operator !=(Int2Key left, Int2Key right) => !left.Equals(right);
	}
}
