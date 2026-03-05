
using System;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Fable3SkipIntroPatcherGui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
	public bool ShouldBackupOriginalFiles
	{
		get;
		set
		{
			if (value == field)
			{
				return;
			}

			field = value;
			OnPropertyChanged();
		}
	} = true;

	public string Fable3DataFolderLocation
	{
		get;
		set
		{
			if (value == field)
			{
				return;
			}

			field = value;
			OnPropertyChanged();
		}
	} = "";

	public string LevelsBnkIndexFileLocation
	{
		get;
		set
		{
			if (value == field)
			{
				return;
			}

			field = value;
			OnPropertyChanged();
		}
	} = "";

	public string LevelsBnkContentFileLocation
	{
		get;
		set
		{
			if (value == field)
			{
				return;
			}

			field = value;
			OnPropertyChanged();
		}
	} = "";

	public string JobStatusOutputLogText
	{
		get;
		set
		{
			if (value == field)
			{
				return;
			}

			field = value;
			OnPropertyChanged();
		}
	} = "Ready!";


	private readonly StringBuilder jobStatusOutputLogTextStringBuilder = new();


	[RelayCommand]
	// ReSharper disable once InconsistentNaming
	private void PatchFable3BnkFiles()
	{
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
}
