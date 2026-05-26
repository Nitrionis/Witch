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
}
