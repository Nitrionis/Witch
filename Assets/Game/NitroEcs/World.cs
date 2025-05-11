using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Assets.Game.NitroEcs
{
	public sealed class World
	{
		private const int EntityGenBitCount = 16;
		internal const ushort UnusedGeneration = 0;
		private const ushort LowestPossibleGeneration = 1;

		private const int BitCountPerSlot = 64;
		private int slotCountPerEntity;
		private ulong[] slots;

		private int recycledEntitiesCount;
		private ushort[] recycledEntities;

		private IPool[] pools;
		private readonly Dictionary<Type, IPool> poolHashes;

		private int systemCount;
		private ISystem[] systems;

		private int lockCount;

		public int SlotCountPerEntity => slotCountPerEntity;

		public World()
		{
			poolHashes = new Dictionary<Type, IPool>();
			systems = new ISystem[16];
		}

		public Chain.Segment GetEntitiesWithout(Span<ComponentMask> componentsToExclude, Chain chain)
		{
			Span<ulong> maskMemory = stackalloc ulong[SlotCountPerEntity];
			foreach (var component in componentsToExclude) {
				maskMemory[component.SlotOffset] |= component.SlotMask;
			}
			var componentsMask = new ComponentsMask(this, maskMemory);

			var headSegment = chain.Rent();
			var tailSegment = headSegment;

			ushort entityCount = 0;
			int segmentLocalIndex = 1;
			int maxEntityCount = slots.Length * slotCountPerEntity;
			for (ushort i = 0; i < maxEntityCount; i++) {
				if (componentsMask.Match(i, out var entityGeneration)) {
					if (segmentLocalIndex == Chain.Segment.LastItemIndex) {
						ConnectNextSegment();
						continue;
					}
					chain[tailSegment, segmentLocalIndex++] =
						new Chain.Item(new Entity(i, entityGeneration));
					entityCount++;
				}
			}
			chain[headSegment, 0] = entityCount;
			if (segmentLocalIndex == Chain.Segment.LastItemIndex) {
				ConnectNextSegment();
			}
			chain[tailSegment, segmentLocalIndex++] = 0;
			if (segmentLocalIndex == Chain.Segment.LastItemIndex) {
				ConnectNextSegment();
			}
			chain[tailSegment, segmentLocalIndex++] = 0;
			
			return headSegment;

			void ConnectNextSegment()
			{
				var segment = chain.Rent();
				chain[tailSegment, segmentLocalIndex++] = new Chain.Item(segment);
				segmentLocalIndex = 0;
				tailSegment = segment;
			}
		}

		internal void Lock() => ++lockCount;

		internal void Unlock() => --lockCount;

		internal readonly ref struct ComponentsMask
		{
			private readonly ReadOnlySpan<ulong> worldSlots;
			private readonly ReadOnlySpan<ulong> slotsMasks;

			public ComponentsMask(World world, ReadOnlySpan<ulong> slotsMasks)
			{
				if (slotsMasks.Length != world.slotCountPerEntity) {
					throw new Exception();
				}
				worldSlots = world.slots;
				this.slotsMasks = slotsMasks;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool Match(ushort entityIndex, out ushort entityGeneration)
			{
				unchecked {
					int offset = entityIndex * slotsMasks.Length;
					if (offset + slotsMasks.Length > worldSlots.Length) {
						throw new Exception();
					}
					ulong mask = slotsMasks[0];
					ulong firstSlot = worldSlots[offset];
					entityGeneration = (ushort)firstSlot;
					bool result = (firstSlot & mask) == mask;
					for (int i = 1; i < slotsMasks.Length; ++i) {
						mask = slotsMasks[i];
						result &= (worldSlots[offset + i] & mask) == mask;
					}
					return result;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ushort GetGeneration(ushort index) =>
			unchecked((ushort)slots[index * slotCountPerEntity]);

		public bool Contains(Entity entity)
		{
			if (
				entity.Generation == UnusedGeneration ||
				entity.Index >= slots.Length
			) {
				return false;
			}
			return GetGeneration(entity.Index) == entity.Generation;
		}

		public void Tick()
		{
			for (int i = 0; i < systemCount; i++) {
				systems[i].Tick();
			}
		}

		public World RegisterSystem(ISystem system)
		{
			if (pools != null) {
				throw new Exception($"Can't add new system after {nameof(Construct)}");
			}
			if (system == null || systems.Contains(system)) {
				throw new ArgumentException();
			}
			systemCount++;
			if (systemCount == systems.Length) {
				Array.Resize(ref systems, systems.Length << 1);
			}
			systems[systemCount - 1] = system;
			return this;
		}

		public World AddPool<T>() where T : struct
		{
			if (pools != null) {
				throw new Exception($"Can't add new pool after {nameof(Construct)}");
			}
			if (!poolHashes.ContainsKey(typeof(T))) {
				poolHashes.Add(typeof(T), new Pool<T>(this));
			}
			return this;
		}

		public void Construct()
		{
			if (pools != null) {
				throw new Exception($"Dublicate {nameof(World)}.{nameof(Construct)} call");
			}
			for (int i = 0; i < systemCount; i++) {
				systems[i].RegisterComponentTypes();
			}
			pools = poolHashes.Values.ToArray();
			for (int i = 0; i < pools.Length; i++) {
				pools[i].ComponentMask = GetComponentInfo(i);
			}
			int requiredBitCount = pools.Length + EntityGenBitCount;
			while (++slotCountPerEntity * BitCountPerSlot <= requiredBitCount) {}
			for (int i = 0; i < systemCount; i++) {
				systems[i].FinalInitialize();
			}
			FillRecycledEntities();
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void FillRecycledEntities()
		{
			if (recycledEntitiesCount != 0) {
				throw new Exception();
			}
			if (slots == null) {
				const int initialCapacity = 512;
				slots = new ulong[initialCapacity * slotCountPerEntity];
				recycledEntities = new ushort[initialCapacity];
				for (ushort i = 0; i < recycledEntities.Length; i++) {
					recycledEntities[i] = i;
				}
			} else {
				Tools.ThreadSafeResize(ref slots, slots.Length << 1);
				int halfEntityCount = (slots.Length / slotCountPerEntity) >> 1;
				if (halfEntityCount != recycledEntities.Length) {
					recycledEntities = new ushort[halfEntityCount];
				}
				if (halfEntityCount == 65536) {
					throw new Exception("Too many entities.");
				}
				for (int i = 0; i < halfEntityCount; i++) {
					recycledEntities[i] = (ushort)(halfEntityCount + i);
				}
			}
			recycledEntitiesCount = recycledEntities.Length;
		}

		public Entity NewEntity()
		{
			unchecked {
				if (recycledEntitiesCount == 0) {
					FillRecycledEntities();
				}
				ushort index = recycledEntities[--recycledEntitiesCount];
				int offset = index * slotCountPerEntity;
				ushort generation = (ushort)slots[offset];
				generation = generation != ushort.MaxValue
					? (ushort)(generation + 1)
					: LowestPossibleGeneration;
				slots[offset] = generation;
				return new Entity(index, generation);
			}
		}

		public bool IsRemoveLocked => lockCount > 0;

		public bool TryRemoveEntity(Entity entity)
		{
			unchecked {
				if (IsRemoveLocked) {
					throw new InvalidOperationException();
				}
				int offset = slotCountPerEntity * entity.Index;
				if (offset >= slots.Length) {
					return false;
				}
				ushort generation = (ushort)slots[offset];
				if (generation == UnusedGeneration || generation != entity.Generation) {
					return false;
				}
				if (recycledEntitiesCount == recycledEntities.Length) {
					Array.Resize(ref recycledEntities, recycledEntitiesCount << 1);
				}
				recycledEntities[recycledEntitiesCount++] = entity.Index;

				int slot = 0;
				int poolIndexOffset = EntityGenBitCount;
				do {
					const ulong mask1 = 0x1u;
					const ulong mask4 = 0xFu;
					const ulong mask8 = 0xFFu;
					const ulong mask16 = 0xFFFFu;
					const ulong mask32 = 0xFFFF_FFFFu;
					int poolIndex = 0;
					ulong componentBits = slots[offset + slot] >> poolIndexOffset;
					while (componentBits > 0) {
						if ((componentBits & mask4) == 0) {
							componentBits >>= 4;
							poolIndex += 4;
							if ((componentBits & mask32) == 0) {
								componentBits >>= 32;
								poolIndex += 32;
							}
							if ((componentBits & mask16) == 0) {
								componentBits >>= 16;
								poolIndex += 16;
							}
							if ((componentBits & mask8) == 0) {
								componentBits >>= 8;
								poolIndex += 8;
							}
							if ((componentBits & mask4) == 0) {
								componentBits >>= 4;
								poolIndex += 4;
							}
						}
						if ((componentBits & mask1) > 0) {
							pools[poolIndex].TryRemove(entity);
						}
						componentBits >>= 1;
						poolIndex++;
					}
					poolIndexOffset = 0;
					// clear component bits
					slots[offset + slot] = 0;
				} while (++slot < slotCountPerEntity);
				// mark entity as free
				slots[offset] = (ulong)(generation << EntityGenBitCount);
				return true;
			}
		}

		/// <summary>
		/// Used to update masks to quickly check the presence of a component on an entity.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void OnRemoveComponent(ushort entityIndex, ComponentMask componentMask)
		{
			slots[entityIndex * slotCountPerEntity + componentMask.SlotOffset] &= ~componentMask.SlotMask;
		}

		/// <summary>
		/// Used to update masks to quickly check the presence of a component on an entity.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void OnAddComponent(ushort entityIndex, ComponentMask componentMask)
		{
			slots[entityIndex * slotCountPerEntity + componentMask.SlotOffset] |= componentMask.SlotMask;
		}

		private ComponentMask GetComponentInfo(int componentPoolIndex)
		{
			const int ulongBitCount = 64;
			componentPoolIndex += EntityGenBitCount;
			int componentSlotOffset = componentPoolIndex >> 6; // divide by ulongBitCount
			ulong mask = 1ul << (componentPoolIndex - componentSlotOffset * ulongBitCount);
			return new ComponentMask { SlotMask = mask, SlotOffset = componentSlotOffset };
		}

		public Pool<T> GetPool<T>() where T : struct
		{
			if (poolHashes.TryGetValue(typeof(T), out var pool)) {
				return (Pool<T>)pool;
			}
			throw new ArgumentException($"Type {typeof(T).Name} not registered");
		}

		public bool TryGetPool<T>(out Pool<T> pool) where T : struct
		{
			pool = null;
			if (poolHashes.TryGetValue(typeof(T), out IPool p)) {
				pool = (Pool<T>)p;
			}
			return pool != null;
		}

		public struct ComponentMask
		{
			public int SlotOffset;
			public ulong SlotMask;
		}

		public interface ISystem
		{
			void RegisterComponentTypes();
			void FinalInitialize();
			void Tick();
		}

		internal interface IPool
		{
			bool Has(Entity entity);
			bool TryRemove(Entity entity);
			void Lock();
			void Unlock();
			ComponentMask ComponentMask { set; }
		}
	}
}