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
		internal readonly NameRecord[] NameRecords;
		internal readonly NumberRecord[] NumberRecords;

		internal BsaNameRecordFooter(BinaryReader FooterDataBinaryReader, in BsaHeader Header, out bool WasSuccessful)
		{
			switch (Header._BsaType)
			{
				case BsaHeader.BsaType.NameRecord:
					NumberRecords = Array.Empty<NumberRecord>();

					NameRecords = new NameRecord[Header.RecordCount];
					for (int i = 0; i < Header.RecordCount; ++i)
					{
						NameRecords[i] = new NameRecord(FooterDataBinaryReader, out bool endOfStreamReached);

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
					NameRecords = Array.Empty<NameRecord>();

					NumberRecords = new NumberRecord[Header.RecordCount];

					for (int i = 0; i < Header.RecordCount; ++i)
						NumberRecords[i] = new NumberRecord(FooterDataBinaryReader);

					break;

				default:
					NameRecords = Array.Empty<NameRecord>();
					NumberRecords = Array.Empty<NumberRecord>();
					break;
			}

			WasSuccessful = true;
		}
	}

	internal readonly struct NameRecord
	{
		internal readonly string RecordName;
		internal readonly bool IsCompressed;
		internal readonly int RecordSize;

		internal NameRecord(BinaryReader NameRecordBinaryReader, out bool EndOfStreamReached)
		{
			Span<byte> nameBytes = stackalloc byte[12];
			int numBytesRead = NameRecordBinaryReader.BaseStream.Read(nameBytes);
			RecordName = Encoding.ASCII.GetString(nameBytes);

			if (numBytesRead < 12)
			{
				EndOfStreamReached = true;
				IsCompressed = false;
				RecordSize = 0;
				return;
			}

			IsCompressed = (NameRecordBinaryReader.ReadUInt16() != 0);
			RecordSize = NameRecordBinaryReader.ReadInt32();
			EndOfStreamReached = false;
		}
	}

	internal readonly struct NumberRecord
	{
		internal readonly ushort RecordId;
		internal readonly bool IsCompressed;
		internal readonly int RecordSize;

		internal NumberRecord(BinaryReader NumberRecordBinaryReader)
		{
			RecordId = NumberRecordBinaryReader.ReadUInt16();
			IsCompressed = (NumberRecordBinaryReader.ReadUInt16() != 0);
			RecordSize = NumberRecordBinaryReader.ReadInt32();
		}
	}
}
