using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Assets.Game.Tools
{
	internal class Alignment<T> where T : unmanaged
	{
		private static int value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Get()
		{
			if (value == 0) {
				value = CalculateAlignment(typeof(T));
			}
			return value;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static int CalculateAlignment(Type type)
		{
			var structLayout = type.StructLayoutAttribute;
			if (structLayout != null && structLayout.Value == LayoutKind.Explicit) {
				return structLayout.Pack;
			}
			int maxAlignment = 1;
			foreach (var field in type.GetFields()) {
				int fieldAlignment = field.FieldType.IsPrimitive
					? Marshal.SizeOf(field.FieldType)
					: CalculateAlignment(field.FieldType);
				maxAlignment = Math.Max(maxAlignment, fieldAlignment);
			}
			return maxAlignment;
		}
	}
}
