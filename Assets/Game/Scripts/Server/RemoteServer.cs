using Game.Storage;
using static Game.Storage.Storage;

namespace Game.Server
{
	internal struct RemoteServer
	{
		private ClientSide clientStorage;

		public RemoteServer(ClientSide clientStorage) => this.clientStorage = clientStorage;

		private void GetChunkPatches(ChunkPatches expectedPatches)
		{
			throw new System.NotImplementedException();
		}

		public ServerStorageDelegates GetServerStorageDelegates()
		{
			throw new System.NotImplementedException();
		}
	}
}
