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
		BnkDecompressedIndexData decompressedIndexData = new(ref bnkIndexFile);
		endTime = Stopwatch.GetTimestamp();
		elapsedTime = Stopwatch.GetElapsedTime(startTime, endTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();

		Console.Write("Reading levels.bnk.dat ...");
		startTime = Stopwatch.GetTimestamp();
		BnkContentFileContents bnkContentFile;
		using (FileStream bnkContentFileStream = File.OpenRead("D:/Games/Steam/steamapps/common/Fable 3/data/levels.bnk.dat"))
		{
			bnkContentFile = BnkContentFileContents.CreateFromStream(bnkContentFileStream, ref decompressedIndexData, bnkIndexFile.IsBnkContentDataCompressed);
		}
		endTime = Stopwatch.GetTimestamp();
		elapsedTime = Stopwatch.GetElapsedTime(startTime, endTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();

		for (int i = 0; i < decompressedIndexData.NumberOfFiles; i++)
		{
			Console.Write("Writing blank videos to memory copies of BNK files ...");
			startTime = Stopwatch.GetTimestamp();
			if (decompressedIndexData.AllFileEntries[i].FilePath.Equals("art\\videos\\lionhead_logo.bik") || decompressedIndexData.AllFileEntries[i].FilePath.Equals("art\\videos\\microsoft_logo.bik"))
			{
				// i should be 35 for Microsoft video and 38 for Lionhead
				BlankBinkVideo.ReplaceBnkContentFileEntry(i, ref bnkContentFile, ref decompressedIndexData, bnkIndexFile.IsBnkContentDataCompressed);
			}
			endTime = Stopwatch.GetTimestamp();
			elapsedTime = Stopwatch.GetElapsedTime(startTime, endTime);
			Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
			Console.WriteLine();
		}

		bool keepBackups = true;	// TODO: Prompt user for this.
		if (keepBackups)
		{
			Console.Write("Backing up original files ...");
			startTime = Stopwatch.GetTimestamp();
			File.Move("D:/Games/Steam/steamapps/common/Fable 3/data/levels.bnk.dat", "D:/Games/Steam/steamapps/common/Fable 3/data/levels-backup.bnk.dat");
			File.Move("D:/Games/Steam/steamapps/common/Fable 3/data/levels.bnk", "D:/Games/Steam/steamapps/common/Fable 3/data/levels-backup.bnk");
			endTime = Stopwatch.GetTimestamp();
			elapsedTime = Stopwatch.GetElapsedTime(startTime, endTime);
			Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
			Console.WriteLine();
		}

		Console.Write("Writing new levels.bnk.dat to file system ...");
		startTime = Stopwatch.GetTimestamp();
		using (FileStream bnkContentFileStream = File.Create("D:/Games/Steam/steamapps/common/Fable 3/data/levels.bnk.dat"))
		{
			bnkContentFile.WriteToStream(bnkContentFileStream);
		}
		endTime = Stopwatch.GetTimestamp();
		elapsedTime = Stopwatch.GetElapsedTime(startTime, endTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();


		Console.WriteLine("Finished reading BNK Index file");
	}
}
