using System;
using Game.Collections;
using Game.Storage;

namespace Game.Server
{
	internal struct MultiPlayerCache
	{
		private bool CanSaveChunk(RegionChangesLocation location)
		{
			throw new NotImplementedException();
		}

		private bool CanSaveChunk(RegionBaseLocation regionBaseLocation)
		{
			throw new NotImplementedException();
		}

		private void ChunkRecived(RegionChangesLocation location, Pool<RegionChanges>.Slot container)
		{
			throw new NotImplementedException();
		}

		private void ChunkRecived(RegionBaseLocation regionBaseLocation, Pool<RegionBase>.Slot container)
		{
			throw new NotImplementedException();
		}

		public PlayerCacheDelegates GetPlayerCacheDelegates()
		{
			throw new NotImplementedException();
		}
	}
}
