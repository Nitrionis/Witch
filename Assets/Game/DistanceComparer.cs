using System.Collections.Generic;
using Unity.Mathematics;

namespace Assets.Game
{
	internal class DistanceComparer : IComparer<int2>
	{
		public int2 Point;

		public DistanceComparer(int2 point) => Point = point;

		public int Compare(int2 x, int2 y)
		{
			int2 yd = y - Point;
			int2 xd = x - Point;
			return math.dot(yd, yd).CompareTo(math.dot(xd, xd));
		}
	}
}
