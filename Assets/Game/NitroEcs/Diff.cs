using System;
using System.Runtime.CompilerServices;

namespace Assets.Game.NitroEcs
{
	public class Diff
	{
		private const ushort UnusedEntity = ushort.MaxValue;

		private ushort[] sparseEntities;
		private ushort[] denseEntities;
		private ushort entityCount;

		internal ushort[] EntitiesBuffer => denseEntities;
		public ushort EntityCount => entityCount;

		public Diff()
		{
			sparseEntities = new ushort[128];
			Array.Fill(sparseEntities, UnusedEntity);
			denseEntities = new ushort[64];
		}

		public void Add(ushort entity)
		{
			if (entity >= sparseEntities.Length) {
				ResizeSparseEntitiesFor(entity);
			}
			var index = sparseEntities[entity];
			if (index == UnusedEntity) {
				if (entityCount == denseEntities.Length) {
					ResizeDenseEntities();
				}
				ushort denseIndex = entityCount++;
				sparseEntities[entity] = denseIndex;
				denseEntities[denseIndex] = entity;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ResizeSparseEntitiesFor(ushort entityIndex)
		{
			int oldLength = sparseEntities.Length;
			int targetLength = sparseEntities.Length;
			while ((targetLength <<= 1) <= entityIndex) { }
			Array.Resize(ref sparseEntities, targetLength);
			Array.Fill(
				sparseEntities, value: UnusedEntity,
				startIndex: oldLength, count: targetLength - oldLength
			);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ResizeDenseEntities()
		{
			int oldLength = denseEntities.Length;
			Array.Resize(ref denseEntities, 2 * denseEntities.Length);
		}

		public void Clear()
		{
			for (ushort i = 0; i < entityCount; i++) {
				sparseEntities[denseEntities[i]] = UnusedEntity;
			}
			entityCount = 0;
		}
	}
}
