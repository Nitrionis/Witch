
namespace Game.World
{
	internal struct ChunkLod2
	{
		/// <summary>
		/// Terrain height at 4 meter intervals
		/// </summary>
		public Repeat4096<float> TerrainHeights;

		/// <summary>
		/// Biome per Terrain height at 4 meter intervals
		/// </summary>
		public Repeat4096<byte> Biomes;
	}
}