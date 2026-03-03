using System.IO.Compression;

namespace Fable3SkipIntroPatcher.FileFormats;

public struct BnkIndexFileFormat
{
	private const int MAX_DECOMPRESSED_DATA_CHUNK_SIZE = 65536;

	public uint TotalDataSize;
	public uint Unknown_AlwaysEqualToFour;
	public bool IsBnkContentDataCompressed;
	public BnkIndexCompressedDataChunk[] CompressedIndexDataChunks;

	public BnkIndexFileFormat(Stream BnkIndexFileData)
	{
		if (!BnkIndexFileData.CanSeek)
		{
			throw new ArgumentException("The Stream holding the BnkIndex must be seekable.", nameof(BnkIndexFileData));
		}

		TotalDataSize = BnkIndexFileData.ReadBigEndianUInt32();
		if (TotalDataSize != BnkIndexFileData.Length)
		{
			throw new InvalidDataException(
				$"Error: {nameof(BnkIndexFileFormat)}: Bytes 0-3 indicate an incorrect file size. {BnkIndexFileData.Length} was expected, got {TotalDataSize} instead. " +
				$"This could indicate data corruption or a file that is not a Fable 3 BNK Index file."
			);
		}

		Unknown_AlwaysEqualToFour = BnkIndexFileData.ReadBigEndianUInt32();
		if (Unknown_AlwaysEqualToFour != 4)
		{
			Console.WriteLine($"Warning: {nameof(BnkIndexFileFormat)}: Bytes 4-7 are not the expected value: 4 expected, got {Unknown_AlwaysEqualToFour} instead.");
		}

		IsBnkContentDataCompressed = (BnkIndexFileData.ReadByte() != 0);

		List<BnkIndexCompressedDataChunk> compressedDataChunks = new();
		int i = 0;
		while (BnkIndexFileData.Position < BnkIndexFileData.Length)
		{
			compressedDataChunks.Add(new(BnkIndexFileData));
			i++;
		}
		CompressedIndexDataChunks = compressedDataChunks.ToArray();
	}

	public BnkIndexFileFormat(byte[] UncompressedData, bool IsContentDataCompressed)
	{
		using MemoryStream compressedDataStream = new();

		int currentArrayPos = 0;
		uint totalSize = (2 * sizeof(uint)) + sizeof(bool);
		List<BnkIndexCompressedDataChunk> compressedDataChunks = new();

		ZLibCompressionOptions compressionOptions = new()
		{
			CompressionLevel = 9,
			CompressionStrategy = ZLibCompressionStrategy.Default
		};
		using (ZLibStream zlibCompressorStream = new(compressedDataStream, compressionOptions, true))
		{
			for (int i = 0; currentArrayPos < UncompressedData.Length; i++)
			{
				int chunkSize = ((UncompressedData.Length - currentArrayPos) > MAX_DECOMPRESSED_DATA_CHUNK_SIZE) ?
					MAX_DECOMPRESSED_DATA_CHUNK_SIZE :
					UncompressedData.Length - currentArrayPos;
				ReadOnlySpan<byte> uncompressedDataChunk = UncompressedData.AsSpan(currentArrayPos, chunkSize);
				compressedDataChunks.Add(new(zlibCompressorStream, uncompressedDataChunk));

				currentArrayPos += chunkSize;
				totalSize += (uint)((2 * sizeof(int)) + compressedDataChunks[i].CompressedData.Length);
			}
		}

		TotalDataSize = totalSize;
		Unknown_AlwaysEqualToFour = 4;
		IsBnkContentDataCompressed = IsContentDataCompressed;
		CompressedIndexDataChunks = compressedDataChunks.ToArray();

		compressedDataStream.Position = 0;
		for (int i = 0; i < CompressedIndexDataChunks.Length; i++)
		{
			CompressedIndexDataChunks[i].FillCompressedDataArrayFromStream(compressedDataStream);
		}
	}

	public void WriteToStream(Stream BytesDestination)
	{
		BytesDestination.WriteBigEndianUInt32(TotalDataSize);
		BytesDestination.WriteBigEndianUInt32(Unknown_AlwaysEqualToFour);
		byte isBnkDataCompressedByteRepresentation = (IsBnkContentDataCompressed) ?
			(byte)1 :
			(byte)0;
		BytesDestination.WriteByte(isBnkDataCompressedByteRepresentation);

		for (int i = 0; i < CompressedIndexDataChunks.Length; i++)
		{
			CompressedIndexDataChunks[i].WriteToStream(BytesDestination);
		}
	}
}

public struct BnkIndexCompressedDataChunk
{
	public int CompressedDataSize;
	public int DecompressedDataSize;	// Seems to always be 65536 bytes (except for last chunk).
	public byte[] CompressedData;

	public BnkIndexCompressedDataChunk(Stream BnkIndexFileData)
	{
		CompressedDataSize = BnkIndexFileData.ReadBigEndianInt32();
		DecompressedDataSize = BnkIndexFileData.ReadBigEndianInt32();
		CompressedData =  new byte[CompressedDataSize];

		BnkIndexFileData.ReadExactly(CompressedData, 0, CompressedDataSize);
	}

	public BnkIndexCompressedDataChunk(ZLibStream ZLibCompressorStream, ReadOnlySpan<byte> UncompressedDataSegment)
	{
		DecompressedDataSize =  UncompressedDataSegment.Length;

		long startPos = ZLibCompressorStream.BaseStream.Position;
		ZLibCompressorStream.Write(UncompressedDataSegment);
		ZLibCompressorStream.Flush();
		long endPos = ZLibCompressorStream.BaseStream.Position;

		CompressedDataSize = (int)(endPos - startPos);
		CompressedData = new byte[CompressedDataSize];
	}

	public void FillCompressedDataArrayFromStream(Stream BytesSource)
	{
		BytesSource.ReadExactly(CompressedData, 0, CompressedDataSize);
	}

	public void WriteToStream(Stream BytesDestination)
	{
		BytesDestination.WriteBigEndianInt32(CompressedDataSize);
		BytesDestination.WriteBigEndianInt32(DecompressedDataSize);
		BytesDestination.Write(CompressedData);
	}
}
