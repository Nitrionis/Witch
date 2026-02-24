using System;
using System.Collections.Generic;
using Assets.Game.Scripts.Storage;
using Game.Collections;

namespace Game.Storage
{
	using DecoratedRegionBase = Storage.Decorated<RegionBase, Storage.RegionProceduralBaseStatus>;
	using DecoratedRegionPatches = Storage.Decorated<RegionPatches, Storage.PatchesStatus>;
	using DecoratedChunkPatch = Storage.Decorated<ChunkPatch, Storage.ChunkPatchStatus>;

	public interface IServerStorage
	{

	}

	public unsafe partial class Storage
	{
		private readonly Pool<DecoratedRegionBase> regionProceduralBasesPool = new();
		private readonly Pool<DecoratedRegionPatches> regionPatchesPool = new();
		private readonly Pool<DecoratedChunkPatch> chunkPatchesPool = new();

		// TODO How to apply patch from network?

		internal struct Decorated<TItem, TStatus>
			where TItem : unmanaged
			where TStatus : Enum
		{
			public int ReferenceCounter;
			public TStatus Status;
			public TItem Decoratable;
		}

		private readonly Dictionary<PatchKey, Pool<DecoratedRegionPatches>.Slot> phkjkbjb;

		protected void Update()
        {

			
			


			throw new NotImplementedException();
		}

        protected void Destroy()
        {
			regionProceduralBasesPool.Destroy();
			regionPatchesPool.Destroy();
			chunkPatchesPool.Destroy();
		}

		internal enum PatchesStatus
		{
			Unloaded,
			LoadingFromFile,
			Ready,
		}

		internal enum RegionProceduralBaseStatus
		{
			Unloaded,
			LoadingFromFile,
			Deserialization,
			Ready,
		}

		[Flags]
		internal enum ChunkPatchStatus : ulong
		{
			None = 0,
			Loaded = 1 << 0,
			GenerationChunkLoad0 = 1 << 1,
			GenerationChunkLoad1 = 1 << 2,
			LoadingFromFile = 1 << 3,
			Deserialization = 1 << 4,
			SavingDiffToFile = 1 << 5,
		}

		internal enum ChunkStatus
        {
			Requested,
			Building,
			Ready,
		}
    }
}
