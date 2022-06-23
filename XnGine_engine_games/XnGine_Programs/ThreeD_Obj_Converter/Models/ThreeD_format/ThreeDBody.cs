// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System;
using System.IO;

namespace ThreeD_Obj_Converter.Models.ThreeD_format
{
	internal readonly struct ThreeDBody
	{
		internal readonly Point[] _PointLists;
		internal readonly Plane[] _PlaneLists;
		internal readonly Point[] _NormalLists;
		internal readonly PlaneData[] _PlaneDataList;
		internal readonly ObjectData[] _ObjectDataList;

		internal ThreeDBody(BinaryReader DataBinaryReader, ThreeDHeader Header)
		{
			// First, check if the Stream's current position is where the header says it should be.
			if (DataBinaryReader.BaseStream.Position != Header._StreamHeaderStartPos + Header._PointListOffset)
				throw new NotImplementedException();	// TODO: Add failure logic

			// Since the Stream position is in the correct place, create the needed arrays.
			_PointLists = new Point[Header._PointCount];
			_PlaneLists = new Plane[Header._PlaneCount];
			_NormalLists = new Point[Header._PlaneCount];
			_PlaneDataList = new PlaneData[Header._PlaneCount];
			_ObjectDataList = new ObjectData[Header._ObjectDataCount];

			for (int i = 0; i < Header._PointCount; ++i)
			{
				_PointLists[i] = new Point(DataBinaryReader);
			}

			for (int i = 0; i < Header._PlaneCount; ++i)
			{
				_PlaneLists[i] = new Plane(DataBinaryReader);
			}

			for (int i = 0; i < Header._PlaneCount; ++i)
			{
				_NormalLists[i] = new Point(DataBinaryReader);
			}

			for (int i = 0; i < Header._PlaneCount; ++i)
			{
				_PlaneDataList[i] = new PlaneData(DataBinaryReader);
			}

			for (int i = 0; i < Header._ObjectDataCount; ++i)
			{
				_ObjectDataList[i] = new ObjectData(DataBinaryReader);
			}
		}

		internal ThreeDBody(Point[] PointLists, Plane[] PlaneLists, Point[] NormalLists, PlaneData[] PlaneDataList, ObjectData[] ObjectDataList)
		{
			_PointLists = PointLists;
			_PlaneLists = PlaneLists;
			_NormalLists = NormalLists;
			_PlaneDataList = PlaneDataList;
			_ObjectDataList = ObjectDataList;
		}
	}

	internal readonly struct Point
	{
		internal readonly int _X;
		internal readonly int _Y;
		internal readonly int _Z;

		internal Point(BinaryReader DataBinaryReader)
		{
			_X = DataBinaryReader.ReadInt32();
			_Y = DataBinaryReader.ReadInt32();
			_Z = DataBinaryReader.ReadInt32();
		}

		internal Point(int X, int Y, int Z)
		{
			_X = X;
			_Y = Y;
			_Z = Z;
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


		internal Plane(BinaryReader DataBinaryReader)
		{
			_PlanePointCount = DataBinaryReader.ReadSByte();
			DataBinaryReader.BaseStream.Position += 1;

			ushort texture = DataBinaryReader.ReadUInt16();
			_TextureImageIndex = (byte)(texture & 0x7f);
			_TextureFileIndex = (short)(texture >> 7);

			DataBinaryReader.BaseStream.Position += 4;

			_PlanePoints = new PlanePoint[_PlanePointCount];
			for (int i = 0; i < _PlanePointCount; ++i)
			{
				_PlanePoints[i] = new(DataBinaryReader);
			}
		}

		internal Plane(sbyte PlanePointCount, byte TextureImageIndex,short TextureFileIndex, PlanePoint[] PlanePoints)
		{
			_PlanePointCount = PlanePointCount;
			_TextureImageIndex = TextureImageIndex;
			_TextureFileIndex = TextureFileIndex;
			_PlanePoints = PlanePoints;
		}

		internal readonly struct PlanePoint
		{
			internal readonly int _PointOffset;
			internal readonly short _U;
			internal readonly short _V;

			internal PlanePoint(BinaryReader DataBinaryReader)
			{
				_PointOffset = DataBinaryReader.ReadInt32();
				_U = DataBinaryReader.ReadInt16();
				_V = DataBinaryReader.ReadInt16();
			}

			internal PlanePoint(int PointOffset, short U, short V)
			{
				_PointOffset = PointOffset;
				_U = U;
				_V = V;
			}
		}
	}

	internal readonly struct PlaneData
	{
		internal readonly ulong _Unknown1;
		internal readonly ulong _Unknown2;
		internal readonly ulong _Unknown3;

		internal PlaneData(BinaryReader DataBinaryReader)
		{
			_Unknown1 = DataBinaryReader.ReadUInt64();
			_Unknown2 = DataBinaryReader.ReadUInt64();
			_Unknown3 = DataBinaryReader.ReadUInt64();
		}

		internal PlaneData(ulong Unknown1, ulong Unknown2, ulong Unknown3)
		{
			_Unknown1 = Unknown1;
			_Unknown2 = Unknown2;
			_Unknown3 = Unknown3;
		}
	}

	internal readonly struct ObjectData
	{
		internal readonly int _Unknown1;
		internal readonly int _Unknown2;
		internal readonly int _Unknown3;
		internal readonly int _Unknown4;
		internal readonly short _SubrecordCount;
		internal readonly ObjectDataValue[] _ObjectDataValues;

		internal ObjectData(BinaryReader DataBinaryReader)
		{
			_Unknown1 = DataBinaryReader.ReadInt32();
			_Unknown2 = DataBinaryReader.ReadInt32();
			_Unknown3 = DataBinaryReader.ReadInt32();
			_Unknown4 = DataBinaryReader.ReadInt32();
			_SubrecordCount = DataBinaryReader.ReadInt16();

			if (_SubrecordCount == 0)
			{
				_ObjectDataValues = Array.Empty<ObjectDataValue>();
			}
			else
			{
				_ObjectDataValues = new ObjectDataValue[_SubrecordCount];
				for (int i = 0; i < _SubrecordCount; ++i)
				{
					_ObjectDataValues[i] = new ObjectDataValue(DataBinaryReader);
				}
			}
		}

		internal ObjectData(int Unknown1, int Unknown2, int Unknown3, int Unknown4,
			short SubrecordCount, ObjectDataValue[] ObjectDataValues)
		{
			_Unknown1 = Unknown1;
			_Unknown2 = Unknown2;
			_Unknown3 = Unknown3;
			_Unknown4 = Unknown4;
			_SubrecordCount = SubrecordCount;
			_ObjectDataValues = ObjectDataValues;
		}

		internal readonly struct ObjectDataValue
		{
			internal readonly uint _Unknown1;
			internal readonly ushort _Unknown2;

			internal ObjectDataValue(BinaryReader DataBinaryReader)
			{
				_Unknown1 = DataBinaryReader.ReadUInt32();
				_Unknown2 = DataBinaryReader.ReadUInt16();
			}

			internal ObjectDataValue(uint Unknown1, ushort Unknown2)
			{
				_Unknown1 = Unknown1;
				_Unknown2 = Unknown2;
			}
		}
	}
}
