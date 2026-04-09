using Game.Storage;

namespace Game.Server
{
	internal class LocalServer : IServerStorage
	{
		private readonly IClientStorage clientStorage;

		public LocalServer(IClientStorage clientStorage) => this.clientStorage = clientStorage;

		void IServerStorage.GetChunkPatches(ChunkPatches expectedPatches)
		{
			//clientStorage.ChunkRecived(default);
		}
	}
}
