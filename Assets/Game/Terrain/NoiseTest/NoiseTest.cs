using System.IO;
using UnityEngine;

public class NoiseTest : MonoBehaviour
{
	[Range(0.0f, 1.0f)]
	public float threshold = 0;
	public int octaves = 4;
	public float persistence = 0.5f; // How much each octave contributes
	public float lacunarity = 2f; // Frequency increase per octave
	public Vector2 origin;
	public float scale;

	private Vector2 cachedOrigin;
	private float cachedScale;
	
	private Texture2D noiseTex;
	private Color[] pix;
	private Renderer rend;
	private float cachedPersistence;
	private float cachedLacunarity;

	private const int width = 4096;
	private const int height = 4096;
	
	private void Start()
	{
		rend = GetComponent<Renderer>();

		// Set up the texture and a Color array to hold pixels during processing.
		noiseTex = new Texture2D(width, height);
		pix = new Color[noiseTex.width * noiseTex.height];
		rend.material.mainTexture = noiseTex;
	}

	private float[] GenerateHeights(Vector2 origin, int octaves, float scale)
	{
		var heights = new float[width * height];

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				float amplitude = 1;
				float frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < octaves; i++) {
					float sampleX = origin.x + x / scale * frequency;
					float sampleY = origin.y + y / scale * frequency;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // Range [-1, 1]
					noiseHeight += perlinValue * amplitude;

					amplitude *= persistence;
					frequency *= lacunarity;
				}

				float h = Mathf.InverseLerp(-1, 1, noiseHeight); // Normalize to [0,1]
				h = Mathf.Clamp(h, threshold, 1f);
				h = (h - threshold) * (1f / (1f - threshold));
				heights[y * width + x] = h;
			}
		}

		return heights;
	}

	private void Update()
	{
		if (
			origin == cachedOrigin &&
		    scale == cachedScale &&
		    lacunarity == cachedLacunarity &&
		    persistence == cachedPersistence
		) {
			return;
		}
		
		Debug.Log("Reload Noise");
		
		cachedOrigin = origin;
		cachedScale = scale;
		cachedLacunarity = lacunarity;
		cachedPersistence = persistence;

		UpdateTexture();
	}

	private void UpdateTexture()
	{
		var heights = GenerateHeights(origin, octaves, scale);

		var heights_1 = GenerateHeights(origin + 100f * Vector2.one, octaves: 2, scale * 3f);
		var heights_2 = GenerateHeights(origin + 200f * Vector2.one, octaves: 2, scale * 3f);
		var heights_3 = GenerateHeights(origin + 300f * Vector2.one, octaves: 2, scale * 3f);
		var heights_4 = GenerateHeights(origin + 100f * Vector2.up, octaves: 2, scale * 3f);
		var heights_5 = GenerateHeights(origin + 200f * Vector2.down, octaves: 2, scale * 3f);
		var heights_6 = GenerateHeights(origin + 300f * Vector2.left, octaves: 2, scale * 3f);

		var data = new ushort[heights.Length];

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				int i = y * width + x;
				var v = heights[i];
				var v_1 = heights_1[i];
				var v_2 = heights_2[i];
				var v_3 = heights_3[i];
				var v_4 = heights_4[i];
				var v_5 = heights_5[i];
				var v_6 = heights_6[i];
				float v_t = v_1;
				var biome = Biome.Meadow;
				if (v_t < v_2) {
					v_t = v_2;
					biome = Biome.DeciduousForest;
				}
				if (v_t < v_3) {
					v_t = v_3;
					biome = Biome.ConiferousForest;
				}
				if (v_t < v_4) {
					v_t = v_4;
					biome = Biome.Flame;
				}
				if (v_t < v_5) {
					v_t = v_5;
					biome = Biome.Swamp;
				}
				if (v_t < v_6) {
					v_t = v_6;
					biome = Biome.Desert;
				}
				if (v < 0.0001f) {
					biome = Biome.None;
				}
				if (v > 0.65f) {
					biome = Biome.Mountains;
				}
				pix[i] = v < 0.01f ? Color.blue : v * BiomeToColor(biome);
				const int MaxHeight = 4096;
				data[i] = (ushort)(Mathf.Clamp((int)(v * MaxHeight), 0, MaxHeight) + ((int)biome << 12));
			}
		}

		// Copy the pixel data to the texture and load it into the GPU.
		noiseTex.SetPixels(pix);
		noiseTex.Apply();

		using var fs = File.Create(@"C:\Users\nitro\Witch\Assets\Game\Terrain\map.bin");
		using var bw = new BinaryWriter(fs);
		foreach (var v in data) {
			bw.Write(v);
		}
		bw.Flush();
	}

	private static Color BiomeToColor(Biome biome)
	{
		return biome switch { 
			Biome.None => Color.blue,
			Biome.Mountains => Color.white,
			Biome.Flame => Color.red,
			Biome.DeciduousForest => Color.green,
			Biome.ConiferousForest => Color.cyan,
			Biome.Meadow => Color.yellow,
			Biome.Swamp => Color.magenta,
			Biome.Desert => new Color(1.0f, 0.54f, 0.0f),
		};
	}

	private enum Biome : int
	{
		None = 0,
		Mountains = 1,
		Flame = 2,
		DeciduousForest = 3,
		ConiferousForest = 4,
		Meadow = 5,
		Swamp = 6,
		Desert = 7,
	}
}