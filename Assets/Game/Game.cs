using UnityEngine;
using SimpleInjector;
using Assets.Game.Systems;
using Assets.Game.Systems.Components;
using System.Diagnostics;

namespace Assets.Game
{
	public class Game : MonoBehaviour
	{
		private Container c;

		// Start is called once before the first execution of Update after the MonoBehaviour is created
		void Start()
		{
			var sw = Stopwatch.StartNew();
			c = new Container();

			c.RegisterSingleton<Player>();
			c.RegisterSingleton(() => {
				var w = new NitroEcs.World();
				w.AddPool<TransformComponent>();
				w.AddPool<Chunk>();
				w.Construct();
				return w;
			});
			c.RegisterSingleton(() => c.GetInstance<NitroEcs.World>().GetPool<Chunk>());
			c.RegisterSingleton(() => c.GetInstance<NitroEcs.World>().GetPool<TransformComponent>());
			c.RegisterSingleton<NitroEcs.Filter<TransformComponent>>();
			c.RegisterSingleton<NitroEcs.Filter<Chunk>>();
			c.RegisterSingleton<FloatingOrigin>();
			c.RegisterSingleton<TerrainChunks>();
			c.RegisterSingleton<World>();

			c.Verify();

			var world = c.GetInstance<World>();
			UnityEngine.Debug.Log($"{world.FloatingOrigin.GetType().Name} {sw.ElapsedMilliseconds} ms");
		}

		// Update is called once per frame
		void Update()
		{

		}
	}
}
