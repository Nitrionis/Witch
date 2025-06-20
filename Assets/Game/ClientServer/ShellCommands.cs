using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Assets.Game.ClientServer
{
	internal static class ShellCommands
	{
		public static readonly Guid Dummy = new Guid("{F8D7D7E2-C2AB-4FC2-B66F-F600A42B46BB}");
		public static readonly Guid CreateEntity = new Guid("{1A57A7E7-F9BB-4D4D-B618-84B44C8E1E3F}");
		public static readonly Guid DestroyEntity = new Guid("{5ECE897C-1AB5-4C75-8C06-01A117226AA1}");
		public static readonly Guid SetEntityHost = new Guid("{A518702A-ECFB-43E5-ADED-3DDC36AB6C75}");
		public static readonly Guid Interact = new Guid("{74117FEA-63C7-4841-BEE3-9C6AE6168D22}");
		public static readonly Guid SetPredictionPosition = new Guid("{FCD42AEB-7153-434D-AE87-9135DF6524E4}");
		public static readonly Guid AddElementalAuras = new Guid("{2C145CBF-2C37-486A-9285-F0864A745681}");
		public static readonly Guid SetElementalAuras = new Guid("{AC0D2B61-825C-4AE6-B4C9-C7BE16832BE6}");
		public static readonly Guid GetElementalAuras = new Guid("{FC8C79D4-4237-4F95-A304-9615015C2859}");
		public static readonly Guid GetInventory = new Guid("{01722F51-A645-460D-B7DB-3314F2D12EC5}");
		public static readonly Guid RemoveInventoryItem = new Guid("{A1708B16-A2E2-45BF-8E45-68B8B91E2BFF}");
		public static readonly Guid CreateInventoryItem = new Guid("{EF08C172-E8FD-4069-A2C6-45B4F82C16EF}");

		private static Dictionary<Guid, ushort> guidToId;

		public static ushort GetId(Guid guid)
		{
			if (guidToId is null) {
				Init();
			}
			return guidToId[guid];
		}

		public static byte[] ComputeSHA256()
		{
			// Ensure the list is ordered consistently for the same hash output
			var orderedGuids = guidToId.Keys.OrderBy(g => g).ToList();

			using (SHA256 sha256 = SHA256.Create()) {
				// Combine all GUID bytes into a single byte array
				byte[] allBytes = orderedGuids
					.SelectMany(g => g.ToByteArray())
					.ToArray();

				return sha256.ComputeHash(allBytes);
			}
		}

		private static void Init()
		{
			var fields = typeof(ShellCommands).GetFields(
					System.Reflection.BindingFlags.Public |
					System.Reflection.BindingFlags.Static
				);
			var list = new List<Guid>();
			foreach (var field in fields) {
				if (field.FieldType != typeof(Guid)) {
					continue;
				}
				list.Add((Guid)field.GetValue(null));
			}
			list.Sort();
			guidToId = new();
			for (var i = 0; i < list.Count; i++) {
				guidToId.Add(list[i], (ushort)i);
			}
		}
	}
}
