using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Assets.Game.Terrain;
using Unity.Collections;
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

	private float GenerateHeight(Vector2Int pos, Vector2 origin, int octaves, float scale)
	{
		float amplitude = 1;
		float frequency = 1;
		float noiseHeight = 0;

		for (int i = 0; i < octaves; i++) {
			float sampleX = origin.x + pos.x / scale * frequency;
			float sampleY = origin.y + pos.y / scale * frequency;

			float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // Range [-1, 1]
			noiseHeight += perlinValue * amplitude;

			amplitude *= persistence;
			frequency *= lacunarity;
		}

		float h = Mathf.InverseLerp(-1, 1, noiseHeight); // Normalize to [0,1]
		h = Mathf.Clamp(h, threshold, 1f);
		h = (h - threshold) * (1f / (1f - threshold));

		return h;
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
		
		UnityEngine.Debug.Log("Reload Noise");
		
		cachedOrigin = origin;
		cachedScale = scale;
		cachedLacunarity = lacunarity;
		cachedPersistence = persistence;

		UpdateTexture();
	}

	private void UpdateTexture()
	{
		var data = new ushort[width * height];

		Parallel.For(0, data.Length, (i) => {
			int y = i / width;
			int x = i % width;
			Vector2Int pos = new(x, y);
			var b_h = GenerateHeight(pos, origin, octaves, scale);
			var v_1 = GenerateHeight(pos, origin + 100f * Vector2.one, octaves: 2, scale * 3);
			var v_2 = GenerateHeight(pos, origin + 200f * Vector2.one, octaves: 2, scale * 3);
			var v_3 = GenerateHeight(pos, origin + 300f * Vector2.one, octaves: 2, scale * 3);
			var v_4 = GenerateHeight(pos, origin + 100f * Vector2.up, octaves: 2, scale * 3);
			var v_5 = GenerateHeight(pos, origin + 200f * Vector2.down, octaves: 2, scale * 3);
			var v_6 = GenerateHeight(pos, origin + 300f * Vector2.left, octaves: 2, scale * 3);
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
			if (b_h < 0.0001f) {
				biome = Biome.None;
			}
			if (b_h > 0.65f) {
				biome = Biome.Mountains;
			}
			pix[i] = b_h < 0.01f ? Color.blue : b_h * BiomeToColor(biome);
			const int MaxHeight = 4096;
			//data[i] = (ushort)(Mathf.Clamp((int)(b_h * MaxHeight), 0, MaxHeight) + ((int)biome << 12));
			data[i] = (ushort)Mathf.Clamp((int)(b_h * MaxHeight), 0, MaxHeight);
		});

		// Copy the pixel data to the texture and load it into the GPU.
		noiseTex.SetPixels(pix);
		noiseTex.Apply();

		/*using var fs = File.Create(@"C:\Users\nitro\Witch\Assets\Game\Terrain\map.bin");
		using var bw = new BinaryWriter(fs);
		foreach (var v in data) {
			bw.Write(v);
		}
		bw.Flush();*/

		var src = new NativeArray<ushort>(data.Length, Allocator.TempJob);
		var unpacked = new NativeArray<ushort>(data.Length, Allocator.TempJob);
		var packed = new NativeArray<byte>(data.Length * 2, Allocator.TempJob);
		for (int i = 0; i < src.Length; i++) {
			src[i] = data[i];
		}
		try {
			var sw = Stopwatch.StartNew();
			Compressor.Pack(src, packed, out var usedLength);
			UnityEngine.Debug.Log($"Compressor.Pack: src_len:{src.Length} packed_len:{usedLength} elapsed: {sw.ElapsedMilliseconds} ms");
			sw = Stopwatch.StartNew();
			Compressor.Unpack(packed.Slice(0, usedLength), unpacked);
			UnityEngine.Debug.Log($"Compressor.Unpack: packed_len:{usedLength} unpacked_len:{unpacked.Length} elapsed: {sw.ElapsedMilliseconds} ms");

			int errorCount = 0;
			for (int i = 0; i < src.Length; i++) {
				if (src[i] != unpacked[i]) {
					errorCount++;
					if (errorCount < 10) {
						UnityEngine.Debug.Log($"error: i:{i} src[i]:{src[i]} unpacked[i]:{unpacked[i]}");
					}
				}
			}
			UnityEngine.Debug.Log($"errorCount: {errorCount}");
		} finally {
			src.Dispose();
			unpacked.Dispose();
			packed.Dispose();
		}
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