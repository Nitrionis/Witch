using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;

namespace Game.ClientServer
{

	internal struct ClientServerMessagePack
	{
		public SetTransformCommand SetPlayerTransformCommand;
		public NativeList<PlaceObjectCommand> PlaceObjectCommands;
		public NativeList<UseMagicStaffCommand> UseMagicStaffCommands;
		public NativeList<AddElementalAurasCommand> AddElementalAurasCommands;
		public NativeList<UseInventoryItemCommand> UseInventoryItemCommands;
		public NativeList<DropInventoryItemCommand> DropInventoryItemCommands;
		public NativeList<PickupCommand> PickupCommands;
	}

	internal struct ServerClientMessagePack
	{
		public NativeList<CreateEntityCommand> CreateEntityCommands;
		public NativeList<DestroyEntityCommand> DestroyEntityCommands;
		public NativeList<SetTransformCommand> SetTransformCommands;
		public NativeList<SetHealthCommand> SetHealthCommands;
		public NativeList<SetElementalAurasCommand> SetElementalAurasCommands;
		public NativeList<SetInventoryItemCommand> SetInventoryItemCommands;
	}

	internal struct CreateEntityCommand
	{
		public Entity Entity;
		public int ObjectDatabaseId;
	}

	internal struct DestroyEntityCommand
	{
		public Entity Entity;
	}

	internal struct SetEntityHostCommand
	{
		public int HostId;
		public Entity Entity;
	}

	internal struct PlaceObjectCommand
	{
		public int ObjectDatabaseId;
		public int Health;
		public float3 Position;
	}
	
	internal struct SetTransformCommand
	{
		public Entity Entity;
		public float3 Position;
		public float3 Direction;
		public double TimeSinceStartup;
	}

	internal struct SetHealthCommand
	{
		public int Health;
		public Entity Entity;
	}

	internal struct UseMagicStaffCommand
	{
		public int MagicStaffId;
		public float3 Direction;
	}

	internal struct AddElementalAurasCommand
	{
		public ElementalAura Aura;
		public float Value;
	}

	internal struct SetElementalAurasCommand
	{
		public ElementalAura Aura;
		public float Value;
	}

	internal struct SetInventoryItemCommand
	{
		public int ItemId;
		public int Count;
	}

	internal struct DropInventoryItemCommand
	{
		public int ItemId;
		public int Count;
	}

	internal struct UseInventoryItemCommand
	{
		public int ItemId;
		public int Count;
		public Entity Target;
	}

	internal struct PickupCommand
	{
		public Entity Target;
	}
}
