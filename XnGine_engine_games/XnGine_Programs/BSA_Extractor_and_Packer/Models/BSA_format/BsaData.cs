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
		internal BsaHeader _Header;


		internal BsaData(Stream BsaData)
		{
			using (BinaryReader bsaDataBinaryReader = new(BsaData))
			{
				_Header = new BsaHeader(bsaDataBinaryReader, out bool wasSuccessful);

				if (wasSuccessful)
				{
				}
			}
		}
	}
}
