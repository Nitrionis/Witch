using UnityEngine;
using Assets.Game.NitroEcs;
using Assets.Game.Systems.Components;

namespace Assets.Game.Systems
{
	internal class TerrainChunks : NitroEcs.World.ISystem
	{
		private readonly Player player;
		private readonly Filter<Chunk> chunksFilter;

		public TerrainChunks(
			Player player,
			Filter<Chunk> chunksFilter
			)
		{
			this.player = player;
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
