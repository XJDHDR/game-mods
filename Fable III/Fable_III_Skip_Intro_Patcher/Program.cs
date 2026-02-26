using System.Diagnostics;
using Fable3SkipIntroPatcher.FileFormats;

namespace Fable3SkipIntroPatcher;

class Program
{
	private static void Main()
	{
		Console.Write("Reading levels.bnk ...");
		long startTime = Stopwatch.GetTimestamp();
		BnkIndexFileFormat bnkIndexFile;
		using (FileStream bnkIndexFileStream = File.OpenRead("D:/Games/Steam/steamapps/common/Fable 3/data/levels.bnk"))
		{
			bnkIndexFile = new(bnkIndexFileStream);
		}
		long endTime = Stopwatch.GetTimestamp();
		TimeSpan elapsedTime = Stopwatch.GetElapsedTime(startTime, endTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();

		Console.Write("Decompressing levels.bnk file indices ...");
		startTime = Stopwatch.GetTimestamp();
		BnkDecompressedIndexData decompressedIndexData = BnkDecompressedIndexData.CreateFromIndexFileData(ref bnkIndexFile);
		endTime = Stopwatch.GetTimestamp();
		elapsedTime = Stopwatch.GetElapsedTime(startTime, endTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();

		Console.Write("Reading levels.bnk.dat ...");
		startTime = Stopwatch.GetTimestamp();
		FileStream bnkContentFileStream = File.OpenRead("D:/Games/Steam/steamapps/common/Fable 3/data/levels.bnk.dat");
		BnkContentFileContents bnkContentFile = BnkContentFileContents.CreateFromStream(bnkContentFileStream, ref decompressedIndexData, bnkIndexFile.IsBnkContentDataCompressed);
		bnkContentFileStream.Close();
		endTime = Stopwatch.GetTimestamp();
		elapsedTime = Stopwatch.GetElapsedTime(startTime, endTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();

		for (int i = 0; i < decompressedIndexData.NumberOfFiles; i++)
		{
			if (decompressedIndexData.AllFileEntries[i].FilePath.Equals("art\\videos\\lionhead_logo.bik") || decompressedIndexData.AllFileEntries[i].FilePath.Equals("art\\videos\\microsoft_logo.bik"))
			{
				// i should be 35 for Microsoft video and 38 for Lionhead
				BlankBinkVideo.ReplaceBnkContentFileEntry(i, ref bnkContentFile, ref decompressedIndexData, bnkIndexFile.IsBnkContentDataCompressed);
			}
		}

		Console.WriteLine("Finished reading BNK Index file");
	}
}
