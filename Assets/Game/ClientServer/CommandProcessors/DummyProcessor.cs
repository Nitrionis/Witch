using System;
using System.Runtime.InteropServices;
using static Assets.Game.ClientServer.GameShell;

namespace Assets.Game.ClientServer.CommandProcessors
{
	public interface IClientDummyProcessor : ICommandProcessor { }
	public interface IServerDummyProcessor : ICommandProcessor { }

	[StructLayout(LayoutKind.Sequential, Size = 8)]
	public struct DummyDataUnit
	{
		public const long TestValue = 1317322991759528520;
		public long Value;

		public static DummyDataUnit Create() => new DummyDataUnit { Value = TestValue };
	}

	public class DummyProcessor : IClientDummyProcessor, IServerDummyProcessor
	{
		public Guid Guid => ShellCommands.Dummy;
		public ushort CommandId => ShellCommands.GetId(Guid);
		public unsafe int DataUnitSize => sizeof(DummyDataUnit);
		public Type DataUnitType => typeof(DummyDataUnit);

		public int ProcessedUnitCount { get; private set; }

		public unsafe void Process(void* dataUnits, int dataUnitCount)
		{
			var ptr = (DummyDataUnit*)dataUnits;
			for (int i = 0; i < dataUnitCount; i++) {
				var unit = ptr[i];
				if (unit.Value != DummyDataUnit.TestValue) {
					throw new Exception($"Invalid value: {unit.Value} binary: {Convert.ToString(unit.Value, 2)}");
				}
				ProcessedUnitCount++;
			}
		}
	}
}
