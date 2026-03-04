// This file is or was originally a part of the Fable III Skip Intro Patcher project, which can be found here: https://github.com/XJDHDR/game-mods/blob/master/Fable%20III/Fable_III_Skip_Intro_Patcher/License.txt
//
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
//
// This Source Code Form is "Incompatible With Secondary Licenses", as
// defined by the Mozilla Public License, v. 2.0.
//
//  List of this Source Code Form's contributors:
//  - Xavier "XJDHDR" du Hecquet de Rauville
//


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
		BlankBinkVideo.ReplaceAllIntroVideos(ref bnkContentFile, ref decompressedIndexData, bnkIndexFile.IsBnkContentDataCompressed);
		TimeSpan elapsedTime = Stopwatch.GetElapsedTime(startTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();

		writeNewBnkFilesToDisk(bnkContentFile, decompressedIndexData, bnkIndexFile);

		Console.WriteLine("Finished BNK Intro Video patching");
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

	private static void writeNewBnkFilesToDisk(BnkContentFileContents BnkContentFile, BnkDecompressedIndexData DecompressedIndexData, BnkIndexFileFormat BnkIndexFile)
	{
		long startTime;
		TimeSpan elapsedTime;
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
			BnkContentFile.WriteToStream(bnkContentFileStream);
		}
		elapsedTime = Stopwatch.GetElapsedTime(startTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();

		Console.Write("Writing new levels.bnk to file system ...");
		startTime = Stopwatch.GetTimestamp();
		BnkIndexFileFormat newIndexFile = DecompressedIndexData.CompressAndWriteToIndexFile(BnkIndexFile.IsBnkContentDataCompressed);
		using (FileStream bnkIndexFileStream = File.Create("D:/Games/Steam/steamapps/common/Fable 3/data/levels-new.bnk"))
		{
			newIndexFile.WriteToStream(bnkIndexFileStream);
		}
		elapsedTime = Stopwatch.GetElapsedTime(startTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();
	}
}
