using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Game.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Game.Server
{
	internal unsafe struct EventsStream
	{
		private const int CommandAlignment = 4;

		private volatile AllocationBlock* disposeChainFirst;
		private volatile AllocationBlock* disposeChainLast;

		private volatile EventsBlock* freeBlocksChainFirst;
		private volatile EventsBlock* freeBlocksChainLast;

		private volatile EventsBlock* processingChainFirst;
		private volatile EventsBlock* processingChainLast;

		private struct AllocationBlock
		{
			public volatile AllocationBlock* Next;
			public Repeat8<EventsBlock> EventBlocks;
		}

		public struct EventsBlock
		{
			public const int DataSize = 4096;

			public volatile EventsBlock* Next;
			public Repeat4096<byte> Data;
		}

		private const int IterationCountToTimeout = 100_000;

		public EventsStream(IEnumerable<ICommandProcessor> commandProcessors, Pool<EventsBlock> pool)
		{
			//rareAccessData = new(pool);
			//readPosition = EventsBlock.DataSize;
			//writePosition = EventsBlock.DataSize;
			//var processors = new List<ICommandProcessor>(commandProcessors);
			//this.commandProcessors = new ICommandProcessor[1 + processors.Count];
			//foreach (var processor in processors) {
			//	int processorId = 1 + processor.CommandId;
			//	if (this.commandProcessors[processorId] != null) {
			//		throw new Exception($"Duplicate command id: {processor.CommandId}");
			//	}
			//	this.commandProcessors[processorId] = processor;
			//	if (processor.CommandAlignment != 4) {
			//		throw new Exception($"Unsupported command alignment command id: {processor.CommandId}");
			//	}
			//}
			//if (sizeof(CommandInfo) != 4 || UnsafeUtility.AlignOf<CommandInfo>() != 1) {
			//	throw new Exception($"Invalid CommandInfo layout");
			//}
		}

		public struct Writer
		{
			private EventsStream* stream;
			private EventsBlock* writeBlockPtr;
			private byte* dataPtr;
			private int writePosition;

			public void Push<T>(T evt) where T : unmanaged, ICommand
			{
				if (writePosition + sizeof(CommandInfo) + sizeof(T) >= EventsBlock.DataSize) {
					ConnectNextWriteBlock();
				}
				*(CommandInfo*)(dataPtr + writePosition) = new CommandInfo {
					CommandId = evt.Id,
					CommandCount = 1,
					CommandSize = unchecked((byte)sizeof(T))
				};
				writePosition += CommandAlignment;
				*(T*)(dataPtr + writePosition) = evt;
				writePosition += sizeof(T);
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			private void ConnectNextWriteBlock()
			{
				if (writePosition + sizeof(CommandInfo) <= EventsBlock.DataSize) {
					*(CommandInfo*)((byte*)&writeBlockPtr->Data + writePosition) = new CommandInfo {
						CommandId = 0,
						CommandCount = 1,
						CommandSize = 0
					};
				}
				
				writeBlockPtr = ;
				writePosition = 0;
			}
		}

		public struct Reader
		{
			private EventsStream* stream;
			private EventsBlock* readBlockPtr;
			private byte* readDataPtr;
			private int readPosition;
			private NativeArray<ProcessorMethodPointer> commandProcessors;

			public Reader(NativeArray<ProcessorMethodPointer> commandProcessors) : this()
			{
				this.commandProcessors = commandProcessors;
			}

			public void Process()
			{
				while (true) {
					if (readPosition + sizeof(CommandInfo) >= EventsBlock.DataSize) {
						if (!TryDequeueNextReadBlock()) {
							return;
						}
					}
					var info = *(CommandInfo*)(readDataPtr + readPosition);
					readPosition += CommandAlignment;
					var method = commandProcessors[info.CommandId];
					method.Process(
						method.Processor,
						info.CommandCount,
						readDataPtr + readPosition
					);
					readPosition += info.CommandSize * info.CommandCount;
				}
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			private bool TryDequeueNextReadBlock()
			{
				if (readBlockPtr != null) {
					EventsBlock* freeLast = stream->freeBlocksChainLast;
					if (freeLast == null) {
						var field = (IntPtr*)&stream->freeBlocksChainLast;
						freeLast = (EventsBlock*)Interlocked.CompareExchange(
							ref *field, (IntPtr)readBlockPtr, (IntPtr)null
						);

					}

					readBlockPtr = null;
				}


				readBlockPtr = ;
				readPosition = 0;
				return true;
			}

			public struct ProcessorMethodPointer
			{
				public void* Processor;
				public delegate* unmanaged[Cdecl]<void*, int, byte*, void> Process;

				public delegate void ProcessDelegate(void* processor, int commandCount, byte* commands);
			}
		}

		private struct CommandInfo
		{
			public byte CommandId;
			public byte CommandCount;
			public byte CommandSize;
			public byte PlayerId;
		}
	}



	/*internal unsafe class EventsStream
	{
		public const int BlockSize = 4096;
		private const int CommandAlignment = 4;

		private RareAccessData rareAccessData;
		private readonly ICommandProcessor[] commandProcessors;
		
		private int readPosition;
		private byte* readBlockPtr;
		
		private int writePosition;
		private byte* writeBlockPtr;
		
		public struct EventsBlock
		{
			public Repeat4096<byte> Data;
		}

		private class RareAccessData
		{
			public readonly Pool<EventsBlock> pool;
			public readonly Queue<Pool<EventsBlock>.Slot> processingQueue = new();

			public Pool<EventsBlock>.Slot readBlock;
			public Pool<EventsBlock>.Slot writeBlock;

			public RareAccessData(Pool<EventsBlock> pool) => this.pool = pool;
		}

		// TODO pool is unsafe in this context
		public EventsStream(IEnumerable<ICommandProcessor> commandProcessors, Pool<EventsBlock> pool)
		{
			rareAccessData = new(pool);
			readPosition = BlockSize;
			writePosition = BlockSize;
			var processors = new List<ICommandProcessor>(commandProcessors);
			this.commandProcessors = new ICommandProcessor[1 + processors.Count];
			foreach (var processor in processors) {
				int processorId = 1 + processor.CommandId;
				if (this.commandProcessors[processorId] != null) {
					throw new Exception($"Duplicate command id: {processor.CommandId}");
				}
				this.commandProcessors[processorId] = processor;
				if (processor.CommandAlignment != 4) {
					throw new Exception($"Unsupported command alignment command id: {processor.CommandId}");
				}
			}
			if (sizeof(CommandInfo) != 4 || UnsafeUtility.AlignOf<CommandInfo>() != 1) {
				throw new Exception($"Invalid CommandInfo layout");
			}
		}

		public void Process()
		{
			while (true) {
				if (readPosition + sizeof(CommandInfo) >= BlockSize) {
					if (!TryDequeueNextReadBlock()) {
						return;
					}
				}
				var info = *(CommandInfo*)(readBlockPtr + readPosition);
				readPosition += CommandAlignment;
				commandProcessors[info.CommandId].Process(info.CommandCount, readBlockPtr + readPosition);
				readPosition += info.CommandSize * info.CommandCount;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private bool TryDequeueNextReadBlock()
		{
			if (rareAccessData.processingQueue.TryDequeue(out var block)) {
				if (readBlockPtr != null) {
					rareAccessData.pool.Release(rareAccessData.readBlock);
				}
				rareAccessData.readBlock = block;
				readBlockPtr = (byte*)block.ItemPointer;
				readPosition = 0;
				return true;
			}
			return false;
		}

		public void Push<T>(T evt) where T : unmanaged, ICommand
		{
			if (writePosition + sizeof(CommandInfo) + sizeof(T) >= BlockSize) {
				ConnectNextWriteBlock();
			}
			*(CommandInfo*)(writeBlockPtr + writePosition) = new CommandInfo {
				CommandId = evt.Id,
				CommandCount = 1,
				CommandSize = unchecked((byte)sizeof(T))
			};
			writePosition += CommandAlignment;
			*(T*)(writeBlockPtr + writePosition) = evt;
			writePosition += sizeof(T);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ConnectNextWriteBlock()
		{
			if (writePosition + sizeof(CommandInfo) <= BlockSize) {
				*(CommandInfo*)(readBlockPtr + writePosition) = new CommandInfo {
					CommandId = 0,
					CommandCount = 1,
					CommandSize = 0
				};
			}
			rareAccessData.processingQueue.Enqueue(rareAccessData.writeBlock);
			rareAccessData.writeBlock = rareAccessData.pool.Rent();
			writeBlockPtr = (byte*)rareAccessData.writeBlock.ItemPointer;
			writePosition = 0;
		}

		public void Push(Pool<EventsBlock>.Slot block)
		{
			ConnectNextWriteBlock();
			rareAccessData.processingQueue.Enqueue(block);
		}

		public Pool<EventsBlock>.Slot RentBlock() => rareAccessData.pool.Rent();

		public void Flush()
		{
			if (writePosition == 0) {
				return;
			}
			ConnectNextWriteBlock();
		}

		private struct CommandInfo
		{
			public byte CommandId;
			public byte CommandCount;
			public byte CommandSize;
			public byte FakeFieldForPadding;
		}
	}*/
}
