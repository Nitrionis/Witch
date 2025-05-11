using System;
using System.Threading;

namespace Assets.Game.NitroEcs
{
	public class Tools
	{
		public static void ThreadSafeResize<T>(ref T[] array, int newSize)
		{
			var newArray = new T[newSize];
			int count = Math.Min(array.Length, newSize);
			for (int i = 0; i < count; i++) {
				newArray[i] = array[i];
			}
			Interlocked.MemoryBarrier();
			array = newArray;
		}
	}
}
