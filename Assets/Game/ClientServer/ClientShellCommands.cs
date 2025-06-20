using Assets.Game.ClientServer.CommandProcessors;
using static Assets.Game.ClientServer.GameShell;

namespace Assets.Game.ClientServer
{
	public class ClientShellCommands
	{
		public readonly Command<DummyDataUnit> DummyCommand;

		public ClientShellCommands(ICommandProcessors processors)
		{
			DummyCommand = new(processors.DummyProcessor);
		}

		public interface ICommandProcessors : ICommandProcessorCollection
		{
			IClientDummyProcessor DummyProcessor { get; }
		}

		public class CommandProcessors : CommandProcessorCollection, ICommandProcessors
		{
			public IClientDummyProcessor DummyProcessor { get; private set; }

			public CommandProcessors(
				IClientDummyProcessor dummyProcessor
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
