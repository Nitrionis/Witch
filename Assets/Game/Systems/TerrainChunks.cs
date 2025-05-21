using UnityEngine;

namespace Assets.Game.Systems
{
	internal class TerrainChunks : NitroEcs.World.ISystem
	{
		private readonly Transform playerTransform;
		private readonly NitroEcs.World ecsWorld;
		private NitroEcs.Filter<Chunk> chunksFilter;

		public TerrainChunks(Transform playerTransform, NitroEcs.World world)
		{
			this.playerTransform = playerTransform;
			ecsWorld = world;
		}

		public void FinalInitialize()
		{
			
		}

		public void RegisterComponentTypes()
		{
			ecsWorld.AddPool<Chunk>();
			var pool = ecsWorld.GetPool<Chunk>();
			chunksFilter = new(pool);
		}

		public void Tick()
		{
			
		}

		private struct Chunk
		{

		}
	}
}
