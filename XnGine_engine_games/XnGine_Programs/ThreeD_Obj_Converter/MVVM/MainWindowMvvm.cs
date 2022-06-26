// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace ThreeD_Obj_Converter.MVVM
{
	public class MainWindowMvvm : INotifyPropertyChanged
	{
		// ==== Properties ====
		public string Input3DModelTextBox { get; set; } = string.Empty;
		public string Input3DModelsFolderTextBox { get; set; } = string.Empty;
		public string OutputObjModelsFolderTextBox { get; set; } = string.Empty;

		public ICommand Select3dFileToConvert =>
			// If command hasn't been created yet, create it
			select3dFileToConvert ??= new RelayCommand(
				_ => select3dFileToConvertMethod()
			);


		// ==== Fields ====
		public event PropertyChangedEventHandler? PropertyChanged;

		private ICommand? select3dFileToConvert;


		// ==== Public Methods ====
		private void select3dFileToConvertMethod()
		{
			OpenFileDialog fileSelectorFor3dFile = new()
			{
				Filter = "*.3D",
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
	}
}
