using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Game.Allocators;
using Game.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

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

	internal interface IChunkProcessor
	{
		void ChunkLoadedOrUpdated(Pool<ChunkPatches>.Slot chunkView);
		void RegionBaseLoaded(RegionBaseLocation location, Pool<RegionBase>.Slot regionBase);
	}

	/// <remarks>Access only from <see cref="ThreadRules.MainJob"/></remarks>
	[BurstCompile]
	internal unsafe struct Storage :
		PatchPointer.IPoolsHolder,
		RegionChanges.LoadTask.IPoolsHolder
	{
		private readonly Pool<ChunkPatches> chunkPatchesPool;
		private readonly Pool<RegionChanges> regionChangesPool;
		private readonly Pool<RegionBase> regionBasesPool;
		private readonly Pool<PatchPointer.SinglePatch> singlePatchPool;
		private readonly Pool<PatchPointer.PatchesGroup> patchGroupPool;
		private readonly ChainSegment<PatchView>.Pool patchesSegmentsPool;

		private NativeHashMap<RegionBaseLocation, Pool<RegionBase>.Slot> bases;
		private NativeHashMap<RegionChangesLocation, Pool<RegionChanges>.Slot> changes;

		private NativeList<RegionBaseLocation> loadRegionBaseIntents;
		private NativeList<RegionChangesLocation> loadRegionChangesIntents;
		private NativeList<RegionChangesLocation> unloadRegionChangesIntents;

		private NativeList<Pool<ChunkPatches>.Slot> chunksNotInCache;
		private NativeList<RegionBaseLocation> regionBasesToRemove;
		private NativeList<RegionChangesLocation> regionChangesToRemove;

		private PlayersCache* playerCache;

		readonly Pool<PatchPointer.SinglePatch> PatchPointer.IPoolsHolder.PatchesPool => singlePatchPool;
		readonly Pool<PatchPointer.PatchesGroup> PatchPointer.IPoolsHolder.PatchGroupsPool => patchGroupPool;

		readonly Pool<PatchPointer.PatchesGroup> RegionChanges.LoadTask.IPoolsHolder.PatchGroupsPool => patchGroupPool;
		readonly ChainSegment<PatchView>.Pool RegionChanges.LoadTask.IPoolsHolder.SegmentsPool => patchesSegmentsPool;

		/// <remarks>Access only from <see cref="ThreadRules.SyncPhase"/></remarks>
		public class Managed
		{
			private readonly Storage* storage;
			private readonly FileManager fileManager;
			private readonly IChunkProcessor chunkProcessor;

			private readonly Queue<RegionBase.LoadTask> freeLoadRegionBaseTasks;
			private readonly List<Task<RegionBase.LoadTask>> loadRegionBaseTasks;

			private readonly Queue<RegionChanges.LoadTask> freeLoadRegionChangesTasks;
			private readonly List<Task<RegionChanges.LoadTask>> loadRegionChangesTasks;

			private readonly Queue<RegionChanges.UnloadTask> freeUnloadRegionChangesTasks;
			private readonly List<Task<RegionChanges.UnloadTask>> unloadRegionChangesTasks;

			private bool isSync;

			public Managed(FileManager fileManager, Storage* storage, IChunkProcessor chunkProcessor)
			{
				this.fileManager = fileManager;
				this.storage = storage;
				this.chunkProcessor = chunkProcessor;
				freeLoadRegionBaseTasks = new();
				loadRegionBaseTasks = new();
				freeLoadRegionChangesTasks = new();
				loadRegionChangesTasks = new();
				freeUnloadRegionChangesTasks = new();
				unloadRegionChangesTasks = new();
			}

			private void ProcessLoadRegionBaseTasks()
			{
				var bases = storage->bases;
				for (int i = loadRegionBaseTasks.Count - 1; i >= 0; i--) {
					var task = loadRegionBaseTasks[i];
					if (task.IsCompleted) {
						var loadTask = task.Result;
						loadRegionBaseTasks.RemoveAt(i);
						freeLoadRegionBaseTasks.Enqueue(loadTask);
						var (location, slot) =
							(loadTask.RegionBaseLocation, loadTask.RegionBaseSlot);
						bases[location] = slot;
						slot.Pointer->UsageState.SetBits(RegionBase.UsageBits.Cached);
						chunkProcessor.RegionBaseLoaded(location, slot);
						loadTask.RegionBaseLocation = default;
						loadTask.RegionBaseSlot = default;
					}
				}
			}

			private void ProcessLoadRegionChangesTasks()
			{
				var changes = storage->changes;
				for (int i = loadRegionChangesTasks.Count - 1; i >= 0; i--) {
					var task = loadRegionChangesTasks[i];
					if (task.IsCompleted) {
						var loadTask = task.Result;
						if (loadTask.IsCompleted) {
							loadRegionChangesTasks.RemoveAt(i);
							freeLoadRegionChangesTasks.Enqueue(loadTask);
							changes[loadTask.RegionChangesLocation] = loadTask.RegionChangesSlot;
							var chunks = UnmanagedArray.From(
								&loadTask.RegionChangesSlot.Pointer->Chunks
							);
							foreach (var chunk in chunks) {
								chunk.Pointer->UsageState.SetBits(ChunkPatches.UsageBits.Cached);
								chunkProcessor.ChunkLoadedOrUpdated(chunk);
							}
							loadTask.RegionChangesSlot = default;
							loadTask.RegionChangesLocation = default;
						} else {
							loadTask.FillBuffers(storage);
							loadRegionChangesTasks[i] = Task.Run(loadTask.ActionDelegate);
						}
					}
				}
			}

			private void ProcessUnloadRegionChangesTasks()
			{
				var playerCache = storage->playerCache;
				for (int i = unloadRegionChangesTasks.Count - 1; i >= 0; i--) {
					var task = unloadRegionChangesTasks[i];
					if (task.IsCompleted) {
						var unloadTask = task.Result;
						unloadRegionChangesTasks.RemoveAt(i);
						freeUnloadRegionChangesTasks.Enqueue(unloadTask);
						var slot = storage->changes[unloadTask.RegionChangesLocation];
						slot.Pointer->UnloadTaskCount--;
					}
				}
			}

			private int GetNearestLocationToPlayer<T>(
					NativeList<T> locations,
					UnmanagedArray<T> playerPositions
				) where T : unmanaged, ILoacation
			{
				int bestMatchIndex = 0;
				float minDistancesq = float.MaxValue;
				for (int i = locations.Count - 1; i >= 0; i--) {
					var location = locations[i];
					for (int playerIndex = 0; playerIndex < playerPositions.Length; playerIndex++) {
						float distancesq = math.distancesq(
							(int2)location.AxisIndices, (int2)playerPositions[playerIndex].AxisIndices
						);
						if (distancesq < minDistancesq) {
							minDistancesq = distancesq;
							bestMatchIndex = i;
						}
					}
				}
				return bestMatchIndex;
			}

			private int GetFarthestLocationToPlayer<T>(
					NativeList<T> locations,
					UnmanagedArray<T> playerPositions
				) where T : unmanaged, ILoacation
			{
				int bestMatchIndex = 0;
				float minDistancesq = float.MaxValue;
				for (int i = locations.Count - 1; i >= 0; i--) {
					var location = locations[i];
					for (int playerIndex = 0; playerIndex < playerPositions.Length; playerIndex++) {
						float distancesq = math.distancesq(
							(int2)location.AxisIndices, (int2)playerPositions[playerIndex].AxisIndices
						);
						if (distancesq > minDistancesq) {
							minDistancesq = distancesq;
							bestMatchIndex = i;
						}
					}
				}
				return bestMatchIndex;
			}

			private void ProcessLoadRegionBaseIntents()
			{
				var loadIntents = storage->loadRegionBaseIntents;
				if (loadRegionBaseTasks.Count > 0 || loadIntents.Count == 0)
					return;
				var playerCache = storage->playerCache;
				for (int i = loadIntents.Count - 1; i >= 0; i--) {
					var location = loadIntents[i];
					if (!playerCache->CanSaveChunks(location)) {
						loadIntents.RemoveAtSwapBack(i);
					}
				}
				if (loadIntents.Count == 0)
					return;
				int bestMatchIndex = GetNearestLocationToPlayer(
					loadIntents, playerCache->CurrentBasesRegionPerPlayer
				);
				if (!freeLoadRegionBaseTasks.TryDequeue(out var task)) {
					task = new RegionBase.LoadTask(fileManager);
				}
				task.RegionBaseLocation = loadIntents[bestMatchIndex];
				loadIntents.RemoveAtSwapBack(bestMatchIndex);
				task.RegionBaseSlot = storage->regionBasesPool.Rent();
				loadRegionBaseTasks.Add(Task.Run(task.ActionDelegate));
			}

			private void ProcessLoadRegionChangesIntents()
			{
				var loadIntents = storage->loadRegionChangesIntents;
				if (loadRegionChangesTasks.Count > 0 || loadIntents.Count == 0) {
					return;
				}
				var playerCache = storage->playerCache;
				for (int i = loadIntents.Count - 1; i >= 0; i--) {
					var location = loadIntents[i];
					if (!playerCache->CanSaveChunks(location)) {
						loadIntents.RemoveAtSwapBack(i);
					}
				}
				if (loadIntents.Count == 0)
					return;
				int bestMatchIndex = GetNearestLocationToPlayer(
					loadIntents, playerCache->CurrentChangesRegionPerPlayer
				);
				if (!freeLoadRegionChangesTasks.TryDequeue(out var task)) {
					task = new RegionChanges.LoadTask(fileManager);
				}
				task.RegionChangesLocation = loadIntents[bestMatchIndex];
				loadIntents.RemoveAtSwapBack(bestMatchIndex);
				task.RegionChangesSlot = storage->regionChangesPool.Rent();
				var chunkPatchesArray =
					UnmanagedArray.From(&task.RegionChangesSlot.Pointer->Chunks);
				for (int i = 0; i < chunkPatchesArray.Length; i++) {
					chunkPatchesArray[i] = storage->chunkPatchesPool.Rent();
				}
				loadRegionChangesTasks.Add(Task.Run(task.ActionDelegate));
			}

			private void ProcessUnloadRegionChangesIntents()
			{
				var changes = storage->changes;
				var unloadIntents = storage->unloadRegionChangesIntents;
				var playerCache = storage->playerCache;
				for (int i = unloadIntents.Count - 1; i >= 0; i--) {
					var location = unloadIntents[i];
					if (playerCache->CanSaveChunks(location)) {
						unloadIntents.RemoveAtSwapBack(i);
						var regionChangesPtr = changes[location].Pointer;
						regionChangesPtr->UnloadTaskCount--;
						regionChangesPtr->IsModified = true;
					}
				}
				if (unloadIntents.Count == 0)
					return;
				int bestMatchIndex = GetFarthestLocationToPlayer(
					unloadIntents, playerCache->CurrentChangesRegionPerPlayer
				);
				var regionChangesLocation = unloadIntents[bestMatchIndex];
				unloadIntents.RemoveAtSwapBack(bestMatchIndex);
				var slot = changes[regionChangesLocation];
				var regionChanges = slot.Pointer;
				if (!freeUnloadRegionChangesTasks.TryDequeue(out var task)) {
					task = new RegionChanges.UnloadTask(fileManager);
				}
				task.RegionChangesLocation = regionChangesLocation;
				*task.Chunks = regionChanges->Chunks;
				unloadRegionChangesTasks.Add(Task.Run(task.ActionDelegate));
			}

			public void Sync()
			{
				var playerCache = storage->playerCache;
				var bases = storage->bases;
				var changes = storage->changes;

				ProcessLoadRegionBaseTasks();
				ProcessLoadRegionChangesTasks();
				ProcessUnloadRegionChangesTasks();

				ProcessLoadRegionBaseIntents();
				ProcessLoadRegionChangesIntents();
				ProcessUnloadRegionChangesIntents();
			}
		}

		public Storage(
			FileManager fileManager,
			SessionRewindableAllocator allocator,
			DisposeList sessionDisposeList,
			PlayersCache* playerCache)
		{
			this.playerCache = playerCache;

			var dl = sessionDisposeList;
			dl.Add(changes = new(initialCapacity: 128, Allocator.Persistent));
			dl.Add(bases = new(initialCapacity: 128, Allocator.Persistent));

			chunkPatchesPool = new(dl, allocator);
			regionChangesPool = new(dl, allocator);
			regionBasesPool = new(dl, allocator);
			singlePatchPool = new(dl, allocator, itemCountPerAllocation: 256);
			patchGroupPool = new(dl, allocator, itemCountPerAllocation: 16);
			patchesSegmentsPool = new(dl, allocator, itemCountPerAllocation: 1024);

			dl.Add(loadRegionBaseIntents = new(Allocator.Persistent));
			dl.Add(loadRegionChangesIntents = new(Allocator.Persistent));
			dl.Add(unloadRegionChangesIntents = new(Allocator.Persistent));

			dl.Add(chunksNotInCache = new(Allocator.Persistent));
			dl.Add(regionBasesToRemove = new(Allocator.Persistent));
			dl.Add(regionChangesToRemove = new(Allocator.Persistent));
		}

		public void RequsetRegion(RegionChangesLocation location)
		{
			if (changes.ContainsKey(location))
				return;
			changes[location] = default;
			loadRegionChangesIntents.Add(location);
		}

		public void RequsetRegion(RegionBaseLocation location)
		{
			if (bases.ContainsKey(location))
				return;
			bases[location] = default;
			loadRegionBaseIntents.Add(location);
		}

		public bool TryGetRegionBase(RegionBaseLocation location, out Pool<RegionBase>.Slot slot) =>
			bases.TryGetValue(location, out slot);

		public bool TryGetRegionBase(RegionChangesLocation location, out Pool<RegionChanges>.Slot slot) =>
			changes.TryGetValue(location, out slot);

		public void ReplaceChunk(ChunkLocation location, ChunkPatches newChunkPatches)
		{
			if (newChunkPatches.PatchesChainStart.IsNull)
				throw new Exception("Receved chunk can't be null");
			if (newChunkPatches.UsageState != ChunkPatches.UsageBits.None)
				throw new Exception("Invalid state: chunkPatches already in use");
			foreach (var patch in newChunkPatches) {
				patch.Patch.IncrementReferenceCount();
			}
			var regionLocation = RegionChangesLocation.From(location);
			if (!changes.TryGetValue(regionLocation, out var currentChangesSlot)) {
				throw new Exception("Invalid chunk flow: chunk not in cache.");
			}
			var regionChanges = currentChangesSlot.Pointer;
			regionChanges->IsModified = true;
			int chunkLocalIndex = RegionChanges.GetChunkIndexInsideRegion(location);
			var chunks = UnmanagedArray.From(&regionChanges->Chunks);
			var slot = chunks[chunkLocalIndex];
			if (slot.Pointer->UsageState == ChunkPatches.UsageBits.Cached) {
				var chain = slot.PointerUnchecked->PatchesChainStart;
				if (chain.IsNotNull) {
					foreach (var view in chain.EnumerateChain()) {
						if (view.Patch.DecrementReferenceCount() == 0) {
							view.Patch.Release(this);
						}
					}
					chain.ReleaseChain(patchesSegmentsPool);
				}
				chunkPatchesPool.Release(slot);
			} else {
				chunksNotInCache.Add(slot);
			}
			newChunkPatches.UsageState.SetBits(ChunkPatches.UsageBits.Cached);
			slot = chunkPatchesPool.Rent();
			*slot.Pointer = newChunkPatches;
			chunks[chunkLocalIndex] = slot;
		}

		public void CollectGarbage()
		{
			foreach (var slot in chunksNotInCache) {
				var chunk = slot.Pointer;
				if (chunk->UsageState == ChunkPatches.UsageBits.None) {
					foreach (var view in *chunk) {
						if (view.Patch.DecrementReferenceCount() == 0) {
							view.Patch.Release(this);
						}
					}
					chunk->PatchesChainStart.ReleaseChain(patchesSegmentsPool);
					chunkPatchesPool.Release(slot);
				}
			}
			foreach (var pair in bases) {
				var slot = pair.Value;
				var loacation = pair.Key;
				var regionBase = slot.Pointer;
				if (
					!playerCache->CanSaveChunks(loacation) &&
					regionBase->UsageState == RegionBase.UsageBits.Cached
				) {
					regionBasesPool.Release(slot);
					regionBasesToRemove.Add(loacation);
				}
			}
			foreach (var pair in changes) {
				var slot = pair.Value;
				var loacation = pair.Key;
				var regionChanges = slot.Pointer;
				if (regionChanges->IsModified) {
					if (regionChanges->UnloadTaskCount != 0)
						continue;
					regionChanges->IsModified = false;
					regionChanges->UnloadTaskCount++;
					unloadRegionChangesIntents.Add(loacation);
				}
				if (regionChanges->UnloadTaskCount != 0)
					continue;
				var chunks = UnmanagedArray.From(&regionChanges->Chunks);
				var mergedFlags = ChunkPatches.UsageBits.None;
				foreach (var chunk in chunks) {
					mergedFlags |= chunk.Pointer->UsageState;
				}
				if (mergedFlags != ChunkPatches.UsageBits.None)
					continue;
				foreach (var chunk in chunks) {
					var chain = chunk.Pointer->PatchesChainStart;
					if (chain.IsNotNull) {
						foreach (var view in chain.EnumerateChain()) {
							if (view.Patch.DecrementReferenceCount() == 0) {
								view.Patch.Release(this);
							}
						}
						chain.ReleaseChain(patchesSegmentsPool);
					}
					chunkPatchesPool.Release(chunk);
				}
				regionChangesPool.Release(slot);
				regionChangesToRemove.Add(loacation);
			}
			for (int i = regionBasesToRemove.Count; i >= 0; i--) {
				bases.Remove(regionBasesToRemove[i]);
			}
			regionBasesToRemove.Clear();
			for (int i = regionChangesToRemove.Count; i >= 0; i--) {
				changes.Remove(regionChangesToRemove[i]);
			}
			regionChangesToRemove.Clear();
		}

		private void DecrementReferenceCountForeachPatch(Pool<ChunkPatches>.Slot chunkPatches)
		{
			foreach (var patchView in *chunkPatches.Pointer) {
				int referenceCount = patchView.Patch.DecrementReferenceCount();
				if (referenceCount < 0)
					throw new Exception($"Invalid patch reference count: {referenceCount}");
				if (referenceCount == 0)
					patchView.Patch.Release(this);
			}
		}
	}
}
