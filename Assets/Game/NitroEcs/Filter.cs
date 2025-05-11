using System;
using System.Runtime.CompilerServices;

namespace Assets.Game.NitroEcs
{
	internal struct Enumerator
	{
		private readonly World world;
		private readonly ushort[] entities;
		private readonly int count;
		private int index;
		private Entity current;

		public Entity Current
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => current;
		}

		public Enumerator(ushort[] entities, int count, World world)
		{
			this.entities = entities;
			this.count = count;
			if (count > this.entities.Length) {
				throw new Exception();
			}
			index = -1;
			current = default;
			this.world = world;
			world.Lock();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool MoveNext(World.ComponentsMask componentMask)
		{
			while (++index < count) {
				ushort entityIndex = entities[index];
				if (componentMask.Match(entityIndex, out ushort generation)) {
					current = new Entity(entityIndex, generation);
					return true;
				}
			}
			return false;
		}

		public void Dispose() => world.Unlock();
	}

	public sealed class Filter<T1> where T1 : struct
	{
		private readonly Pool<T1> p1;
		private readonly ulong[] maskMemory;

		public Filter(Pool<T1> p1)
		{
			this.p1 = p1;
			maskMemory = new ulong[p1.World.SlotCountPerEntity];
			maskMemory[p1.ComponentMask.SlotOffset] |= p1.ComponentMask.SlotMask;
		}

		public Enumerator GetEnumerator(Diff diff = null)
		{
			return diff != null
				? new(diff.EntitiesBuffer, diff.EntityCount, maskMemory, p1)
				: new(p1.entities, p1.entityCount, maskMemory, p1);
		}

		public ref struct Enumerator
		{
			private NitroEcs.Enumerator enumerator;
			private readonly World.ComponentsMask masks;
			private readonly Pool<T1> p1;

			public Entity Entity
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => enumerator.Current;
			}

			public ref T1 C1
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => ref p1.UnsafeGet(enumerator.Current.Index);
			}

