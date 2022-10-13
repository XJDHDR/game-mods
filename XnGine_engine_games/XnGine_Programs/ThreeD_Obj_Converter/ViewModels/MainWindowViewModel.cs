using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.Enums;
using ReactiveUI;
using ThreeD_Obj_Converter.Models;

namespace ThreeD_Obj_Converter.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		// ==== Properties ====
		public string Input3DModelTextBox
		{
			get => input3DModelTextBox;
			set => this.RaiseAndSetIfChanged(ref input3DModelTextBox, value);
		}

		public string Input3DModelsFolderTextBox
		{
			get => input3DModelsFolderTextBox;
			set => this.RaiseAndSetIfChanged(ref input3DModelsFolderTextBox, value);
		}

		public string OutputObjModelsFolderTextBox
		{
			get => outputObjModelsFolderTextBox;
			set => this.RaiseAndSetIfChanged(ref outputObjModelsFolderTextBox, value);
		}

		public Window MainWindow =>
			mainWindow ??= (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow!;


		// ==== Fields ====
		private string input3DModelTextBox = string.Empty;
		private string input3DModelsFolderTextBox = string.Empty;
		private string outputObjModelsFolderTextBox = string.Empty;

		private Window? mainWindow;


		// ==== Public methods ====
		public async void Select3dFileToConvert()
		{
			List<FileDialogFilter> fileDialogFilter = new(2)
			{
				new FileDialogFilter
				{
					Extensions = new List<string>(1) { "*.3D" },
					Name = "3D Model File (*.3D)"
				},
				new FileDialogFilter
				{
					Extensions = new List<string>(1) { "*.*" },
					Name = "All Files (*.*)"
				}
			};

			OpenFileDialog fileSelectorFor3dFile = new()
			{
				AllowMultiple = false,
				Directory = Directory.GetCurrentDirectory(),
				Filters = fileDialogFilter,
				Title = "Please select the 3D model you want to convert"
			};

			string[]? dialogResult = await fileSelectorFor3dFile.ShowAsync(MainWindow);

			if (dialogResult == null || dialogResult.Length == 0)
			{
				// No files were selected so return early.
				return;
			}

			if (dialogResult.Length > 1)
			{
				// More than 1 file was selected, somehow. This is not supported so return early.
				IMsBoxWindow<ButtonResult>? messageBox = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("File selection error",
					"More than one file selected\n\n" +
					"You selected more than one file, which is not supported. Please try again, selecting only a single file this time.");
				await messageBox.Show();
				return;
			}

			if (dialogResult[0].Length < 1)
			{
				// The string has no characters in it, somehow. This is not supported so return early.
				IMsBoxWindow<ButtonResult>? messageBox = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("File selection error",
					"Empty file path provided\n\n" +
					"You provided an empty file path, which is not supported. Please try again, selecting a single file this time.");
				await messageBox.Show();
				return;
			}

			string fileName = Path.GetFileName(dialogResult[0]);
			string folderPath = dialogResult[0].Replace(fileName, "").Replace("\\", "/");
			string[] folderPathDirectories = folderPath.Split('/');
			char[] invalidCharacters = Path.GetInvalidPathChars();
			for (int i = 0; i < invalidCharacters.Length; ++i)
			{
				for (int j = 0; j < folderPathDirectories.Length; ++j)
				{
					if (folderPathDirectories[j].Contains(invalidCharacters[i]))
					{
						// Folder path contains an invalid character so return early.
						IMsBoxWindow<ButtonResult>? messageBox = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("File selection error",
							"File path contains invalid characters\n\n" +
							"The specified file path contains at least one folder with a name " +
							$"(name: {folderPathDirectories[j]})which uses characters that are not allowed in the current filesystem " +
							$"(character: {invalidCharacters[i]}). Please rename this folder to remove the invalid characters.");
						await messageBox.Show();
						return;
					}
				}
			}

			invalidCharacters = Path.GetInvalidFileNameChars();
			for (int i = 0; i < invalidCharacters.Length; ++i)
			{
				if (fileName.Contains(invalidCharacters[i]))
				{
					// File name contains an invalid character so return early.
					IMsBoxWindow<ButtonResult>? messageBox = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("File selection error",
						"File name contains invalid characters\n\n" +
						$"The specified file name contains at least one character that is not allowed in the current filesystem (character: {invalidCharacters[i]}). " +
						"Please rename this file to remove the invalid characters.");
					await messageBox.Show();
					return;
				}
			}

			if (!File.Exists(dialogResult[0]))
			{
				// File doesn't exist or can't be read so return early.
				IMsBoxWindow<ButtonResult>? messageBox = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("File selection error",
					"File doesn't exist or can't be read\n\n" +
					"There was an error while checking if the file could be read. This can happen " +
					"by either typing in a path for a file that doesn't exist or there being a permission or other issue that " +
					"prevents the selected file from being read. Please fix this problem then try again.");
				await messageBox.Show();
				return;
			}

			// Passed all of the above checks so file path is valid. Assign it
			Input3DModelTextBox = dialogResult[0];
		}

		public async void Select3dFolderToConvert()
		{
			OpenFolderDialog folderSelectorFor3dFiles = new()
			{
				Directory = Directory.GetCurrentDirectory(),
				Title = "Please select the folder containing the 3D models you want to convert"
			};

			string? dialogResult = await folderSelectorFor3dFiles.ShowAsync(MainWindow);
			if (string.IsNullOrEmpty(dialogResult))
			{
				// No folder was selected so return early.
				return;
			}

			Input3DModelsFolderTextBox = dialogResult;
		}

		public void StartConversion()
		{
			// TODO: Check if input valid before execution
			int positionOfLastPathSeparatorChar = Input3DModelTextBox.LastIndexOf('\\');
			string folderPath = Input3DModelTextBox.Substring(0, positionOfLastPathSeparatorChar);
			string fileName = Input3DModelTextBox.Substring(positionOfLastPathSeparatorChar + 1);
			try
			{
				Convert3DToObj._Convert(folderPath, fileName, OutputObjModelsFolderTextBox);
			}
			catch (Exception e)
			{
				IMsBoxWindow<ButtonResult>? messageBox = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
					"Exception in conversion process", $"An exception occurred in StartConversionMethod:\n{e}");
				messageBox.Show();
			}
		}
	}
}
