namespace Fable3SkipIntroPatcher.FileFormats;

public struct BnkIndexFileFormat
{
	private const int MAX_DECOMPRESSED_DATA_CHUNK_SIZE = 65536;

	public uint TotalDataSize;
	public uint Unknown_AlwaysEqualToFour;
	public bool IsBnkContentDataCompressed;
	public BnkIndexCompressedDataChunk[] CompressedIndexDataChunks;

	public BnkIndexFileFormat(Stream BnkIndexData)
	{
		if (!BnkIndexData.CanSeek)
		{
			throw new ArgumentException("The Stream holding the BnkIndex must be seekable.", nameof(BnkIndexData));
		}

		TotalDataSize = BnkIndexData.ReadBigEndianUInt32();
		if (TotalDataSize != BnkIndexData.Length)
		{
			throw new InvalidDataException(
				$"Error: BnkIndexFileFormat: Bytes 0-3 indicate an incorrect file size. {BnkIndexData.Length} was expected, got {TotalDataSize} instead. " +
				$"This could indicate data corruption or a file that is not a Fable 3 BNK Index file."
			);
		}

		Unknown_AlwaysEqualToFour = BnkIndexData.ReadBigEndianUInt32();
		if (Unknown_AlwaysEqualToFour != 4)
		{
			Console.WriteLine($"Warning: BnkIndexFileFormat: Bytes 4-7 are not the expected value: 4 expected, got {Unknown_AlwaysEqualToFour} instead.");
		}

		IsBnkContentDataCompressed = (BnkIndexData.ReadByte() != 0);

		List<BnkIndexCompressedDataChunk> compressedDataChunks = new();
		int i = 0;
		while (BnkIndexData.Position < BnkIndexData.Length)
		{
			compressedDataChunks.Add(new(BnkIndexData));
			i++;
		}
		CompressedIndexDataChunks = compressedDataChunks.ToArray();
	}

	public BnkIndexFileFormat(byte[] DecompressedData)
	{
		int currentArrayPos = 0;
		List<BnkIndexCompressedDataChunk> compressedDataChunks = new();

		while (currentArrayPos < DecompressedData.Length)
		{
			int chunkSize = ((DecompressedData.Length - currentArrayPos) <= MAX_DECOMPRESSED_DATA_CHUNK_SIZE) ?
				DecompressedData.Length - currentArrayPos :
				MAX_DECOMPRESSED_DATA_CHUNK_SIZE;
			ReadOnlySpan<byte> decompressedDataChunk = DecompressedData.AsSpan(currentArrayPos, chunkSize);
			compressedDataChunks.Add(new(decompressedDataChunk));

			currentArrayPos += chunkSize;
		}

	}

	public void WriteToStream(Stream BytesDestination)
	{
	}
}

public struct BnkIndexCompressedDataChunk
{
	public int CompressedDataSize;
	public int DecompressedDataSize;	// Seems to always be 65536 bytes (except for last chunk).
	public byte[] CompressedData;

	public BnkIndexCompressedDataChunk(Stream BnkIndexData)
	{
		CompressedDataSize = BnkIndexData.ReadBigEndianInt32();
		DecompressedDataSize = BnkIndexData.ReadBigEndianInt32();
		CompressedData =  new byte[CompressedDataSize];

		int bytesReadFromFile = BnkIndexData.Read(CompressedData, 0, CompressedDataSize);
		if (bytesReadFromFile != CompressedDataSize)
		{
			throw new InvalidDataException(
				$"Error: BnkIndexCompressedDataChunk: Read error for compressed data ending at 0x{BnkIndexData.Position:x}. Tried to read {CompressedDataSize} bytes, got {bytesReadFromFile} instead."
			);
		}
	}

	public BnkIndexCompressedDataChunk(ReadOnlySpan<byte> DecompressedDataSegment)
	{
		DecompressedDataSize =  DecompressedDataSegment.Length;
	}
}
