// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System;
using System.Diagnostics;
using System.Windows.Input;

namespace ThreeD_Obj_Converter.MVVM
{
	/// <summary>
	/// A command whose sole purpose is to relay its functionality to other
	/// objects by invoking delegates. The default return value for the
	/// CanExecute method is 'true'.
	/// </summary>
	public class RelayCommand : ICommand
	{
		private readonly Predicate<object> canExecute;
		private readonly Action<object> execute;

		public RelayCommand(Predicate<object> CanExecute, Action<object> Execute)
		{
			canExecute = CanExecute;
			execute = Execute;
		}

		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		public bool CanExecute(object Parameter) =>
			canExecute(Parameter);

		public void Execute(object Parameter) =>
			execute(Parameter);
	}
}
