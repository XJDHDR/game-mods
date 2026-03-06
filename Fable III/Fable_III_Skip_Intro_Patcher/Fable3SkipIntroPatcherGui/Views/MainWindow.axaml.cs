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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;

namespace Fable3SkipIntroPatcherGui.Views;

public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
		Closing += onWindowClosingHandler;
	}

	public async void ChooseFable3DataFolder(object Sender, RoutedEventArgs Args)
	{
		try
		{
			string fable3DataFolder = await getFable3LevelsBnkLocation();
			if (fable3DataFolder == "")
			{
				// User exited the folder selector.
				return;
			}

			Fable3LevelsDataFolderTextBox.Text = fable3DataFolder;
			Fable3LevelsIndexFileLocationTextBox.Text = $"{fable3DataFolder}levels.bnk";
			Fable3LevelsContentFileLocationTextBox.Text = $"{fable3DataFolder}levels.bnk.dat";
		}
		catch (Exception e)
		{
			JobStatusOutputTextBlock.Text = e.ToString();
		}
	}

	private async Task<string> getFable3LevelsBnkLocation()
	{
		while (true)
		{
			IReadOnlyList<IStorageFolder> selectedFolders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = "Open Text File",
				AllowMultiple = false,
			});

			if (selectedFolders.Count == 0)
			{
				return "";
			}

			string folderToCheck = selectedFolders[0].Path.LocalPath;

			if (File.Exists($"{folderToCheck}levels.bnk") && File.Exists($"{folderToCheck}levels.bnk.dat"))
			{
				// User selected the Data folder
				return folderToCheck;
			}

			if (File.Exists($"{folderToCheck}data/levels.bnk") && File.Exists($"{folderToCheck}data/levels.bnk.dat"))
			{
				// User selected Fable 3's root folder.
				return $"{folderToCheck}data{Path.DirectorySeparatorChar}";
			}

			if (File.Exists($"{folderToCheck}Fable 3/data/levels.bnk") && File.Exists($"{folderToCheck}Fable 3/data/levels.bnk.dat"))
			{
				// User selected the folder Fable 3 was installed to.
				return $"{folderToCheck}Fable 3{Path.DirectorySeparatorChar}data{Path.DirectorySeparatorChar}";
			}

			IMsBox<ButtonResult> messageBox = MessageBoxManager.GetMessageBoxStandard(
				"Invalid Folder Selected",
				"levels.bnk and/or levels.bnk.dat were not found in the location you selected. Please select the correct folder.",
				ButtonEnum.OkCancel
			);

			ButtonResult result = await messageBox.ShowAsync();

			if (result != ButtonResult.Ok)
			{
				return "";
			}
		}
	}

	private void onWindowClosingHandler(object? Sender, WindowClosingEventArgs Args)
	{
		if (IsJobRunningCheckBox.IsChecked != true)
		{
			return;
		}

		IMsBox<ButtonResult> messageBox = MessageBoxManager.GetMessageBoxStandard(
			"Background job still running",
			"A background job is still running. Please wait for it to finish before exiting."
		);
		messageBox.ShowAsPopupAsync(this);

		Args.Cancel = true;
	}
}
