using UnityEngine;

public class IslandGenerator
{
	private const int INITIAL_GRID_SIZE = 10;
	private const int FINAL_TILE_SIZE = 4; // 1 cell = 4x4 tile
	private const int FINAL_WORLD_SIZE = 20000;
	private const int FINAL_GRID_SIZE = FINAL_WORLD_SIZE / FINAL_TILE_SIZE;

	private readonly int seed;
	private LCG rng;

	public IslandGenerator(int seed)
	{
		this.seed = seed;
		this.rng = new LCG(seed);
	}

	public int[,] GenerateIslandMap()
	{
		int[,] map = new int[INITIAL_GRID_SIZE, INITIAL_GRID_SIZE];

		// Initial fill with random 0 (water) or 1 (land)
		for (int y = 0; y < INITIAL_GRID_SIZE; y++) {
			for (int x = 0; x < INITIAL_GRID_SIZE; x++) {
				map[x, y] = rng.NextDouble() < 0.5 ? 0 : 1;
			}
		}

		int[,] currentMap = map;

		// Perform zooms until each cell is 4x4 in the final 20000x20000 map
		int zoomLevels = (int)Mathf.Log(FINAL_GRID_SIZE / INITIAL_GRID_SIZE, 2);

		for (int i = 0; i < zoomLevels; i++) {
			currentMap = ZoomAndExpand(currentMap);
		}

		return currentMap; // Final 5000x5000 map
	}

	private int[,] ZoomAndExpand(int[,] input)
	{
		int w = input.GetLength(0);
		int h = input.GetLength(1);
		int newW = w * 2;
		int newH = h * 2;

		int[,] output = new int[newW, newH];

		for (int y = 0; y < h; y++) {
			for (int x = 0; x < w; x++) {
				int val = input[x, y];

				// Determine probabilities based on current cell type
				float expansionChance = val == 1 ? 0.6f : 0.4f;

				// Each cell expands into 2x2 area with some variation
				for (int dy = 0; dy < 2; dy++) {
					for (int dx = 0; dx < 2; dx++) {
						int nx = x * 2 + dx;
						int ny = y * 2 + dy;

						bool expand = rng.NextDouble() < expansionChance;
						output[nx, ny] = expand ? val : 1 - val; // Flip with some chance
					}
				}
			}
		}

		return output;
	}
}

// Linear Congruential Generator
public class LCG
{
	private uint state;

	private const uint a = 1664525;
	private const uint c = 1013904223;
	private const uint m = 0xFFFFFFFF;

	public LCG(int seed)
	{
		state = (uint)seed;
	}

	public uint Next()
	{
		state = (a * state + c) & m;
		return state;
	}

	public float NextFloat()
	{
		return (float)Next() / uint.MaxValue;
	}

	public double NextDouble()
	{
		return (double)Next() / uint.MaxValue;
	}

	public int NextInt(int minInclusive, int maxExclusive)
	{
		return (int)(NextDouble() * (maxExclusive - minInclusive)) + minInclusive;
	}
}