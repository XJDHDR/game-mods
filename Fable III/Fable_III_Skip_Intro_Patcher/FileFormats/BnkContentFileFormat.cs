using System.Buffers;
using System.Text;

namespace Fable3SkipIntroPatcher.FileFormats;

public class BnkContentFileFormat
{
    public int NumberOfFiles;
    public BnkContentFileEntry[] AllFileEntries;

    public static BnkContentFileFormat CreateFromStream(Stream DecompressedData, bool IsContentCompressed)
    {
        BnkContentFileFormat contentFileFormat = new();

        int unknown_AlwaysEqualToZero = DecompressedData.ReadBigEndianInt32();
        if (unknown_AlwaysEqualToZero != 0)
        {
            Console.WriteLine($"Warning: BnkContentFileFormat: Bytes 0-3 are not the expected value: 0 expected, got {unknown_AlwaysEqualToZero} instead.");
        }
        
        contentFileFormat.NumberOfFiles = DecompressedData.ReadBigEndianInt32();

        contentFileFormat.AllFileEntries = new BnkContentFileEntry[contentFileFormat.NumberOfFiles];
        for (int i = 0; i < contentFileFormat.NumberOfFiles; i++)
        {
            contentFileFormat.AllFileEntries[i] = BnkContentFileEntry.CreateFromStream(DecompressedData, IsContentCompressed);
        }
        
        for (int i = 0; i < contentFileFormat.NumberOfFiles; i++)
        {
            int filePathStringLength = DecompressedData.ReadBigEndianInt32() - 1;   // String is null terminated so we don't need to read the last byte.
            byte[] filePathStringBytesArray =  ArrayPool<byte>.Shared.Rent(filePathStringLength);
            Span<byte> filePathStringBytes = filePathStringBytesArray.AsSpan(0, filePathStringLength);
            int bytesRead = DecompressedData.Read(filePathStringBytes);

            if (bytesRead != filePathStringLength)
            {
                throw new EndOfStreamException(
                    $"Error: BnkContentFileFormat: Read error for compressed data ending at {DecompressedData.Position} for file {i}. " +
                    $"Tried to read {filePathStringLength} bytes, got {bytesRead} instead."
                );
            }
            
            contentFileFormat.AllFileEntries[i].FilePath = Encoding.ASCII.GetString(filePathStringBytes);
            ArrayPool<byte>.Shared.Return(filePathStringBytesArray);
            
            DecompressedData.Position += 28;     // Skip over 27 NUL bytes
            int endOfFileEntry = DecompressedData.ReadByte();
            if (endOfFileEntry != 0x10)
            {
                throw new InvalidDataException(
                    $"Error: BnkContentFileFormat: Byte at {DecompressedData.Position} was supposed to indicate the end of a file path string with 0x10. {endOfFileEntry:x} was read instead."
                );
            }
        }
        
        return contentFileFormat;
    }
}

public class BnkContentFileEntry
{
    public uint Hash;
    public uint FileOffset;
    public int DataSize;
    
    public int DecompressedDataSize;
    public int NumChunks;

    public string FilePath;

    public static BnkContentFileEntry CreateFromStream(Stream DecompressedData, bool IsContentCompressed)
    {
        BnkContentFileEntry fileEntry = new();

        fileEntry.Hash = DecompressedData.ReadBigEndianUInt32();
        fileEntry.FileOffset = DecompressedData.ReadBigEndianUInt32();

        fileEntry.DecompressedDataSize = (IsContentCompressed) ?
            DecompressedData.ReadBigEndianInt32() :
            -1;
        
        fileEntry.DataSize = DecompressedData.ReadBigEndianInt32();

        if (IsContentCompressed)
        {
            fileEntry.NumChunks = DecompressedData.ReadBigEndianInt32();
            DecompressedData.Position += fileEntry.NumChunks * 4;   // Skip over unknown data
        }

        return fileEntry;
    }
}

