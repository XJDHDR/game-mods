// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using ThreeD_Obj_Converter.Models;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace ThreeD_Obj_Converter.MVVM
{
	public class MainWindowMvvm : INotifyPropertyChanged
	{
		// ==== Properties ====
		public string Input3DModelTextBox
		{
			get => input3DModelTextBox;
			set
			{
				if (value == input3DModelTextBox)
					return;

				input3DModelTextBox = value;
				raisePropertyChanged(nameof(Input3DModelTextBox));
			}
		}

		public string Input3DModelsFolderTextBox
		{
			get => input3DModelsFolderTextBox;
			set
			{
				if (value == input3DModelsFolderTextBox)
					return;

				input3DModelsFolderTextBox = value;
				raisePropertyChanged(nameof(Input3DModelsFolderTextBox));
			}
		}

		public string OutputObjModelsFolderTextBox
		{
			get => outputObjModelsFolderTextBox;
			set
			{
				if (value == outputObjModelsFolderTextBox)
					return;

				outputObjModelsFolderTextBox = value;
				raisePropertyChanged(nameof(OutputObjModelsFolderTextBox));
			}
		}

		public ICommand Select3dFileToConvert =>
			// If command hasn't been created yet, create it
			select3dFileToConvert ??= new RelayCommand(
				_ => true,
				_ => select3dFileToConvertMethod()
				);

		public ICommand Select3dFolderToConvert =>
			// If command hasn't been created yet, create it
			select3dFolderToConvert ??= new RelayCommand(
				_ => true,
				_ => select3dFolderToConvertMethod()
				);

		public ICommand StartConversion =>
			startConversion ??= new RelayCommand(
				_ => true,
				_ => startConversionMethod()
			);


		// ==== Fields ====
		public event PropertyChangedEventHandler? PropertyChanged;

		private string input3DModelTextBox = string.Empty;
		private string input3DModelsFolderTextBox = string.Empty;
		private string outputObjModelsFolderTextBox = string.Empty;

		private ICommand? select3dFileToConvert;
		private ICommand? select3dFolderToConvert;
		private ICommand? startConversion;


		// ==== Public Methods ====
		private void raisePropertyChanged(string Property) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Property));

		private void select3dFileToConvertMethod()
		{
			OpenFileDialog fileSelectorFor3dFile = new()
			{
				Filter = "3D Model File (*.3D) | *.3D | All Files (*.*) | *.*",
				Title = "Please select the 3D model you want to convert",
				CheckFileExists = true,
				CheckPathExists = true
			};

			bool? userClickedOk = fileSelectorFor3dFile.ShowDialog();
			if (userClickedOk == true)
			{
				Input3DModelTextBox = fileSelectorFor3dFile.FileName;
			}
		}

		private void select3dFolderToConvertMethod()
		{
			FolderBrowserDialog folderSelectorFor3dFiles = new()
			{
				InitialDirectory = Directory.GetCurrentDirectory()
			};

			DialogResult dialogResult = folderSelectorFor3dFiles.ShowDialog();
			if (dialogResult == DialogResult.OK)
			{
				Input3DModelsFolderTextBox = folderSelectorFor3dFiles.SelectedPath;
			}
		}

		private void startConversionMethod()
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
				MessageBox.Show($"An exception occurred in StartConversionMethod:\n{e}");
			}
		}
	}
}
