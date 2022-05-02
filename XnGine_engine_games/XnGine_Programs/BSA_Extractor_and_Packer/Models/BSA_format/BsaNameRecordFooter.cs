// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

namespace BSA_Extractor_and_Packer.Models.BSA_format
{
	internal struct BsaNameRecordFooter
	{
		internal NameRecord[] _NameRecord;
		internal NumberRecord[] _NumberRecords;
	}

	internal struct NameRecord
	{
		internal string _RecordName;
		internal bool _IsCompressed;
		internal int _RecordSize;
	}

	internal struct NumberRecord
	{
		internal ushort _RecordId;
		internal bool _IsCompressed;
		internal int _RecordSize;
	}
}
