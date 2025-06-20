using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Assets.Game.Collections;
using Assets.Game.Tools;

namespace Assets.Game.ClientServer
{
	/// <summary>
	/// The only way to transfer data between client and server parts.
	/// </summary>
	public unsafe class GameShell : IDisposable
	{
		private object producer;
		private object consumer;

		private readonly NativeMemoryChain memoryChain = new();

		private int commandCountForProcessing;

		public GameShell()
		{
			if (sizeof(Command) != MemoryController.Alignment) {
				throw new Exception($"sizeof({nameof(Command)}) does not match alignment");
			}
			memoryChain.ConnectNextSection();
		}

		public void Dispose()
		{
			memoryChain.Dispose();
		}

		public interface ICommandProcessor
		{
			Guid Guid { get; }
			ushort CommandId { get; }
			int DataUnitSize { get; }
			Type DataUnitType { get; }
			void Process(void* dataUnits, int dataUnitCount);
		}

		public interface ICommandProcessorCollection
		{
			ICommandProcessor this[int commandId] { get; }
			ICommandProcessor this[Guid guid] { get; }
		}

		public readonly struct Command<TDataUnit> where TDataUnit : unmanaged
		{
			public readonly ushort Id;
			public Command(ICommandProcessor processor)
			{
				Id = processor.CommandId;
				if (!ReferenceEquals(typeof(TDataUnit), processor.DataUnitType)) {
					throw new ArgumentException(
						$"Invalid data unit type: {typeof(TDataUnit).FullName} != {processor.DataUnitType.FullName}"
					);
				}
				int alignment = Alignment<TDataUnit>.Get();
				if (alignment > MemoryController.Alignment) {
					throw new ArgumentException($"Unsupported struct (alignment > 8): {alignment}");
				}
				int size = sizeof(TDataUnit);
				if (size % MemoryController.Alignment != 0) {
					throw new ArgumentException($"Unsupported struct (size % 8 != 0): {alignment}");
				}
			}
		}

		[StructLayout(LayoutKind.Sequential, Size = MemoryController.Alignment)]
		private struct Command
		{
			/// <summary>
			/// Command processor index.
			/// </summary>
			public ushort Id;

			/// <summary>
			/// Number of data components.
			/// </summary>
			public ushort DataUnitCount;
		}

		[StructLayout(LayoutKind.Sequential, Pack = MemoryController.Alignment)]
		private struct PairCommandAnd<TDataUnit>
			where TDataUnit : unmanaged
		{
			public Command Command;
			public TDataUnit DataUnit;
		}

		public abstract class MemoryController
		{
			// Never change alignment.
			public const int Alignment = 8;

			protected readonly GameShell commandShell;

			protected MemoryController(GameShell commandShell) => this.commandShell = commandShell;

