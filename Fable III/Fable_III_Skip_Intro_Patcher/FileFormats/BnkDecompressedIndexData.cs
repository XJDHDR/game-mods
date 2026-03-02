using System.Buffers;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Fable3SkipIntroPatcher.FileFormats;

public class BnkDecompressedIndexData
{
	// ReSharper disable once MemberCanBePrivate.Global
	public int Unknown_AlwaysEqualToZero;
	public int NumberOfFiles;
	public BnkContentFileEntry[] AllFileEntries;

	private bool isContentDataCompressed;

	public BnkDecompressedIndexData(ref BnkIndexFileFormat IndexFile)
	{
		isContentDataCompressed = IndexFile.IsBnkContentDataCompressed;

		byte[] decompressedData = decompressIndexFileData(ref IndexFile, out int totalDecompressedDataSize);

		using (MemoryStream decompressedDataStream = new(decompressedData, 0, totalDecompressedDataSize))
		{
			Unknown_AlwaysEqualToZero = decompressedDataStream.ReadBigEndianInt32();
			if (Unknown_AlwaysEqualToZero != 0)
			{
				Console.WriteLine($"Warning: {nameof(BnkDecompressedIndexData)}: Bytes 0-3 are not the expected value: 0 expected, got {Unknown_AlwaysEqualToZero} instead.");
			}

			NumberOfFiles = decompressedDataStream.ReadBigEndianInt32();
			AllFileEntries = new BnkContentFileEntry[NumberOfFiles];
			for (int i = 0; i < NumberOfFiles; i++)
			{
				AllFileEntries[i] = new(decompressedDataStream, isContentDataCompressed);
			}

			for (int i = 0; i < NumberOfFiles; i++)
			{
				AllFileEntries[i].ReadFilePathStringsFromStream(decompressedDataStream);
			}
		}
	}

	public BnkIndexFileFormat CompressAndWriteToIndexFile(bool IsContentDataCompressed)
	{
		byte[] decompresedData;
		using (MemoryStream decompresedDataStream = new())
		{
			decompresedDataStream.WriteBigEndianInt32(Unknown_AlwaysEqualToZero);
			decompresedDataStream.WriteBigEndianInt32(NumberOfFiles);

			for (int i = 0; i < AllFileEntries.Length; i++)
			{
				AllFileEntries[i].WriteIntegersToStream(decompresedDataStream, isContentDataCompressed);
			}

			for (int i = 0; i < AllFileEntries.Length; i++)
			{
				AllFileEntries[i].WriteFilePathStringsToStream(decompresedDataStream);
			}

			decompresedData = decompresedDataStream.ToArray();
		}

		BnkIndexFileFormat indexFile = new(decompresedData, IsContentDataCompressed);
		return indexFile;
	}

	private byte[] decompressIndexFileData(ref BnkIndexFileFormat IndexFile, out int TotalDecompressedDataSize)
	{
		int totalCompressedDataSize = 0;
		TotalDecompressedDataSize = 0;
		for (int i = 0; i < IndexFile.CompressedIndexDataChunks.Length; i++)
		{
			totalCompressedDataSize += IndexFile.CompressedIndexDataChunks[i].CompressedDataSize;
			TotalDecompressedDataSize += IndexFile.CompressedIndexDataChunks[i].DecompressedDataSize;
		}

		byte[] collatedCompressedDataBorrowedArray = ArrayPool<byte>.Shared.Rent(totalCompressedDataSize);
		int destIndexPosition = 0;
		for (int i = 0; i < IndexFile.CompressedIndexDataChunks.Length; i++)
		{
			Span<byte> compressedDataChunk = IndexFile.CompressedIndexDataChunks[i].CompressedData.AsSpan();
			Span<byte> currentSectionOfCollatedData = collatedCompressedDataBorrowedArray.AsSpan(destIndexPosition, compressedDataChunk.Length);

			compressedDataChunk.CopyTo(currentSectionOfCollatedData);
			destIndexPosition += compressedDataChunk.Length;
		}

		byte[] decompressedData = new byte[TotalDecompressedDataSize];
		Inflater inflater = new();
		inflater.SetInput(collatedCompressedDataBorrowedArray, 0, totalCompressedDataSize);
		int decompressedSize = inflater.Inflate(decompressedData, 0, TotalDecompressedDataSize);
		if (decompressedSize != TotalDecompressedDataSize)
		{
			throw new InvalidDataException(
				$"Error: {nameof(BnkDecompressedIndexData)}: Deflated data was not the correct size. Expected {TotalDecompressedDataSize} bytes, got {decompressedSize} instead."
			);
		}
		ArrayPool<byte>.Shared.Return(collatedCompressedDataBorrowedArray);
		return decompressedData;
	}
}

public class BnkContentFileEntry
{
	// ReSharper disable once MemberCanBePrivate.Global
	public uint FileNameHash;
	public uint FileOffset;
	public int ContentFileDataSize;

	public int TotalDecompressedDataSize;
	public int NumChunks;
	public int[] ChunkDecompressedSizes = Array.Empty<int>();

