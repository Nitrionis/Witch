using System;
using Unity.Mathematics;
using UnityEngine;
using Assets.Game.NitroEcs;

namespace Assets.Game.Systems
{
	internal class FloatingOrigin : NitroEcs.World.ISystem
	{
		private readonly Transform playerTransform;
		private readonly NitroEcs.World ecsWorld;
		private NitroEcs.Filter<TransformComponent> transformsFilter;

		public double2 HighPrecisionOffset { get; private set; }

		public FloatingOrigin(Transform playerTransform, NitroEcs.World world)
		{
			this.playerTransform = playerTransform;
			ecsWorld = world;
		}

		public void FinalInitialize()
		{
			var pool = ecsWorld.GetPool<TransformComponent>();
			transformsFilter = new(pool);
		}

		public void RegisterComponentTypes() {}

		public void Tick()
		{
			var playerPosition = playerTransform.position;
			if (Math.Abs(playerPosition.x) < 1000 && Math.Abs(playerPosition.z) < 1000) {
				return;
			}

			var offset = new Vector3(playerPosition.x, 0, playerPosition.z);
			playerTransform.position = new Vector3(0, playerPosition.y, 0);

			HighPrecisionOffset += new double2(offset.x, offset.z);

			var transforms = transformsFilter.GetEnumerator();
			while (transforms.MoveNext()) {
				var t = transforms.C1.Transform;
				t.position -= offset;
			}
			Physics.SyncTransforms();
		}
	}
}
