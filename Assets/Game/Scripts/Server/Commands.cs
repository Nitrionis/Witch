using Unity.Mathematics;
using Unity.Entities;
using System.Runtime.CompilerServices;
using Game.Database;
using Unity.Collections.LowLevel.Unsafe;

namespace Game.Server
{
	internal interface ICommand
	{
		byte Id { get; }
		int Aligment { get; }
		int Size { get; }
	}

	internal static class Command
	{
		internal struct CreateEntity : ICommand
		{
			public Entity Entity;
			public DatabaseObjectId Object;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 1;
			}

			public int Aligment => UnsafeUtility.AlignOf<CreateEntity>();
			public int Size => UnsafeUtility.SizeOf<CreateEntity>();
		}

		internal struct DestroyEntity : ICommand
		{
			public Entity Entity;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 2;
			}

			public int Aligment => UnsafeUtility.AlignOf<DestroyEntity>();
			public int Size => UnsafeUtility.SizeOf<DestroyEntity>();
		}

		internal struct SetEntityHost : ICommand
		{
			public int HostId;
			public Entity Entity;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 3;
			}

			public int Aligment => UnsafeUtility.AlignOf<SetEntityHost>();
			public int Size => UnsafeUtility.SizeOf<SetEntityHost>();
		}

		internal struct PlaceObject : ICommand // TODO
		{
			public DatabaseObjectId Object;
			public float3 Position;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 4;
			}

			public int Aligment => UnsafeUtility.AlignOf<PlaceObject>();
			public int Size => UnsafeUtility.SizeOf<PlaceObject>();
		}

		internal struct SetTransform : ICommand
		{
			public Entity Entity;
			public float3 Position;
			public float3 Direction;
			//public double TimeSinceStartup; TODO split in two parts

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 5;
			}

			public int Aligment => UnsafeUtility.AlignOf<SetTransform>();
			public int Size => UnsafeUtility.SizeOf<SetTransform>();
		}

		internal struct SetHealth : ICommand
		{
			public int Health;
			public Entity Entity;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 6;
			}

			public int Aligment => UnsafeUtility.AlignOf<SetHealth>();
			public int Size => UnsafeUtility.SizeOf<SetHealth>();
		}

		internal struct UseMagicStaff : ICommand
		{
			public int MagicStaffId;
			public float3 Direction;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 7;
			}

			public int Aligment => UnsafeUtility.AlignOf<UseMagicStaff>();
			public int Size => UnsafeUtility.SizeOf<UseMagicStaff>();
		}

		internal struct AddElementalAuras : ICommand
		{
			public ElementalAura Aura;
			public float Value;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 8;
			}

			public int Aligment => UnsafeUtility.AlignOf<AddElementalAuras>();
			public int Size => UnsafeUtility.SizeOf<AddElementalAuras>();
		}

		internal struct SetElementalAuras : ICommand
		{
			public ElementalAura Aura;
			public float Value;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 9;
			}

			public int Aligment => UnsafeUtility.AlignOf<SetElementalAuras>();
			public int Size => UnsafeUtility.SizeOf<SetElementalAuras>();
		}

		internal struct SetInventoryItem : ICommand // TODO local item id inside inventory
		{
			public int ItemId;
			public int Count;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 10;
			}

			public int Aligment => UnsafeUtility.AlignOf<SetInventoryItem>();
			public int Size => UnsafeUtility.SizeOf<SetInventoryItem>();
		}

		internal struct DropInventoryItem : ICommand // TODO local item id inside inventory
		{
			public int ItemId;
			public int Count;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 11;
			}

			public int Aligment => UnsafeUtility.AlignOf<DropInventoryItem>();
			public int Size => UnsafeUtility.SizeOf<DropInventoryItem>();
		}

		internal struct UseInventoryItem : ICommand // TODO local item id inside inventory
		{
			public int ItemId;
			public int Count;
			public Entity Target;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 12;
			}

			public int Aligment => UnsafeUtility.AlignOf<UseInventoryItem>();
			public int Size => UnsafeUtility.SizeOf<UseInventoryItem>();
		}

		internal struct Pickup : ICommand
		{
			public Entity Target;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 13;
			}

			public int Aligment => UnsafeUtility.AlignOf<Pickup>();
			public int Size => UnsafeUtility.SizeOf<Pickup>();
		}
	}
}
