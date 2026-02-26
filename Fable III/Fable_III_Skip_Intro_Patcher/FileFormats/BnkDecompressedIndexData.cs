using System.Buffers;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Fable3SkipIntroPatcher.FileFormats;

public class BnkDecompressedIndexData
{
	public byte[] DecompressedData;
	public int NumberOfFiles;
	public BnkContentFileEntry[] AllFileEntries;

	public static BnkDecompressedIndexData CreateFromIndexFileData(ref BnkIndexFileFormat IndexFile)
	{
		int totalCompressedDataSize = 0;
		int totalDecompressedDataSize = 0;
		for (int i = 0; i < IndexFile.CompressedIndexDataChunks.Length; i++)
		{
			totalCompressedDataSize += IndexFile.CompressedIndexDataChunks[i].CompressedDataSize;
			totalDecompressedDataSize += IndexFile.CompressedIndexDataChunks[i].DecompressedDataSize;
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

		byte[] decompressedDataBorrowedArray = ArrayPool<byte>.Shared.Rent(totalDecompressedDataSize);
		Inflater inflater = new();
		inflater.SetInput(collatedCompressedDataBorrowedArray, 0, totalCompressedDataSize);
		int decompressedSize = inflater.Inflate(decompressedDataBorrowedArray, 0, totalDecompressedDataSize);
		if (decompressedSize != totalDecompressedDataSize)
		{
			throw new InvalidDataException(
				$"Error: {nameof(BnkDecompressedIndexData)}: Deflated data was not the correct size. Expected {totalDecompressedDataSize} bytes, got {decompressedSize} instead."
			);
		}
		ArrayPool<byte>.Shared.Return(collatedCompressedDataBorrowedArray);

		BnkDecompressedIndexData decompressedIndexData = new();
		using (MemoryStream decompressedDataStream = new(decompressedDataBorrowedArray, 0, totalDecompressedDataSize))
		{
			createFromDecompressedDataStream(decompressedDataStream, ref decompressedIndexData, ref IndexFile);
		}
		ArrayPool<byte>.Shared.Return(decompressedDataBorrowedArray);

		return decompressedIndexData;
	}

	private static void createFromDecompressedDataStream(Stream DecompressedDataStream, ref BnkDecompressedIndexData DecompressedIndexData, ref BnkIndexFileFormat IndexFile)
	{
		int unknown_AlwaysEqualToZero = DecompressedDataStream.ReadBigEndianInt32();
		if (unknown_AlwaysEqualToZero != 0)
		{
			Console.WriteLine($"Warning: {nameof(BnkDecompressedIndexData)}: Bytes 0-3 are not the expected value: 0 expected, got {unknown_AlwaysEqualToZero} instead.");
		}

		DecompressedIndexData.NumberOfFiles = DecompressedDataStream.ReadBigEndianInt32();

		DecompressedIndexData.AllFileEntries = new BnkContentFileEntry[DecompressedIndexData.NumberOfFiles];
		for (int i = 0; i < DecompressedIndexData.NumberOfFiles; i++)
		{
			DecompressedIndexData.AllFileEntries[i] = BnkContentFileEntry.ReadIntsFromStream(DecompressedDataStream, IndexFile.IsBnkContentDataCompressed);
		}

		for (int i = 0; i < DecompressedIndexData.NumberOfFiles; i++)
		{
			DecompressedIndexData.AllFileEntries[i].ReadFilePathStringsFromStream(DecompressedDataStream);
		}
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

	public static BnkContentFileEntry ReadIntsFromStream(Stream DecompressedData, bool IsContentCompressed)
	{
		BnkContentFileEntry fileEntry = new();

		fileEntry.FileNameHash = DecompressedData.ReadBigEndianUInt32();
		fileEntry.FileOffset = DecompressedData.ReadBigEndianUInt32();

		fileEntry.TotalDecompressedDataSize = (IsContentCompressed) ?
			DecompressedData.ReadBigEndianInt32() :
			-1;

		fileEntry.ContentFileDataSize = DecompressedData.ReadBigEndianInt32();

		if (IsContentCompressed)
		{
			fileEntry.NumChunks = DecompressedData.ReadBigEndianInt32();
			fileEntry.ChunkDecompressedSizes = new int[fileEntry.NumChunks];
			for (int i = 0; i < fileEntry.NumChunks; i++)
			{
				fileEntry.ChunkDecompressedSizes[i] = DecompressedData.ReadBigEndianInt32();
			}
		}

		return fileEntry;
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
		int endOfFileEntry = DecompressedDataStream.ReadByte();
		if (endOfFileEntry == 0)
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
}

