using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Collections;

namespace Game.Storage
{
	internal interface IServerStorage
	{
		void GetChunkPatches(ChunkPatches expectedPatches);
	}

	internal interface IClientStorage
	{
		void ChunkRecived(ChunkPatches chunkPatches);
		void CacheReleased(RegionChangesLocation location, Pool<RegionChanges>.Slot container);
		void CacheReleased(RegionBaseLocation location, Pool<RegionBase>.Slot container);
	}

	internal interface IPlayerCache
	{
		void ChunkRecived(RegionChangesLocation location, Pool<RegionChanges>.Slot container);
		void ChunkRecived(RegionBaseLocation regionBaseLocation, Pool<RegionBase>.Slot container);
		bool CanSaveChunk(RegionChangesLocation location);
		bool CanSaveChunk(RegionBaseLocation regionBaseLocation);
	}

	internal class LocalServer : IServerStorage
	{
		private readonly IClientStorage clientStorage;

		public LocalServer(IClientStorage clientStorage) => this.clientStorage = clientStorage;

		void IServerStorage.GetChunkPatches(ChunkPatches expectedPatches)
		{
			clientStorage.ChunkRecived(expectedPatches);
		}
	}

	internal class Storage :
		PatchPointer.IPoolsHolder,
		IClientStorage,
		RegionChanges.LoadTask.IPoolsHolder
	{
		private readonly Pool<RegionChanges> regionChangesPool;
		private readonly Pool<RegionBase> regionBasesPool;
		private readonly Pool<PatchPointer.SinglePatch> patchesPool;
		private readonly Pool<PatchPointer.PatchesGroup> patchGroupsPool;
		private readonly PatchesSegment.Pool patchesSegmentsPool;

		private readonly Dictionary<RegionBaseLocation, Pool<RegionBase>.Slot> bases;
		private readonly Dictionary<RegionChangesLocation, Pool<RegionChanges>.Slot> changes;

		private readonly Queue<RegionBaseLocation> loadRegionBaseIntents;
		private readonly Queue<RegionBase.LoadTask> freeLoadRegionBaseTasks;
		private readonly List<Task<RegionBase.LoadTask>> loadRegionBaseTasks;

		private readonly Queue<RegionChangesLocation> loadRegionChangesIntents;
		private readonly Queue<RegionChanges.LoadTask> freeLoadRegionChangesTasks;
		private readonly List<Task<RegionChanges.LoadTask>> loadRegionChangesTasks;

		private readonly Queue<(RegionChangesLocation, Pool<RegionChanges>.Slot)> unloadRegionChangesIntents;
		private readonly Queue<RegionChanges.UnloadTask> freeUnloadRegionChangesTasks;
		private readonly List<Task<RegionChanges.UnloadTask>> unloadRegionChangesTasks;

		private readonly IChunkProcessor chunkProcessor;
		private readonly IServerStorage serverStorage;
		private readonly IPlayerCache playerCache;
		private readonly FileManager fileManager;

		Pool<PatchPointer.SinglePatch> PatchPointer.IPoolsHolder.PatchesPool => patchesPool;
		Pool<PatchPointer.PatchesGroup> PatchPointer.IPoolsHolder.PatchGroupsPool => patchGroupsPool;

		Pool<PatchPointer.PatchesGroup> RegionChanges.LoadTask.IPoolsHolder.PatchGroupsPool => patchGroupsPool;
		PatchesSegment.Pool RegionChanges.LoadTask.IPoolsHolder.SegmentsPool => patchesSegmentsPool;

		public interface IChunkProcessor
		{
			void ChunkLoadedOrUpdated(ChunkPatches chunkView);
			void RegionBaseLoaded(RegionBaseLocation location, Pool<RegionBase>.Slot regionBase);
		}

		public Storage(
			FileManager fileManager,
			IChunkProcessor chunkProcessor,
			IServerStorage serverStorage,
			IPlayerCache playerCache)
		{
			this.chunkProcessor = chunkProcessor;
			this.serverStorage = serverStorage;
			this.fileManager = fileManager;

			changes = new();
			bases = new();

			regionChangesPool = new();
			regionBasesPool = new();
			patchesPool = new(itemCountPerAllocation: 256);
			patchGroupsPool = new(itemCountPerAllocation: 16);
			patchesSegmentsPool = new(itemCountPerAllocation: 1024);
			this.playerCache = playerCache;

			loadRegionBaseIntents = new();
			freeLoadRegionBaseTasks = new();
			loadRegionBaseTasks = new();

			loadRegionChangesIntents = new();
			loadRegionChangesTasks = new();
			freeLoadRegionChangesTasks = new();

			unloadRegionChangesIntents = new();
			unloadRegionChangesTasks = new();
			freeUnloadRegionChangesTasks = new();
		}

		public unsafe void RequsetRegionChanges(ChunkLocation chunkLocation)
		{
			var regionChangesLocation = RegionChangesLocation.From(chunkLocation);
			if (changes.TryGetValue(regionChangesLocation, out var regionChangesSlot)) {
				if (regionChangesSlot.IsNull) {
					return;
				}
				int localIndex = RegionChanges.GetLocalChunkIndex(chunkLocation);
				var chunkPatches = UnmanagedArray.From(
					&regionChangesSlot.ItemPointerUnchecked->Chunks
				)[localIndex];
				chunkProcessor.ChunkLoadedOrUpdated(chunkPatches);
				return;
			}
			if (!playerCache.CanSaveChunk(regionChangesLocation)) {
				return;
			}
			changes[regionChangesLocation] = default;
			if (!freeLoadRegionChangesTasks.TryDequeue(out var loadTask)) {
				loadTask = new RegionChanges.LoadTask(fileManager);
			}
			loadRegionChangesIntents.Enqueue(regionChangesLocation);
		}

		public void RequsetRegionBase(RegionBaseLocation regionBaseLocation)
		{
			if (bases.TryGetValue(regionBaseLocation, out var regionBasePointer)) {
				if (regionBasePointer.IsNull) {
					return;
				}
				chunkProcessor.RegionBaseLoaded(regionBaseLocation, regionBasePointer);
				return;
			}
			bases[regionBaseLocation] = default;
			if (!freeLoadRegionBaseTasks.TryDequeue(out var loadTask)) {
				loadTask = new RegionBase.LoadTask(fileManager);
			}
			loadRegionBaseIntents.Enqueue(regionBaseLocation);
		}

		// TODO what todo if region not exists
		protected unsafe void Update()
        {
			for (int i = loadRegionBaseTasks.Count - 1; i >= 0; i--) {
				var task = loadRegionBaseTasks[i];
				if (task.IsCompleted) {
					var loadTask = task.Result;
					loadRegionBaseTasks.RemoveAt(i);
					freeLoadRegionBaseTasks.Enqueue(loadTask);
					playerCache.ChunkRecived(loadTask.RegionBaseLocation, loadTask.RegionBaseSlot);
					var regionBase = loadTask.RegionBaseSlot.ItemPointer;
					if (regionBase->PlayerUsageBits == 0) {
						ReleaseRegion(loadTask.RegionBaseSlot);
					} else {
						bases[loadTask.RegionBaseLocation] = loadTask.RegionBaseSlot;
						CopyRegionBaseReference(loadTask.RegionBaseSlot);
					}
					loadTask.RegionBaseLocation = default;
					loadTask.RegionBaseSlot = default;
				}
			}
			for (int i = loadRegionChangesTasks.Count - 1; i >= 0; i--) {
				var task = loadRegionChangesTasks[i];
				if (task.IsCompleted) {
					var loadTask = task.Result;
					if (loadTask.IsCompleted) {
						loadRegionChangesTasks.RemoveAt(i);
						freeLoadRegionChangesTasks.Enqueue(loadTask);
						playerCache.ChunkRecived(loadTask.RegionChangesLocation, loadTask.RegionChangesSlot);
						var regionChanges = loadTask.RegionChangesSlot.ItemPointer;
						if (regionChanges->ChunkPlayerUsageBits == 0) {
							ReleaseRegion(loadTask.RegionChangesLocation, loadTask.RegionChangesSlot);
						} else {
							changes[loadTask.RegionChangesLocation] = loadTask.RegionChangesSlot;
							var chunks = UnmanagedArray.From(&regionChanges->Chunks);
							foreach (var chunk in chunks) {
								CopyChunkReference(chunk);
							}
						}
						loadTask.RegionChangesSlot = default;
						loadTask.RegionChangesLocation = default;
					} else {
						loadTask.FillBuffers(this);
						loadRegionChangesTasks[i] = Task.Run(loadTask.ActionDelegate);
					}
				}
			}
			for (int i = unloadRegionChangesTasks.Count - 1; i >= 0; i--) {
				var task = unloadRegionChangesTasks[i];
				if (task.IsCompleted) {
					var unloadTask = task.Result;
					loadRegionChangesTasks.RemoveAt(i);
					freeUnloadRegionChangesTasks.Enqueue(unloadTask);
					var regionChanges = unloadTask.RegionChangesSlot.ItemPointer;
					int unloadTaskCount = --regionChanges->UnloadTaskCount;
					if (regionChanges->ChunkPlayerUsageBits == 0 && unloadTaskCount == 0) {
						ReleaseRegion(unloadTask.RegionChangesLocation, unloadTask.RegionChangesSlot);
					}
				}
			}
			// Start loading or unloading world data.
			if (
				loadRegionBaseTasks.Count == 0
				&& loadRegionBaseIntents.TryDequeue(out var regionBaseLocation)
				&& playerCache.CanSaveChunk(regionBaseLocation)
			) {
				if (!freeLoadRegionBaseTasks.TryDequeue(out var task)) {
					task = new RegionBase.LoadTask(fileManager);
				}
				task.RegionBaseLocation = regionBaseLocation;
				task.RegionBaseSlot = regionBasesPool.Rent();
				loadRegionBaseTasks.Add(Task.Run(task.ActionDelegate));
			}
			if (
				loadRegionChangesTasks.Count == 0
				&& loadRegionChangesIntents.TryDequeue(out var regionChangesLocation)
				&& playerCache.CanSaveChunk(regionChangesLocation)
			) {
				if (!freeLoadRegionChangesTasks.TryDequeue(out var task)) {
					task = new RegionChanges.LoadTask(fileManager);
				}
				task.RegionChangesLocation = regionChangesLocation;
				task.RegionChangesSlot = regionChangesPool.Rent();
				loadRegionChangesTasks.Add(Task.Run(task.ActionDelegate));
			}
			if (
				unloadRegionChangesTasks.Count == 0
				&& unloadRegionChangesIntents.TryDequeue(
					out (RegionChangesLocation Location, Pool<RegionChanges>.Slot Slot) intent
				)
				&& intent.Slot.ItemPointer->ChunkPlayerUsageBits == 0
			) {
				var regionChanges = intent.Slot.ItemPointer;
				regionChanges->UnloadTaskCount++;
				if (!freeUnloadRegionChangesTasks.TryDequeue(out var task)) {
					task = new RegionChanges.UnloadTask(fileManager);
				}
				task.RegionChangesLocation = intent.Location;
				task.RegionChangesSlot = intent.Slot;
				unloadRegionChangesTasks.Add(Task.Run(task.ActionDelegate));
			}
		}

        protected void Destroy() // TODO
        {
			regionChangesPool.Destroy();
			regionBasesPool.Destroy();
			patchesPool.Destroy();
			patchGroupsPool.Destroy();
			patchesSegmentsPool.Destroy();

		}

		unsafe void IClientStorage.ChunkRecived(ChunkPatches chunkPatches)
		{
			if (chunkPatches.PatchesChainStart.Segment.IsNull) {
				return;
			}
			var regionLocation = RegionChangesLocation.From(chunkPatches.Location);
			if (changes.TryGetValue(regionLocation, out var regionChangesSlot)) {
				if (regionChangesSlot.IsNull) {
					ReleaseChunkReference(chunkPatches);
					return;
				}
			} else {
				ReleaseChunkReference(chunkPatches);
				return;
			}
			var regionChanges = regionChangesSlot.ItemPointerUnchecked;
			int chunkLocalIndex = RegionChanges.GetLocalChunkIndex(chunkPatches.Location);
			var curentChunkPatches = PointerArray.From(&regionChanges->Chunks)[chunkLocalIndex];
			if (!curentChunkPatches->PatchesChainStart.Segment.IsNull) {
				ReleaseChunkReference(*curentChunkPatches);
			}
			CopyChunkReference(chunkPatches);
			chunkPatches.IsModifed = true;
			* curentChunkPatches = chunkPatches;
			chunkProcessor.ChunkLoadedOrUpdated(chunkPatches);
			playerCache.ChunkRecived(regionLocation, regionChangesSlot);
			// If the cache rejects a region, mark it for deletion.
			if (regionChanges->ChunkPlayerUsageBits == 0) {
				unloadRegionChangesIntents.Enqueue((regionLocation, regionChangesSlot));
			}
		}

		// TODO
		private unsafe void ReleaseRegion(RegionChangesLocation location, Pool<RegionChanges>.Slot regionChangesSlot)
		{
			var regionChanges = regionChangesSlot.ItemPointer;
			if (regionChanges->ChunkPlayerUsageBits != 0) {
				throw new Exception("ReleaseRegion error: ChunkPlayerUsageBits != 0");
			}

			// TODO
		}

		// TODO
		private unsafe void ReleaseRegion(Pool<RegionBase>.Slot regionBaseSlot)
		{
			var regionChanges = regionChangesSlot.ItemPointer;
			if (regionChanges->ChunkPlayerUsageBits != 0) {
				throw new Exception("ReleaseRegion error: ChunkPlayerUsageBits != 0");
			}

			// TODO
		}

		private unsafe void FinishRegionUnloading(RegionChanges.UnloadTask unloadTask)
		{
			var regionChanges = unloadTask.RegionChangesSlot.ItemPointer;
			regionChanges->UnloadTaskCount--;
			if (regionChanges->UnloadTaskCount > 0) {
				return;
			}
			if (regionChanges->ChunkPlayerUsageBits != 0) {
				return;
			}
			var chunks = PointerArray.From(&regionChanges->Chunks);
			for (int i = 0; i < chunks.Length; i++) {
				ReleaseChunkReference(*chunks[i]);
			}
			changes.Remove(unloadTask.RegionChangesLocation);
		}

		public void CopyChunkReference(ChunkPatches chunkPatches)
		{
			chunkPatches.ForEachPatchAddReferenceCount(count: 1);
		}

		public void ReleaseChunkReference(ChunkPatches chunkPatches)
		{
			foreach (var patchView in chunkPatches.PatchesChainStart.EnumerateChain()) {
				int referenceCount = patchView.Patch.DecrementReferenceCount();
				if (referenceCount < 0) {
					throw new Exception($"Invalid patch reference count: {referenceCount}");
				}
				if (referenceCount == 0) {
					patchView.Patch.Release(this);
				}
			}
		}

		public unsafe void CopyRegionBaseReference(Pool<RegionBase>.Slot regionBaseSlot)
		{
			int referenceCount = ++regionBaseSlot.ItemPointer->ReferenceCount;
			if (referenceCount < 0) {
				throw new Exception($"Invalid patch reference count: {referenceCount}");
			}
		}

		void IClientStorage.CacheReleased(RegionChangesLocation location, Pool<RegionChanges>.Slot container)
		{
			throw new NotImplementedException();
		}

		void IClientStorage.CacheReleased(RegionBaseLocation location, Pool<RegionBase>.Slot container)
		{
			throw new NotImplementedException();
		}
	}
}
