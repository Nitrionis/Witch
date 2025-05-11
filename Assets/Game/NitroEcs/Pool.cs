using System;
using System.Runtime.CompilerServices;

namespace Assets.Game.NitroEcs
{
	public sealed class Pool<T> : World.IPool where T : struct
	{
		private const string CollectionLockedMessage = "Collection is locked";
		private const ushort FreeComponentSlotMarker = ushort.MaxValue;

		public readonly World World;
		private ushort componentCount;
		private T[] components;
		private ushort[] entityToComponent;
		private ushort[] recycledComponents;
		private ushort freeComponentCount;
		private ushort[] sparseEntities;
		internal ushort[] entities;
		internal ushort entityCount;
		private int lockCount;

		private World.ComponentMask componentMask;
		public World.ComponentMask ComponentMask => this.componentMask;
		World.ComponentMask World.IPool.ComponentMask { set => componentMask = value; }

		internal Pool(World world)
		{
			this.World = world;
			entityToComponent = new ushort[512];
			Array.Fill(entityToComponent, FreeComponentSlotMarker);
			recycledComponents = new ushort[128];
			for (ushort i = 0; i < recycledComponents.Length; i++) {
				recycledComponents[i] = i;
			}
			components = new T[128];
			sparseEntities = new ushort[entityToComponent.Length];
			entities = new ushort[components.Length];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Has(Entity entity)
		{
			ValidateEntity(entity);
			return entity.Index < entityToComponent.Length &&
			       entityToComponent[entity.Index] != FreeComponentSlotMarker;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref T Get(Entity entity)
		{
			ValidateEntity(entity);
			if (entity.Index >= entityToComponent.Length) {
				throw new ArgumentException();
			}
			ushort denseEntityIndex = entityToComponent[entity.Index];
			if (denseEntityIndex == FreeComponentSlotMarker) {
				throw new ArgumentException();
			}
			return ref components[entityToComponent[entity.Index]];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ValidateEntity(Entity entity)
		{
			if (!World.Contains(entity)) {
				throw new ArgumentException();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal ref T UnsafeGet(ushort entityIndex) => ref components[entityToComponent[entityIndex]];

		public bool IsLocked => lockCount > 0;

		public ref T Add(Entity entity)
		{
			if (IsLocked) {
				throw new Exception(CollectionLockedMessage);
			}
			if (entityToComponent.Length <= entity.Index) {
				ResizeSparseEntitiesFor(entity.Index);
			}
			ref var redirection = ref entityToComponent[entity.Index];
			if (redirection != FreeComponentSlotMarker) {
				throw new Pool.ComponentAlreadyExistsException(entity);
			}
			if (freeComponentCount == 0) {
				ResizeDenseEntities();
			}
			World.OnAddComponent(entity.Index, componentMask);
			ushort recycledIndex = recycledComponents[--freeComponentCount];
			redirection = recycledIndex;
			sparseEntities[entity.Index] = entityCount;
			entities[entityCount++] = entity.Index;
			return ref components[recycledIndex];
		}

		public ref T AddOrGet(Entity entity)
		{
			if (IsLocked) {
				throw new Exception(CollectionLockedMessage);
			}
			if (entityToComponent.Length <= entity.Index) {
				ResizeSparseEntitiesFor(entity.Index);
			}
			ref var redirection = ref entityToComponent[entity.Index];
			if (redirection != FreeComponentSlotMarker) {
				return ref components[redirection];
			}
			if (freeComponentCount == 0) {
				ResizeDenseEntities();
			}
			World.OnAddComponent(entity.Index, componentMask);
			ushort recycledIndex = recycledComponents[--freeComponentCount];
			redirection = recycledIndex;
			sparseEntities[entity.Index] = entityCount;
			entities[entityCount++] = entity.Index;
			return ref components[recycledIndex];
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ResizeDenseEntities()
		{
			int oldLength = entityToComponent.Length;
			int targetLength = oldLength << 1;
			Array.Resize(ref components, targetLength);
			if (oldLength > recycledComponents.Length) {
				ResizeRecycledEntitiesFor(entityIndex: unchecked((ushort)(oldLength - 1)));
			}
			for (int i = oldLength; i < targetLength; i++) {
				int index = freeComponentCount;
				recycledComponents[index] = ++freeComponentCount;
			}
			Array.Resize(ref entities, targetLength);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ResizeSparseEntitiesFor(ushort entityIndex)
		{
			int oldLength = entityToComponent.Length;
			int targetLength = oldLength;
			while ((targetLength <<= 1) <= entityIndex) { }
			Array.Resize(ref entityToComponent, targetLength);
			Array.Fill(
				entityToComponent, FreeComponentSlotMarker,
				startIndex: oldLength, count: targetLength - oldLength
			);
			Array.Resize(ref sparseEntities, targetLength);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ResizeRecycledEntitiesFor(ushort entityIndex)
		{
			int targetLength = recycledComponents.Length;
			while ((targetLength <<= 1) <= entityIndex) { }
			Array.Resize(ref recycledComponents, targetLength);
		}

		public bool TryRemove(Entity entity)
		{
			if (IsLocked) {
				throw new Exception(CollectionLockedMessage);
			}
			ushort entityIndex = entity.Index;
			ValidateEntity(entity);
			if (entity.Index >= entityToComponent.Length) {
				return false;
			}
			ushort redirection = entityToComponent[entityIndex];
			if (redirection == FreeComponentSlotMarker) {
				return false;
			}
			entityToComponent[entityIndex] = FreeComponentSlotMarker;
			components[redirection] = default;
			if (freeComponentCount == recycledComponents.Length) {
				ResizeRecycledEntitiesFor(freeComponentCount);
			}
			World.OnRemoveComponent(entityIndex, componentMask);
			recycledComponents[freeComponentCount] = redirection;
			freeComponentCount++;

			ushort idx = sparseEntities[entityIndex];
			if (idx < --entityCount) {
				ushort lastEntity = entities[entityCount];
				entities[idx] = lastEntity;
				sparseEntities[lastEntity] = idx;
			}
			return true;
		}

		void World.IPool.Lock() => ++lockCount;

		void World.IPool.Unlock() => --lockCount;


	}

	public static class Pool
	{
		public class ComponentAlreadyExistsException : System.Exception
		{
			public ComponentAlreadyExistsException(Entity entity)
				: base($"A component has already been created for entity {entity.Index}")
			{ }
		}
	}
}