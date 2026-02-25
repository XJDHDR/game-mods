// This file is or was originally a part of the Fable III Skip Intro Patcher project, which can be found here: https://github.com/XJDHDR/???
//
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
//  List of this Source Code Form's contributors:
//  - Xavier "XJDHDR" du Hecquet de Rauville

using System.Buffers;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Fable3SkipIntroPatcher.FileFormats;

public struct BnkContentFileContents
{
	public BnkContentFileData[] FileData;

	public static BnkContentFileContents CreateFromStream(Stream ContentFile, ref BnkContentFileFormat FileEntries, bool IsContentDataCompressed)
	{
		BnkContentFileContents fileContents = new();

		fileContents.FileData = new BnkContentFileData[FileEntries.NumberOfFiles];
		for (int i = 0; i < FileEntries.NumberOfFiles; i++)
		{
			fileContents.FileData[i] = BnkContentFileData.CreateFromStream(ContentFile, ref FileEntries.AllFileEntries[i], IsContentDataCompressed);
		}

		return fileContents;
	}
}

public struct BnkContentFileData
{
	public byte[] ContentFileData;
	public byte[] DecompressedData;

	public static BnkContentFileData CreateFromStream(Stream ContentFile, ref BnkContentFileEntry FileEntry, bool IsContentDataCompressed)
	{
		BnkContentFileData fileData = new();

		fileData.ContentFileData = new byte[FileEntry.ContentFileDataSize];
		ContentFile.Position = FileEntry.FileOffset;
		int bytesRead = ContentFile.Read(fileData.ContentFileData,  0, FileEntry.ContentFileDataSize);

		if (bytesRead != FileEntry.ContentFileDataSize)
		{
			throw new EndOfStreamException(
				$"Error: BnkContentFileData: Read error for data ending at {ContentFile.Position}. " +
				$"Tried to read {FileEntry.ContentFileDataSize} bytes, got {bytesRead} instead."
			);
		}

		if (!IsContentDataCompressed)
		{
			return fileData;
		}

		int chunkSize = 32768;
		int sizeOfRemainingData = FileEntry.ContentFileDataSize;
		fileData.DecompressedData =  new byte[FileEntry.DecompressedDataSize];
		MemoryStream decompressedDataStream = new(fileData.DecompressedData);
		byte[] decompressedDataChunk = ArrayPool<byte>.Shared.Rent(FileEntry.DecompressedDataSize);
		Inflater inflator = new();
		for (int i = 0; i < FileEntry.NumChunks; i++)
		{
			int thisChunkOffset = i * chunkSize;
			int thisChunkSize = (sizeOfRemainingData < chunkSize) ?
				sizeOfRemainingData :
				chunkSize;

			inflator.SetInput(fileData.ContentFileData, thisChunkOffset, thisChunkSize);
			int deflatedChunkSize = inflator.Inflate(decompressedDataChunk, 0, FileEntry.DecompressedDataSize);
			decompressedDataStream.Write(decompressedDataChunk, 0, deflatedChunkSize);
			inflator.Reset();
			sizeOfRemainingData -= chunkSize;
		}

		decompressedDataStream.Close();
		ArrayPool<byte>.Shared.Return(decompressedDataChunk);

		return fileData;
	}
}
