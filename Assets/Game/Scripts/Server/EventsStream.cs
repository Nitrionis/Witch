using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AOT;
using Game.Allocators;
using Game.Collections;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Game.Server
{
	internal readonly unsafe struct EventsStream
	{
		private const int CommandAlignment = 4;
		private const byte SkipRestOfBlockCommandId = 0;

		private readonly StreamData* data;

		public EventsStream(
			DisposeList disposeList,
			SessionRewindableAllocator allocator,
			int writerCount,
			NativeArray<Reader.ProcessorMethod> processors
			)
		{
			data = allocator.AllocateArray<StreamData>(length: 1);
			*data = new StreamData(data, disposeList, allocator, writerCount, processors);
		}

		private struct StreamData
		{
			public NativeQueue<Pointer<EventsBlock>> FreeBlocks;
			public NativeQueue<Pointer<EventsBlock>> ProcessingQueue;

			public UnmanagedArray<WriterInfo> Writers;
			public ReaderInfo Reader;

			public readonly SessionRewindableAllocator Allocator;

			public StreamData(
				StreamData* self,
				DisposeList disposeList,
				SessionRewindableAllocator allocator,
				int writerCount,
				NativeArray<Reader.ProcessorMethod> processors
			) {
				if (!processors.IsCreated)
					throw new ArgumentException($"{nameof(processors)} is not created");
				Allocator = allocator;
				FreeBlocks = new NativeQueue<Pointer<EventsBlock>>(Unity.Collections.Allocator.Persistent);
				disposeList.Add(FreeBlocks);
				ProcessingQueue = new NativeQueue<Pointer<EventsBlock>>(Unity.Collections.Allocator.Persistent);
				disposeList.Add(ProcessingQueue);
				Writers = new UnmanagedArray<WriterInfo>(
					allocator.AllocateArray<WriterInfo>(writerCount), writerCount
				);
				for (int i = 0; i < Writers.Length; i++) {
					Writers[i] = new WriterInfo {
						CurrentBlock = default,
						DataPointer = null,
						WritePosition = EventsBlock.DataSize,
						ProcessingQueueWriter = ProcessingQueue.AsParallelWriter(),
						FreeBlocksWriter = FreeBlocks.AsParallelWriter()
					};
				}
				Reader = new ReaderInfo {
					CurrentBlock = default,
					DataPointer = null,
					ReadPosition = EventsBlock.DataSize,
					FreeBlocksWriter = FreeBlocks.AsParallelWriter(),
					CommandProcessors = processors
				};
				if (sizeof(CommandInfo) != 4 || UnsafeUtility.AlignOf<CommandInfo>() != 1) {
					throw new Exception($"Invalid CommandInfo layout");
				}
				if (typeof(ICommand).GetField(nameof(ICommand.Id)).FieldType != typeof(byte))
					throw new Exception($"ICommand.Id type must be byte");
				if (processors.Length != byte.MaxValue)
					throw new Exception($"Invalid {nameof(processors)} legth");
				var ids = new bool[byte.MaxValue];
				foreach (var commandType in FindAllCommandStructs()) {
					var command = (ICommand)Activator.CreateInstance(commandType);
					if (command.Aligment > 4 || command.Aligment < 4 && command.Size % 4 != 0)
						throw new Exception($"Invalid Command {commandType.Name} Aligment: {command.Aligment} Size: {command.Size}");
					if (command.Id == 0)
						throw new Exception($"Invalid Command {commandType.Name} Id {command.Id}");
					if (ids[command.Id])
						throw new Exception($"Non-unique Id {command.Id} Command {commandType.Name}");
					ids[command.Id] = true;
				}
				if (processors[0].Process != null || processors[0].Processor != null)
					throw new Exception($"processors[0] must be default because reserved");

				var processSkipRestOfBlock =
					BurstCompiler.CompileFunctionPointer<Reader.ProcessorMethod.ProcessDelegate>(SkipRestOfBlock);
				processors[0] = new Reader.ProcessorMethod(self, processSkipRestOfBlock);

				var riseErrorMethodPointer =
					BurstCompiler.CompileFunctionPointer<Reader.ProcessorMethod.ProcessDelegate>(RiseError);
				for (var i = 1; i < processors.Length; i++) {
					var p = processors[i];
					if (p.Process != null && p.Processor != null)
						continue;
					if (p.Process == null && p.Processor == null) {
						processors[i] = new Reader.ProcessorMethod((void*)i, riseErrorMethodPointer);
					} else {
						throw new Exception($"Incomplete processor [{i}] initialization");
					}
				}
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public bool TrySwitchToNextReadBlock()
			{
				if (!ProcessingQueue.TryDequeue(out var block)) {
					Reader.ReadPosition = EventsBlock.DataSize;
					return false;
				}
				if (Reader.CurrentBlock.IsNotNull) {
					Reader.FreeBlocksWriter.Enqueue(Reader.CurrentBlock);
				}
				Reader.ReadPosition = 0;
				Reader.CurrentBlock = block;
				Reader.DataPointer = (byte*)block.TypedPointer;
				return true;
			}
		}

		public struct EventsBlock
		{
			public const int DataSize = 2048;
			public Repeat2048<byte> Data;
		}

		[BurstCompile]
		[MonoPInvokeCallback(typeof(Reader.ProcessorMethod.ProcessDelegate))]
		private static void RiseError(Reader.ProcessorMethod.CallArgs args) =>
			throw new Exception($"No processor for command {(int)args.Processor}");

		[BurstCompile]
		[MonoPInvokeCallback(typeof(Reader.ProcessorMethod.ProcessDelegate))]
		private static void SkipRestOfBlock(Reader.ProcessorMethod.CallArgs args) =>
			((StreamData*)args.Processor)->TrySwitchToNextReadBlock();

		/// <summary>
		/// Finds all struct types that implement ICommand from non-Unity assemblies
		/// </summary>
		public static List<Type> FindAllCommandStructs()
		{
			var commandTypes = new List<Type>();
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				if (AssemblyTools.IsStandardAssembly(assembly))
					continue;
				foreach (var type in assembly.GetTypes()) {
					if (
						type.IsValueType &&
						!type.IsEnum &&
						typeof(ICommand).IsAssignableFrom(type)
					) {
						commandTypes.Add(type);
					}
				}
			}
			return commandTypes;
		}

		public struct WriterInfo
		{
			public Pointer<EventsBlock> CurrentBlock;
			public byte* DataPointer;
			public int WritePosition;
			public NativeQueue<Pointer<EventsBlock>>.ParallelWriter ProcessingQueueWriter;
			public NativeQueue<Pointer<EventsBlock>>.ParallelWriter FreeBlocksWriter;
		}

		private struct ReaderInfo
		{
			public Pointer<EventsBlock> CurrentBlock;
			public byte* DataPointer;
			public int ReadPosition;
			public NativeQueue<Pointer<EventsBlock>>.ParallelWriter FreeBlocksWriter;
			public NativeArray<Reader.ProcessorMethod> CommandProcessors;
		}

		private struct Command<T> where T : unmanaged, ICommand
		{
			public CommandInfo CommandInfo;
			public T Data;
		}

		public readonly struct Writer
		{
			private readonly StreamData* stream;
			private readonly WriterInfo* writer;

			public Writer(EventsStream* stream, int writerIndex)
			{
				this.stream = stream->data;
				writer = this.stream->Writers.GetSlotPointer(writerIndex);
			}

			public void Push<T>(T evt) where T : unmanaged, ICommand
			{
				var writePosition = writer->WritePosition;
				if (writePosition + sizeof(Command<T>) > EventsBlock.DataSize) {
					ConnectNextWriteBlock();
					writePosition = 0;
				}
				var dataPointer = writer->DataPointer;
				*(CommandInfo*)(dataPointer + writePosition) = new CommandInfo {
					CommandId = evt.Id,
					CommandCount = 1,
					CommandSize = unchecked((byte)sizeof(T))
				};
				*(T*)(dataPointer + writePosition + sizeof(CommandInfo)) = evt;
				writer->WritePosition = writePosition + sizeof(Command<T>);
			}

			public void Flush()
			{
				var writePosition = writer->WritePosition;
				var currentBlock = writer->CurrentBlock;
				if (
					writePosition + sizeof(CommandInfo) <= EventsBlock.DataSize &&
					currentBlock.IsNotNull
				) {
					*(CommandInfo*)(writer->DataPointer + writePosition) = new CommandInfo {
						CommandId = SkipRestOfBlockCommandId,
						CommandCount = 1,
						CommandSize = 0
					};
				}
				if (currentBlock.IsNotNull) {
					stream->ProcessingQueue.Enqueue(currentBlock);
				}
				writer->CurrentBlock = default;
				writer->DataPointer = default;
				writer->WritePosition = EventsBlock.DataSize;
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			private void ConnectNextWriteBlock()
			{
				Flush();
				if (!stream->FreeBlocks.TryDequeue(out var currentBlock)) { // TODO race condition
					const int itemCountPerAllocation = 8;
					currentBlock = stream->Allocator.AllocateArray<EventsBlock>(itemCountPerAllocation);
					for (var i = 1; i < itemCountPerAllocation; i++) {
						var block = currentBlock.TypedPointer + i;
						writer->FreeBlocksWriter.Enqueue(block);
					}
				}
				writer->WritePosition = 0;
				writer->DataPointer = (byte*)&currentBlock.TypedPointer->Data;
				writer->CurrentBlock = currentBlock;
			}
		}

		public readonly struct Reader
		{
			private readonly StreamData* stream;

			public Reader(EventsStream* stream) => this.stream = stream->data;

			public void Process()
			{
				var reader = &stream->Reader;
				while (true) {
					var readPosition = reader->ReadPosition;
					if (readPosition + sizeof(CommandInfo) >= EventsBlock.DataSize) {
						if (!stream->TrySwitchToNextReadBlock())
							return;
						readPosition = 0;
					}
					var dataPointer = reader->DataPointer;
					var info = *(CommandInfo*)(dataPointer + readPosition);
					readPosition += sizeof(CommandInfo);
					reader->ReadPosition = readPosition + info.CommandSize * info.CommandCount;
					var commandProcessor = reader->CommandProcessors[info.CommandId];
					commandProcessor.Process(new(
						commandProcessor.Processor,
						info.CommandCount,
						dataPointer + readPosition,
						info.PlayerId
					));
				}
			}

			public struct ProcessorMethod
			{
				public void* Processor;
				public delegate* unmanaged[Cdecl]<CallArgs, void> Process;

				public ProcessorMethod(void* processor, FunctionPointer<ProcessDelegate> process)
				{
					if (processor == null || !process.IsCreated)
						throw new ArgumentException($"processor is null or process is null");
					Processor = processor;
					Process = (delegate* unmanaged[Cdecl]<CallArgs, void>)process.Value;
				}

				public delegate void ProcessDelegate(CallArgs args);

				public struct CallArgs
				{
					public void* Processor;
					public byte CommandCount;
					public byte* Commands;
					public byte PlayerId;

					[MethodImpl(MethodImplOptions.AggressiveInlining)]
					public CallArgs(void* processor, byte commandCount, byte* commands, byte playerId)
					{
						Processor = processor;
						CommandCount = commandCount;
						Commands = commands;
						PlayerId = playerId;
					}
				}
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
}
