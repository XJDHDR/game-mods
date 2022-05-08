// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System;
using System.IO;
using System.Numerics;
using System.Text;

namespace ThreeD_Obj_Converter.Models.OBJ_format
{
	internal readonly struct ObjFileData
	{
		/// <summary> Add your comments here, including newlines and the starting # for each line. </summary>
		internal readonly string _HeaderComments;
		internal readonly string _MaterialLibraryFilename;

		internal readonly Vector4[] _AllVertices;
		internal readonly Vector3[] _AllVertexTextures;
		internal readonly Vector3[] _AllVertexNormals;
		internal readonly FaceDefinition[] _AllFaces;

		internal readonly GroupDefinition[] _AllGroups;

		internal readonly MaterialDefinition[] _AllMaterials;


		internal ObjFileData(Stream ObjDataStream)
		{
			using (StreamReader objDataStreamReader = new StreamReader(ObjDataStream))
			{
				string? readString = string.Empty;
				while ((readString = objDataStreamReader.ReadLine()) != null)
				{
					if (string.IsNullOrWhiteSpace(readString))
						continue;

					if (readString.StartsWith("#"))
					{
						// This line is a comment
					}
				}
			}
		}


		internal void _Write(Stream OutputStream)
		{
			StringBuilder outputStringBuilder = new();

			outputStringBuilder.AppendLine(_HeaderComments);
			outputStringBuilder.AppendLine();

			outputStringBuilder.AppendLine($"mtllib {_MaterialLibraryFilename}");
			outputStringBuilder.AppendLine();

			for (int i = 0; i < _AllVertices.Length; ++i)
			{
				outputStringBuilder.AppendLine($"v {_AllVertices[i].X} {_AllVertices[i].Y} {_AllVertices[i].Z} {_AllVertices[i].W}");
			}
			outputStringBuilder.AppendLine();

			for (int i = 0; i < _AllVertexTextures.Length; ++i)
			{
				outputStringBuilder.AppendLine($"vt {_AllVertexTextures[i].X} {_AllVertexTextures[i].Y} {_AllVertexTextures[i].Z}");
			}
			outputStringBuilder.AppendLine();

			for (int i = 0; i < _AllVertexNormals.Length; ++i)
			{
				outputStringBuilder.AppendLine($"vn {_AllVertexNormals[i].X} {_AllVertexNormals[i].Y} {_AllVertexNormals[i].Z}");
			}
			outputStringBuilder.AppendLine();

			bool mustAddNewlineBeforeGroupOrMaterialDefinition = false;
			int nextGroup = 0;
			int nextMaterial = 0;
			for (int i = 0; i < _AllFaces.Length; ++i)
			{
				writeIndividualFace(i, outputStringBuilder, ref nextGroup, ref nextMaterial, ref mustAddNewlineBeforeGroupOrMaterialDefinition);
			}
			outputStringBuilder.AppendLine();
		}

		private void writeIndividualFace(int I, StringBuilder OutputStringBuilder, ref int NextGroup,
			ref int NextMaterial, ref bool MustAddNewlineBeforeGroupOrMaterialDefinition)
		{
			if (NextGroup < _AllGroups.Length && I >= _AllGroups[NextGroup]._GroupStartIndex)
			{
				if (MustAddNewlineBeforeGroupOrMaterialDefinition)
				{
					OutputStringBuilder.AppendLine();
					MustAddNewlineBeforeGroupOrMaterialDefinition = false;
				}

				OutputStringBuilder.AppendLine($"g {_AllGroups[NextGroup]._GroupName}");
				++NextGroup;
			}

			if (NextMaterial < _AllMaterials.Length && I >= _AllMaterials[NextMaterial]._MaterialStartIndex)
			{
				if (MustAddNewlineBeforeGroupOrMaterialDefinition)
				{
					OutputStringBuilder.AppendLine();
				}

				OutputStringBuilder.AppendLine($"usemtl {_AllMaterials[NextMaterial]._MaterialName}");
				++NextMaterial;
			}

			if (_AllFaces[I]._Corner1._IsUsed)
			{
				OutputStringBuilder.Append(
					$"f {_AllFaces[I]._Corner1._VertexIndex}/{_AllFaces[I]._Corner1._VertexTextureIndex}");

				if (_AllFaces[I]._Corner1._IsVertexNormalUsed)
					OutputStringBuilder.Append($"/{_AllFaces[I]._Corner1._VertexNormalIndex}");

				if (_AllFaces[I]._Corner2._IsUsed)
				{
					OutputStringBuilder.Append(
						$" {_AllFaces[I]._Corner2._VertexIndex}/{_AllFaces[I]._Corner2._VertexTextureIndex}");

					if (_AllFaces[I]._Corner2._IsVertexNormalUsed)
						OutputStringBuilder.Append($"/{_AllFaces[I]._Corner2._VertexNormalIndex}");

					if (_AllFaces[I]._Corner3._IsUsed)
					{
						OutputStringBuilder.Append(
							$" {_AllFaces[I]._Corner3._VertexIndex}/{_AllFaces[I]._Corner3._VertexTextureIndex}");

						if (_AllFaces[I]._Corner3._IsVertexNormalUsed)
							OutputStringBuilder.Append($"/{_AllFaces[I]._Corner3._VertexNormalIndex}");

						if (_AllFaces[I]._Corner4._IsUsed)
						{
							OutputStringBuilder.Append(
								$" {_AllFaces[I]._Corner4._VertexIndex}/{_AllFaces[I]._Corner4._VertexTextureIndex}");

							if (_AllFaces[I]._Corner4._IsVertexNormalUsed)
								OutputStringBuilder.Append($"/{_AllFaces[I]._Corner4._VertexNormalIndex}");
						}
					}
				}
			}

			OutputStringBuilder.AppendLine();
			MustAddNewlineBeforeGroupOrMaterialDefinition = true;
		}
	}

	internal readonly struct FaceDefinition
	{
		internal readonly FaceCornerDefinition _Corner1;
		internal readonly FaceCornerDefinition _Corner2;
		internal readonly FaceCornerDefinition _Corner3;
		internal readonly FaceCornerDefinition _Corner4;
	}

	internal readonly struct FaceCornerDefinition
	{
		internal readonly bool _IsUsed = false;
		internal readonly uint _VertexIndex = 0;
		internal readonly uint _VertexTextureIndex = 0;
		internal readonly bool _IsVertexNormalUsed = false;
		internal readonly uint _VertexNormalIndex = 0;

		internal FaceCornerDefinition(BinaryReader FaceCornerBinaryReader)
		{
			_IsUsed = true;
		}
	}

	internal readonly struct GroupDefinition
	{
		internal readonly string _GroupName;
		internal readonly int _GroupStartIndex;
	}

	internal readonly struct MaterialDefinition
	{
		internal readonly string _MaterialName;
		internal readonly int _MaterialStartIndex;
	}
}
