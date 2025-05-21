using UnityEngine;

namespace Assets.Game
{
	internal class World
	{
		public readonly Player Player;
		public readonly NitroEcs.World EcsWorld;
		public readonly Systems.FloatingOrigin FloatingOrigin;
		public readonly Systems.TerrainChunks TerrainChunks;

		public World(
			Player player,
			NitroEcs.World ecsWorld,
			Systems.FloatingOrigin floatingOrigin,
			Systems.TerrainChunks terrainChunks
			)
		{
			Player = player;
			EcsWorld = ecsWorld;
			FloatingOrigin = floatingOrigin;
			TerrainChunks = terrainChunks;
		}
	}
}
