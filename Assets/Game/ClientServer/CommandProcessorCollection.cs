using System;
using System.Collections.Generic;
using System.Linq;
using static Assets.Game.ClientServer.GameShell;

namespace Assets.Game.ClientServer
{
	public class CommandProcessorCollection : ICommandProcessorCollection
	{
		private readonly ICommandProcessor[] processors;
		private readonly Dictionary<Guid, ICommandProcessor> guidToProcessor;

		public ICommandProcessor this[int commandId] => processors[commandId];
		public ICommandProcessor this[Guid guid] => guidToProcessor[guid];

		public CommandProcessorCollection(IEnumerable<ICommandProcessor> processors)
		{
			guidToProcessor = new();
			foreach (var processor in processors) {
				guidToProcessor.Add(processor.Guid, processor);
			}
			ushort maxId = processors.Select(p => ShellCommands.GetId(p.Guid)).Max();
			this.processors = new ICommandProcessor[maxId + 1];
			foreach (var processor in processors) {
				this.processors[ShellCommands.GetId(processor.Guid)] = processor;
			}
		}

		protected static ICommandProcessor EnsureGuid(ICommandProcessor cp, Guid guid) =>
			cp.Guid == guid ? cp : throw new Exception("Invalid Guid");
	}
}
