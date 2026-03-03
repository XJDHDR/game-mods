using System.Diagnostics;
using Fable3SkipIntroPatcher.FileFormats;

namespace Fable3SkipIntroPatcher;

internal static class Program
{
	private static void Main()
	{
		readBnkFilesIntoMemory(out BnkIndexFileFormat bnkIndexFile, out BnkDecompressedIndexData decompressedIndexData, out BnkContentFileContents bnkContentFile);

		Console.Write("Writing blank videos to memory copies of BNK files ...");
		long startTime = Stopwatch.GetTimestamp();
		for (int i = 0; i < decompressedIndexData.NumberOfFiles; i++)
		{
			if (decompressedIndexData.AllFileEntries[i].FilePath.Equals("art\\videos\\lionhead_logo.bik") || decompressedIndexData.AllFileEntries[i].FilePath.Equals("art\\videos\\microsoft_logo.bik"))
			{
				// i should be 35 for the Microsoft video and 38 for Lionhead
				BlankBinkVideo.ReplaceBnkContentFileEntry(i, ref bnkContentFile, ref decompressedIndexData, bnkIndexFile.IsBnkContentDataCompressed);
			}
		}
		TimeSpan elapsedTime = Stopwatch.GetElapsedTime(startTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();

		bool keepBackups = false;	// TODO: Prompt user for this.
		if (keepBackups)
		{
			Console.Write("Backing up original files ...");
			startTime = Stopwatch.GetTimestamp();
			File.Move("D:/Games/Steam/steamapps/common/Fable 3/data/levels.bnk.dat", "D:/Games/Steam/steamapps/common/Fable 3/data/levels-backup.bnk.dat");
			File.Move("D:/Games/Steam/steamapps/common/Fable 3/data/levels.bnk", "D:/Games/Steam/steamapps/common/Fable 3/data/levels-backup.bnk");
			elapsedTime = Stopwatch.GetElapsedTime(startTime);
			Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
			Console.WriteLine();
		}

		Console.Write("Writing new levels.bnk.dat to file system ...");
		startTime = Stopwatch.GetTimestamp();
		using (FileStream bnkContentFileStream = File.Create("D:/Games/Steam/steamapps/common/Fable 3/data/levels-new.bnk.dat"))
		{
			bnkContentFile.WriteToStream(bnkContentFileStream);
		}
		elapsedTime = Stopwatch.GetElapsedTime(startTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();

		Console.Write("Writing new levels.bnk to file system ...");
		startTime = Stopwatch.GetTimestamp();
		BnkIndexFileFormat newIndexFile = decompressedIndexData.CompressAndWriteToIndexFile(bnkIndexFile.IsBnkContentDataCompressed);
		using (FileStream bnkIndexFileStream = File.Create("D:/Games/Steam/steamapps/common/Fable 3/data/levels-new.bnk"))
		{
			newIndexFile.WriteToStream(bnkIndexFileStream);
		}
		elapsedTime = Stopwatch.GetElapsedTime(startTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();

		Console.WriteLine("Finished reading BNK Index file");
	}

	private static void readBnkFilesIntoMemory(out BnkIndexFileFormat BnkIndexFile, out BnkDecompressedIndexData DecompressedIndexData, out BnkContentFileContents BnkContentFile)
	{
		Console.Write("Reading levels.bnk ...");
		long startTime = Stopwatch.GetTimestamp();
		using (FileStream bnkIndexFileStream = File.OpenRead("D:/Games/Steam/steamapps/common/Fable 3/data/levels.bnk"))
		{
			BnkIndexFile = new(bnkIndexFileStream);
		}
		TimeSpan elapsedTime = Stopwatch.GetElapsedTime(startTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();

		Console.Write("Decompressing levels.bnk file indices ...");
		startTime = Stopwatch.GetTimestamp();
		DecompressedIndexData = new(ref BnkIndexFile);
		elapsedTime = Stopwatch.GetElapsedTime(startTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();

		Console.Write("Reading levels.bnk.dat ...");
		startTime = Stopwatch.GetTimestamp();
		using (FileStream bnkContentFileStream = File.OpenRead("D:/Games/Steam/steamapps/common/Fable 3/data/levels.bnk.dat"))
		{
			BnkContentFile = BnkContentFileContents.CreateFromStream(bnkContentFileStream, ref DecompressedIndexData, BnkIndexFile.IsBnkContentDataCompressed);
		}
		elapsedTime = Stopwatch.GetElapsedTime(startTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();
	}
}
