// This file is or was originally a part of the Fable III Skip Intro Patcher project, which can be found here: https://github.com/XJDHDR/???
// 
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// 
//  List of this Source Code Form's contributors:
//  - Xavier "XJDHDR" du Hecquet de Rauville
// 

using System.Buffers;
using LibDeflate;

namespace Fable_III_BNK_file_format;

public struct BnkFileData
{
	public static bool Read(string BnkHeaderFilePath, string BnkDataFilePath, Action<float, string>? ReportProgress)
	{
		byte[] bnkDataFileBytes;
		Fable3BnkHeaderFile headerData;
		Fable3BnkCompressedFileDataEntries? compressedHeaderData;
		using (Task<byte[]> readBnkDataIntoArrayTask = File.ReadAllBytesAsync(BnkDataFilePath))
		{
			using (FileStream bnkHeaderFileStream = new(BnkHeaderFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				using (BinaryReader bnkHeaderFileBinaryReader = new(bnkHeaderFileStream))
				{
					headerData = new(bnkHeaderFileBinaryReader, out compressedHeaderData);
				}
			}

			if (!readBnkDataIntoArrayTask.IsCompleted)
			{
				readBnkDataIntoArrayTask.Wait();
			}

			bnkDataFileBytes = readBnkDataIntoArrayTask.Result;
		}

		ParallelExtractFileDataHelper temp = new()
		{
			NumberOfEntries = headerData.FileEntries.Offsets.Length,
			BnkDataFileBytes = bnkDataFileBytes,
			HeaderData = headerData,
			CompressedHeaderData = compressedHeaderData,
			ReportProgress = ReportProgress
		};
		temp.Extract();

		return true;
	}
	
	private struct ParallelExtractFileDataHelper
	{
		internal int NumberOfEntries;
		internal byte[] BnkDataFileBytes;
		internal Fable3BnkHeaderFile HeaderData;
		internal Fable3BnkCompressedFileDataEntries? CompressedHeaderData;
		internal Action<float, string>? ReportProgress;

		private uint numberOfCompletedLoops;
		private byte[][] result;

		internal byte[][] Extract()
		{
			result = new byte[NumberOfEntries][];
			numberOfCompletedLoops = 0;

			Parallel.For(0, NumberOfEntries, extractIndividualFileBytes);

			return result;
		}

		private void extractIndividualFileBytes(int I)
		{
			result[I] = new byte[HeaderData.FileEntries.UncompressedDataSizes[I]];
			Span<byte> finalReadData = new(result[I]);
			if (CompressedHeaderData != null)
			{
				Fable3BnkCompressedFileDataEntries compressedHeaderDataNoNull = (Fable3BnkCompressedFileDataEntries)CompressedHeaderData;
				ReadOnlySpan<byte> compressedData = new(BnkDataFileBytes, HeaderData.FileEntries.Offsets[I], compressedHeaderDataNoNull.CompressedDataSizes[I]);
				DeflateDecompressor deflateDecompressor = new();
				deflateDecompressor.Decompress(compressedData, finalReadData, out int numberOfBytesWritten, out int numberOfBytesRead);

				if (numberOfBytesRead != compressedHeaderDataNoNull.CompressedDataSizes[I])
				{
					string exceptionMessage = 
						$"""
						While decompressing the data for file {HeaderData.FileEntries.FileFullPaths[I]}, the decompressor was supposed to read 
						{compressedHeaderDataNoNull.CompressedDataSizes[I]}, but {numberOfBytesRead} bytes were read instead.
						""";
					throw new ArgumentException(exceptionMessage, nameof(BnkDataFileBytes));
				}

				if (numberOfBytesWritten != HeaderData.FileEntries.UncompressedDataSizes[I])
				{
					string exceptionMessage = 
						$"""
						While decompressing the data for file {HeaderData.FileEntries.FileFullPaths[I]}, the decompressor was supposed to write 
						{HeaderData.FileEntries.UncompressedDataSizes[I]}, but {numberOfBytesWritten} bytes were written instead.
						""";
					throw new ArgumentException(exceptionMessage, nameof(BnkDataFileBytes));
				}
			}
			else
			{
				ReadOnlySpan<byte> segmentContainingData = new(BnkDataFileBytes, HeaderData.FileEntries.Offsets[I], HeaderData.FileEntries.UncompressedDataSizes[I]);
				segmentContainingData.CopyTo(finalReadData);
			}

			float newNumberOfCompletedLoops = Interlocked.Increment(ref numberOfCompletedLoops);
			ReportProgress?.Invoke(newNumberOfCompletedLoops / NumberOfEntries, "Extracting file data.");
		}
	}
}
