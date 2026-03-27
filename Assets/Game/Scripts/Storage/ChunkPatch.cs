using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Game.Storage
{
	[StructLayout(LayoutKind.Explicit)]
	internal struct ChunkPatch
	{
		[FieldOffset(0)]
		private ulong unusedFieldForAlignment;

		[FieldOffset(0)]
		public Repeat4096<byte> CommandsMemory;

		public struct Metadata : IEquatable<Metadata>
		{
			public ushort FreeSpace;
			public ulong Version;

			public Metadata(ushort freeSpace, ulong version)
			{
				FreeSpace = freeSpace;
				Version = version;
			}

			public override bool Equals(object obj) =>
				obj is Metadata other && Equals(other);

			public bool Equals(Metadata other) =>
				FreeSpace == other.FreeSpace &&
				Version == other.Version;

			public override int GetHashCode() =>
				HashCode.Combine(FreeSpace, Version);

			public static bool operator ==(Metadata left, Metadata right) => left.Equals(right);
			public static bool operator !=(Metadata left, Metadata right) => !left.Equals(right);
		}

		public readonly struct CommandId
		{
			public const int MaxCommandIndex = 127;

			public readonly byte InternalValue;

			public int CommandIndex => InternalValue & 0x7F;

			/// <summary>
			/// <para>
			/// If true, indicates that the next byte in the stream is the number of commands. 
			/// This byte may also be followed by unused bytes to provide proper alignment.
			/// </para>
			/// <para>If false, indicates that the next byte in the stream is the first byte of the command or unused bytes to provide proper alignment.</para>
			/// </summary>
			public bool HasCommandCountFlag => (InternalValue & 0x80) != 0;

			public CommandId(byte internalValue) => InternalValue = internalValue;
		}

		/// <summary>
		/// Used to quickly find commands for creating or deleting objects of a certain type.
		/// </summary>
		public struct NextItemLocation
		{
			public ObjectId Id;

			/// <summary>
			/// The location of the next command to create or delete an object with specific ObjectId.
			/// </summary>
			public ushort Location;

			public NextItemLocation(ObjectId id, ushort location)
			{
				Id = id;
				Location = location;
			}
		}

		public struct CreateEntityCommand
		{
			public ChunkEntityId Entity;

			/// <summary>
			/// Chunk relative position.
			/// </summary>
			public float3 Position;

			/// <summary>
			/// These could be creatures, environments, or loot.
			/// </summary>
			public ObjectId Id;

			public CreateEntityCommand(ChunkEntityId entity, float3 position, ObjectId id)
			{
				Entity = entity;
				Position = position;
				Id = id;
			}
		}

		public struct CreateLootObjectCommand
		{
			public CreateEntityCommand EntityData;
			public int Count;

			public CreateLootObjectCommand(CreateEntityCommand entityData, int count)
			{
				EntityData = entityData;
				Count = count;
			}
		}

		public struct DeleteObjectCommand
		{
			public ChunkEntityId Entity;

			public DeleteObjectCommand(ChunkEntityId entity) => Entity = entity;
		}

		public struct SetHealthCommand
		{
			public ChunkEntityId Entity;
			public ushort Health;

			public SetHealthCommand(ChunkEntityId entity, ushort health)
			{
				Entity = entity;
				Health = health;
			}
		}

		public struct SetWorldHeightMapCommand
		{
			public byte2 ChunkRelativePosition;
			/// <summary>
			/// Memory for 16 (4*4) heights
			/// </summary>
			public Repeat16<ushort> Heights;
		}
	}
}
