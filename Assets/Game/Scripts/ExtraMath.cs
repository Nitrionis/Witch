
namespace Game
{
	/// <summary>
	/// Steel & Vigna's LCG64 (Linear Congruential Generator, 64-bit)
	/// </summary>
	public struct LCG64
	{
		public const int StructSize = sizeof(ulong);

		private ulong _state;

		public LCG64(ulong seed) => _state = seed;

		private const ulong MULTIPLIER = 0xd1342543de82ef95UL;
		public ulong Next() => _state = MULTIPLIER * _state + 1;

		// Excellent float quality
		public double NextDouble()
		{
			return (Next() >> 11) * (1.0 / 9007199254740992.0); // 2^53
		}
	}
}
