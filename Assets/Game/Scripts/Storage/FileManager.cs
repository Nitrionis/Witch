using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Storage
{
	public class FileManager
	{
		private const string worldInfoFile = "WorldInfo.json";
		private const string RegionFileSuffix = "Region";
		private const string PatchFileSuffix = "Patch";
		private const string fileExtension = ".bin";

		private readonly string worldDirectory;
		private readonly string regionsDirectory;

		public readonly WorldInfo World;

		private FileManager(WorldInfo world, string worldDirectoryName)
		{
			// Get platform-specific persistent data path
			worldDirectory = Path.Combine(Application.persistentDataPath, worldDirectoryName);
			regionsDirectory = Path.Combine(worldDirectory, "Regions");

			if (!Directory.Exists(regionsDirectory)) {
				Directory.CreateDirectory(regionsDirectory);
				Debug.Log($"Created Region directory: {regionsDirectory}");
			}

			World = world;
		}

		public static FileManager CreateWorld(WorldInfo world)
		{
			string worldDirectoryName = Guid.NewGuid().ToString();
			string worldDirectory;
			do {
				worldDirectory = Path.Combine(Application.persistentDataPath, worldDirectoryName);
			} while (Directory.Exists(worldDirectory));
			Directory.CreateDirectory(worldDirectory);
			string jsonOutput = JsonUtility.ToJson(world, prettyPrint: true);
			string worldInfoFilePath = Path.Combine(worldDirectory, worldInfoFile);
			File.WriteAllText(worldInfoFilePath, jsonOutput);
			return new FileManager(world, worldDirectoryName);
		}

		public static FileManager LoadWorld(string worldDirectoryName)
		{
			string worldDirectory = Path.Combine(Application.persistentDataPath, worldDirectoryName);
			if (!Directory.Exists(worldDirectory)) {
				throw new ArgumentException($"World no exists: {worldDirectory}");
			}
			string worldInfoFilePath = Path.Combine(worldDirectory, worldInfoFile);
			if (!File.Exists(worldInfoFilePath)) {
				throw new Exception($"Broken World no WorldInfo.json: {worldDirectory}");
			}
			string worldInfoContent = File.ReadAllText(worldInfoFilePath);
			var worldInfo = JsonUtility.FromJson<WorldInfo>(worldInfoContent);
			return new FileManager(worldInfo, worldDirectoryName);
		}

		/// <summary>
		/// Gets main file path for a specific Region.
		/// </summary>
		public string GetRegionFilePath(int2 location)
		{
			string fileName = $"_x{location.x}_y{location.y}_{RegionFileSuffix}{fileExtension}";
			return Path.Combine(regionsDirectory, fileName);
		}

		/// <summary>
		/// Gets patch file path for a specific Region.
		/// </summary>
		public string GetPatchFilePath(int2 location, byte fileIndex)
		{
			string fileName = $"_x{location.x}_y{location.y}_L{fileIndex}_{PatchFileSuffix}{fileExtension}";
			return Path.Combine(regionsDirectory, fileName);
		}

		public readonly struct RegionFiles
		{
			public readonly string PackedRegionFile;
			public readonly string PatchFile;
		}

		public List<string> GetRegionFiles(int2 location)
		{
			List<string> files = null;
			var path = GetRegionFilePath(location);
			if (File.Exists(path)) {
				files = new List<string> { path };
			}
			byte patchLevel = 0;
			path = GetPatchFilePath(location, patchLevel);
			while (File.Exists(path)) {
				files ??= new List<string>();
				files.Add(path);
				patchLevel++;
				path = GetPatchFilePath(location, patchLevel);
			}
			return files;
		}

		public void RemoveAllFilesForRegion(int2 location)
		{
			var files = GetRegionFiles(location);
			if (files == null) {
				return;
			}
			foreach (var file in files) {
				File.Delete(file);
			}
		}

		/// <summary>
		/// Deletes the world associated with the manager.
		/// </summary>
		public void DeleteAllRegions()
		{
			try {
				if (Directory.Exists(regionsDirectory)) {
					Directory.Delete(regionsDirectory, true);
					Directory.CreateDirectory(regionsDirectory);
					Debug.Log("Deleted all Region files");
				}
			} catch (Exception e) {
				Debug.LogError($"Failed to delete all Regions: {e.Message}");
			}
		}

		[Serializable]
		public class WorldInfo
		{
			[SerializeField]
			private string name;

			public string Name => name;

			public WorldInfo(string name)
			{
				if (string.IsNullOrEmpty(name)) {
					name = Guid.NewGuid().ToString();
				}
				this.name = name;
			}
		}
	}
}


