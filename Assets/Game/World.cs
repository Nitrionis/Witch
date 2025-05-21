using UnityEngine;

namespace Assets.Game
{
	internal class World
	{
		public readonly NitroEcs.World EcsWorld = new();
		public readonly Systems.FloatingOrigin FloatingOrigin;
		public readonly GameObject Player;

		public World()
		{
			FloatingOrigin = new(Player.transform, EcsWorld);

			EcsWorld.AddPool<TransformComponent>();
			EcsWorld.RegisterSystem(FloatingOrigin);
			EcsWorld.Construct();
		}
	}
}
