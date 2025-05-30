using System;

namespace Assets.Game.Tools
{
	internal class Alignment<T> where T : unmanaged
	{
		private static int value;

		public static int Get()
		{
			if (value != 0) {
				return value;
			}
			value = CalculateAlignment(typeof(T));
			return value;
		}

		public static int CalculateAlignment(Type type)
		{
			int alignment = 1;
			var fields = type.GetFields();

			foreach (var field in fields) {
				int fieldAlignment = 1;

				if (field.FieldType == typeof(byte) || field.FieldType == typeof(sbyte))
					fieldAlignment = 1;
				else if (field.FieldType == typeof(short) || field.FieldType == typeof(ushort))
					fieldAlignment = 2;
				else if (field.FieldType == typeof(int) || field.FieldType == typeof(uint) ||
						 field.FieldType == typeof(float))
					fieldAlignment = 4;
				else if (field.FieldType == typeof(long) || field.FieldType == typeof(ulong) ||
						 field.FieldType == typeof(double))
					fieldAlignment = 8;
				else if (field.FieldType == typeof(IntPtr) || field.FieldType == typeof(UIntPtr))
					fieldAlignment = IntPtr.Size;
				else if (field.FieldType.IsValueType) // For nested structs
					fieldAlignment = CalculateAlignment(field.FieldType);

				if (fieldAlignment > alignment)
					alignment = fieldAlignment;
			}

			return alignment;
		}
	}
}
