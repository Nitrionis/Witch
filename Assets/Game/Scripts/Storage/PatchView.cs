using System.Runtime.CompilerServices;
using Game.Allocators;
using Game.Collections;

namespace Game.Storage
{
	internal readonly struct PatchView
	{
		public readonly PatchPointer Patch;
		public readonly ChunkPatch.Metadata Metadata;

		public PatchView(PatchPointer patch, ChunkPatch.Metadata metadata)
		{
			Metadata = metadata;
			Patch = patch;
		}
	}

	internal readonly struct PatchesSegment
	{
		public readonly Segment<Repeat8<PatchView>, PatchView> Segment;

		public readonly bool IsNull
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Segment.IsNull;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PatchesSegment(Segment<Repeat8<PatchView>, PatchView> segment) => Segment = segment;

		public void ReleaseChain(Pool pool) => Segment.ReleaseChain(pool);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Segment<Repeat8<PatchView>, PatchView>.EnumerableChain EnumerateChain() =>
			new Segment<Repeat8<PatchView>, PatchView>.EnumerableChain(Segment);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Segment<Repeat8<PatchView>, PatchView>(PatchesSegment segment) => segment.Segment;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator PatchesSegment(Segment<Repeat8<PatchView>, PatchView> segment) => new PatchesSegment(segment);

		public struct ChainBuilder
		{
			private Segment<Repeat8<PatchView>, PatchView>.ChainBuilder chainBuilder;
			
			public PatchesSegment First
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => chainBuilder.First;
			}

			/// <summary>
			/// Allows you to continue a chain from its last element.
			/// </summary>
			/// <remarks>The default constructor is also valid.</remarks>
			public ChainBuilder(PatchesSegment chainStart) => chainBuilder = new Segment<Repeat8<PatchView>, PatchView>.ChainBuilder(chainStart);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Add(PatchView item, in Pool pool) => chainBuilder.Add(item, in pool.BasePool);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool TryAddNoResize(PatchView item) => chainBuilder.TryAddNoResize(item);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void ConnectNextSegment(PatchesSegment segment) => chainBuilder.ConnectNextSegment(segment.Segment);
		}

		public readonly struct Pool
		{
			public readonly Segment<Repeat8<PatchView>, PatchView>.Pool BasePool;

			public Pool(DisposeList disposeList, in SessionRewindableAllocator poolsAllocator, int itemCountPerAllocation) =>
				BasePool = new(disposeList, in poolsAllocator, itemCountPerAllocation);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public readonly PatchesSegment Rent() => new(BasePool.Rent());

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public readonly void Return(Segment<Repeat8<PatchView>, PatchView> segment) => BasePool.Return(segment);
		}
	}
}
