using NUnit.Framework;
using SimpleInjector;
using Assets.Game;
using Assets.Game.ClientServer;
using Assets.Game.ClientServer.CommandProcessors;
using Assets.Game.Collections;

namespace Tests
{
	internal class GameShellTests
	{

		[Test]
		public void TestNativeMemoryChain()
		{
			for (int i = 0; i < 100; i++) {
				NativeMemoryChain memoryChain = new();
				memoryChain.Dispose();
			}
		}

		private static Container CreateShellComposition()
		{
			var c = new Container();

			c.RegisterSingleton<GameShell>();
			c.RegisterSingleton<GameShell.Consumer>();
			c.RegisterSingleton<GameShell.Producer>();
			c.RegisterSingleton<ClientShellCommands>();
			c.RegisterSingleton<
				GameShell.ICommandProcessorCollection,
				ClientShellCommands.CommandProcessors
			>();
			c.RegisterSingleton<
				ClientShellCommands.ICommandProcessors,
				ClientShellCommands.CommandProcessors
			>();
			c.RegisterSingleton<
				IClientDummyProcessor,
				DummyProcessor
			>();

			c.Verify();
			return c;
		}

		[Test]
		public void Push_Consume_Single_Item()
		{
			var c = CreateShellComposition();

			using var shell = c.GetInstance<GameShell>();
			var consumer = c.GetInstance<GameShell.Consumer>();
			var producer = c.GetInstance<GameShell.Producer>();
			var commands = c.GetInstance<ClientShellCommands>();
			var processor = (DummyProcessor)c.GetInstance<IClientDummyProcessor>();

			var dataUnit = DummyDataUnit.Create();
			producer.Push(commands.DummyCommand, ref dataUnit);
			producer.FlushStream();

			consumer.Consume();
			shell.Dispose();

			Assert.AreEqual(expected: 1, actual: processor.ProcessedUnitCount);
		}

		[Test]
		public void Push_Consume_Small_Sequence_Of_Items()
		{
			var c = CreateShellComposition();

			using var shell = c.GetInstance<GameShell>();
			var consumer = c.GetInstance<GameShell.Consumer>();
			var producer = c.GetInstance<GameShell.Producer>();
			var commands = c.GetInstance<ClientShellCommands>();
			var processor = (DummyProcessor)c.GetInstance<IClientDummyProcessor>();

			var dataUnit = DummyDataUnit.Create();

			const int DataUnitCount = 10;
			for (var i = 0; i < DataUnitCount; i++) {
				producer.Push(commands.DummyCommand, ref dataUnit);
			}
			producer.FlushStream();

			consumer.Consume();
			shell.Dispose();

			Assert.AreEqual(expected: DataUnitCount, actual: processor.ProcessedUnitCount);
		}

		[Test]
		public void Push_Consume_Big_Sequence_Of_Items()
		{
			var c = CreateShellComposition();

			using var shell = c.GetInstance<GameShell>();
			var consumer = c.GetInstance<GameShell.Consumer>();
			var producer = c.GetInstance<GameShell.Producer>();
			var commands = c.GetInstance<ClientShellCommands>();
			var processor = (DummyProcessor)c.GetInstance<IClientDummyProcessor>();

			var dataUnit = DummyDataUnit.Create();

			const int DataUnitCount = 10000;
			for (var i = 0; i < DataUnitCount; i++) {
				producer.Push(commands.DummyCommand, ref dataUnit);
			}
			producer.FlushStream();

			consumer.Consume();
			shell.Dispose();

			Assert.AreEqual(expected: DataUnitCount, actual: processor.ProcessedUnitCount);
		}

		[Test]
		public void Push_Consume_Big_Sequence_Of_Items_With_Small_Subsequences()
		{
			var c = CreateShellComposition();

			using var shell = c.GetInstance<GameShell>();
			var consumer = c.GetInstance<GameShell.Consumer>();
			var producer = c.GetInstance<GameShell.Producer>();
			var commands = c.GetInstance<ClientShellCommands>();
			var processor = (DummyProcessor)c.GetInstance<IClientDummyProcessor>();

			var dataUnit = DummyDataUnit.Create();

			const int DataUnitCountInSubsequence = 10;
			const int IterationCount = 10000;
			for (int i = 0; i < IterationCount; i++) {
				for (var ssi = 0; ssi < DataUnitCountInSubsequence; ssi++) {
					producer.Push(commands.DummyCommand, ref dataUnit);
				}
				producer.FlushStream();

				consumer.Consume();

				Assert.AreEqual(
					expected: (i + 1) * DataUnitCountInSubsequence,
					actual: processor.ProcessedUnitCount
				);
			}
			
			shell.Dispose();
		}
	}
}
