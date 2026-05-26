
namespace Game
{
	public enum ThreadNames
	{
		/// <summary>
		/// 
		/// </summary>
		MainThread,

		/// <summary>
		/// Performs processing of command streams.
		/// Responsible for creating or desroing server side entities.
		/// </summary>
		/// <remarks>
		/// Read: [all systems]
		/// Write: [all systems]
		/// </remarks>
		MainJob,

		/// <summary>
		/// Artificial Intelligence logic processing.
		/// [2D route calculation] [search for points of interest] [defining the target player]
		/// </summary>
		/// <remarks>
		/// Read: [pre-allocated buffers]
		/// Write: [pre-allocated buffers]
		/// </remarks>
		AiSystemUpdateJob,

		/// <summary>
		/// Synchronization point for starting and ending Jobs.
		/// </summary>
		/// <remarks>
		/// Read: [all systems]
		/// Write: [all systems]
		/// Execution: [after MainJob]
		/// </remarks>
		SyncPhase,

		/// <summary>
		/// Responsible for data exchange over the network.
		/// </summary>
		/// <remarks>
		/// Read: [Network]
		/// Write: [EventsStream.Writer] [pre-allocated buffers]
		/// </remarks>
		NetworkJob,

		/// <summary>
		/// Building chunk meshes.
		/// </summary>
		/// <remarks>
		/// Read: [pre-allocated buffers]
		/// Write: [pre-allocated buffers]
		/// </remarks>
		MeshBuilderJob,

		/// <summary>
		/// Building segments of an artificial intelligence navigation map.
		/// </summary>
		/// <remarks>
		/// Read: [pre-allocated buffers]
		/// Write: [pre-allocated buffers]
		/// </remarks>
		NavmeshBuilderJob,

		/// <summary>
		/// Reading data from disk.
		/// </summary>
		/// <remarks>
		/// Read: [pre-allocated buffers]
		/// Write: [pre-allocated buffers]
		/// </remarks>
		FileIoTask,
	}
}
