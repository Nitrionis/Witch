using Unity.Mathematics;
using Unity.Entities;
using System.Runtime.CompilerServices;
using Game.Database;

namespace Game.Server
{
	internal interface ICommand
	{
		byte Id { get; }
	}

	internal unsafe interface ICommandProcessor
	{
		byte CommandId { get; }
		byte CommandAlignment { get; }
		void Process(int count, byte* commands);
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
				get => 0;
			}
		}

		internal struct DestroyEntity : ICommand
		{
			public Entity Entity;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 1;
			}
		}

		internal struct SetEntityHost : ICommand
		{
			public int HostId;
			public Entity Entity;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 2;
			}
		}

		internal struct PlaceObject : ICommand
		{
			public DatabaseObjectId Object;
			public float3 Position;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 3;
			}
		}

		internal struct SetTransform : ICommand
		{
			public Entity Entity;
			public float3 Position;
			public float3 Direction;
			public double TimeSinceStartup;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 4;
			}
		}

		internal struct SetHealth : ICommand
		{
			public int Health;
			public Entity Entity;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 5;
			}
		}

		internal struct UseMagicStaff : ICommand
		{
			public int MagicStaffId;
			public float3 Direction;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 6;
			}
		}

		internal struct AddElementalAuras : ICommand
		{
			public ElementalAura Aura;
			public float Value;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 7;
			}
		}

		internal struct SetElementalAuras : ICommand
		{
			public ElementalAura Aura;
			public float Value;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 8;
			}
		}

		internal struct SetInventoryItem : ICommand // TODO local item id inside inventory
		{
			public int ItemId;
			public int Count;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 9;
			}
		}

		internal struct DropInventoryItem : ICommand // TODO local item id inside inventory
		{
			public int ItemId;
			public int Count;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 10;
			}
		}

		internal struct UseInventoryItem : ICommand // TODO local item id inside inventory
		{
			public int ItemId;
			public int Count;
			public Entity Target;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 11;
			}
		}

		internal struct Pickup : ICommand
		{
			public Entity Target;

			public byte Id
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => 12;
			}
		}
	}
}
