namespace Fable3SkipIntroPatcher.FileFormats;

public struct BnkIndexFileFormat
{
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
		List<byte> compressedData = new();
		int i = 0;
		while (BnkIndexData.Position < BnkIndexData.Length)
		{
			compressedDataChunks.Add(BnkIndexCompressedDataChunk.CreateFromFileStream(BnkIndexData));
			compressedData.AddRange(compressedDataChunks[i].CompressedData);
			i++;
		}
		CompressedIndexDataChunks = compressedDataChunks.ToArray();
	}
}

public struct BnkIndexCompressedDataChunk
{
	public int CompressedDataSize;
	public int DecompressedDataSize;	// Seems to always be 65536 bytes (except for last chunk).
	public byte[] CompressedData;

	public static BnkIndexCompressedDataChunk CreateFromFileStream(Stream BnkIndexData)
	{
		BnkIndexCompressedDataChunk compressedDataChunk = new();

		compressedDataChunk.CompressedDataSize = BnkIndexData.ReadBigEndianInt32();
		compressedDataChunk.DecompressedDataSize = BnkIndexData.ReadBigEndianInt32();
		compressedDataChunk.CompressedData =  new byte[compressedDataChunk.CompressedDataSize];

		int bytesReadFromFile = BnkIndexData.Read(compressedDataChunk.CompressedData, 0, compressedDataChunk.CompressedDataSize);
		if (bytesReadFromFile != compressedDataChunk.CompressedDataSize)
		{
			throw new InvalidDataException(
				$"Error: BnkIndexCompressedDataChunk: Read error for compressed data ending at 0x{BnkIndexData.Position:x}. Tried to read {compressedDataChunk.CompressedDataSize} bytes, got {bytesReadFromFile} instead."
			);
		}

		return compressedDataChunk;
	}
}
