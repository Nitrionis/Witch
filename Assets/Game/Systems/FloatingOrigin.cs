using System;
using Unity.Mathematics;
using UnityEngine;
using Assets.Game.NitroEcs;

namespace Assets.Game.Systems
{
	internal class FloatingOrigin : NitroEcs.World.ISystem
	{
		private readonly Player player;
		private readonly Filter<TransformComponent> transformsFilter;

		public double2 HighPrecisionOffset { get; private set; }

		public FloatingOrigin(
			Player player,
			Filter<TransformComponent> transformsFilter
			)
		{
			this.player = player;
			this.transformsFilter = transformsFilter;
		}

		public void FinalInitialize()
		{
		}

		public void RegisterComponentTypes() {}

		public void Tick()
		{
			var playerPosition = player.Transform.position;
			if (Math.Abs(playerPosition.x) < 1000 && Math.Abs(playerPosition.z) < 1000) {
				return;
			}

			var offset = new Vector3(playerPosition.x, 0, playerPosition.z);
			player.Transform.position = new Vector3(0, playerPosition.y, 0);

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
