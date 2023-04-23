// This file is or was originally a part of the Fable III Skip Intro Patcher project, which can be found here: https://github.com/XJDHDR/???
// 
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// 
//  List of this Source Code Form's contributors:
//  - Xavier "XJDHDR" du Hecquet de Rauville
// 

namespace Fable_III_BNK_file_format;

public struct Fable3BnkCompressedFileDataEntries
{
	internal int[] CompressedDataSizes;
	internal int[] NumbersOfChunks;

	internal byte[][] UnknownByteSequences;
}
