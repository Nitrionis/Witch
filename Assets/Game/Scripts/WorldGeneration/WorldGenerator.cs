using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Game.WorldGeneration
{
	public class WorldGenerator : MonoBehaviour
	{
		private IGeneratorPhase[] generatorPhases;
		public ImagePreview imagePreview;

		// Start is called once before the first execution of Update after the MonoBehaviour is created
		void Start()
		{
			var random = new System.Random();
			generatorPhases = new IGeneratorPhase[] {
				new AddIsland(random, probability: 0.2f, skipWorldBoundary: true), // 16x16
				new ZoomPhase(),  // 32x32
				new AddTemperature(random),
				new SmoothTemperature(skipCorners: false),
				new TemperatureToBiome(random),
				//new BiomeDiffusion(random, 0.03f),
				new AddIsland(random, probability: 0.3f, skipWorldBoundary: true),
				new ZoomPhase(),  // 64x64
				new ChangeCoastline(
					random,
					expandOceanProbability: 0.2f,
					expandLandProbability: 0.5f,
					skipWorldBoundary: false,
					landCountToExpandOcean: 4,
					landCountToExpandLand: 2
				),
				new BiomeDiffusion(random, 0.04f),
				new ZoomPhase(),  // 128x128
				new ChangeCoastline(
					random,
					expandOceanProbability: 0.2f,
					expandLandProbability: 0.5f,
					skipWorldBoundary: false,
					landCountToExpandOcean: 4,
					landCountToExpandLand: 2
				),
				new BiomeDiffusion(random, 0.05f),
				new AddIsland(random, probability: 0.1f, skipWorldBoundary: true),
				new ZoomPhase(),  // 256x256
				new ChangeCoastline(
					random,
					expandOceanProbability: 0.2f,
					expandLandProbability: 0.3f,
					skipWorldBoundary: false,
					landCountToExpandOcean: 4,
					landCountToExpandLand: 2
				),
				new BiomeDiffusion(random, 0.05f),
				new ZoomPhase(),  // 512x512
				new ChangeCoastline(
					random,
					expandOceanProbability: 0.2f,
					expandLandProbability: 0.2f,
					skipWorldBoundary: false,
					landCountToExpandOcean: 4,
					landCountToExpandLand: 2
				),
				new BiomeDiffusion(random, 0.05f),
				new ZoomPhase(),   // 1024x1024
				new ChangeCoastline(
					random,
					expandOceanProbability: 0.2f,
					expandLandProbability: 0.2f,
					skipWorldBoundary: false,
					landCountToExpandOcean: 4,
					landCountToExpandLand: 2
				),
				new BiomeDiffusion(random, 0.05f),
				new ZoomPhase(),   // 2048x2048
				new ChangeCoastline(
					random,
					expandOceanProbability: 0.2f,
					expandLandProbability: 0.2f,
					skipWorldBoundary: false,
					landCountToExpandOcean: 4,
					landCountToExpandLand: 2
				),
				new BiomeDiffusion(random, 0.05f),
				new ZoomPhase(),   // 4096x4096
				new ChangeCoastline(
					random,
					expandOceanProbability: 0.2f,
					expandLandProbability: 0.2f,
					skipWorldBoundary: false,
					landCountToExpandOcean: 4,
					landCountToExpandLand: 2
				),
			};
			if (imagePreview == null) {
				imagePreview = GetComponent<ImagePreview>();
			}
			StartCoroutine(GenerateWorld());
		}

		// Update is called once per frame
		void Update()
		{

		}

		IEnumerator GenerateWorld()
		{
			yield return Enumerable.Range(start: 0, count: 240);
			var layer = new Layer(sideLength: 16);
			foreach (var phase in generatorPhases) {
				Debug.Log($"phase {phase.GetType().Name} {Array.IndexOf(generatorPhases, phase)}/{generatorPhases.Length}");
				layer = phase.Execute(layer);
				var colors = new Color[layer.SideLength * layer.SideLength];
				for (int y = 0; y < layer.SideLength; y++) {
					for (int x = 0; x < layer.SideLength; x++) {
						var cell = layer[x, y];
						var color = TemperatureToColor(cell.Biome, cell.IsLand);
						colors[x + y * layer.SideLength] = color;
					}
				}
				imagePreview.UpdateTextureWithColors(colors, layer.SideLength);
				//yield return new WaitForSeconds(2f);
				yield return null;
			}
		}

		private Color TemperatureToColor(byte temperature, bool isLand)
		{
			/*if (temperature < 25) {
				return Color.white;
			}
			if (temperature < 50) {
				return Color.darkGreen;
			}
			if (temperature < 75) {
				return Color.green;
			}
			return Color.orange;*/
			var biome = (Biome)temperature;
			var color = Color.black;
			switch (biome) {
				case Biome.Null: color = Color.cyan; break;
				case Biome.Snow: color = Color.white; break;
				case Biome.Desert: color = Color.orange; break;
				case Biome.Jungle: color = Color.greenYellow; break;
				case Biome.Forest: color = Color.darkGreen; break;
				case Biome.PineForest: color = Color.forestGreen; break;
				default: color = Color.cyan; break;
			}
			if (!isLand) {
				//color = Color.Lerp(color, Color.blue, 0.5f);
				color = Color.blue;
			}
			return color;
		}
	}
}
