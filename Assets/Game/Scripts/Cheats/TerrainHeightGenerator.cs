using UnityEngine;

public enum BiomeType
{
	Ocean,
	Beach,
	Plains,
	Forest,
	Jungle,
	Desert,
	Mountains,
	Snow
}

public class TerrainHeightGenerator
{
	private const int WIDTH = 5000;
	private const int HEIGHT = 5000;

	private readonly int seed;
	private readonly LCG rng;
	private readonly float baseScale = 0.005f;

	public TerrainHeightGenerator(int seed)
	{
		this.seed = seed;
		rng = new LCG(seed);
	}

	public float[,] GenerateHeightMap(BiomeType[,] biomeMap)
	{
		float[,] heightMap = new float[WIDTH, HEIGHT];

		// Use LCG to generate noise offset for deterministic variation
		float offsetX = rng.NextFloat() * 10000f;
		float offsetY = rng.NextFloat() * 10000f;

		for (int y = 0; y < HEIGHT; y++) {
			for (int x = 0; x < WIDTH; x++) {
				BiomeType biome = biomeMap[x, y];

				// Get biome modulation settings
				GetBiomeNoiseParams(biome, out float amplitude, out int octaves, out float frequency);

				// Compute fBm noise
				float noise = FractalBrownianNoise(x, y, offsetX, offsetY, octaves, frequency);
				float heightValue = noise * amplitude;

				heightMap[x, y] = Mathf.Clamp01(heightValue);
			}
		}

		return heightMap;
	}

	private float FractalBrownianNoise(int x, int y, float offsetX, float offsetY, int octaves, float frequency)
	{
		float total = 0f;
		float maxValue = 0f;
		float amplitude = 1f;

		for (int i = 0; i < octaves; i++) {
			float nx = (x + offsetX) * baseScale * frequency;
			float ny = (y + offsetY) * baseScale * frequency;
			float perlin = Mathf.PerlinNoise(nx, ny);

			total += perlin * amplitude;
			maxValue += amplitude;

			amplitude *= 0.5f;
			frequency *= 2f;
		}

		return total / maxValue;
	}

	private void GetBiomeNoiseParams(BiomeType biome, out float amplitude, out int octaves, out float frequency)
	{
		switch (biome) {
			case BiomeType.Ocean:
				amplitude = 0.1f;
				octaves = 2;
				frequency = 0.5f;
				break;
			case BiomeType.Beach:
				amplitude = 0.15f;
				octaves = 2;
				frequency = 0.8f;
				break;
			case BiomeType.Plains:
				amplitude = 0.3f;
				octaves = 3;
				frequency = 1.0f;
				break;
			case BiomeType.Forest:
				amplitude = 0.4f;
				octaves = 4;
				frequency = 1.2f;
				break;
			case BiomeType.Jungle:
				amplitude = 0.5f;
				octaves = 5;
				frequency = 1.3f;
				break;
			case BiomeType.Desert:
				amplitude = 0.3f;
				octaves = 2;
				frequency = 0.9f;
				break;
			case BiomeType.Mountains:
				amplitude = 1.0f;
				octaves = 6;
				frequency = 2.5f;
				break;
			case BiomeType.Snow:
				amplitude = 0.6f;
				octaves = 4;
				frequency = 1.5f;
				break;
			default:
				amplitude = 0.2f;
				octaves = 2;
				frequency = 1.0f;
				break;
		}
	}
}