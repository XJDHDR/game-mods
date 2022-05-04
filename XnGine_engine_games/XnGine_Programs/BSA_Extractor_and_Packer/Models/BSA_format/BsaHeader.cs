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
	internal readonly struct BsaHeader
	{
		internal readonly ushort _RecordCount;
		internal readonly BsaType _BsaType;

		internal BsaHeader(BinaryReader BsaStreamBinaryReader, out bool WasSuccessful)
		{
			// First two bytes are the number of records stored in the BSA.
			_RecordCount = BsaStreamBinaryReader.ReadUInt16();

			// Next two bytes define the BSA type. All this really does is determine how many bytes the
			// record's "name" takes up in the footer.
			// A Name record's name is 12 bytes in length, consisting of up to 12 ASCII characters.
			// A Number record's name is 2 bytes in length, consisting of the string representation of a UInt16.
			ushort bsaTypeDataRead = BsaStreamBinaryReader.ReadUInt16();
			switch (bsaTypeDataRead)
			{
				case 0x0100:
					_BsaType = BsaType.NameRecord;
					WasSuccessful = true;
					return;

				case 0x0200:
					_BsaType = BsaType.NumberRecord;
					WasSuccessful = true;
					return;

				default:
					MessageBox.Show("While reading the file's BSA Type, a value of either 0x0100 or 0x0200 was expected. " +
						$"{bsaTypeDataRead:x} was read instead. As a result, it's record type can't be determined and reading can't continue.");
					_BsaType = BsaType.Unknown;
					WasSuccessful = false;
					return;
			}
		}

		internal enum BsaType : ushort
		{
			Unknown = 0,
			NameRecord = 0x0100,
			NumberRecord = 0x0200
		}
	}
}
