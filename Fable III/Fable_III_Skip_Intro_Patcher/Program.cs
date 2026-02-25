using Fable3SkipIntroPatcher.FileFormats;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Fable3SkipIntroPatcher;

class Program
{
	static void Main(string[] args)
	{
		FileStream bnkIndexFileStream = File.OpenRead("D:/Games/Steam/steamapps/common/Fable 3/data/levels.bnk");
		BnkIndexFileFormat bnkIndexFile = BnkIndexFileFormat.CreateFromFileStream(bnkIndexFileStream);
		bnkIndexFileStream.Close();

		Inflater inflator = new();
		inflator.SetInput(bnkIndexFile.CompressedIndexDataCollated);
		byte[] decompressedData = new byte[bnkIndexFile.TotalSizeOfDecompressedData];
		inflator.Inflate(decompressedData);
		inflator.Reset();

		MemoryStream decompressedIndexDataStream = new(decompressedData);
		BnkContentFileFormat contentFileFormat = BnkContentFileFormat.CreateFromStream(decompressedIndexDataStream, bnkIndexFile.IsBnkContentDataCompressed);
		decompressedIndexDataStream.Close();

		FileStream bnkContentFileStream = File.OpenRead("D:/Games/Steam/steamapps/common/Fable 3/data/levels.bnk.dat");
		BnkContentFileContents bnkContentFile = BnkContentFileContents.CreateFromStream(bnkContentFileStream, ref contentFileFormat, bnkIndexFile.IsBnkContentDataCompressed);
		bnkContentFileStream.Close();

		for (int i = 0; i < contentFileFormat.NumberOfFiles; i++)
		{
			if (contentFileFormat.AllFileEntries[i].FilePath.Equals("art\\videos\\lionhead_logo.bik") || contentFileFormat.AllFileEntries[i].FilePath.Equals("art\\videos\\microsoft_logo.bik"))
			{
				// i should be 35 for Microsoft video and 38 for Lionhead
				BlankBinkVideo.ReplaceBnkContentFileEntry(i, ref bnkContentFile, ref contentFileFormat, bnkIndexFile.IsBnkContentDataCompressed);
			}
		}

		Console.WriteLine("Finished reading BNK Index file");
	}
}
