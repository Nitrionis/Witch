using Assets.Game.ClientServer.CommandProcessors;
using static Assets.Game.ClientServer.GameShell;

namespace Assets.Game.ClientServer
{
	public class ServerShellCommands
	{
		public readonly Command<DummyDataUnit> DummyCommand;

		public ServerShellCommands(ICommandProcessors processors)
		{
			DummyCommand = new(processors.DummyProcessor);
		}

		public interface ICommandProcessors : ICommandProcessorCollection
		{
			IServerDummyProcessor DummyProcessor { get; }
		}

		public class CommandProcessors : CommandProcessorCollection, ICommandProcessors
		{
			public IServerDummyProcessor DummyProcessor { get; private set; }

			public CommandProcessors(
				IServerDummyProcessor dummyProcessor
			)
				: base(new[] {
					EnsureGuid(dummyProcessor, ShellCommands.Dummy),
				})
			{
				DummyProcessor = dummyProcessor;
			}
		}
	}
}
