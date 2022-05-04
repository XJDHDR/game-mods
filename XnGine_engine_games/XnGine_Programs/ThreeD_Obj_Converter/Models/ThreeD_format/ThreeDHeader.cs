// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System;
using System.Globalization;
using System.IO;
using System.Windows;

namespace ThreeD_Obj_Converter.Models.ThreeD_format
{
	internal readonly struct ThreeDHeader
	{
		internal readonly float _Version;
		internal readonly int _PointStructCount;
		internal readonly int _PlaneStructCount;
		internal readonly int _OriginToMostDistantPointRadius;
		//internal readonly ulong _NullBytes1;
		internal readonly int _PlaneDataListOffset;
		internal readonly int _ObjectDataListOffset;
		internal readonly int _ObjectDataListElementCount;
		//internal readonly uint _UnknownData1;
		//internal readonly ulong _NullBytes2;
		internal readonly int _PointListOffset;
		internal readonly int _NormalListOffset;
		//internal readonly uint _UnknownData2;
		internal readonly int _PlaneListOffset;

		internal ThreeDHeader(BinaryReader HeaderBinaryReader, out bool WasSuccessful)
		{
			byte readByte = HeaderBinaryReader.ReadByte();
			if (readByte != 0x76)
			{
				MessageBox.Show("While reading the 3D file's first byte, a value of 0x76 was expected. " +
					$"However, 0x{readByte:x} was read instead. This could indicate either data corruption, or the input " +
					"data is not from a 3D model file.",
					"Error: Unexpected data read from 3D file");
				WasSuccessful = false;
				_Version = 0;
				_PointStructCount = 0;
				_PlaneStructCount = 0;
				_OriginToMostDistantPointRadius = 0;
				_PlaneDataListOffset = 0;
				_ObjectDataListOffset = 0;
				_ObjectDataListElementCount = 0;
				_PointListOffset = 0;
				_NormalListOffset = 0;
				_PlaneListOffset = 0;
				return;
			}

			Span<char> versionString = stackalloc char[3];
			for (int i = 0; i < 3; ++i)
			{
				versionString[i] = HeaderBinaryReader.ReadChar();
			}
			_Version = float.Parse(versionString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture.NumberFormat);

			_PointStructCount = HeaderBinaryReader.ReadInt32();
			_PlaneStructCount = HeaderBinaryReader.ReadInt32();
			_OriginToMostDistantPointRadius = HeaderBinaryReader.ReadInt32();

			HeaderBinaryReader.BaseStream.Position += 8;

			_PlaneDataListOffset = HeaderBinaryReader.ReadInt32();
			_ObjectDataListOffset = HeaderBinaryReader.ReadInt32();
			_ObjectDataListElementCount = HeaderBinaryReader.ReadInt32();

			HeaderBinaryReader.BaseStream.Position += (4 + 8);

			_PointListOffset = HeaderBinaryReader.ReadInt32();
			_NormalListOffset = HeaderBinaryReader.ReadInt32();

			HeaderBinaryReader.BaseStream.Position += 4;

			_PlaneListOffset = HeaderBinaryReader.ReadInt32();

			WasSuccessful = true;
		}
	}
}
