using System;
using Unity.Collections;

namespace Game.Terrain
{
	internal static class Compressor
	{
		public const short MaxValue = 16384;
		public const short MinValue = -16384;

		public static void Pack(NativeSlice<ushort> src, NativeSlice<byte> dst, out int usedLength)
		{
			if (dst.Length < src.Length * 2)
				throw new ArgumentException("Destination buffer must be at least twice the size of source");

			usedLength = 0;
			ushort prevValue = 0;

			for (int i = 0; i < src.Length; i++) {
				int d = src[i] - prevValue;
				prevValue = src[i];
				if (d >= -0x3F && d <= 0x3F) {
					dst[usedLength++] = unchecked((byte)(d >= 0 ? d : (d & 0x7F) | 0x40));
				} else {
					// Need two bytes, set high bit of first byte to 1
					dst[usedLength++] = unchecked((byte)((d >> 8) | (d >= 0 ? 0x80 : 0xC0)));
					dst[usedLength++] = unchecked((byte)(d & 0xFF));
				}
			}
		}

		public static void Unpack(NativeSlice<byte> src, NativeSlice<ushort> dst)
		{
			if (dst.Length * 2 < src.Length)
				throw new ArgumentException("Destination buffer must be at least twice the size of source");

			int srcIndex = 0;
			int dstIndex = 0;
			ushort prevValue = 0;

			while (srcIndex < src.Length && dstIndex < dst.Length) {
				byte firstByte = src[srcIndex++];
				int difference = firstByte & 0x3F;
				if ((firstByte & 0x40) != 0) {
					difference |= 0xFFC0;
				}
				// Check if high bit is set (indicating two-byte encoding)
				if ((firstByte & 0x80) != 0) {
					difference = (difference << 8) | src[srcIndex++];
				}
				dst[dstIndex] = unchecked((ushort)(prevValue + difference));
				prevValue = dst[dstIndex];
				dstIndex++;
			}
		}
	}
}
