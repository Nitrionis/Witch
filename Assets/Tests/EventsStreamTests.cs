using System;
using System.Linq;
using AOT;
using Game.Allocators;
using Game.Collections;
using Game.Server;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Tests
{
	internal unsafe class EventsStreamTests
	{
		private struct ProcessingState
		{
			public int ProcessedCount;
			public int LastHealth;
		}

		[BurstCompile]
		private static class SetHealthProcessor
		{
			[BurstCompile]
			[MonoPInvokeCallback(typeof(EventsStream.Reader.ProcessorMethod.ProcessDelegate))]
			public static void Process(EventsStream.Reader.ProcessorMethod.CallArgs args)
			{
				var state = (ProcessingState*)args.Processor;
				for (byte i = 0; i < args.CommandCount; i++) {
					ref var command = ref *(Command.SetHealth*)(args.Commands + i * sizeof(Command.SetHealth));
					state->ProcessedCount++;
					state->LastHealth = command.Health;
				}
			}
		}

		private sealed class Harness : IDisposable
		{
			private readonly DisposeList disposeList = new();
			private readonly SessionRewindableAllocator allocator;
			private readonly ProcessingState* state;
			private EventsStream* stream;

			public Harness(int writerCount = 1)
			{
				allocator = new SessionRewindableAllocator(initialBlockSize: 64 * 1024);
				state = allocator.AllocateArray<ProcessingState>(length: 1);
				*state = default;

				var processors = allocator.CreateNativeArray<EventsStream.Reader.ProcessorMethod>(
					byte.MaxValue,
					NativeArrayOptions.ClearMemory
				);
				var processSetHealth = BurstCompiler.CompileFunctionPointer<
					EventsStream.Reader.ProcessorMethod.ProcessDelegate
				>(SetHealthProcessor.Process);
				processors[6] = new EventsStream.Reader.ProcessorMethod(state, processSetHealth);

				stream = allocator.AllocateArray<EventsStream>(length: 1);
				*stream = new EventsStream(disposeList, allocator, writerCount, processors);
			}

			public EventsStream.Writer CreateWriter(int writerIndex = 0) =>
				new EventsStream.Writer(stream, writerIndex);

			public EventsStream.Reader CreateReader() => new(stream);

			public int ProcessedCount => state->ProcessedCount;

			public int LastHealth => state->LastHealth;

			public void Dispose()
			{
				disposeList.Dispose();
				allocator.Dispose();
			}
		}

		private static Command.SetHealth CreateSetHealth(int health) =>
			new() {
				Entity = new Entity { Index = 42, Version = 1 },
				Health = health
			};

		[Test]
		public void Process_Empty_Stream_Does_Nothing()
		{
			using var harness = new Harness();
			harness.CreateReader().Process();
			Assert.AreEqual(0, harness.ProcessedCount);
		}

		[Test]
		public void Push_Process_Single_Command()
		{
			using var harness = new Harness();
			var writer = harness.CreateWriter();
			writer.Push(CreateSetHealth(health: 100));
			writer.Flush();

			harness.CreateReader().Process();

			Assert.AreEqual(1, harness.ProcessedCount);
			Assert.AreEqual(100, harness.LastHealth);
		}

		[Test]
		public void Push_Process_Small_Sequence_Of_Commands()
		{
			using var harness = new Harness();
			var writer = harness.CreateWriter();

			const int commandCount = 10;
			for (var i = 0; i < commandCount; i++) {
				writer.Push(CreateSetHealth(i));
			}
			writer.Flush();

			harness.CreateReader().Process();

			Assert.AreEqual(commandCount, harness.ProcessedCount);
			Assert.AreEqual(commandCount - 1, harness.LastHealth);
		}

		[Test]
		public void Push_Process_Large_Sequence_Of_Commands()
		{
			using var harness = new Harness();
			var writer = harness.CreateWriter();

			const int commandCount = 10_000;
			for (var i = 0; i < commandCount; i++) {
				writer.Push(CreateSetHealth(i));
			}
			writer.Flush();

			harness.CreateReader().Process();

			Assert.AreEqual(commandCount, harness.ProcessedCount);
			Assert.AreEqual(commandCount - 1, harness.LastHealth);
		}

		[Test]
		public void Push_Process_Sequence_With_Multiple_Flushes()
		{
			using var harness = new Harness();
			var writer = harness.CreateWriter();
			var reader = harness.CreateReader();

			const int commandsPerFlush = 10;
			const int flushCount = 100;
			for (var flushIndex = 0; flushIndex < flushCount; flushIndex++) {
				for (var i = 0; i < commandsPerFlush; i++) {
					writer.Push(CreateSetHealth(i));
				}
				writer.Flush();
				reader.Process();

				Assert.AreEqual(
					(flushIndex + 1) * commandsPerFlush,
					harness.ProcessedCount
				);
			}
		}

		[Test]
		public void Push_Process_Spans_Multiple_Blocks()
		{
			using var harness = new Harness();
			var writer = harness.CreateWriter();

			const int commandCount = 500;
			for (var i = 0; i < commandCount; i++) {
				writer.Push(CreateSetHealth(i));
			}
			writer.Flush();

			harness.CreateReader().Process();

			Assert.AreEqual(commandCount, harness.ProcessedCount);
		}

		[Test]
		public void Multiple_Writers_Are_Processed_In_Order()
		{
			using var harness = new Harness(writerCount: 2);
			var writer0 = harness.CreateWriter(writerIndex: 0);
			var writer1 = harness.CreateWriter(writerIndex: 1);

			writer0.Push(CreateSetHealth(10));
			writer0.Flush();
			writer1.Push(CreateSetHealth(20));
			writer1.Flush();

			harness.CreateReader().Process();

			Assert.AreEqual(2, harness.ProcessedCount);
			Assert.AreEqual(20, harness.LastHealth);
		}

		[Test]
		public void Process_Unregistered_Command_Throws()
		{
			using var harness = new Harness();
			var writer = harness.CreateWriter();
			writer.Push(new Command.SetTransform {
				Entity = Entity.Null,
				Position = float3.zero,
				Direction = math.forward(),
				// TODO TimeSinceStartup = 0
			});
			writer.Flush();

			var exception = Assert.Throws<Exception>(() => harness.CreateReader().Process());
			Assert.That(exception!.Message, Does.Contain("No processor for command"));
		}

		[Test]
		public void FindAllCommandStructs_Finds_Game_Commands()
		{
			var commandTypes = EventsStream.FindAllCommandStructs();
			var commandTypeNames = commandTypes.Select(type => type.Name).ToArray();

			Assert.That(commandTypeNames, Does.Contain(nameof(Command.CreateEntity)));
			Assert.That(commandTypeNames, Does.Contain(nameof(Command.SetHealth)));
			Assert.GreaterOrEqual(commandTypes.Count, 13);
		}
	}
}