			internal Enumerator(
				ushort[] entities, int count, ReadOnlySpan<ulong> masks, Pool<T1> p1
			) {
				this.p1 = p1;
				enumerator = new NitroEcs.Enumerator(entities, count, p1.World);
				this.masks = new World.ComponentsMask(p1.World, masks);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext() => enumerator.MoveNext(masks);

			public void Dispose() => enumerator.Dispose();
		}
	}

	public sealed class Filter<T1, T2>
		where T1 : struct where T2 : struct
	{
		private readonly Pool<T1> p1;
		private readonly Pool<T2> p2;
		private readonly ulong[] maskMemory;

		public Filter(Pool<T1> p1, Pool<T2> p2)
		{
			(this.p1, this.p2) = (p1, p2);
			if (p1.World != p2.World) {
				throw new Exception("You cannot create a filter for components from different worlds.");
			}
			maskMemory = new ulong[p1.World.SlotCountPerEntity];
			maskMemory[p1.ComponentMask.SlotOffset] |= p1.ComponentMask.SlotMask;
			maskMemory[p2.ComponentMask.SlotOffset] |= p2.ComponentMask.SlotMask;
		}

		public Enumerator GetEnumerator(Diff diff = null)
		{
			if (diff != null) {
				return new Enumerator(diff.EntitiesBuffer, diff.EntityCount, maskMemory, p1, p2);
			}
			var entities = p1.entities;
			int entitiesCount = p1.entityCount;
			if (entitiesCount > p2.entityCount) {
				entities = p2.entities;
				entitiesCount = p2.entityCount;
			}
			return new Enumerator(entities, entitiesCount, maskMemory, p1, p2);
		}

		public ref struct Enumerator
		{
			private NitroEcs.Enumerator enumerator;
			private readonly World.ComponentsMask masks;
			private readonly Pool<T1> p1;
			private readonly Pool<T2> p2;

			public Entity Entity
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => enumerator.Current;
			}

			public ref T1 C1
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => ref p1.UnsafeGet(enumerator.Current.Index);
			}

			public ref T2 C2
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => ref p2.UnsafeGet(enumerator.Current.Index);
			}

			internal Enumerator(
				ushort[] entities, int count, ReadOnlySpan<ulong> masks,
				Pool<T1> p1, Pool<T2> p2
			) {
				(this.p1, this.p2) = (p1, p2);
				enumerator = new NitroEcs.Enumerator(entities, count, p1.World);
				this.masks = new World.ComponentsMask(p1.World, masks);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext() => enumerator.MoveNext(masks);

			public void Dispose() => enumerator.Dispose();
		}
	}

	public sealed class Filter<T1, T2, T3>
		where T1 : struct where T2 : struct where T3 : struct
	{
		private readonly Pool<T1> p1;
		private readonly Pool<T2> p2;
		private readonly Pool<T3> p3;
		private readonly ulong[] maskMemory;

		public Filter(Pool<T1> p1, Pool<T2> p2, Pool<T3> p3)
		{
			(this.p1, this.p2, this.p3) = (p1, p2, p3);
			if (p1.World != p2.World || p1.World != p3.World) {
				throw new Exception("You cannot create a filter for components from different worlds.");
			}
			maskMemory = new ulong[p1.World.SlotCountPerEntity];
			maskMemory[p1.ComponentMask.SlotOffset] |= p1.ComponentMask.SlotMask;
			maskMemory[p2.ComponentMask.SlotOffset] |= p2.ComponentMask.SlotMask;
			maskMemory[p3.ComponentMask.SlotOffset] |= p3.ComponentMask.SlotMask;
		}

		public Enumerator GetEnumerator(Diff diff = null)
		{
			if (diff != null) {
				return new Enumerator(diff.EntitiesBuffer, diff.EntityCount, maskMemory, p1, p2, p3);
			}
			var entities = p1.entities;
			int entitiesCount = p1.entityCount;
			if (entitiesCount > p2.entityCount) {
				entities = p2.entities;
				entitiesCount = p2.entityCount;
			}
			if (entitiesCount > p3.entityCount) {
				entities = p3.entities;
				entitiesCount = p3.entityCount;
			}
			return new Enumerator(entities, entitiesCount, maskMemory, p1, p2, p3);
		}

		public ref struct Enumerator
		{
			private NitroEcs.Enumerator enumerator;
			private readonly World.ComponentsMask masks;
			private readonly Pool<T1> p1;
			private readonly Pool<T2> p2;
			private readonly Pool<T3> p3;

			public Entity Entity
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => enumerator.Current;
			}

			public ref T1 C1
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => ref p1.UnsafeGet(enumerator.Current.Index);
			}

			public ref T2 C2
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => ref p2.UnsafeGet(enumerator.Current.Index);
			}

			public ref T3 C3
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => ref p3.UnsafeGet(enumerator.Current.Index);
			}

			internal Enumerator(
				ushort[] entities, int count, ReadOnlySpan<ulong> masks,
				Pool<T1> p1, Pool<T2> p2, Pool<T3> p3
			) {
				(this.p1, this.p2, this.p3) = (p1, p2, p3);
				enumerator = new NitroEcs.Enumerator(entities, count, p1.World);
				this.masks = new World.ComponentsMask(p1.World, masks);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext() => enumerator.MoveNext(masks);

			public void Dispose() => enumerator.Dispose();
		}
	}

	public sealed class Filter<T1, T2, T3, T4>
		where T1 : struct where T2 : struct where T3 : struct where T4 : struct
	{
		private readonly Pool<T1> p1;
		private readonly Pool<T2> p2;
		private readonly Pool<T3> p3;
		private readonly Pool<T4> p4;
		private readonly ulong[] maskMemory;

		public Filter(Pool<T1> p1, Pool<T2> p2, Pool<T3> p3, Pool<T4> p4, Diff diff = null)
		{
			(this.p1, this.p2, this.p3, this.p4) = (p1, p2, p3, p4);
			World w = p1.World;
			if (w != p2.World || w != p3.World || w != p4.World) {
				throw new Exception("You cannot create a filter for components from different worlds.");
			}
			maskMemory = new ulong[p1.World.SlotCountPerEntity];
			maskMemory[p1.ComponentMask.SlotOffset] |= p1.ComponentMask.SlotMask;
			maskMemory[p2.ComponentMask.SlotOffset] |= p2.ComponentMask.SlotMask;
			maskMemory[p3.ComponentMask.SlotOffset] |= p3.ComponentMask.SlotMask;
			maskMemory[p4.ComponentMask.SlotOffset] |= p4.ComponentMask.SlotMask;
		}

		public Enumerator GetEnumerator(Diff diff = null)
		{
			if (diff != null) {
				return new Enumerator(diff.EntitiesBuffer, diff.EntityCount, maskMemory, p1, p2, p3, p4);
			}
			var entities = p1.entities;
			int entitiesCount = p1.entityCount;
			if (entitiesCount > p2.entityCount) {
				entities = p2.entities;
				entitiesCount = p2.entityCount;
			}
			if (entitiesCount > p3.entityCount) {
				entities = p3.entities;
				entitiesCount = p3.entityCount;
			}
			if (entitiesCount > p4.entityCount) {
				entities = p4.entities;
				entitiesCount = p4.entityCount;
			}
			return new Enumerator(entities, entitiesCount, maskMemory, p1, p2, p3, p4);
		}

		public ref struct Enumerator
		{
			private NitroEcs.Enumerator enumerator;
			private readonly World.ComponentsMask masks;
			private readonly Pool<T1> p1;
			private readonly Pool<T2> p2;
			private readonly Pool<T3> p3;
			private readonly Pool<T4> p4;

			public Entity Entity
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => enumerator.Current;
			}

			public ref T1 C1
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => ref p1.UnsafeGet(enumerator.Current.Index);
			}

			public ref T2 C2
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => ref p2.UnsafeGet(enumerator.Current.Index);
			}

			public ref T3 C3
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => ref p3.UnsafeGet(enumerator.Current.Index);
			}

			public ref T4 C4
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => ref p4.UnsafeGet(enumerator.Current.Index);
			}

			internal Enumerator(
				ushort[] entities, int count, ReadOnlySpan<ulong> masks,
				Pool<T1> p1, Pool<T2> p2, Pool<T3> p3, Pool<T4> p4
			) {
				(this.p1, this.p2, this.p3, this.p4) = (p1, p2, p3, p4);
				enumerator = new NitroEcs.Enumerator(entities, count, p1.World);
				this.masks = new World.ComponentsMask(p1.World, masks);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext() => enumerator.MoveNext(masks);

			public void Dispose() => enumerator.Dispose();
		}
	}
}