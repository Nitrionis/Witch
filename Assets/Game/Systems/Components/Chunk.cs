using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Game.Systems.Components
{
	internal struct Chunk
	{
		public double2 Position;
		public NativeArray<ushort> Heights;
		public Hash128 HeightsHash;
	}
}
