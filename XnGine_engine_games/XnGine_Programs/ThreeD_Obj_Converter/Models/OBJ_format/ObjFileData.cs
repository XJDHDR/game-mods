// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using Vector = System.Numerics.Vector;

namespace ThreeD_Obj_Converter.Models.OBJ_format
{
	internal readonly struct ObjFileData
	{
		/// <summary> Add your comments here, including newlines and the starting # for each line. </summary>
		internal readonly string _HeaderComments;

		internal readonly string[] _InlineCommentStrings;
		internal readonly int[] _InlineCommentStartIndex;

		internal readonly string[] _MaterialLibraryFilenames;

		internal readonly Vector4[] _AllVertices;
		internal readonly Vector3[] _AllVertexTextures;
		internal readonly Vector3[] _AllVertexNormals;
		internal readonly FaceDefinition[] _AllFaces;

		internal readonly GroupDefinition[] _AllGroups;

		internal readonly MaterialDefinition[] _AllMaterials;


		internal ObjFileData(Stream ObjDataStream)
		{
			_HeaderComments = string.Empty;
			_InlineCommentStrings = Array.Empty<string>();
			_InlineCommentStartIndex = Array.Empty<int>();

			using (StreamReader objDataStreamReader = new StreamReader(ObjDataStream))
			{
				// Lists and StringBuilder used for temp storage of arrays and strings that are being constructed
				List<string> inlineCommentStrings = new();
				List<int> inlineCommentStartIndex = new();
				List<string> materialLibraryFilenames = new();
				List<Vector4> allVertices = new();
				List<Vector3> allVertexTextures = new();
				List<Vector3> allVertexNormals = new();
				StringBuilder commonStringsBuilder = new();
				StringBuilder messagesStringBuilder = new();

				// Fields that indicate the reading status for comments
				bool havePassedHeader = false;
				bool isBusyReadingHeader = true;
				bool isBusyReadingInlineComment = false;
				int linesOfObjDataRead = 0;

				char[] whitespaceChars = {' ', '	'};

				string? readString = objDataStreamReader.ReadLine()?.Trim();
				for (int i = 0; i < int.MaxValue; ++i)
				{
					// Check if the end of the stream has been reached.
					if (readString == null)
						break;

					// Check if this is an empty line.
					if (string.IsNullOrWhiteSpace(readString))
						continue;

					// Check if this is a comment
					if (readString.StartsWith("#"))
					{
						// This line is a comment
						if (havePassedHeader)
							isBusyReadingInlineComment = true;

						commonStringsBuilder.Append(readString);
						continue;
					}

					// Since this line is not a comment, was a comment being read previously?
					if (isBusyReadingInlineComment || isBusyReadingHeader)
					{
						if (havePassedHeader)
						{
							inlineCommentStartIndex.Add(linesOfObjDataRead);
							inlineCommentStrings.Add(commonStringsBuilder.ToString());
						}
						else
						{
							_HeaderComments = commonStringsBuilder.ToString();
							isBusyReadingHeader = false;
							havePassedHeader = true;
						}
						isBusyReadingInlineComment = false;
						commonStringsBuilder.Clear();
					}

					++linesOfObjDataRead;
					string[] readSubstrings = readString.Split(
						whitespaceChars, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
						);

					if (readSubstrings.Length < 2)
					{
						messagesStringBuilder.AppendLine($"Error: Line {i} in the OBJ stream does not contain recognised info and will be skipped:");
						messagesStringBuilder.AppendLine($"{readString}");
						messagesStringBuilder.AppendLine();
						continue;
					}

					switch (readSubstrings[0])
					{
						case "mtllib":
							// This line defines a Material resource file
							for (int j = 1; j < readSubstrings.Length; ++j)
							{
								commonStringsBuilder.Append(readSubstrings[j]);
							}
							materialLibraryFilenames.Add(commonStringsBuilder.ToString());
							commonStringsBuilder.Clear();
							break;

						case "f":
							// This line defines a Face
							break;

						case "v":
							// This line defines a Vertex
							if (readSubstrings.Length is not (4 or 5))
							{
								messagesStringBuilder.Append($"Error: Line {i} defining a Vertex in the OBJ stream does not ");
								messagesStringBuilder.AppendLine("have 3 or 4 parts defined, and will be skipped:");
								messagesStringBuilder.AppendLine($"{readString}");
								messagesStringBuilder.AppendLine();
							}

							// Is the 4th parameter defined. If not, insert the default value of 1
							float wParameterValue = (readSubstrings.Length is 5) ?
								float.Parse(readSubstrings[4], NumberStyles.Float, CultureInfo.InvariantCulture) :
								1;

							allVertices.Add(new Vector4(
								float.Parse(readSubstrings[1], NumberStyles.Float, CultureInfo.InvariantCulture),
								float.Parse(readSubstrings[2], NumberStyles.Float, CultureInfo.InvariantCulture),
								float.Parse(readSubstrings[3], NumberStyles.Float, CultureInfo.InvariantCulture),
								wParameterValue
							));
							break;

						case "vn":
							// This line defines a Vertex Normal
							if (readSubstrings.Length is not 4)
							{
								messagesStringBuilder.Append($"Error: Line {i} defining a Vertex Normal in the OBJ stream does not ");
								messagesStringBuilder.AppendLine("have 4 parts defined, and will be skipped:");
								messagesStringBuilder.AppendLine($"{readString}");
								messagesStringBuilder.AppendLine();
							}

							float normalXValue = float.Parse(readSubstrings[1], NumberStyles.Float, CultureInfo.InvariantCulture);
							float normalYValue = float.Parse(readSubstrings[2], NumberStyles.Float, CultureInfo.InvariantCulture);
							float normalZValue = float.Parse(readSubstrings[3], NumberStyles.Float, CultureInfo.InvariantCulture);

							allVertexNormals.Add(new Vector3(normalXValue, normalYValue, normalZValue));
							break;

						case "vt":
							// This line defines a Vertex Texture
							if (readSubstrings.Length is not (2 or 3 or 4))
							{
								messagesStringBuilder.Append($"Error: Line {i} defining a Vertex Texture in the OBJ stream does not ");
								messagesStringBuilder.AppendLine("have between 2 and 4 parts defined, and will be skipped:");
								messagesStringBuilder.AppendLine($"{readString}");
								messagesStringBuilder.AppendLine();
							}

							float textureXValue = float.Parse(readSubstrings[1], NumberStyles.Float, CultureInfo.InvariantCulture);
							float textureYValue = (readSubstrings.Length >= 3) ?
								float.Parse(readSubstrings[2], NumberStyles.Float, CultureInfo.InvariantCulture) :
								0;
							float textureZValue = (readSubstrings.Length == 4) ?
								float.Parse(readSubstrings[3], NumberStyles.Float, CultureInfo.InvariantCulture) :
								0;

							allVertexTextures.Add(new Vector3(textureXValue, textureYValue, textureZValue));
							break;
					}
				}

				_InlineCommentStrings = inlineCommentStrings.ToArray();
				_InlineCommentStartIndex = inlineCommentStartIndex.ToArray();
				_MaterialLibraryFilenames = materialLibraryFilenames.ToArray();
				_AllVertices = allVertices.ToArray();
				_AllVertexTextures = allVertexTextures.ToArray();
				_AllVertexNormals = allVertexNormals.ToArray();

				if (messagesStringBuilder.Length > 0)
					MessageBox.Show(messagesStringBuilder.ToString());
			}
		}


		internal void _Write(Stream OutputStream)
		{
			StringBuilder outputStringBuilder = new();

			outputStringBuilder.AppendLine(_HeaderComments);
			outputStringBuilder.AppendLine();

			for (int i = 0; i < _MaterialLibraryFilenames.Length; ++i)
				outputStringBuilder.AppendLine($"mtllib {_MaterialLibraryFilenames[i]}");

			outputStringBuilder.AppendLine();

			for (int i = 0; i < _AllVertices.Length; ++i)
			{
				// Is the W parameter equal to 1 (default value)? If not, don't include it in the final output stream.
				outputStringBuilder.AppendLine(
					(Math.Abs(_AllVertices[i].W - 1) < 0.00000000001) ?
					$"v {_AllVertices[i].X} {_AllVertices[i].Y} {_AllVertices[i].Z} {_AllVertices[i].W}" :
					$"v {_AllVertices[i].X} {_AllVertices[i].Y} {_AllVertices[i].Z}"
				);
			}
			outputStringBuilder.AppendLine();

			for (int i = 0; i < _AllVertexTextures.Length; ++i)
			{
				// Is the Z parameter equal to 0 (default value)? If not, don't include it in the final output stream.
				if (Math.Abs(_AllVertexTextures[i].Z) > 0.00000000001)
					outputStringBuilder.AppendLine($"vt {_AllVertexTextures[i].X} {_AllVertexTextures[i].Y} {_AllVertexTextures[i].Z}");

				// Is the Y parameter also equal to 0 (default value)? If not, don't include it in the final output stream either.
				else if (Math.Abs(_AllVertexTextures[i].Y) > 0.00000000001)
					outputStringBuilder.AppendLine($"vt {_AllVertexTextures[i].X} {_AllVertexTextures[i].Y}");

				else
					outputStringBuilder.AppendLine($"vt {_AllVertexTextures[i].X}");
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
