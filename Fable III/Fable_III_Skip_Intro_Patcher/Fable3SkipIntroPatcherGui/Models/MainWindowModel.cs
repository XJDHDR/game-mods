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
using System.IO;
using System.Threading;
using Fable3SkipIntroPatcherGui.Models.PatchFable3BnkFiles;

namespace Fable3SkipIntroPatcherGui.Models;

public sealed class MainWindowModel
{
	public bool PatchFable3BnkFiles(
		Action<ReadOnlySpan<char>, bool> UodateStatusLogFunc, Action SignalWorkCompletedFunc, bool BackupOriginalFiles,
		string Fable3DataFolderPath, string LevelsBnkIndexFilePath, string LevelsBnkContentFilePath, out string ErrorMessage
	)
	{
		if (!File.Exists(LevelsBnkIndexFilePath))
		{
			ErrorMessage = "levels.bnk was not found in the location you selected. Please select the correct folder.";
			return false;
		}

		if (!File.Exists(LevelsBnkContentFilePath))
		{
			ErrorMessage = "levels.bnk.dat was not found in the location you selected. Please select the correct folder.";
			return false;
		}

		WorkerThread workerThread = new(BackupOriginalFiles, Fable3DataFolderPath, LevelsBnkIndexFilePath, LevelsBnkContentFilePath, UodateStatusLogFunc, SignalWorkCompletedFunc);
		Thread thread = new(workerThread.Run);
		thread.Start();

		ErrorMessage = "";
		return true;
	}
}
