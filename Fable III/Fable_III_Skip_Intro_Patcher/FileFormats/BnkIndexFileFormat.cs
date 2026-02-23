namespace Fable3SkipIntroPatcher.FileFormats;

public struct BnkIndexFileFormat
{
	public uint TotalDataSize;
	public uint Unknown_AlwaysEqualToFour;
	public bool IsBnkContentDataCompressed;
	public BnkIndexCompressedDataChunk[] CompressedIndexDataChunks;

	public int TotalSizeOfDecompressedData;
	public byte[] CompressedIndexDataCollated;

	public static BnkIndexFileFormat CreateFromFileStream(FileStream fileStream)
	{
		BnkIndexFileFormat indexFile = new BnkIndexFileFormat();
		indexFile.TotalDataSize = fileStream.ReadBigEndianUInt32();
		if (indexFile.TotalDataSize != fileStream.Length)
		{
			throw new InvalidDataException(
				$"Error: BnkIndexFileFormat: Bytes 0-3 indicate an incorrect file size. {fileStream.Length} was expected, got {indexFile.TotalDataSize} instead. " +
				$"This could indicate data corruption or a file that is not a Fable 3 BNK Index file."
			);
		}

		indexFile.Unknown_AlwaysEqualToFour = fileStream.ReadBigEndianUInt32();
		if (indexFile.Unknown_AlwaysEqualToFour != 4)
		{
			Console.WriteLine($"Warning: BnkIndexFileFormat: Bytes 4-7 are not the expected value: 4 expected, got {indexFile.Unknown_AlwaysEqualToFour} instead.");
		}

		indexFile.IsBnkContentDataCompressed = (fileStream.ReadByte() != 0);

		indexFile.TotalSizeOfDecompressedData = 0;
		List<BnkIndexCompressedDataChunk> compressedDataChunks = new();
		List<byte> compressedData = new();
		int i = 0;
		while (fileStream.Position < fileStream.Length)
		{
			compressedDataChunks.Add(BnkIndexCompressedDataChunk.CreateFromFileStream(fileStream));
			compressedData.AddRange(compressedDataChunks[i].CompressedData);
			indexFile.TotalSizeOfDecompressedData += compressedDataChunks[i].DecompressedDataSize;
			i++;
		}
		indexFile.CompressedIndexDataChunks = compressedDataChunks.ToArray();
		indexFile.CompressedIndexDataCollated = compressedData.ToArray();
		
		return indexFile;
	}
}

public struct BnkIndexCompressedDataChunk
{
	public int CompressedDataSize;
	public int DecompressedDataSize;
	public byte[] CompressedData;

	public static BnkIndexCompressedDataChunk CreateFromFileStream(FileStream fileStream)
	{
		BnkIndexCompressedDataChunk compressedDataChunk = new();
		
		compressedDataChunk.CompressedDataSize = fileStream.ReadBigEndianInt32();
		compressedDataChunk.DecompressedDataSize = fileStream.ReadBigEndianInt32();
		compressedDataChunk.CompressedData =  new byte[compressedDataChunk.CompressedDataSize];
		
		int bytesReadFromFile = fileStream.Read(compressedDataChunk.CompressedData, 0, compressedDataChunk.CompressedDataSize);
		if (bytesReadFromFile != compressedDataChunk.CompressedDataSize)
		{
			throw new InvalidDataException(
				$"Error: BnkIndexCompressedDataChunk: Read error for compressed data ending at 0x{fileStream.Position:x}. Tried to read {compressedDataChunk.CompressedDataSize} bytes, got {bytesReadFromFile} instead."
			);
		}
		
		return compressedDataChunk;
	}
}
