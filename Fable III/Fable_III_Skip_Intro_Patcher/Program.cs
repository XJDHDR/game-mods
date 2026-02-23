using Fable3SkipIntroPatcher.FileFormats;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Fable3SkipIntroPatcher;

class Program
{
	static void Main(string[] args)
	{
		FileStream fileStream = File.OpenRead("D:/Games/Steam/steamapps/common/Fable 3/data/levels.bnk");
		BnkIndexFileFormat bnkIndexFile = BnkIndexFileFormat.CreateFromFileStream(fileStream);

		Inflater inflator = new Inflater();
		inflator.SetInput(bnkIndexFile.CompressedIndexDataCollated);
		byte[] decompressedData = new byte[bnkIndexFile.TotalSizeOfDecompressedData];
		inflator.Inflate(decompressedData);
		inflator.Reset();
		
		File.AppendAllBytes("D:/Games/Steam/steamapps/common/Fable 3/data/levels.bnk.decompressed", decompressedData);
		
		MemoryStream decompressedIndexDataStream = new(decompressedData);
		BnkContentFileFormat contentFileFormat = BnkContentFileFormat.CreateFromStream(decompressedIndexDataStream, bnkIndexFile.IsBnkContentDataCompressed);

		for (int i = 0; i < contentFileFormat.NumberOfFiles; i++)
		{
			if (contentFileFormat.AllFileEntries[i].FilePath.Equals("art\\videos\\lionhead_logo.bik"))
			{
				Console.WriteLine($"Found Lionhead Logo at file index {i}");
			}
			
			if (contentFileFormat.AllFileEntries[i].FilePath.Equals("art\\videos\\microsoft_logo.bik"))
			{
				Console.WriteLine($"Found Microsoft Logo at file index {i}");
			}
		}
		
		Console.WriteLine("Finished reading BNK Index file");
	}
}
