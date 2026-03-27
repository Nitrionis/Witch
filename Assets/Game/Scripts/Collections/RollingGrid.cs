using System;
using Unity.Mathematics;

namespace Game.Collections
{
	public class RollingGrid<T>
		where T : RollingGrid<T>.IItem
	{
		private readonly T[] buffer;
		private readonly int sideLength;

		private int2 offset;

		public int2 Offset
		{
			get => offset;
			set => Shift(value);
		}

		/// <summary>
		/// Gets or sets the value associated with the specified key.
		/// </summary>
		public T this[int2 key]
		{
			get
			{
				if (!IsInBounds(key))
					throw new ArgumentOutOfRangeException($"Key {key} is outside the current grid bounds.");
				return buffer[GetIndex(key.x, key.y)];
			}
			set
			{
				if (!IsInBounds(key))
					throw new ArgumentOutOfRangeException(nameof(key), $"Key {key} is outside the current grid bounds.");
				buffer[GetIndex(key.x, key.y)] = value;
			}
		}

		public RollingGrid(int sideLength)
		{
			this.sideLength = sideLength;
			buffer = new T[sideLength * sideLength];
		}

		public bool TryGet(int2 position, out T item)
		{
			item = default;
			if (IsInBounds(position)) {
				item = buffer[GetIndex(position.x, position.y)];
				return true;
			}
			return false;
		}

		private void Shift(int2 newOffset)
		{
			int2 shift = newOffset - offset;
			if (math.cmax(math.abs(shift)) == 1) {
				if (shift.x != 0) {
					int colToDispose = offset.x + shift.x > 0 ? 0 : sideLength - 1;
					for (int y = offset.y; y < sideLength + offset.y; y++) {
						int index = GetIndex(colToDispose, y);
						buffer[index]?.Release();
						buffer[index] = default;
					}
				}
				if (shift.y != 0) {
					int rowToDispose = offset.y + shift.y > 0 ? 0 : sideLength - 1;
					for (int x = offset.x; x < sideLength + offset.x; x++) {
						int index = GetIndex(x, rowToDispose);
						buffer[index]?.Release();
						buffer[index] = default;
					}
				}
				offset = newOffset;
			} else {
				if (shift.Equals(int2.zero)) {
					return;
				}
				offset = newOffset;
				for (int x = 0; x < sideLength; x++) {
					for (int y = 0; y < sideLength; y++) {
						if (!IsInBounds(new int2(x, y))) {
							int index = GetIndex(x, y);
							buffer[index]?.Release();
							buffer[index] = default;
						}
					}
				}
			}
		}

		private bool IsInBounds(int2 offset)
		{
			return this.offset.x <= offset.x && offset.x < this.offset.x + sideLength &&
				this.offset.y <= offset.y && offset.y < this.offset.y + sideLength;
		}

		private int GetIndex(int x, int y)
		{
			// Apply origin offsets (modulo arithmetic for ring buffer)
			x = (x + sideLength) % sideLength;
			y = (y + sideLength) % sideLength;
			return x + y * sideLength;
		}

		public interface IItem
		{
			void Release();
		}
	}
}
