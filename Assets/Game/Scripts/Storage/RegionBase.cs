using Unity.Collections;
using Unity.Mathematics;

namespace Game.Storage
{
	internal unsafe struct RegionBase
    {
		public const int ChunkCountPerSide = 8;
		public const int ChunkCount = ChunkCountPerSide * ChunkCountPerSide;
		public const int SideLength = Chunk.SideLength * ChunkCountPerSide;

		public static unsafe NativeList<byte> PackRegion(RegionBase* region)
		{
			int bufferCapacity = 2 * sizeof(RegionBase);
			var buffer = new NativeList<byte>(bufferCapacity, Allocator.Persistent);

			var chunks = new Chunks(region);
			for (int chunkIndex = 0; chunkIndex < ChunkCount; chunkIndex++) {
				Chunk* chunk = chunks[chunkIndex];

				var ptrToLCG = (byte*)&chunk->LCG;
				for (int i = 0; i < sizeof(LCG64); i++) {
					buffer.Add(*(ptrToLCG + i));
				}

				var terrainHeights = new Chunk.TerrainHeights.Ptr(&chunk->Heights);
				int currentHeight = 0;
				int previusHeight = terrainHeights[0];
				for (int i = 1; i < Chunk.MeshVertexCount; i++) {
					currentHeight = math.min(terrainHeights[i], 0x7FFF);
					int signedDelta = currentHeight - previusHeight;
					int unsignedDelta = math.abs(signedDelta);
                    if (unsignedDelta <= 0x3F) {
                        int b = unsignedDelta | (signedDelta < 0 ? 0x40 : 0);
                        buffer.Add((byte)b);
                    } else {
                        int b = (currentHeight >> 8) | 0x80;
                        buffer.Add((byte)b);
                        b = currentHeight & 0xFF;
                        buffer.Add((byte)b);
                    }
                    previusHeight = currentHeight;
				}

				var biomeFlags = new Chunk.BiomeFlags.Ptr(&chunk->Biomes);
				byte repeatCount = 1;
				byte previusBiome = biomeFlags[0];
				for (int i = 1; i < Chunk.MeshVertexCount; i++) {
					byte currentBiome = biomeFlags[i];
					if (
						currentBiome != previusBiome ||
						repeatCount == byte.MaxValue
					) {
						buffer.Add(repeatCount);
						buffer.Add(previusBiome);
						previusBiome = currentBiome;
						repeatCount = 0;
					}
					repeatCount++;
				}
				if (repeatCount > 0) {
					buffer.Add(repeatCount);
					buffer.Add(previusBiome);
				}

			}
			return buffer;
		}

		public static unsafe void UnpackRegion(RegionBase* region, NativeArray<byte> buffer)
		{
			int bufferOffset = 0;
			var chunks = new Chunks(region);
			for (int chunkIndex = 0; chunkIndex < ChunkCount; chunkIndex++) {
				Chunk* chunk = chunks[chunkIndex];

				var ptrToLCG = (byte*)&chunk->LCG;
				for (int i = 0; i < sizeof(LCG64); i++) {
					*(ptrToLCG + i) = buffer[bufferOffset + i];
				}
				bufferOffset += sizeof(LCG64);

				var terrainHeights = new Chunk.TerrainHeights.Ptr(&chunk->Heights);
				int currentHeight = 0;
				for (int i = 0; i < Chunk.MeshVertexCount; i++) {
					byte b = buffer[bufferOffset++];
					if ((b & 0x80) == 0) {
						currentHeight += (b & 0x3F) * ((b & 0x40) != 0 ? -1 : 1);
					} else {
						currentHeight = (b & 0x7F) << 8;
						b = buffer[bufferOffset++];
						currentHeight += b;
					}
					terrainHeights[i] = (ushort)currentHeight;
				}

				var biomeFlags = new Chunk.BiomeFlags.Ptr(&chunk->Biomes);
				for (int i = 0; i < Chunk.MeshVertexCount;) {
					// TODO validate repeatCount
					byte repeatCount = buffer[bufferOffset++];
					byte biome = buffer[bufferOffset++];
					for (int ri = 0; ri < repeatCount; ri++) {
						biomeFlags[i] = biome;
					}
				}
			}
		}

		public readonly unsafe struct Chunks
		{
			public readonly RegionBase* Region;
            public Chunks(RegionBase* region) => Region = region;
            public Chunk* this[int index] => (Chunk*)Region + index;
		}

		public struct Chunk
        {
			/// <summary>
			/// Chunk side length in units.
			/// </summary>
			public const int SideLength = 256;

			/// <summary>
			/// first valuse is count of try to create tree
			/// second valuse is count of try to create rocks...
			/// other values is terrain height variations
			/// </summary>
			public LCG64 LCG;
			public TerrainHeights Heights;
			public BiomeFlags Biomes;

			/// <summary>
			/// 4096 | vertex count in <see cref="TerrainHeights"/> and <see cref="BiomeFlags"/>
			/// </summary>
			public const int MeshVertexCount = MeshVertexCountPerSide * MeshVertexCountPerSide;
			/// <summary>
			/// 64 | the number of control points per side has been reduced by a factor of 4 to save memory
			/// </summary>
			public const int MeshVertexCountPerSide = SideLength / 4;

			public struct TerrainHeights
			{
				public Repeat4096<ushort> Values;

				public unsafe readonly struct Ptr
				{
					public readonly ushort* TerrainHeights;
                    public Ptr(TerrainHeights* terrainHeights) => TerrainHeights = (ushort*)terrainHeights;
					public ushort this[int i]
					{
						get => *(TerrainHeights + i);
						set => *(TerrainHeights + i) = value;
					}
				}
			}

			public struct BiomeFlags
			{
				public Repeat4096<byte> Values;

				public unsafe readonly struct Ptr
				{
					public readonly byte* BiomeFlags;
					public Ptr(BiomeFlags* biomeFlags) => BiomeFlags = (byte*)biomeFlags;
					public byte this[int i]
					{
						get => *(BiomeFlags + i);
						set => *(BiomeFlags + i) = value;
					}
				}
			}
		}
    }
}
