// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System.IO;

namespace BSA_Extractor_and_Packer.Models.BSA_format
{
	internal struct BsaData
	{
		internal BsaHeader Header;
		internal BsaNameRecordFooter AllNameRecords;

		internal BsaData(Stream BsaData, out bool WasSuccessful)
		{
			using (BinaryReader bsaDataBinaryReader = new(BsaData))
			{
				Header = new BsaHeader(bsaDataBinaryReader, out bool wasSuccessful);

				if (!wasSuccessful)
				{
					WasSuccessful = false;
					AllNameRecords = new BsaNameRecordFooter();
					return;
				}

				// Need to move the stream reading position to the start of the footer.
				int footerStartPosition;
				switch (Header._BsaType)
				{
					case BsaHeader.BsaType.NameRecord:
						// Each Name Record is exactly 18 bytes in size. Position the stream pointer at (NumRecords * 18) bytes from end.
						footerStartPosition = (int)bsaDataBinaryReader.BaseStream.Length - (18 * Header.RecordCount);
						bsaDataBinaryReader.BaseStream.Position = footerStartPosition;
						AllNameRecords = new BsaNameRecordFooter(bsaDataBinaryReader, in Header, out wasSuccessful);
						break;

					case BsaHeader.BsaType.NumberRecord:
						// Each Name Record is exactly 8 bytes in size. Position the stream pointer at (NumRecords * 8) bytes from end.
						footerStartPosition = (int)bsaDataBinaryReader.BaseStream.Length - (8 * Header.RecordCount);
						bsaDataBinaryReader.BaseStream.Position = footerStartPosition;
						AllNameRecords = new BsaNameRecordFooter(bsaDataBinaryReader, in Header, out wasSuccessful);
						break;

					default:
						AllNameRecords = new BsaNameRecordFooter();
						WasSuccessful = false;
						return;
				}

				if (!wasSuccessful)
				{
					WasSuccessful = false;
					return;
				}

				// Position stream reader pointer at the 4th byte to start reading the first record
				bsaDataBinaryReader.BaseStream.Position = 4;
				// if footerStartPosition
			}

			WasSuccessful = true;
		}
	}
}
