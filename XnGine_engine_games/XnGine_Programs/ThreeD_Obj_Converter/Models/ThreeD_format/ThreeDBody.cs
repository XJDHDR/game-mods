// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Windows.Documents;

namespace ThreeD_Obj_Converter.Models.ThreeD_format
{
	internal readonly struct ThreeDBody
	{
		internal readonly Point[] _PointList;
		internal readonly Plane[] _PlaneList;

		internal ThreeDBody(BinaryReader DataBinaryReader, ThreeDHeader Header)
		{
			// First, check if the Stream's current position is where the header says it should be.
			if (Header._StreamHeaderStartPos + Header._PointListOffset != DataBinaryReader.BaseStream.Position)
				throw new NotImplementedException();	// TODO: Add failure logic

			// Since the Stream position is in the correct place, create the needed arrays.
			_PointList = new Point[Header._PointStructCount];


			for (int i = 0; i < Header._PointStructCount; ++i)
			{
				_PointList[i] = new(DataBinaryReader);
			}


		}
	}

	internal readonly struct Point
	{
		internal readonly int X;
		internal readonly int Y;
		internal readonly int Z;

		internal Point(BinaryReader DataBinaryReader)
		{
			X = DataBinaryReader.ReadInt32();
			Y = DataBinaryReader.ReadInt32();
			Z = DataBinaryReader.ReadInt32();
		}
	}

	internal readonly struct Plane
	{
		internal readonly sbyte _PlanePointCount;
		//internal readonly byte _Unknown1;

		//internal readonly ushort _Texture;
		internal readonly byte _TextureImageIndex;
		internal readonly short _TextureFileIndex;

		//internal readonly uint _Unknown2;
		internal readonly PlanePoint[] _PlanePoints;



		internal readonly struct PlanePoint
		{
			internal readonly int _PointOffset;
			internal readonly short _U;
			internal readonly short _V;
		}
	}
}
