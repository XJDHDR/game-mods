// This file is or was originally a part of the Fable III Skip Intro Patcher project, which can be found here: https://github.com/XJDHDR/???
// 
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// 
//  List of this Source Code Form's contributors:
//  - Xavier "XJDHDR" du Hecquet de Rauville
// 

using System.Buffers;
using System.Text;
using LibDeflate;

namespace Fable_III_BNK_file_format;

public struct Fable3BnkHeaderFile
{
	// ==== Fields ====

	public int UnknownUInt;
    public BnkFileEntries FileEntries;

    
    // ==== Constructors ====
    public Fable3BnkHeaderFile(BinaryReader BnkHeaderFileStreamBinaryReader, out Fable3BnkCompressedFileDataEntries? CompressedFileDataEntries)
    {
	    Span<byte> fourByteSpan = stackalloc byte[4];

	    // First, read the contents of the Bnk header file
	    uint sizeOfFile = readBigEndianUInt32(BnkHeaderFileStreamBinaryReader, ref fourByteSpan);
        if (sizeOfFile != BnkHeaderFileStreamBinaryReader.BaseStream.Length)
        {
	        string exceptionMessage = 
		        $"""
				The first 4 bytes of the Bnk Header are supposed to give the file's length. 
				{sizeOfFile} was read, but the file's actual length is {BnkHeaderFileStreamBinaryReader.BaseStream.Length}.
				""";
	        throw new ArgumentException(exceptionMessage, nameof(BnkHeaderFileStreamBinaryReader));
        }

        // This number serves an unknown purpose. It is always equal to 4 in Fable III. 
        // Since it is found at the address 0x4 in the file, maybe it is the address of the first byte after the SizeOfFile value?
        uint sizeOfFileEndAddress = readBigEndianUInt32(BnkHeaderFileStreamBinaryReader, ref fourByteSpan);
        if (sizeOfFile != 4)
        {
	        string exceptionMessage = $"The second 4 bytes of the Bnk Header are supposed to be '4', but '{sizeOfFileEndAddress}' was read instead.";
	        throw new ArgumentException(exceptionMessage, nameof(BnkHeaderFileStreamBinaryReader));
        }
        
        bool isDataCompressed = BnkHeaderFileStreamBinaryReader.ReadBoolean();

        byte[] decompressedHeaderData = readAndDecompressBnkHeaderDataChunks(BnkHeaderFileStreamBinaryReader, ref fourByteSpan);

        using (MemoryStream decompressedHeaderDataStream = new(decompressedHeaderData))
        {
	        using (BinaryReader decompressedHeaderDataBinaryReader = new(decompressedHeaderDataStream))
	        {
		        UnknownUInt = readBigEndianInt32(decompressedHeaderDataBinaryReader, ref fourByteSpan);
		        int numberOfFileEntries = readBigEndianInt32(decompressedHeaderDataBinaryReader, ref fourByteSpan);
		        
		        FileEntries = new BnkFileEntries(decompressedHeaderDataBinaryReader, isDataCompressed, numberOfFileEntries, ref fourByteSpan, out CompressedFileDataEntries);
	        }
        }
    }


    // ==== Private methods ====
    private static uint readBigEndianUInt32(BinaryReader StreamBinaryReader, ref Span<byte> FourByteSpan)
    {
	    FourByteSpan.Clear();
        int numberOfBytesRead = StreamBinaryReader.Read(FourByteSpan);
        if (numberOfBytesRead != 4)
        {
	        string exceptionMessage = $"While trying to read a Big Endian UInt32, 4 bytes were supposed to be read, but '{numberOfBytesRead}' were read instead.";
	        throw new ArgumentException(exceptionMessage, nameof(StreamBinaryReader));
        }

        uint bigEndianUInt32 = (uint)((FourByteSpan[0] << 24) | (FourByteSpan[1] << 16) | (FourByteSpan[2] << 8) | FourByteSpan[3]);
        return bigEndianUInt32;
    }

    private static int readBigEndianInt32(BinaryReader StreamBinaryReader, ref Span<byte> FourByteSpan)
    {
	    FourByteSpan.Clear();
	    int numberOfBytesRead = StreamBinaryReader.Read(FourByteSpan);
	    if (numberOfBytesRead != 4)
	    {
		    string exceptionMessage = $"While trying to read a Big Endian Int32, 4 bytes were supposed to be read, but '{numberOfBytesRead}' were read instead.";
		    throw new ArgumentException(exceptionMessage, nameof(StreamBinaryReader));
	    }
        
	    int bigEndianInt32 = (FourByteSpan[0] << 24) | (FourByteSpan[1] << 16) | (FourByteSpan[2] << 8) | FourByteSpan[3];
	    return bigEndianInt32;
    }
    
