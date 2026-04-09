using System;
using Game.Collections;
using Game.Storage;

namespace Game.Server
{
	internal class SinglePlayerCache : IPlayerCache
	{


		bool IPlayerCache.CanSaveChunk(RegionChangesLocation location)
		{
			throw new NotImplementedException();
		}

		bool IPlayerCache.CanSaveChunk(RegionBaseLocation regionBaseLocation)
		{
			throw new NotImplementedException();
		}

		void IPlayerCache.ChunkRecived(RegionChangesLocation location, Pool<RegionChanges>.Slot container)
		{
			throw new NotImplementedException();
		}

		void IPlayerCache.ChunkRecived(RegionBaseLocation regionBaseLocation, Pool<RegionBase>.Slot container)
		{
			throw new NotImplementedException();
		}
	}
}
