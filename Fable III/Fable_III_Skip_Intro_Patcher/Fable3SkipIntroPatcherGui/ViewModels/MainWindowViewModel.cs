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
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fable3SkipIntroPatcherGui.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;

namespace Fable3SkipIntroPatcherGui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
	[ObservableProperty]
	private bool isJobRunning = false;

	[ObservableProperty]
	private bool allowUserInteractionWithUi = true;

	[ObservableProperty]
	private bool allowUserToStartNewJob = true;

	[ObservableProperty]
	private bool shouldBackupOriginalFiles = true;

	[ObservableProperty]
	private string fable3DataFolderLocation = "";

	[ObservableProperty]
	private string levelsBnkIndexFileLocation = "";

	[ObservableProperty]
	private string levelsBnkContentFileLocation = "";

	[ObservableProperty]
	private string jobStatusOutputLogText = "Ready!";


	private readonly MainWindowModel mainWindowModel =  new();
	private readonly StringBuilder jobStatusOutputLogTextStringBuilder = new();


	[RelayCommand]
	private async Task patchFable3BnkFiles()
	{
		AllowUserToStartNewJob = false;
		AllowUserInteractionWithUi = false;
		jobStatusOutputLogTextStringBuilder.Clear();
		JobStatusOutputLogText = "";

		bool wasThreadLaunched = mainWindowModel.PatchFable3BnkFiles(
			updateStatusOutput, workCompleted, ShouldBackupOriginalFiles, Fable3DataFolderLocation, LevelsBnkIndexFileLocation, LevelsBnkContentFileLocation, out string errorMessage
		);

		AllowUserInteractionWithUi = true;
		IsJobRunning = true;

		if (!wasThreadLaunched)
		{
			IsJobRunning = false;
			IMsBox<ButtonResult> messageBox = MessageBoxManager.GetMessageBoxStandard("Invalid Folder Selected", errorMessage);
			await messageBox.ShowAsync();

			workCompleted();
		}
	}

	private void updateStatusOutput(ReadOnlySpan<char> NewText, bool AppendNewline)
	{
		jobStatusOutputLogTextStringBuilder.Append(NewText);

		if (AppendNewline)
		{
			jobStatusOutputLogTextStringBuilder.Append(Environment.NewLine);
		}

		JobStatusOutputLogText = jobStatusOutputLogTextStringBuilder.ToString();
	}

	private void workCompleted()
	{
		jobStatusOutputLogTextStringBuilder.AppendLine();
		jobStatusOutputLogTextStringBuilder.AppendLine("Ready!");
		JobStatusOutputLogText = jobStatusOutputLogTextStringBuilder.ToString();

		jobStatusOutputLogTextStringBuilder.Clear();

		IsJobRunning = false;
		AllowUserToStartNewJob = true;
	}
}
