using Game.Storage;
using static Game.Storage.Storage;

namespace Game.Server
{
	internal struct RemoteServer
	{
		private LocalClient clientStorage;

		public RemoteServer(LocalClient clientStorage) => this.clientStorage = clientStorage;

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
