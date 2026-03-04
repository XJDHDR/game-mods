// This file is or was originally a part of the Fable III Skip Intro Patcher project, which can be found here: https://github.com/XJDHDR/game-mods/blob/master/Fable%20III/Fable_III_Skip_Intro_Patcher/License.txt
//
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// This Source Code Form is "Incompatible With Secondary Licenses", as
// defined by the Mozilla Public License, v. 2.0.
//
//  List of this Source Code Form's contributors:
//  - Xavier "XJDHDR" du Hecquet de Rauville
//


using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace Fable3SkipIntroPatcher.FileFormats;

public class BnkDecompressedIndexData
{
	// ReSharper disable MemberCanBePrivate.Global
	// ReSharper disable FieldCanBeMadeReadOnly.Global
	// ReSharper disable once InconsistentNaming
	public int Unknown_AlwaysEqualToZero;
	public int NumberOfFiles;
	public BnkContentFileEntry[] AllFileEntries;

	// ReSharper disable once FieldCanBeMadeReadOnly.Local
	private bool isContentDataCompressed;
	// ReSharper restore FieldCanBeMadeReadOnly.Global
	// ReSharper restore MemberCanBePrivate.Global

	public BnkDecompressedIndexData(ref BnkIndexFileFormat IndexFile)
	{
		isContentDataCompressed = IndexFile.IsBnkContentDataCompressed;

		byte[] decompressedData = decompressIndexFileData(ref IndexFile, out int totalDecompressedDataSize);

		using MemoryStream decompressedDataStream = new(decompressedData, 0, totalDecompressedDataSize);
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

	public BnkIndexFileFormat CompressAndWriteToIndexFile(bool IsContentDataCompressed)
	{
		byte[] decompressedData;
		using (MemoryStream decompressedDataStream = new())
		{
			decompressedDataStream.WriteBigEndianInt32(Unknown_AlwaysEqualToZero);
			decompressedDataStream.WriteBigEndianInt32(NumberOfFiles);

			for (int i = 0; i < AllFileEntries.Length; i++)
			{
				AllFileEntries[i].WriteIntegersToStream(decompressedDataStream, isContentDataCompressed);
			}

			for (int i = 0; i < AllFileEntries.Length; i++)
			{
				AllFileEntries[i].WriteFilePathStringsToStream(decompressedDataStream);
			}

			decompressedData = decompressedDataStream.ToArray();
		}

		BnkIndexFileFormat indexFile = new(decompressedData, IsContentDataCompressed);
		return indexFile;
	}

	private byte[] decompressIndexFileData(ref BnkIndexFileFormat IndexFile, out int TotalDecompressedDataSize)
	{
		TotalDecompressedDataSize = 0;
		using MemoryStream compressedDataStream = new();

		for (int i = 0; i < IndexFile.CompressedIndexDataChunks.Length; i++)
		{
			long startPos = compressedDataStream.Position;
			compressedDataStream.Write(IndexFile.CompressedIndexDataChunks[i].CompressedData, 0, IndexFile.CompressedIndexDataChunks[i].CompressedDataSize);
			long endPos = compressedDataStream.Position;

			if ((endPos - startPos) != IndexFile.CompressedIndexDataChunks[i].CompressedDataSize)
			{
				throw new InvalidDataException(
					$"Error: {nameof(BnkDecompressedIndexData)}: Failed to read the correct amount of compressed data for decompression. Expected {IndexFile.CompressedIndexDataChunks[i].CompressedDataSize} bytes, got {endPos - startPos} instead."
				);
			}

			TotalDecompressedDataSize += IndexFile.CompressedIndexDataChunks[i].DecompressedDataSize;
		}

		compressedDataStream.Position = 0;
		byte[] decompressedData = new byte[TotalDecompressedDataSize];

		using ZLibStream zLibDecompressionStream = new(compressedDataStream, CompressionMode.Decompress, true);
		zLibDecompressionStream.ReadExactly(decompressedData, 0, TotalDecompressedDataSize);

		return decompressedData;
	}
}

public class BnkContentFileEntry
{
	// ReSharper disable MemberCanBePrivate.Global
	// ReSharper disable FieldCanBeMadeReadOnly.Global
	public uint FileNameHash;
	public uint FileOffset;
	public int ContentFileDataSize;

	public int TotalDecompressedDataSize;
	public int NumChunks;
	public int[] ChunkDecompressedSizes = Array.Empty<int>();

	public string FilePath = string.Empty;
	public byte EndOfFilePathMarker;
	// ReSharper restore FieldCanBeMadeReadOnly.Global
	// ReSharper restore MemberCanBePrivate.Global

	private const uint FNV1_HASH_SEED = 0x811c9dc5;
	private const uint FNV1_PRIME = 0x1000193;

	public BnkContentFileEntry(Stream DecompressedData, bool IsContentCompressed)
	{
		FileNameHash = DecompressedData.ReadBigEndianUInt32();
		FileOffset = DecompressedData.ReadBigEndianUInt32();
		if ((FileOffset & 0xF) != 0)
		{
			throw new InvalidDataException(
				$"Error: {nameof(BnkContentFileEntry)}: File offsets are supposed to be aligned to 16 byte boundaries. " +
				$"However, the file offset read at 0x{DecompressedData.Position - 4} was {FileOffset} which is not aligned as such. This could indicate data corruption."
			);
		}

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
		DecompressedDataStream.ReadExactly(filePathStringBytes);

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

		uint calculatedFilePathHash = FNV1_HASH_SEED;
		for (int j = 0; j < FilePath.Length; ++j)
		{
			calculatedFilePathHash = (calculatedFilePathHash * FNV1_PRIME) ^ (byte)FilePath[j];
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

