// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System.IO;
using System.Windows;

namespace BSA_Extractor_and_Packer.Models.BSA_format
{
	internal struct BsaHeader
	{
		internal ushort _RecordCount;
		internal ushort _BsaType;

		internal BsaHeader(BinaryReader BsaStreamBinaryReader, out bool WasSuccessful)
		{
			_RecordCount = BsaStreamBinaryReader.ReadUInt16();
			_BsaType = BsaStreamBinaryReader.ReadUInt16();

			switch (_BsaType)
			{
				case 0x0100:
				case 0x0200:
					WasSuccessful = true;
					return;

				default:
					MessageBox.Show("The BSA's header was not equal to either 0x0100 or 0x0200. As a result, " +
					                "it's record type can't be determined and reading can't continue.");
					WasSuccessful = false;
					return;
			}
		}
	}
}
