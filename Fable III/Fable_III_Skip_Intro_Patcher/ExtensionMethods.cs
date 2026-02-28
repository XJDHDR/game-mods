namespace Fable3SkipIntroPatcher;

public static class ExtensionMethods
{
	extension(Stream stream)
	{
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

		public void WriteBigEndianInt32(int Value)
		{
			Span<byte> intBytes = stackalloc byte[sizeof(int)];
			BitConverter.TryWriteBytes(intBytes, Value);

			writeBytesToStreamInBigEndianOrder(stream, ref intBytes);
		}

		public void WriteBigEndianUInt32(uint Value)
		{
			Span<byte> uintBytes = stackalloc byte[sizeof(uint)];
			BitConverter.TryWriteBytes(uintBytes, Value);

			writeBytesToStreamInBigEndianOrder(stream, ref uintBytes);
		}
	}

	private static void readBytesFromBinaryReaderInBigEndianOrder(Stream DataStream, ref Span<byte> ReadBytes)
	{
		int bytesRead = DataStream.Read(ReadBytes);

		if (bytesRead < ReadBytes.Length)
		{
			throw new EndOfStreamException();
		}

		if (BitConverter.IsLittleEndian)
		{
			ReadBytes.Reverse();
		}
	}

	private static void writeBytesToStreamInBigEndianOrder(Stream DataStream, ref Span<byte> Bytes)
	{
		if (BitConverter.IsLittleEndian)
		{
			Bytes.Reverse();
		}

		DataStream.Write(Bytes);
	}
}