	public string FilePath = string.Empty;
	public byte EndOfFilePathMarker;

	public BnkContentFileEntry(Stream DecompressedData, bool IsContentCompressed)
	{
		FileNameHash = DecompressedData.ReadBigEndianUInt32();
		FileOffset = DecompressedData.ReadBigEndianUInt32();

		TotalDecompressedDataSize = (IsContentCompressed) ?
			DecompressedData.ReadBigEndianInt32() :
			-1;

		ContentFileDataSize = DecompressedData.ReadBigEndianInt32();

		if (IsContentCompressed)
		{
			NumChunks = DecompressedData.ReadBigEndianInt32();
			ChunkDecompressedSizes = new int[NumChunks];
			for (int i = 0; i < NumChunks; i++)
			{
				ChunkDecompressedSizes[i] = DecompressedData.ReadBigEndianInt32();
			}
		}
	}

	public void ReadFilePathStringsFromStream(Stream DecompressedDataStream)
	{
		int filePathStringLength = DecompressedDataStream.ReadBigEndianInt32() - 1;   // String is null terminated so we don't need to read the last byte.
		byte[] filePathStringBytesArray =  ArrayPool<byte>.Shared.Rent(filePathStringLength);
		Span<byte> filePathStringBytes = filePathStringBytesArray.AsSpan(0, filePathStringLength);
		int bytesRead = DecompressedDataStream.Read(filePathStringBytes);

		if (bytesRead != filePathStringLength)
		{
			throw new InvalidDataException(
				$"Error: {nameof(BnkContentFileEntry)}: Read error for compressed data ending at {DecompressedDataStream.Position}. " +
				$"Tried to read {filePathStringLength} bytes, got {bytesRead} instead."
			);
		}

		FilePath = Encoding.ASCII.GetString(filePathStringBytes);
		ArrayPool<byte>.Shared.Return(filePathStringBytesArray);

		DecompressedDataStream.Position += 28;     // Skip over 28 NUL bytes
		EndOfFilePathMarker = (byte)DecompressedDataStream.ReadByte();
		if (EndOfFilePathMarker == 0)
		{
			throw new InvalidDataException(
				$"Error: {nameof(BnkContentFileEntry)}: Byte at {DecompressedDataStream.Position} was supposed to indicate the end of a file path string with a non-zero byte. " +
				$"This could indicate data corruption."
			);
		}

		uint calculatedFilePathHash = 0x811c9dc5;
		for (int j = 0; j < FilePath.Length; ++j)
		{
			calculatedFilePathHash = (calculatedFilePathHash * 0x1000193) ^ (byte)FilePath[j];
		}

		if (calculatedFilePathHash != FileNameHash)
		{
			throw new InvalidDataException(
				$"Error: {nameof(BnkContentFileEntry)}: Calculated filename hash for string {FilePath} at {DecompressedDataStream.Position} did not match stored value. " +
				$"Expected 0x{FileNameHash:x}, got 0x{calculatedFilePathHash:x} instead. This could indicate data corruption."
			);
		}
	}

	public void WriteIntegersToStream(Stream TargetStream, bool IsContentCompressed)
	{
		TargetStream.WriteBigEndianUInt32(FileNameHash);
		TargetStream.WriteBigEndianUInt32(FileOffset);

		if (IsContentCompressed)
		{
			TargetStream.WriteBigEndianInt32(TotalDecompressedDataSize);
		}

		TargetStream.WriteBigEndianInt32(ContentFileDataSize);

		if (IsContentCompressed)
		{
			TargetStream.WriteBigEndianInt32(NumChunks);
			for (int i = 0; i < NumChunks; i++)
			{
				TargetStream.WriteBigEndianInt32(ChunkDecompressedSizes[i]);
			}
		}
	}

	public void WriteFilePathStringsToStream(Stream TargetStream)
	{
		TargetStream.WriteBigEndianInt32(FilePath.Length + 1);	// Need to account for NUL termination that isn't present in C# strings.

		byte[] filePathStringBytesArray =  ArrayPool<byte>.Shared.Rent(FilePath.Length);
		Span<byte> filePathStringBytes = filePathStringBytesArray.AsSpan(0, FilePath.Length);

		int bytesEncoded = Encoding.ASCII.GetBytes(FilePath, filePathStringBytes);
		if (bytesEncoded != FilePath.Length)
		{
			throw new InvalidDataException(
				$"Error: {nameof(BnkContentFileEntry)}: Encoding error while trying to convert {FilePath} into a stream of bytes. " +
				$"Expected {FilePath.Length} bytes, got {bytesEncoded} instead."
			);
		}

		TargetStream.Write(filePathStringBytes);
		ArrayPool<byte>.Shared.Return(filePathStringBytesArray);

		for (int i = 0; i < 28; i++)
		{
			TargetStream.WriteByte(0);	// Write 28 NUL bytes.
		}

		TargetStream.WriteByte(EndOfFilePathMarker);
	}
}

