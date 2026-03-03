// This file is or was originally a part of the Fable III Skip Intro Patcher project, which can be found here: https://github.com/XJDHDR/???
//
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
//  List of this Source Code Form's contributors:
//  - Xavier "XJDHDR" du Hecquet de Rauville

using System.IO.Compression;

namespace Fable3SkipIntroPatcher.FileFormats;

public struct BnkContentFileContents
{
	public BnkContentFileData[] FileData;

	public static BnkContentFileContents CreateFromStream(Stream ContentFile, ref BnkDecompressedIndexData FileEntries, bool IsContentDataCompressed)
	{
		BnkContentFileContents fileContents = new();

		fileContents.FileData = new BnkContentFileData[FileEntries.NumberOfFiles];
		for (int i = 0; i < FileEntries.NumberOfFiles; i++)
		{
			int paddingSize = (i != (FileEntries.NumberOfFiles - 1)) ?	// Is this the last file in the Content File?
				(int)((FileEntries.AllFileEntries[i + 1].FileOffset - FileEntries.AllFileEntries[i].FileOffset) - FileEntries.AllFileEntries[i].ContentFileDataSize) :
				0;	// No padding is present for last file.
			fileContents.FileData[i] = new(ContentFile, ref FileEntries.AllFileEntries[i], paddingSize, IsContentDataCompressed);
		}

		return fileContents;
	}

	public void WriteToStream(Stream BytesDestination)
	{
		for (int i = 0; i < FileData.Length; i++)
		{
			FileData[i].WriteToStream(BytesDestination);
		}
	}
}

public struct BnkContentFileData
{
	public byte[] ContentFileData;

	// Files are aligned to 16 byte boundaries (except for the last one). If the file's size is not a multiple of 16, padding is added to make the next file start at this boundary.
	public byte[] Padding;

	private bool isContentDataCompressed;

	private const int COMPRESSED_CHUNK_SIZE = 32768;	// Compressed data is stored in chunks of 32KB.

	public BnkContentFileData(Stream ContentFile, ref BnkContentFileEntry FileEntry, int PaddingSize, bool IsContentDataCompressed)
	{
		isContentDataCompressed = IsContentDataCompressed;
		ContentFileData = new byte[FileEntry.ContentFileDataSize];
		ContentFile.ReadExactly(ContentFileData,  0, FileEntry.ContentFileDataSize);

		Padding = new byte[PaddingSize];
		ContentFile.ReadExactly(Padding,  0, PaddingSize);
	}

	public void WriteToStream(Stream BytesDestination)
	{
		BytesDestination.Write(ContentFileData);

		if (Padding.Length > 0)
		{
			BytesDestination.Write(Padding);
		}
	}

	public byte[] DecompressData(ref BnkContentFileEntry FileEntry)
	{
		if (!isContentDataCompressed)
		{
			Console.WriteLine($"Warning: {nameof(BnkContentFileData)}: Was asked to decompress data that is not compressed.");
			return ContentFileData;
		}

		using MemoryStream decompressedDataStream = new();

		for (int i = 0; i < FileEntry.NumChunks; i++)
		{
			int thisChunkOffset = i * COMPRESSED_CHUNK_SIZE;
			int thisChunkSize = (i >= (FileEntry.NumChunks - 1)) ?
				COMPRESSED_CHUNK_SIZE :
				FileEntry.ContentFileDataSize - thisChunkOffset;

			using MemoryStream compressedDataChunk = new(ContentFileData, thisChunkOffset, thisChunkSize);
			using ZLibStream zlibDecompressionStream = new(compressedDataChunk, CompressionMode.Decompress, true);

			long startPos = decompressedDataStream.Position;
			zlibDecompressionStream.CopyTo(decompressedDataStream);
			long decompressedBytes = decompressedDataStream.Position - startPos;

			if (decompressedBytes != FileEntry.ChunkDecompressedSizes[i])
			{
				throw new InvalidDataException(
					$"Error: {nameof(BnkContentFileData)}: Inflate error for file: {FileEntry.FilePath}. " +
					$"Decompressed data was supposed to be {FileEntry.ChunkDecompressedSizes[i]} bytes, got {decompressedBytes} instead."
				);
			}
		}

		return decompressedDataStream.ToArray();
	}
}
