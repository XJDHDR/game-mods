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


using System;
using System.Diagnostics;
using System.IO;
using Fable3SkipIntroPatcherGui.Models.PatchFable3BnkFiles.FileFormats;

namespace Fable3SkipIntroPatcherGui.Models.PatchFable3BnkFiles;

public sealed class WorkerThread(
	bool BackupOriginalFiles,
	string DataFolderPath,
	string IndexFilePath,
	string ContentFilePath,
	Action<ReadOnlySpan<char>, bool> UpdateStatusLogFunc,
	Action SignalWorkCompletedFunc
)
{
	private readonly Fable3Paths fable3Paths = new()
	{
		DataFolder = DataFolderPath,
		IndexFile = IndexFilePath,
		ContentFile = ContentFilePath,
	};

	private struct Fable3Paths
	{
		internal string DataFolder;
		internal string IndexFile;
		internal string ContentFile;
	}

	public void Run()
	{
		try
		{
			readBnkFilesIntoMemory(out BnkIndexFileFormat bnkIndexFile, out BnkDecompressedIndexData decompressedIndexData, out BnkContentFileContents bnkContentFile);

			UpdateStatusLogFunc("Writing blank videos to memory copies of BNK files ...", false);
			long startTime = Stopwatch.GetTimestamp();
			BlankBinkVideo.ReplaceAllIntroVideos(ref bnkContentFile, ref decompressedIndexData, bnkIndexFile.IsBnkContentDataCompressed);
			TimeSpan elapsedTime = Stopwatch.GetElapsedTime(startTime);
			UpdateStatusLogFunc($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.", true);
			UpdateStatusLogFunc("", true);

			writeNewBnkFilesToDisk(bnkContentFile, decompressedIndexData, bnkIndexFile);

			UpdateStatusLogFunc("Finished BNK Intro Video patching", true);
			SignalWorkCompletedFunc();
		}
		catch (Exception e)
		{
			UpdateStatusLogFunc("", true);
			UpdateStatusLogFunc(e.ToString(), true);
			SignalWorkCompletedFunc();
		}
	}

		private void readBnkFilesIntoMemory(out BnkIndexFileFormat BnkIndexFile, out BnkDecompressedIndexData DecompressedIndexData, out BnkContentFileContents BnkContentFile)
	{
		UpdateStatusLogFunc("Reading levels.bnk ...", false);
		long startTime = Stopwatch.GetTimestamp();
		using (FileStream bnkIndexFileStream = File.OpenRead(fable3Paths.IndexFile))
		{
			BnkIndexFile = new(bnkIndexFileStream);
		}
		TimeSpan elapsedTime = Stopwatch.GetElapsedTime(startTime);
		UpdateStatusLogFunc($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.", true);
		UpdateStatusLogFunc("", true);

		UpdateStatusLogFunc("Decompressing levels.bnk file indices ...", false);
		startTime = Stopwatch.GetTimestamp();
		DecompressedIndexData = new(ref BnkIndexFile);
		elapsedTime = Stopwatch.GetElapsedTime(startTime);
		UpdateStatusLogFunc($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.", true);
		UpdateStatusLogFunc("", true);

		UpdateStatusLogFunc("Reading levels.bnk.dat ...", false);
		startTime = Stopwatch.GetTimestamp();
		using (FileStream bnkContentFileStream = File.OpenRead(fable3Paths.ContentFile))
		{
			BnkContentFile = BnkContentFileContents.CreateFromStream(bnkContentFileStream, ref DecompressedIndexData, BnkIndexFile.IsBnkContentDataCompressed);
		}
		elapsedTime = Stopwatch.GetElapsedTime(startTime);
		UpdateStatusLogFunc($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.", true);
		UpdateStatusLogFunc("", true);
	}

	private void writeNewBnkFilesToDisk(BnkContentFileContents BnkContentFile, BnkDecompressedIndexData DecompressedIndexData, BnkIndexFileFormat BnkIndexFile)
	{
		long startTime;
		TimeSpan elapsedTime;

		if (BackupOriginalFiles)
		{
			UpdateStatusLogFunc("Backing up original files ...", false);
			startTime = Stopwatch.GetTimestamp();
			File.Move(fable3Paths.ContentFile, $"{fable3Paths.DataFolder}/BACKUP-levels.bnk.dat");
			File.Move(fable3Paths.IndexFile, $"{fable3Paths.DataFolder}/BACKUP-levels.bnk");
			elapsedTime = Stopwatch.GetElapsedTime(startTime);
			UpdateStatusLogFunc($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.", true);
			UpdateStatusLogFunc("", true);
		}

		UpdateStatusLogFunc("Writing new levels.bnk.dat to file system ...", false);
		startTime = Stopwatch.GetTimestamp();
		using (FileStream bnkContentFileStream = File.Create(fable3Paths.ContentFile))
		{
			BnkContentFile.WriteToStream(bnkContentFileStream);
		}
		elapsedTime = Stopwatch.GetElapsedTime(startTime);
		UpdateStatusLogFunc($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.", true);
		UpdateStatusLogFunc("", true);

		UpdateStatusLogFunc("Writing new levels.bnk to file system ...", false);
		startTime = Stopwatch.GetTimestamp();
		BnkIndexFileFormat newIndexFile = DecompressedIndexData.CompressAndWriteToIndexFile(BnkIndexFile.IsBnkContentDataCompressed);
		using (FileStream bnkIndexFileStream = File.Create(fable3Paths.IndexFile))
		{
			newIndexFile.WriteToStream(bnkIndexFileStream);
		}
		elapsedTime = Stopwatch.GetElapsedTime(startTime);
		UpdateStatusLogFunc($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.", true);
		UpdateStatusLogFunc("", true);
	}
}
