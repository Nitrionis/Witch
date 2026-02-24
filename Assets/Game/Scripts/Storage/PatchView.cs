namespace Game.Storage
{
	internal readonly struct PatchView
	{
		public readonly PatchPointer Patch;
		public readonly PatchKey PatchKey;
		public readonly ChunkPatch.Metadata Metadata;

		public PatchView(PatchKey patchKey, ChunkPatch.Metadata metadata, PatchPointer patch)
		{
			if (patchKey.Version != metadata.Version) {
				throw new System.Exception("Invalid patch version: patchKey.Version != metadata.Version");
			}
			PatchKey = patchKey;
			Metadata = metadata;
			Patch = patch;
		}
	}
}
