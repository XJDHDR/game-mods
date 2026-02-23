namespace Fable3SkipIntroPatcher;

public static class ExtensionMethods
{
	extension(Stream stream)
	{
		public int ReadBigEndianInt16()
		{
			Span<byte> uintBytes = stackalloc byte[sizeof(short)];
			readBytesFromBinaryReaderInBigEndianOrder(stream, ref uintBytes);
			
			return BitConverter.ToInt16(uintBytes);
		}

		public uint ReadBigEndianUInt16()
		{
			Span<byte> uintBytes = stackalloc byte[sizeof(ushort)];
			readBytesFromBinaryReaderInBigEndianOrder(stream, ref uintBytes);
			
			return BitConverter.ToUInt16(uintBytes);
		}
		
		public int ReadBigEndianInt32()
		{
			Span<byte> uintBytes = stackalloc byte[sizeof(int)];
			readBytesFromBinaryReaderInBigEndianOrder(stream, ref uintBytes);
			
			return BitConverter.ToInt32(uintBytes);
		}

		public uint ReadBigEndianUInt32()
		{
			Span<byte> uintBytes = stackalloc byte[sizeof(uint)];
			readBytesFromBinaryReaderInBigEndianOrder(stream, ref uintBytes);
			
			return BitConverter.ToUInt32(uintBytes);
		}
	}

	private static void readBytesFromBinaryReaderInBigEndianOrder(Stream DataStream, ref Span<byte> ReadBytes)
	{
		int bytesRead = DataStream.Read(ReadBytes);
				
		if (bytesRead < ReadBytes.Length)
			throw new EndOfStreamException();

		if (BitConverter.IsLittleEndian)
			ReadBytes.Reverse();
	}
}