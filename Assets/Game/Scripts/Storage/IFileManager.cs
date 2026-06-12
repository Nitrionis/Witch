namespace Game.Storage
{
	public interface IFileManager
	{
		FileManager.WorldInfo World { get; }

		string GetRegionChangesFilePath(RegionChangesLocation regionChangesLocation);

		string GetRegionBaseFilePath(RegionBaseLocation location);

		void DeleteWorld();
	}
}
