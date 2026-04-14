using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Collections;
using Unity.Collections;
using Unity.Burst;
using System.Runtime.CompilerServices;
using Game.Allocators;

namespace Game.Storage
{
	internal readonly unsafe struct ServerStorageDelegates
	{
		private readonly IntPtr serverStorage;
		private readonly IntPtr getChunkPatches;

		public ServerStorageDelegates(
			IntPtr serverStorage,
			FunctionPointer<GetChunkPatches> getChunkPatches)
		{
			this.serverStorage = serverStorage;
			this.getChunkPatches = getChunkPatches.Value;
			Validate();
		}

		public void Validate()
		{
			if (serverStorage == IntPtr.Zero)
				throw new ArgumentNullException(nameof(serverStorage));
			if (getChunkPatches == IntPtr.Zero)
				throw new ArgumentNullException(nameof(getChunkPatches));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void InvokeGetChunkPatches(ChunkPatches expectedPatches) =>
			((delegate* unmanaged[Cdecl]<IntPtr, ChunkPatches, void>)getChunkPatches)(serverStorage, expectedPatches);

		public delegate void GetChunkPatches(IntPtr serverStorage, ChunkPatches expectedPatches);
	}

	internal readonly unsafe struct PlayerCacheDelegates
	{
		private readonly IntPtr playerCache;
		private readonly IntPtr chunkChangesRecived;
		private readonly IntPtr chunkBaseRecived;
		private readonly IntPtr canSaveChunkChanges;
		private readonly IntPtr canSaveChunkBase;

		public PlayerCacheDelegates(
			IntPtr playerCache,
			FunctionPointer<ChunkChangesRecived> chunkChangesRecived,
			FunctionPointer<ChunkBaseRecived> chunkBaseRecived,
			FunctionPointer<CanSaveChunkChanges> canSaveChunkChanges,
			FunctionPointer<CanSaveChunkBase> canSaveChunkBase)
		{
			this.playerCache = playerCache;
			this.chunkChangesRecived = chunkChangesRecived.Value;
			this.chunkBaseRecived = chunkBaseRecived.Value;
			this.canSaveChunkChanges = canSaveChunkChanges.Value;
			this.canSaveChunkBase = canSaveChunkBase.Value;
			Validate();
		}

		public void Validate()
		{
			if (playerCache == IntPtr.Zero)
				throw new ArgumentNullException(nameof(playerCache));
			if (chunkChangesRecived == IntPtr.Zero)
				throw new ArgumentNullException(nameof(chunkChangesRecived));
			if (chunkBaseRecived == IntPtr.Zero)
				throw new ArgumentNullException(nameof(chunkBaseRecived));
			if (canSaveChunkChanges == IntPtr.Zero)
				throw new ArgumentNullException(nameof(canSaveChunkChanges));
			if (canSaveChunkBase == IntPtr.Zero)
				throw new ArgumentNullException(nameof(canSaveChunkBase));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void InvokeChunkRecived(RegionChangesLocation location, Pool<RegionChanges>.Slot container) =>
			((delegate* unmanaged[Cdecl]<IntPtr, RegionChangesLocation, Pool<RegionChanges>.Slot, void>)chunkChangesRecived)(playerCache, location, container);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void InvokeChunkRecived(RegionBaseLocation location, Pool<RegionBase>.Slot container) =>
			((delegate* unmanaged[Cdecl]<IntPtr, RegionBaseLocation, Pool<RegionBase>.Slot, void>)chunkBaseRecived)(playerCache, location, container);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool InvokeCanSaveChunk(RegionChangesLocation location) =>
			((delegate* unmanaged[Cdecl]<IntPtr, RegionChangesLocation, bool>)canSaveChunkChanges)(playerCache, location);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly bool InvokeCanSaveChunk(RegionBaseLocation location) =>
			((delegate* unmanaged[Cdecl]<IntPtr, RegionBaseLocation, bool>)canSaveChunkBase)(playerCache, location);

		public delegate void ChunkChangesRecived(
			IntPtr playerCache,
			RegionChangesLocation location,
			Pool<RegionChanges>.Slot container
		);
		public delegate void ChunkBaseRecived(
			IntPtr playerCache,
			RegionBaseLocation regionBaseLocation,
			Pool<RegionBase>.Slot container
		);
		public delegate bool CanSaveChunkChanges(
			IntPtr playerCache,
			RegionChangesLocation location
		);
		public delegate bool CanSaveChunkBase(
			IntPtr playerCache,
			RegionBaseLocation regionBaseLocation
		);
	}

	internal unsafe readonly struct ChunkProcessorDelegates
	{
		private readonly IntPtr chunkLoadedOrUpdated;
		private readonly IntPtr regionBaseLoaded;
		private readonly IntPtr chunkProcessor;

		public ChunkProcessorDelegates(
			IntPtr chunkProcessor,
			FunctionPointer<ChunkLoadedOrUpdated> chunkLoadedOrUpdated,
			FunctionPointer<RegionBaseLoaded> regionBaseLoaded)
		{
			this.chunkProcessor = chunkProcessor;
			this.chunkLoadedOrUpdated = chunkLoadedOrUpdated.Value;
			this.regionBaseLoaded = regionBaseLoaded.Value;
			Validate();
		}

		public void Validate()
		{
			if (chunkProcessor == IntPtr.Zero)
				throw new ArgumentNullException(nameof(chunkProcessor));
			if (chunkLoadedOrUpdated == IntPtr.Zero)
				throw new ArgumentNullException(nameof(chunkLoadedOrUpdated));
			if (regionBaseLoaded == IntPtr.Zero)
				throw new ArgumentNullException(nameof(regionBaseLoaded));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void InvokeChunkLoadedOrUpdated(ChunkPatches chunkView) =>
			((delegate* unmanaged[Cdecl]<IntPtr, ChunkPatches, void>)chunkLoadedOrUpdated)(chunkProcessor, chunkView);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void InvokeRegionBaseLoaded(RegionBaseLocation location, Pool<RegionBase>.Slot regionBase) =>
			((delegate* unmanaged[Cdecl]<IntPtr, RegionBaseLocation, Pool<RegionBase>.Slot, void>)regionBaseLoaded)(chunkProcessor, location, regionBase);

		public delegate void ChunkLoadedOrUpdated(
			IntPtr chunkProcessor,
			ChunkPatches chunkView
		);

		public delegate void RegionBaseLoaded(
			IntPtr chunkProcessor,
			RegionBaseLocation location,
			Pool<RegionBase>.Slot regionBase
		);
	}

	[BurstCompile]
	internal unsafe struct Storage :
		PatchPointer.IPoolsHolder,
		RegionChanges.LoadTask.IPoolsHolder
	{
		private readonly Pool<RegionChanges> regionChangesPool;
		private readonly Pool<RegionBase> regionBasesPool;
		private readonly Pool<PatchPointer.SinglePatch> patchesPool;
		private readonly Pool<PatchPointer.PatchesGroup> patchGroupsPool;
		private readonly PatchesSegment.Pool patchesSegmentsPool;

		private NativeHashMap<RegionBaseLocation, Pool<RegionBase>.Slot> bases;
		private NativeHashMap<RegionChangesLocation, Pool<RegionChanges>.Slot> changes;
		private NativeQueue<(RegionChangesLocation, Pool<RegionChanges>.Slot)> unloadRegionChangesIntents;

		private NativeQueue<RegionBaseLocation> loadRegionBaseIntents;
		private NativeQueue<RegionChangesLocation> loadRegionChangesIntents;

		private readonly ChunkProcessorDelegates chunkProcessor;
		private readonly ServerStorageDelegates serverStorage;
		private readonly PlayerCacheDelegates playerCache;

		readonly Pool<PatchPointer.SinglePatch> PatchPointer.IPoolsHolder.PatchesPool => patchesPool;
		readonly Pool<PatchPointer.PatchesGroup> PatchPointer.IPoolsHolder.PatchGroupsPool => patchGroupsPool;

		readonly Pool<PatchPointer.PatchesGroup> RegionChanges.LoadTask.IPoolsHolder.PatchGroupsPool => patchGroupsPool;
		readonly PatchesSegment.Pool RegionChanges.LoadTask.IPoolsHolder.SegmentsPool => patchesSegmentsPool;

		public class ManagedMirror
		{
			private readonly Storage* storage;
			private readonly FileManager fileManager;

			private readonly Queue<RegionBase.LoadTask> freeLoadRegionBaseTasks;
			private readonly List<Task<RegionBase.LoadTask>> loadRegionBaseTasks;

			private readonly Queue<RegionChanges.LoadTask> freeLoadRegionChangesTasks;
			private readonly List<Task<RegionChanges.LoadTask>> loadRegionChangesTasks;

			private readonly Queue<RegionChanges.UnloadTask> freeUnloadRegionChangesTasks;
			private readonly List<Task<RegionChanges.UnloadTask>> unloadRegionChangesTasks;

			public ManagedMirror(FileManager fileManager, Storage* storage)
			{
				this.fileManager = fileManager;
				this.storage = storage;
				freeLoadRegionBaseTasks = new();
				loadRegionBaseTasks = new();
				freeLoadRegionChangesTasks = new();
				loadRegionChangesTasks = new();
				freeUnloadRegionChangesTasks = new();
				unloadRegionChangesTasks = new();
			}

			public void Sync()
			{
				var playerCache = storage->playerCache;
				var bases = storage->bases;
				var changes = storage->changes;

				for (int i = loadRegionBaseTasks.Count - 1; i >= 0; i--) {
					var task = loadRegionBaseTasks[i];
					if (task.IsCompleted) {
						var loadTask = task.Result;
						loadRegionBaseTasks.RemoveAt(i);
						freeLoadRegionBaseTasks.Enqueue(loadTask);
						playerCache.InvokeChunkRecived(loadTask.RegionBaseLocation, loadTask.RegionBaseSlot);
						var regionBase = loadTask.RegionBaseSlot.ItemPointer;
						if (regionBase->PlayerUsageBits == 0) {
							storage->ReleaseRegion(loadTask.RegionBaseSlot);
						} else {
							bases[loadTask.RegionBaseLocation] = loadTask.RegionBaseSlot;
							storage->CopyRegionBaseReference(loadTask.RegionBaseSlot);
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
							playerCache.InvokeChunkRecived(loadTask.RegionChangesLocation, loadTask.RegionChangesSlot);
							var regionChanges = loadTask.RegionChangesSlot.ItemPointer;
							if (regionChanges->ChunkPlayerUsageBits == 0) {
								storage->ReleaseRegion(loadTask.RegionChangesLocation, loadTask.RegionChangesSlot);
							} else {
								changes[loadTask.RegionChangesLocation] = loadTask.RegionChangesSlot;
								var chunks = UnmanagedArray.From(&regionChanges->Chunks);
								foreach (var chunk in chunks) {
									storage->CopyChunkReference(chunk);
								}
							}
							loadTask.RegionChangesSlot = default;
							loadTask.RegionChangesLocation = default;
						} else {
							loadTask.FillBuffers(storage);
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
							storage->ReleaseRegion(unloadTask.RegionChangesLocation, unloadTask.RegionChangesSlot);
						}
					}
				}

				// Start loading or unloading world data.
				var loadRegionBaseIntents = storage->loadRegionBaseIntents;
				if (
					loadRegionBaseTasks.Count == 0
					&& loadRegionBaseIntents.TryDequeue(out var regionBaseLocation)
					&& playerCache.InvokeCanSaveChunk(regionBaseLocation)
				) {
					if (!freeLoadRegionBaseTasks.TryDequeue(out var task)) {
						task = new RegionBase.LoadTask(fileManager);
					}
					task.RegionBaseLocation = regionBaseLocation;
					task.RegionBaseSlot = storage->regionBasesPool.Rent();
					loadRegionBaseTasks.Add(Task.Run(task.ActionDelegate));
				}

				var loadRegionChangesIntents = storage->loadRegionChangesIntents;
				if (
					loadRegionChangesTasks.Count == 0
					&& loadRegionChangesIntents.TryDequeue(out var regionChangesLocation)
					&& playerCache.InvokeCanSaveChunk(regionChangesLocation)
				) {
					if (!freeLoadRegionChangesTasks.TryDequeue(out var task)) {
						task = new RegionChanges.LoadTask(fileManager);
					}
					task.RegionChangesLocation = regionChangesLocation;
					task.RegionChangesSlot = storage->regionChangesPool.Rent();
					loadRegionChangesTasks.Add(Task.Run(task.ActionDelegate));
				}

				var unloadRegionChangesIntents = storage->unloadRegionChangesIntents;
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
		}

		public readonly struct ClientSide
		{
			private readonly Storage* storage;

			public ClientSide(Storage* storage) => this.storage = storage;

			public void ChunkRecived(ChunkPatches chunkPatches) =>
				storage->ChunkRecived(chunkPatches);
			public void CacheReleased(RegionChangesLocation location, Pool<RegionChanges>.Slot container) =>
				storage->CacheReleased(location, container);
			public void CacheReleased(RegionBaseLocation location, Pool<RegionBase>.Slot container) =>
				storage->CacheReleased(location, container);
		}

		public Storage(
			FileManager fileManager,
			SessionRewindableAllocator allocator,
			DisposeList disposeList,
			ChunkProcessorDelegates chunkProcessor,
			ServerStorageDelegates serverStorage,
			PlayerCacheDelegates playerCache)
		{
			this.chunkProcessor = chunkProcessor;
			this.serverStorage = serverStorage;
			this.playerCache = playerCache;
			chunkProcessor.Validate();
			serverStorage.Validate();
			playerCache.Validate();

			var dl = disposeList;
			dl.Add(changes = new(initialCapacity: 128, Allocator.Persistent));
			dl.Add(bases = new(initialCapacity: 128, Allocator.Persistent));

			regionChangesPool = new(disposeList, allocator);
			regionBasesPool = new(disposeList, allocator);
			patchesPool = new(disposeList, allocator, itemCountPerAllocation: 256);
			patchGroupsPool = new(disposeList, allocator, itemCountPerAllocation: 16);
			patchesSegmentsPool = new(disposeList, allocator, itemCountPerAllocation: 1024);

			dl.Add(loadRegionBaseIntents = new(Allocator.Persistent));
			dl.Add(loadRegionChangesIntents = new(Allocator.Persistent));
			dl.Add(unloadRegionChangesIntents = new(Allocator.Persistent));
		}

		public void RequsetRegionChanges(ChunkLocation chunkLocation)
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
				chunkProcessor.InvokeChunkLoadedOrUpdated(chunkPatches);
				return;
			}
			if (!playerCache.InvokeCanSaveChunk(regionChangesLocation)) {
				return;
			}
			changes[regionChangesLocation] = default;
			loadRegionChangesIntents.Enqueue(regionChangesLocation);
		}

		public void RequsetRegionBase(RegionBaseLocation regionBaseLocation)
		{
			if (bases.TryGetValue(regionBaseLocation, out var regionBasePointer)) {
				if (regionBasePointer.IsNull) {
					return;
				}
				chunkProcessor.InvokeRegionBaseLoaded(regionBaseLocation, regionBasePointer);
				return;
			}
			bases[regionBaseLocation] = default;
			loadRegionBaseIntents.Enqueue(regionBaseLocation);
		}

		private void ChunkRecived(ChunkPatches chunkPatches)
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
			*curentChunkPatches = chunkPatches;
			chunkProcessor.InvokeChunkLoadedOrUpdated(chunkPatches);
			playerCache.InvokeChunkRecived(regionLocation, regionChangesSlot);
			// If the cache rejects a region, mark it for deletion.
			if (regionChanges->ChunkPlayerUsageBits == 0) {
				unloadRegionChangesIntents.Enqueue((regionLocation, regionChangesSlot));
			}
		}

		private void ReleaseRegion(RegionChangesLocation location, Pool<RegionChanges>.Slot regionChangesSlot)
		{
			var regionChanges = regionChangesSlot.ItemPointer;
			if (regionChanges->ChunkPlayerUsageBits != 0 || regionChanges->UnloadTaskCount != 0) {
				throw new Exception("ReleaseRegion error: ChunkPlayerUsageBits != 0 || regionChanges->UnloadTaskCount != 0");
			}
			changes.Remove(location);
			var chunks = UnmanagedArray.From(&regionChanges->Chunks);
			foreach (var chunk in chunks) {
				ReleaseChunkReference(chunk);
			}
			regionChangesPool.Release(regionChangesSlot);
		}

		private void ReleaseRegion(Pool<RegionBase>.Slot regionBaseSlot)
		{
			var regionBase = regionBaseSlot.ItemPointer;
			if (regionBase->PlayerUsageBits != 0 || regionBase->ReferenceCount != 0) {
				throw new Exception("ReleaseRegion error: ChunkPlayerUsageBits != 0 || regionBase->ReferenceCount != 0");
			}
			bases.Remove(regionBase->RegionBaseLocation);
			regionBasesPool.Release(regionBaseSlot);
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

		public void CopyRegionBaseReference(Pool<RegionBase>.Slot regionBaseSlot)
		{
			int referenceCount = ++regionBaseSlot.ItemPointer->ReferenceCount;
			if (referenceCount < 0) {
				throw new Exception($"Invalid patch reference count: {referenceCount}");
			}
		}

		public void ReleaseRegionBaseReference(Pool<RegionBase>.Slot regionBaseSlot)
		{
			var regionBase = regionBaseSlot.ItemPointer;
			regionBase->ReferenceCount--;
			if (regionBase->PlayerUsageBits != 0 || regionBase->ReferenceCount != 0) {
				return;
			}
			ReleaseRegion(regionBaseSlot);
		}

		private void CacheReleased(RegionChangesLocation location, Pool<RegionChanges>.Slot regionChangesSlot)
		{
			unloadRegionChangesIntents.Enqueue((location, regionChangesSlot));
		}

		private void CacheReleased(RegionBaseLocation location, Pool<RegionBase>.Slot regionBaseSlot)
		{
			ReleaseRegionBaseReference(regionBaseSlot);
		}
	}
}
