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
using Ookii.Dialogs.Wpf;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Fable3SkipIntroPatcher;

internal static class Program
{
	private struct Fable3Paths
	{
		internal string DataFolder;
		internal string IndexFile;
		internal string ContentFile;
	}

	[STAThread]
	private async static Task Main()
	{
		string fable3DataFolderPath = getFable3LevelsBnkLocation();
		if (fable3DataFolderPath == "")
		{
			// User exited the folder selector.
			return;
		}

		Fable3Paths fable3Paths = new()
		{
			DataFolder = fable3DataFolderPath,
			IndexFile = $"{fable3DataFolderPath}/levels.bnk",
			ContentFile = $"{fable3DataFolderPath}/levels.bnk.dat",
		};

		readBnkFilesIntoMemory(fable3Paths, out BnkIndexFileFormat bnkIndexFile, out BnkDecompressedIndexData decompressedIndexData, out BnkContentFileContents bnkContentFile);

		Console.Write("Writing blank videos to memory copies of BNK files ...");
		long startTime = Stopwatch.GetTimestamp();
		BlankBinkVideo.ReplaceAllIntroVideos(ref bnkContentFile, ref decompressedIndexData, bnkIndexFile.IsBnkContentDataCompressed);
		TimeSpan elapsedTime = Stopwatch.GetElapsedTime(startTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();

		writeNewBnkFilesToDisk(fable3Paths, bnkContentFile, decompressedIndexData, bnkIndexFile);

		Console.WriteLine("Finished BNK Intro Video patching");
		Thread.Sleep(5000);
	}

	private static string getFable3LevelsBnkLocation()
	{
		Console.WriteLine("Waiting for user to select Fable III's install location.");

		while (true)
		{
			VistaFolderBrowserDialog dialog = new()
			{
				Description = "Please select Fable III's Data folder.",
				Multiselect = false,
				ShowNewFolderButton = false,
				UseDescriptionForTitle = true
			};

			if (dialog.ShowDialog() != true)
			{
				return "";
			}

			string selectedFolder = dialog.SelectedPath;
			if (File.Exists($"{selectedFolder}/levels.bnk") && File.Exists($"{selectedFolder}/levels.bnk.dat"))
			{
				// User selected the Data folder
				return selectedFolder;
			}
			if (File.Exists($"{selectedFolder}/data/levels.bnk") && File.Exists($"{selectedFolder}/data/levels.bnk.dat"))
			{
				// User selected Fable 3's root folder.
				return $"{selectedFolder}/data";
			}
			if (File.Exists($"{selectedFolder}/Fable 3/data/levels.bnk") && File.Exists($"{selectedFolder}/Fable 3/data/levels.bnk.dat"))
			{
				// User selected the folder Fable 3 was installed to.
				return $"{selectedFolder}/Fable 3/data";
			}

			string message = "levels.bnk and/or levels.bnk.dat were not found in the location you selected. Please select the correct folder.";
			string caption = "Invalid Folder Selected";
//			MessageBox.Show(message, caption, PInvokeHooks.Buttons.OK);

		}
	}

	private static void readBnkFilesIntoMemory(Fable3Paths Fable3Paths, out BnkIndexFileFormat BnkIndexFile, out BnkDecompressedIndexData DecompressedIndexData, out BnkContentFileContents BnkContentFile)
	{
		Console.Write("Reading levels.bnk ...");
		long startTime = Stopwatch.GetTimestamp();
		using (FileStream bnkIndexFileStream = File.OpenRead(Fable3Paths.IndexFile))
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
		using (FileStream bnkContentFileStream = File.OpenRead(Fable3Paths.ContentFile))
		{
			BnkContentFile = BnkContentFileContents.CreateFromStream(bnkContentFileStream, ref DecompressedIndexData, BnkIndexFile.IsBnkContentDataCompressed);
		}
		elapsedTime = Stopwatch.GetElapsedTime(startTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();
	}

	private static void writeNewBnkFilesToDisk(Fable3Paths Fable3Paths, BnkContentFileContents BnkContentFile, BnkDecompressedIndexData DecompressedIndexData, BnkIndexFileFormat BnkIndexFile)
	{
		long startTime;
		TimeSpan elapsedTime;

		string message = "Do you want to make a backup of the original files before proceeding?";
		string caption = "Backup Original Files";

		bool result = PInvokeHooks.ShowMessageBox(message, caption, PInvokeHooks.Buttons.Yes | PInvokeHooks.Buttons.No);
		if (result)
		{
			Console.Write("Backing up original files ...");
			startTime = Stopwatch.GetTimestamp();
			File.Move(Fable3Paths.ContentFile, $"{Fable3Paths.DataFolder}/BACKUP-levels.bnk.dat");
			File.Move(Fable3Paths.IndexFile, $"{Fable3Paths.DataFolder}/BACKUP-levels-backup.bnk");
			elapsedTime = Stopwatch.GetElapsedTime(startTime);
			Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
			Console.WriteLine();
		}

		Console.Write("Writing new levels.bnk.dat to file system ...");
		startTime = Stopwatch.GetTimestamp();
		using (FileStream bnkContentFileStream = File.Create(Fable3Paths.ContentFile))
		{
			BnkContentFile.WriteToStream(bnkContentFileStream);
		}
		elapsedTime = Stopwatch.GetElapsedTime(startTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();

		Console.Write("Writing new levels.bnk to file system ...");
		startTime = Stopwatch.GetTimestamp();
		BnkIndexFileFormat newIndexFile = DecompressedIndexData.CompressAndWriteToIndexFile(BnkIndexFile.IsBnkContentDataCompressed);
		using (FileStream bnkIndexFileStream = File.Create(Fable3Paths.IndexFile))
		{
			newIndexFile.WriteToStream(bnkIndexFileStream);
		}
		elapsedTime = Stopwatch.GetElapsedTime(startTime);
		Console.WriteLine($" done. Took {elapsedTime.Seconds}s {elapsedTime.Milliseconds}.{elapsedTime.Microseconds}ms.");
		Console.WriteLine();
	}
}
