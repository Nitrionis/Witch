using System;
using System.Runtime.CompilerServices;

namespace Assets.Game.NitroEcs
{
	public class Mirror
	{
		private World targetWorld;
		private (Entity src, Entity dst)[] table;

		public Mirror(World targetWorld)
		{
			this.targetWorld = targetWorld;
			table = new (Entity src, Entity dst)[1024];
		}

		/// <summary>
		/// Creates a copy of an entity if there is no cached one.
		/// </summary>
		public Entity ReflectSafe(Entity entity)
		{
			if (table.Length <= entity.Index) {
				ResizeTable(entity);
			}
			var (src, dst) = table[entity.Index];
			if (src == entity) {
				return ReflectUnsafe(entity.Index);
			}
			if (dst.IsNotNull) {
				throw new System.Exception("Entity was not deleted and cannot be recreated.");
			}
			var targetEntity = targetWorld.NewEntity();
			table[entity.Index] = (entity, targetEntity);
			return targetEntity;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Entity ReflectUnsafe(ushort entityIndex) => table[entityIndex].dst;

		public void Remove(Entity entity)
		{
			if (table.Length < entity.Index) {
				var (src, dst) = table[entity.Index];
				if (src == entity && dst.IsNotNull) {
					table[entity.Index] = (src, default);
					return;
				}
			}
			throw new System.Exception("Entity was not created and cannot be removed.");
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ResizeTable(Entity entity)
		{
			Array.Resize(ref table, 2 * table.Length);
		}
	}
}
