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


using System.Runtime.InteropServices;

namespace Fable3SkipIntroPatcher;

public static class PInvokeHooks
{
	public static bool ShowMessageBox(string WindowTitle, string Message, Buttons Buttons)
	{
		int result = TaskDialog(IntPtr.Zero, IntPtr.Zero, WindowTitle, Message, null, Buttons, IntPtr.Zero, out Buttons buttonPressed);
		return (buttonPressed == Buttons.OK);
	}

	[DllImport("comctl32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern int TaskDialog(IntPtr HwndOwner, IntPtr HResourceLocation, string WindowTitle, string Instructions, string? Content, Buttons Buttons, IntPtr Icon, out Buttons ButtonPressed);

	[Flags]
	public enum Buttons : uint
	{
		OK		= 0x1,
		Yes		= 0x2,
		No		= 0x4,
		Cancel	= 0x8
	}
}
