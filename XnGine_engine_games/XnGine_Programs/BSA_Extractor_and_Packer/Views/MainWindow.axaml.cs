// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using Avalonia.Controls;
using BSA_Extractor_and_Packer.ViewModels;

namespace BSA_Extractor_and_Packer.Views
{
	public partial class MainWindow : Window
	{
		private static Window instance = null!;

		public MainWindow()
		{
			instance = this;
			InitializeComponent();
		}

		[AttachToEventInViewModel("onGetWindow", "instance")]
		public static Window GetInstance() =>
			instance;
	}
}
