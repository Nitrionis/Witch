using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Game.WorldGeneration
{
	public enum Biome : byte
	{
		Null = 0
	}

	public interface IGeneratorPhase
	{
		Layer Execute(Layer layer);
	}

	public class ZoomPhase : IGeneratorPhase
	{
		public Layer Execute(Layer layer) => layer.ZoomX2();
	}

	public class AddIsland : IGeneratorPhase
	{
		private readonly System.Random random;
		private readonly float probability;
		private readonly bool skipWorldBoundary;

		public AddIsland(Random random, float probability, bool skipWorldBoundary = false)
		{
			this.random = random;
			this.probability = probability;
			this.skipWorldBoundary = skipWorldBoundary;
		}

		public Layer Execute(Layer layer)
		{
			int start = skipWorldBoundary ? 1 : 0;
			int end = layer.SideLength - (skipWorldBoundary ? 1 : 0);
			for (int x = start; x < end; x++) {
				for (int y = start; y < end; y++) {
					ref var cell = ref layer[x, y];
					if (!cell.IsLand && random.NextDouble() < probability) {
						cell.IsLand = true;
					}
				}
			}
			return layer;
		}
	}

	public class AddHeight : IGeneratorPhase
	{
		public Layer Execute(Layer layer)
		{
			int start = 0;
			int end = layer.SideLength;
			for (int x = start; x < end; x++) {
				for (int y = start; y < end; y++) {
					ref var cell = ref layer[x, y];
					cell.Height = GetHeight((Biome)cell.Biome);
				}
			}
			return layer;
		}

		private ushort GetHeight(Biome biome)
		{
			// TODO
			return biome switch {
				Biome.Null => 0,
				_ => throw new NotImplementedException()
			};
		}
	}

	public class AddTemperature : IGeneratorPhase
	{
		private readonly System.Random random;

		public AddTemperature(Random random)
		{
			this.random = random;
		}

		public Layer Execute(Layer layer)
		{
			int start = 0;
			int end = layer.SideLength;
			for (int x = start; x < end; x++) {
				for (int y = start; y < end; y++) {
					ref var cell = ref layer[x, y];
					var value = random.NextDouble();
					if (value < 0.79) {
						cell.Biome = 100;
					} else if (value < 0.90) {
						cell.Biome = 25;
					} else {
						cell.Biome = 0;
					}
				}
			}
			return layer;
		}
	}

	public class SmoothTemperature : IGeneratorPhase
	{
		private readonly bool skipCorners;

		public SmoothTemperature(bool skipCorners = false)
		{
			this.skipCorners = skipCorners;
		}

		public Layer Execute(Layer layer)
		{
			var layerDst = layer.Clone();
			int start = 0;
			int end = layer.SideLength;
			for (int x = start; x < end; x++) {
				for (int y = start; y < end; y++) {
					ref var cell = ref layer[x, y];
					ref var dstCell = ref layerDst[x, y];
					var biome = cell.Biome;
					bool hot = false;
					bool warm = false;
					bool medium = false;
					bool cold = false;
					if (!skipCorners)
						HandleBiome(x - 1, y - 1);
					HandleBiome(x + 0, y - 1);
					if (!skipCorners)
						HandleBiome(x + 1, y - 1);
					HandleBiome(x - 1, y + 0);
					HandleBiome(x + 1, y + 0);
					if (!skipCorners)
						HandleBiome(x - 1, y + 1);
					HandleBiome(x + 0, y + 1);
					if (!skipCorners)
						HandleBiome(x + 1, y + 1);
					if (cold) {
						biome = Math.Min(biome, (byte)25);
					} else if (medium) {
						biome = Math.Min(biome, (byte)50);
					}
					dstCell.Biome = biome;

					void HandleBiome(int x, int y)
					{
						var b = layer.GetCellSafe(x, y).Biome;
						if (b >= 100) {
							hot = true;
						} else if (b >= 50) {
							warm = true;
						} else if (b > 0) {
							medium = true;
						} else {
							cold = true;
						}
					}
				}
			}
			return layerDst;
		}
	}

	public class SmoothHeight : IGeneratorPhase
	{
		public Layer Execute(Layer layer)
		{
			var layerDst = layer.Clone();
			int start = 0;
			int end = layer.SideLength;
			for (int x = start; x < end; x++) {
				for (int y = start; y < end; y++) {
					ref var cell = ref layer[x, y];
					ref var dstCell = ref layerDst[x, y];
					var height = cell.Height;
					var neighbourHeight = ushort.MaxValue;
					neighbourHeight = Math.Min(neighbourHeight, layer.GetCellSafe(x - 1, y - 1).Height);
					neighbourHeight = Math.Min(neighbourHeight, layer.GetCellSafe(x + 0, y - 1).Height);
					neighbourHeight = Math.Min(neighbourHeight, layer.GetCellSafe(x + 1, y - 1).Height);
					neighbourHeight = Math.Min(neighbourHeight, layer.GetCellSafe(x - 1, y + 0).Height);
					neighbourHeight = Math.Min(neighbourHeight, layer.GetCellSafe(x + 1, y + 0).Height);
					neighbourHeight = Math.Min(neighbourHeight, layer.GetCellSafe(x - 1, y + 1).Height);
					neighbourHeight = Math.Min(neighbourHeight, layer.GetCellSafe(x + 0, y + 1).Height);
					neighbourHeight = Math.Min(neighbourHeight, layer.GetCellSafe(x + 1, y + 1).Height);
					dstCell.Height = height < neighbourHeight
						? (ushort)UnityEngine.Mathf.Lerp(height, neighbourHeight, 0.25f)
						: (ushort)UnityEngine.Mathf.Lerp(neighbourHeight, height, 0.75f);
				}
			}
			return layerDst;
		}
	}

	public class ChangeCoastline : IGeneratorPhase
	{
		private readonly System.Random random;
		private readonly float expandOceanProbability;
		private readonly float expandLandProbability;
		private readonly int landCountToExpandOcean;
		private readonly int landCountToExpandLand;
		private readonly bool skipWorldBoundary;

		public ChangeCoastline(
			Random random,
			float expandOceanProbability,
			float expandLandProbability,
			bool skipWorldBoundary = false,
			int landCountToExpandOcean = 8,
			int landCountToExpandLand = 0)
		{
			this.random = random;
			this.expandOceanProbability = expandOceanProbability;
			this.expandLandProbability = expandLandProbability;
			this.skipWorldBoundary = skipWorldBoundary;
			this.landCountToExpandOcean = landCountToExpandOcean;
			this.landCountToExpandLand = landCountToExpandLand;
		}

		public Layer Execute(Layer layer)
		{
			var layerDst = layer.Clone();
			int start = skipWorldBoundary ? 1 : 0;
			int end = layer.SideLength - (skipWorldBoundary ? 1 : 0);
			for (int x = start; x < end; x++) {
				for (int y = start; y < end; y++) {
					int landCount =
						IsLand(x - 1, y - 1) +
						IsLand(x    , y - 1) +
						IsLand(x + 1, y - 1) +
						IsLand(x - 1, y    ) +
						IsLand(x + 1, y    ) +
						IsLand(x - 1, y + 1) +
						IsLand(x    , y + 1) +
						IsLand(x + 1, y + 1);
					var cell = layer[x, y];
					ref var dstCell = ref layerDst[x, y];
					var value = random.NextDouble();
					if (!cell.IsLand && value < expandLandProbability) {
						if (landCount > landCountToExpandLand) {
							dstCell.IsLand = true;
						}
					} else if (cell.IsLand && value < expandOceanProbability) {
						if (landCount < landCountToExpandOcean) {
							dstCell.IsLand = false;
						}
					}
				}
			}
			return layerDst;

			int IsLand(int x, int y) => layer.GetCellSafe(x, y).IsLand ? 1 : 0;
		}
	}

	public class BiomeDiffusion : IGeneratorPhase
	{
		private readonly System.Random random;
		private readonly float expandProbability;
		private readonly int biomeCountToExpand;
		private readonly bool skipWorldBoundary;

		public BiomeDiffusion(
			Random random,
			float expandProbability,
			int biomeCountToExpand = 3,
			bool skipWorldBoundary = false)
		{
			this.random = random;
			this.expandProbability = expandProbability;
			this.skipWorldBoundary = skipWorldBoundary;
			this.biomeCountToExpand = biomeCountToExpand;
		}

		public Layer Execute(Layer layer)
		{
			var layerDst = layer.Clone();
			int start = skipWorldBoundary ? 1 : 0;
			int end = layer.SideLength - (skipWorldBoundary ? 1 : 0);
			for (int x = start; x < end; x++) {
				for (int y = start; y < end; y++) {
					var b1 = layer.GetCellSafe(x - 1, y - 1).Biome;
					var b2 = layer.GetCellSafe(x,     y - 1).Biome;
					var b3 = layer.GetCellSafe(x + 1, y - 1).Biome;
					var b4 = layer.GetCellSafe(x - 1, y    ).Biome;
					var b5 = layer[x, y].Biome;
					var b6 = layer.GetCellSafe(x + 1, y    ).Biome;
					var b7 = layer.GetCellSafe(x - 1, y + 1).Biome;
					var b8 = layer.GetCellSafe(x,     y + 1).Biome;
					var b9 = layer.GetCellSafe(x + 1, y + 1).Biome;
					ref var dstCell = ref layerDst[x, y];

					byte neighbour = b5;
					int homeBiomeCounter = 0;
					CountHomeBiome(b1);
					CountHomeBiome(b2);
					CountHomeBiome(b3);
					CountHomeBiome(b4);
					CountHomeBiome(b6);
					CountHomeBiome(b7);
					CountHomeBiome(b8);
					CountHomeBiome(b9);

					GetNeighbourBiome(b1);
					GetNeighbourBiome(b2);
					GetNeighbourBiome(b3);
					GetNeighbourBiome(b4);
					GetNeighbourBiome(b6);
					GetNeighbourBiome(b7);
					GetNeighbourBiome(b8);
					GetNeighbourBiome(b9);
					//if (homeBiomeCounter <= biomeCountToExpand) {
					//	dstCell.Biome = neighbour;
					//}
					dstCell.Biome = neighbour;

					void CountHomeBiome(byte biome) => homeBiomeCounter += biome == b5 ? 1 : 0;

					void GetNeighbourBiome(byte biome)
					{
						if (biome != b5) {
							var value = random.NextDouble();
							if (value < expandProbability) {
								neighbour = biome;
							}
						}
					}
				}
			}
			return layerDst;
		}
	}

	public struct Cell
	{
		private const uint LandMask = 1 << 0;
		private const uint RiverMask = 1 << 1;
		private const uint BeachMask = 1 << 2;
		private const uint BiomeMask = 0xFF00;
		private const uint HeightMask = 0xFFFF0000;
		private uint data;

		public bool IsLand { get => HasBit(LandMask); set => SetBit(LandMask, value); }
		public bool IsRiver { get => HasBit(RiverMask); set => SetBit(RiverMask, value); }
		public bool IsBeach { get => HasBit(BeachMask); set => SetBit(BeachMask, value); }

		public byte Biome
		{
			get => (byte)GetBits(BiomeMask, offset: 8);
			set => SetBits(BiomeMask, offset: 8, value);
		}

		public ushort Height
		{
			get => (byte)GetBits(HeightMask, offset: 16);
			set => SetBits(HeightMask, offset: 16, value);
		}

		private bool HasBit(uint mask) => (data & mask) == mask;
		private void SetBit(uint mask, bool value) => data = (data & ~mask) | (value ? mask : 0);
		private uint GetBits(uint mask, int offset) => (data & mask) >> offset;
		private uint SetBits(uint mask, int offset, uint value) => data = (data & ~mask) | (value << offset) & mask;
	}

	public class Layer
	{
		public readonly PowerOfTwo SideLength;

		private readonly Cell[] data;

		public ref Cell this[int x, int y] => ref data[SideLength * y + x];

		public Layer(PowerOfTwo sideLength)
		{
			SideLength = sideLength;
			data = new Cell[sideLength * sideLength];
		}

		public Layer ZoomX2()
		{
			var layer = new Layer(2 * SideLength);
			for (int y = 0; y < SideLength; y++) {
				for (int x = 0; x < SideLength; x++) {
					var value = this[x, y];
					layer[2*x + 0, 2*y + 0] = value;
					layer[2*x + 0, 2*y + 1] = value;
					layer[2*x + 1, 2*y + 0] = value;
					layer[2*x + 1, 2*y + 1] = value;
				}
			}
			return layer;
		}

		public Cell GetCellSafe(int x, int y)
		{
			if (x < 0 || x >= SideLength || y < 0 || y >= SideLength)
				return default;
			return this[x, y];
		}

		public Layer Clone()
		{
			var layer = new Layer(SideLength);
			Array.Copy(sourceArray: data, destinationArray: layer.data, data.Length);
			return layer;
		}
	}

	public readonly struct PowerOfTwo
	{
		public readonly int value;

		public PowerOfTwo(int value)
		{
			if (!IsValidValue(value)) {
				throw new System.Exception($"{value} is not power of 2");
			}
			this.value = value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator PowerOfTwo(int v) => new PowerOfTwo(v);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator int(PowerOfTwo v) => v.value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsValidValue(int n)
		{
			// A number is a power of two if it's positive and has only one bit set.
			// n > 0 ensures it's positive.
			// (n & (n - 1)) == 0 checks if only one bit is set.
			return n > 0 && (n & (n - 1)) == 0;
		}
	}
}
