using System;
using System.Runtime.CompilerServices;

namespace Assets.Game.NitroEcs
{
	public readonly struct Entity : IEquatable<Entity>
	{
		public const int Size = 4;

		public readonly ushort Index;

		public readonly ushort Generation;

		public bool IsNull => Generation == World.UnusedGeneration;
		public bool IsNotNull => Generation != World.UnusedGeneration;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Entity(ushort index, ushort generation)
		{
			Index = index;
			Generation = generation;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Entity GetNullEntityWithIndex(ushort index) =>
			new Entity(index, World.UnusedGeneration);

		public override bool Equals(object obj) => Equals((Entity)obj);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Entity e) => Index == e.Index && Generation == e.Generation;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(Entity lhs, Entity rhs) => lhs.Equals(rhs);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(Entity lhs, Entity rhs) => !lhs.Equals(rhs);

		public override int GetHashCode() => ((uint)this).GetHashCode();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator uint(Entity entity) =>
			entity.Index | ((uint)entity.Generation << 16);
	}
}