    private byte[] readAndDecompressBnkHeaderDataChunks(BinaryReader StreamBinaryReader, ref Span<byte> FourByteSpan)
    {
	    // The rest of the Bnk header consists of at least one chunk of data.
	    // Each chunk is prepended with the number of bytes representing the compressed data, and the number of bytes taken up by the decompressed data.
	    // The data itself is compressed using the Deflate algorithm.
	    int decompressedDataTotalSize = 0;
	    List<byte> allCompressedData = new();
	    while (StreamBinaryReader.BaseStream.Position < StreamBinaryReader.BaseStream.Length)
	    {
		    int compressedDataSize = readBigEndianInt32(StreamBinaryReader, ref FourByteSpan);
		    decompressedDataTotalSize += readBigEndianInt32(StreamBinaryReader, ref FourByteSpan);

		    byte[] rentedArrayForCompressedDataChunk = ArrayPool<byte>.Shared.Rent(compressedDataSize);
		    Span<byte> compressedDataChunk = rentedArrayForCompressedDataChunk.AsSpan(0, compressedDataSize);

		    int numberOfCompressedBytesRead = StreamBinaryReader.Read(compressedDataChunk);
		    if (numberOfCompressedBytesRead != compressedDataSize)
		    {
			    string exceptionMessage = $"While reading the data for a header data chunk, {compressedDataSize} were supposed to be read, but {numberOfCompressedBytesRead} bytes were read instead.";
			    throw new ArgumentException(exceptionMessage, nameof(StreamBinaryReader));
		    }

		    allCompressedData.AddRange(compressedDataChunk.ToArray());
		    ArrayPool<byte>.Shared.Return(rentedArrayForCompressedDataChunk);
	    }

	    ReadOnlySpan<byte> allCompressedDataSpan = allCompressedData.ToArray();
	    byte[] decompressedData = new byte[decompressedDataTotalSize];
	    Span<byte> decompressedDataSpan = decompressedData.AsSpan();
	    DeflateDecompressor deflateDecompressor = new();
	    
	    deflateDecompressor.Decompress(allCompressedDataSpan, decompressedDataSpan, out int numberOfBytesWritten, out int numberOfDecompressedBytesRead);

	    if (numberOfDecompressedBytesRead != allCompressedDataSpan.Length)
	    {
		    string exceptionMessage = 
			    $"""
				While decompressing the data in the file's header, the decompressor was supposed to read 
				{allCompressedDataSpan.Length}, but {numberOfDecompressedBytesRead} bytes were read instead.
				""";
		    throw new ArgumentException(exceptionMessage, nameof(StreamBinaryReader));
	    }

	    if (numberOfBytesWritten != decompressedDataSpan.Length)
	    {
		    string exceptionMessage = 
			    $"""
				While decompressing the data in the file's header, the decompressor was supposed to write 
				{decompressedDataSpan.Length}, but {numberOfBytesWritten} bytes were written instead.
				""";
		    throw new ArgumentException(exceptionMessage, nameof(StreamBinaryReader));
	    }

	    
	    
	    return decompressedData;
    }
    
    

    public struct BnkFileEntries
    {
	    public uint[] Hashes;
	    public int[] Offsets;
	    public int[] UncompressedDataSizes;

	    public string[] FileFullPaths;

	    private uint[][] unknownUints;
	    