			protected byte* currentPosition;
			protected int remainingSpace;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected TDataUnit* AcquirePlaceFor<TDataUnit>(int slotCount = 1)
				where TDataUnit : unmanaged
			{
				return (TDataUnit*)AcquirePlace(sizeof(TDataUnit), slotCount);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected byte* AcquirePlace(int dataUnitSize, int slotCount = 1)
			{
				int dataSize = dataUnitSize * slotCount;
				// Check if we need a new block
				if (remainingSpace < dataSize) {
					JumpToNextSection();
				}

				// Obtain pointer to write data
				var ptr = currentPosition;

				currentPosition += dataSize;
				remainingSpace -= dataSize;

				return ptr;
			}

			private int GetPaddingToAlignCurrentPosition()
			{
				int padding = (int)((ulong)currentPosition % Alignment);
				if (padding > 0) {
					padding = Alignment - padding;
				}
				return padding;
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			protected void JumpToNextSection()
			{
				UpdateMemoryChainSection();
				int padding = GetPaddingToAlignCurrentPosition();
				currentPosition += padding;
				remainingSpace -= padding;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected void SkipCurrentSectionMemory(int byteCount)
			{
				currentPosition += byteCount;
				remainingSpace -= byteCount;
			}

			protected abstract void UpdateMemoryChainSection();
		}

		public class Producer : MemoryController
		{
			public const int SubsequenceSlotCount = 4;

			private int commandCount;

			public Producer(GameShell commandShell) : base(commandShell)
			{
				if (commandShell.producer != null) {
					throw new InvalidOperationException();
				}
				commandShell.producer = this;
				currentPosition = (byte*)commandShell.memoryChain.Head;
				remainingSpace = NativeMemoryChain.SectionSize;
			}

			public void Push<TDataUnit>(
				Command<TDataUnit> command, ref TDataUnit dataUnit
			)
				where TDataUnit : unmanaged
			{
				// Calculate alignment padding
				int dataSize = sizeof(PairCommandAnd<TDataUnit>);
				if (remainingSpace >= dataSize) {
					var slotPtr =
						(PairCommandAnd<TDataUnit>*)currentPosition;
					currentPosition += dataSize;
					remainingSpace -= dataSize;

					slotPtr->Command = new Command {
						Id = command.Id,
						DataUnitCount = 1
					};
					slotPtr->DataUnit = dataUnit;

					commandCount++;
				} else {
					PushSingleSlow(command, ref dataUnit);
				}
			}

			private void PushSingleSlow<TDataUnit>(
				Command<TDataUnit> command, ref TDataUnit dataUnit
			)
				where TDataUnit : unmanaged
			{
				var commandSlotPtr = AcquirePlaceFor<Command>();
				*AcquirePlaceFor<TDataUnit>() = dataUnit;
				*commandSlotPtr = new Command {
					Id = command.Id,
					DataUnitCount = 1
				};
				commandCount++;
			}

			public void Push<TDataUnit, TEnumerator>(
				Command<TDataUnit> command, TEnumerator dataUnits
			)
				where TEnumerator : IEnumerator<TDataUnit>
				where TDataUnit : unmanaged
			{
				var cmdPtr = AcquirePlaceFor<Command>();
				ushort dataUnitCount = 0;
				int slotIndex = SubsequenceSlotCount;
				TDataUnit* dataPtr = null;
				while (dataUnits.MoveNext()) {
					if (slotIndex == SubsequenceSlotCount) {
						slotIndex = 0;
						dataPtr = AcquirePlaceFor<TDataUnit>(SubsequenceSlotCount);
					}
					dataPtr[slotIndex++] = dataUnits.Current;
					dataUnitCount++;
				}
				*cmdPtr = new Command {
					Id = command.Id,
					DataUnitCount = dataUnitCount
				};
				commandCount++;
			}

			protected override void UpdateMemoryChainSection()
			{
				// Add a new section
				var memoryChain = commandShell.memoryChain;
				memoryChain.ConnectNextSection();
				currentPosition = (byte*)memoryChain.Head;
				remainingSpace = NativeMemoryChain.SectionSize;
			}

			public void FlushStream()
			{
				Interlocked.Add(ref commandShell.commandCountForProcessing, commandCount);
				commandCount = 0;
			}
		}

		public class Consumer : MemoryController
		{
			private readonly ICommandProcessorCollection commandProcessors;

			public Consumer(
				GameShell commandShell,
				ICommandProcessorCollection commandProcessors
			)
				: base(commandShell)
			{
				if (commandShell.consumer != null) {
					throw new InvalidOperationException();
				}
				commandShell.consumer = this;
				this.commandProcessors = commandProcessors;
			}

			public void Consume()
			{
				// atomic read
				int commandCount = commandShell.commandCountForProcessing;
				Interlocked.Add(ref commandShell.commandCountForProcessing, -commandCount);
				while (commandCount > 0) {
					commandCount--;
					var command = *AcquirePlaceFor<Command>();
					var processor = commandProcessors[command.Id];
					if (command.DataUnitCount == 1) {
						if (remainingSpace < processor.DataUnitSize) {
							JumpToNextSection();
						}
						processor.Process(currentPosition, dataUnitCount: 1);
						SkipCurrentSectionMemory(processor.DataUnitSize);
					} else if (command.DataUnitCount > 0) {
						int duc = 0;
						while (true) {
							duc += Producer.SubsequenceSlotCount;
							var subsection = AcquirePlace(
								processor.DataUnitSize,
								Producer.SubsequenceSlotCount
							);
							int dataUnitCount = Producer.SubsequenceSlotCount;
							if (duc >= command.DataUnitCount) {
								dataUnitCount -= duc - command.DataUnitCount;
								processor.Process(subsection, dataUnitCount);
								break;
							}
							processor.Process(subsection, dataUnitCount);
						}
					}
				}
			}

			protected override void UpdateMemoryChainSection()
			{
				var memoryChain = commandShell.memoryChain;
				if (currentPosition != null) {
					memoryChain.ReleaseTailSection();
				}
				if (memoryChain.Tail == null || memoryChain.Head == null) {
					// This is not supported as it causes a data race.
					throw new Exception("Insufficient data to process commands");
				}
				currentPosition = (byte*)memoryChain.Tail;
				remainingSpace = NativeMemoryChain.SectionSize;
			}
		}
	}
}
