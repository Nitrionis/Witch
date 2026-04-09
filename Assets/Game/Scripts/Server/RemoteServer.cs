using Game.Storage;

namespace Game.Server
{
	internal class RemoteServer : IServerStorage
	{
		private readonly IClientStorage clientStorage;

		public RemoteServer(IClientStorage clientStorage) => this.clientStorage = clientStorage;

		void IServerStorage.GetChunkPatches(ChunkPatches expectedPatches)
		{
			throw new System.NotImplementedException();
		}
	}
}
