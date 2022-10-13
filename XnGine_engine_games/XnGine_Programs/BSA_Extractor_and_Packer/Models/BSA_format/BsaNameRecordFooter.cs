// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System;
using System.IO;
using System.Text;
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.Enums;

namespace BSA_Extractor_and_Packer.Models.BSA_format
{
	internal readonly struct BsaNameRecordFooter
	{
		internal readonly NameRecord[] _NameRecords;
		internal readonly NumberRecord[] _NumberRecords;

		internal BsaNameRecordFooter(BinaryReader FooterDataBinaryReader, in BsaHeader Header, out bool WasSuccessful)
		{
			switch (Header._BsaType)
			{
				case BsaHeader.BsaType.NameRecord:
					_NumberRecords = Array.Empty<NumberRecord>();

					_NameRecords = new NameRecord[Header._RecordCount];
					for (int i = 0; i < Header._RecordCount; ++i)
					{
						_NameRecords[i] = new NameRecord(FooterDataBinaryReader, out bool endOfStreamReached);

						if (endOfStreamReached)
						{
							IMsBoxWindow<ButtonResult>? messageBox = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("End of Stream reached",
								$"The end of the BSA stream was reached while reading record #{i}, before it could read all of " +
								$"the required data to populate the footer data. This could indicate data corruption.");
							messageBox.Show();
							WasSuccessful = false;
							return;
						}
					}

					break;

				case BsaHeader.BsaType.NumberRecord:
					_NameRecords = Array.Empty<NameRecord>();

					_NumberRecords = new NumberRecord[Header._RecordCount];

					for (int i = 0; i < Header._RecordCount; ++i)
						_NumberRecords[i] = new NumberRecord(FooterDataBinaryReader);

					break;

				default:
					_NameRecords = Array.Empty<NameRecord>();
					_NumberRecords = Array.Empty<NumberRecord>();
					break;
			}

			WasSuccessful = true;
		}
	}

	internal readonly struct NameRecord
	{
		internal readonly string _RecordName;
		internal readonly bool _IsCompressed;
		internal readonly int _RecordSize;

		internal NameRecord(BinaryReader NameRecordBinaryReader, out bool EndOfStreamReached)
		{
			Span<byte> nameBytes = stackalloc byte[12];
			int numBytesRead = NameRecordBinaryReader.BaseStream.Read(nameBytes);
			_RecordName = Encoding.ASCII.GetString(nameBytes);

			if (numBytesRead < 12)
			{
				EndOfStreamReached = true;
				_IsCompressed = false;
				_RecordSize = 0;
				return;
			}

			_IsCompressed = (NameRecordBinaryReader.ReadUInt16() != 0);
			_RecordSize = NameRecordBinaryReader.ReadInt32();
			EndOfStreamReached = false;
		}
	}

	internal readonly struct NumberRecord
	{
		internal readonly ushort _RecordId;
		internal readonly bool _IsCompressed;
		internal readonly int _RecordSize;

		internal NumberRecord(BinaryReader NumberRecordBinaryReader)
		{
			_RecordId = NumberRecordBinaryReader.ReadUInt16();
			_IsCompressed = (NumberRecordBinaryReader.ReadUInt16() != 0);
			_RecordSize = NumberRecordBinaryReader.ReadInt32();
		}
	}
}