	    internal BnkFileEntries(BinaryReader BnkHeaderDataStreamBinaryReader, bool IsDataCompressed, int NumberOfFileEntries, 
		    ref Span<byte> FourByteSpan, out Fable3BnkCompressedFileDataEntries? CompressedFileDataEntries)
	    {
		    Fable3BnkCompressedFileDataEntries compressedFileDataEntriesNoNull = default;
		    if (IsDataCompressed)
		    {
			    compressedFileDataEntriesNoNull = new Fable3BnkCompressedFileDataEntries
			    {
				    CompressedDataSizes = new int[NumberOfFileEntries],
				    NumbersOfChunks = new int[NumberOfFileEntries],
				    UnknownByteSequences = new byte[NumberOfFileEntries][]
			    };
		    }

		    Hashes = new uint[NumberOfFileEntries];
		    Offsets = new int[NumberOfFileEntries];
		    UncompressedDataSizes = new int[NumberOfFileEntries];
		    FileFullPaths = new string[NumberOfFileEntries];
		    unknownUints = new uint[NumberOfFileEntries][];

		    for (int i = 0; i < NumberOfFileEntries; i++)
		    {
			    Hashes[i] = readBigEndianUInt32(BnkHeaderDataStreamBinaryReader, ref FourByteSpan);
			    Offsets[i] = readBigEndianInt32(BnkHeaderDataStreamBinaryReader, ref FourByteSpan);
			    UncompressedDataSizes[i] = readBigEndianInt32(BnkHeaderDataStreamBinaryReader, ref FourByteSpan);

			    if (!IsDataCompressed)
			    {
				    continue;
			    }
			    readCompressedFileDataInfo(BnkHeaderDataStreamBinaryReader, i, ref FourByteSpan, ref compressedFileDataEntriesNoNull);
		    }
		    
		    // After the above is a list of the file paths of each file in order. Each file path is a null terminated string prepended with an Int32 defining length (including null char).
		    for (int i = 0; i < NumberOfFileEntries; i++)
		    {
			    FileFullPaths[i] = readNullTerminatedStringWithInt32LengthPrefixed(BnkHeaderDataStreamBinaryReader, ref FourByteSpan);

			    unknownUints[i] = new uint[7];
			    for (int j = 0; j < 7; j++)
			    {
				    unknownUints[i][j] = readBigEndianUInt32(BnkHeaderDataStreamBinaryReader, ref FourByteSpan);
			    }
		    }
		    
		    if (IsDataCompressed)
		    {
			    CompressedFileDataEntries = compressedFileDataEntriesNoNull;
		    }
		    else
		    {
			    CompressedFileDataEntries = null;
		    }
	    }

	    private string readNullTerminatedStringWithInt32LengthPrefixed(BinaryReader BnkHeaderDataStreamBinaryReader, ref Span<byte> FourByteSpan)
	    {
		    int lengthOfString = readBigEndianInt32(BnkHeaderDataStreamBinaryReader, ref FourByteSpan);

		    Span<byte> stringChars = stackalloc byte[lengthOfString];
		    int numberOfStringBytesRead = BnkHeaderDataStreamBinaryReader.Read(stringChars);
		    
		    if (numberOfStringBytesRead != lengthOfString)
		    {
			    string exceptionMessage = $"While reading a string in the file's header, {lengthOfString} bytes were supposed to be read, but {numberOfStringBytesRead} bytes were read instead.";
			    throw new ArgumentException(exceptionMessage, nameof(BnkHeaderDataStreamBinaryReader));
		    }

		    if (stringChars[lengthOfString - 1] != 0x0)
		    {
			    string exceptionMessage = $"A string that was being read is supposed to end with a '0', but '{stringChars[lengthOfString - 1]}' was read instead.";
			    throw new ArgumentException(exceptionMessage, nameof(BnkHeaderDataStreamBinaryReader));
		    }

		    string readString = Encoding.ASCII.GetString(stringChars);
		    return readString;
	    }

	    private void readCompressedFileDataInfo(BinaryReader BnkHeaderDataStreamBinaryReader, int I, ref Span<byte> FourByteSpan, ref Fable3BnkCompressedFileDataEntries CompressedFileDataEntries)
	    {
		    CompressedFileDataEntries.CompressedDataSizes[I] = readBigEndianInt32(BnkHeaderDataStreamBinaryReader, ref FourByteSpan);
		    CompressedFileDataEntries.NumbersOfChunks[I] = readBigEndianInt32(BnkHeaderDataStreamBinaryReader, ref FourByteSpan);

		    int lengthOfByteSequence = CompressedFileDataEntries.NumbersOfChunks[I] * 4;
		    CompressedFileDataEntries.UnknownByteSequences[I] = new byte[lengthOfByteSequence];
		    int numberOfBytesRead = BnkHeaderDataStreamBinaryReader.Read(CompressedFileDataEntries.UnknownByteSequences[I]);
		    
		    if (numberOfBytesRead != lengthOfByteSequence)
		    {
			    string exceptionMessage = 
				    $"""
					While reading an unknown byte sequence in the file's header, {lengthOfByteSequence} bytes were supposed to be read, but {numberOfBytesRead} bytes were read instead.
					""";
			    throw new ArgumentException(exceptionMessage, nameof(BnkHeaderDataStreamBinaryReader));
		    }
	    }
    }
}