using Game.Storage;
using static Game.Storage.Storage;

namespace Game.Server
{
	internal struct LocalServer
	{
		private LocalClient clientStorage;

		public LocalServer(LocalClient clientStorage) => this.clientStorage = clientStorage;

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
