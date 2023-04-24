// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.Enums;


namespace BSA_Extractor_and_Packer.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
	// ==== Properties ====
	internal string ExtractBsaLocation
	{
		get => extractBsaLocation;
		set =>
			// Update GUI
			extractBsaLocation = value;
	}

	internal string ExtractDataLocation
	{
		get => extractDataLocation;
		set =>
			// Update GUI
			extractDataLocation = value;
	}


	// ==== Fields ====
	private string extractBsaLocation = "";
	private string extractDataLocation = "C:\\";


	// ==== Constructors ====
	public MainWindowViewModel()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		// Get all types in current assembly, and iterate through them.
		Assembly thisAssembly = Assembly.GetExecutingAssembly();
		Type[] allTypesInAssembly = thisAssembly.GetTypes();
		Parallel.For((long) 0, allTypesInAssembly.Length, i =>
		//for (int i = 0; i < allTypesInAssembly.Length; ++i)
		{
			// If the current type is not a class, skip to the next one.
			if (!allTypesInAssembly[i].IsClass)
				//continue;
				return;

			// Get all methods in current class, and iterate through them.
			BindingFlags bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance;
			bindingFlags |= BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
			MethodInfo[] allMethodsInType = allTypesInAssembly[i].GetMethods(bindingFlags);
			for (int j = 0; j < allMethodsInType.Length; ++j)
			{
				// If the current method does not have the AttachToEventInViewModel attribute attached, skip to the next one.
				MethodInfo method = allMethodsInType[j];
				object[] allAttributesOnMethod = method.GetCustomAttributes(typeof(AttachToEventInViewModelAttribute), false);

				if (allAttributesOnMethod.Length == 0)
					continue;

				AttachToEventInViewModelAttribute methodAttribute = (AttachToEventInViewModelAttribute) allAttributesOnMethod[0];
				bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic;
				EventInfo? methodEventInfo = GetType().GetEvent(methodAttribute.EventName, bindingFlags);
				if (methodEventInfo == null)
					continue;

				Type? methodEventType = methodEventInfo.EventHandlerType;
				if (methodEventType == null)
					continue;

				try
				{
					Delegate methodEventHandler;

					if (method.IsStatic)
						methodEventHandler = Delegate.CreateDelegate(methodEventType, method);

					else
					{
						// TODO: Need to find a way to get a reference to the class instance which contains the discovered method.
						// TODO: That or mark AttachToEventInViewModelAttribute as only applying to Static methods.
						object? methodContainingObject = null;

						methodEventHandler = Delegate.CreateDelegate(methodEventType, methodContainingObject, method);
					}

					MethodInfo? methodEventAddFunc = methodEventInfo.GetAddMethod(true);
					if (methodEventAddFunc == null)
						continue;

					methodEventAddFunc.Invoke(this, new object[] {methodEventHandler});

					Debug.WriteLine($"<XJDHDR> Attached {method} to {methodEventInfo} with {methodEventHandler} in {stopwatch.ElapsedMilliseconds}ms.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"<XJDHDR> Exception occurred creating delegate with {methodEventType}, {allTypesInAssembly[i]} and {method} in {stopwatch.ElapsedMilliseconds}ms.\n{e}");
				}
			}
		});

		stopwatch.Stop();
	}


	// ==== Methods ====
	internal async void SelectLocationOfBsaToExtract()
	{
		try
		{
			List<FileDialogFilter> fileDialogFilters = new()
			{
				new() {Name = "Bethesda Softworks Archive (*.bsa)", Extensions = new(){"bsa"}},
				new() {Name = "All Files (*.*)", Extensions = new(){"*"}}
			};

			OpenFileDialog openFileDialog = new()
			{
				AllowMultiple = false,
				Directory = ExtractBsaLocation,
				Filters = fileDialogFilters
			};

			Window mainWindow = getWindowFromReflectionAttachedMethod();
			string[]? selectedFiles = await openFileDialog.ShowAsync(mainWindow);
			if (selectedFiles?.Length == 1)
			{
				ExtractBsaLocation = selectedFiles[0];
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			IMsBoxWindow<ButtonResult>? messageBox = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Exception occurred.",
				"An exception occurred. \n\n" +
				$"{e}");
			await messageBox.Show();
		}
	}

	internal async void SelectLocationToExtractBsaRecordsTo()
	{
		OpenFolderDialog openFolderDialog = new()
		{
			Directory = ExtractDataLocation
		};
		Window mainWindow = getWindowFromReflectionAttachedMethod();
		string? selectedFolder = await openFolderDialog.ShowAsync(mainWindow);

		if (selectedFolder == null)
			return;

		ExtractDataLocation = selectedFolder;
	}


	// ==== Event invocations ====
	public void StartExtractionFromReflectionAttachedMethod() =>
		onStartExtractionFromReflectionAttachedMethod.Invoke();

	private Window getWindowFromReflectionAttachedMethod() =>
		onGetWindowFromReflectionAttachedMethod.Invoke();


	// ==== Events ====
	private event Action onStartExtractionFromReflectionAttachedMethod;

	private event Func<Window> onGetWindowFromReflectionAttachedMethod;
}
