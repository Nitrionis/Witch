using Unity.Mathematics;
using UnityEngine;

namespace Game.Storage
{
	internal class RegionChangesCache
	{
		public readonly IPlayer Player;
		public readonly int ChunkCountByAxis;

		private readonly ChunkPatches[] table;

		private int2 playerPosition;

		public RegionChangesCache(int chunkCountByAxis, IPlayer player)
		{
			ChunkCountByAxis = chunkCountByAxis;
			Player = player;
			table = new ChunkPatches[ChunkCountByAxis * ChunkCountByAxis];
			playerPosition = new int2(-100);
		}

		public void Update()
		{

		}

		public interface IPlayer
		{
			public Vector2 Position { get; }
		}
	}
}
