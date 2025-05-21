using UnityEngine;
using Assets.Game.NitroEcs;
using Assets.Game.Systems.Components;

namespace Assets.Game.Systems
{
	internal class TerrainChunks : NitroEcs.World.ISystem
	{
		private readonly Player player;
		private readonly NitroEcs.World ecsWorld;
		private readonly Filter<Chunk> chunksFilter;

		public TerrainChunks(
			Player player,
			NitroEcs.World ecsWorld,
			Filter<Chunk> chunksFilter
			)
		{
			this.player = player;
			this.ecsWorld = ecsWorld;
			this.chunksFilter = chunksFilter;
		}

		public void FinalInitialize()
		{
			
		}

		public void RegisterComponentTypes()
		{
		}

		public void Tick()
		{
			
		}
	}
}
