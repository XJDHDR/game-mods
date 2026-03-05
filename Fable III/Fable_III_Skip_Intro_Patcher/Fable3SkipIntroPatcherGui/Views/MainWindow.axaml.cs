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
}
