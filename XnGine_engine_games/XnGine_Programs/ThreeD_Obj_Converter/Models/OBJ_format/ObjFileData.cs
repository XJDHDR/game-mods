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
				List<FaceDefinition> allFaces = new();
				List<MaterialDefinition> allMaterials = new();
				StringBuilder commonStringsBuilder = new();
				StringBuilder messagesStringBuilder = new();

				// Assign an empty array to these fields until the OBJ stream has been completely read.
				_InlineCommentStrings = Array.Empty<string>();
				_InlineCommentStartIndex = Array.Empty<int>();
				_MaterialLibraryFilenames = Array.Empty<string>();
				_AllVertices = Array.Empty<Vector4>();
				_AllVertexTextures = Array.Empty<Vector3>();
				_AllVertexNormals = Array.Empty<Vector3>();
				_AllFaces = Array.Empty<FaceDefinition>();
				_AllMaterials = Array.Empty<MaterialDefinition>();

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
							if (readSubstrings.Length < 4)
							{
								messagesStringBuilder.Append($"Error: Line {i} defining a Face in the OBJ stream has ");
								messagesStringBuilder.AppendLine("less than 3 parts defined, and will be skipped:");
								messagesStringBuilder.AppendLine($"{readString}");
								messagesStringBuilder.AppendLine();
								break;
							}
							allFaces.Add(new FaceDefinition(readSubstrings));
							break;

						case "o":
							break;

						case "usemtl":
							// This line defines a point where all the subsequent faces must use the specified material reference.
							if (readSubstrings.Length is not 2)
							{
								messagesStringBuilder.Append($"Error: Line {i} defining a material reference in the OBJ stream ");
								messagesStringBuilder.AppendLine("does not have 2 parts defined, and will be skipped:");
								messagesStringBuilder.AppendLine($"{readString}");
								messagesStringBuilder.AppendLine();
								break;
							}
							allMaterials.Add(new MaterialDefinition(readSubstrings[1], i));
							break;

						case "v":
							// This line defines a Vertex
							if (readSubstrings.Length is not (4 or 5))
							{
								messagesStringBuilder.Append($"Error: Line {i} defining a Vertex in the OBJ stream does not ");
								messagesStringBuilder.AppendLine("have 3 or 4 parts defined, and will be skipped:");
								messagesStringBuilder.AppendLine($"{readString}");
								messagesStringBuilder.AppendLine();
								break;
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
								break;
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
								break;
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

				// Assign all of the lists created above to the array fields, but check if the lists have anything in them
				// first to prevent excessive heap allocations.
				if (inlineCommentStrings.Count != 0)
					_InlineCommentStrings = inlineCommentStrings.ToArray();

				if (inlineCommentStartIndex.Count != 0)
					_InlineCommentStartIndex = inlineCommentStartIndex.ToArray();

				if (materialLibraryFilenames.Count != 0)
					_MaterialLibraryFilenames = materialLibraryFilenames.ToArray();

				if (allVertices.Count != 0)
					_AllVertices = allVertices.ToArray();

				if (allVertexTextures.Count != 0)
					_AllVertexTextures = allVertexTextures.ToArray();

				if (allVertexNormals.Count != 0)
					_AllVertexNormals = allVertexNormals.ToArray();

				if (allFaces.Count != 0)
					_AllFaces = allFaces.ToArray();

				if (allMaterials.Count != 0)
					_AllMaterials = allMaterials.ToArray();

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

			for (int j = 0; j < _AllFaces[I]._Corners.Length; ++j)
			{
				OutputStringBuilder.Append($"f {_AllFaces[I]._Corners[j]._VertexIndex}");

				if (_AllFaces[I]._Corners[j]._IsVertexTextureUsed)
					OutputStringBuilder.Append(($"/{_AllFaces[I]._Corners[j]._VertexTextureIndex}"));

				else if (_AllFaces[I]._Corners[j]._IsVertexNormalUsed)
					OutputStringBuilder.Append('/');


				if (_AllFaces[I]._Corners[j]._IsVertexNormalUsed)
					OutputStringBuilder.Append($"/{_AllFaces[I]._Corners[j]._VertexNormalIndex}");

				OutputStringBuilder.Append(' ');
			}

			OutputStringBuilder.AppendLine();
			MustAddNewlineBeforeGroupOrMaterialDefinition = true;
		}
	}

	internal readonly struct FaceDefinition
	{
		internal readonly FaceCornerDefinition[] _Corners;

		internal FaceDefinition(string[] FaceStringParts)
		{
			// Are there any empty strings in the array?
			int arrayElemsEmptyCount = 0;
			for (int i = 0; i < FaceStringParts.Length; ++i)
			{
				if (string.IsNullOrWhiteSpace(FaceStringParts[i]))
					++arrayElemsEmptyCount;
			}

			// Did the user pass in the Face definition prefix?
			bool firstPartIsFaceDefinition = FaceStringParts[0].Equals("f", StringComparison.OrdinalIgnoreCase);
			_Corners = firstPartIsFaceDefinition ?
				new FaceCornerDefinition[FaceStringParts.Length - arrayElemsEmptyCount - 1] :
				new FaceCornerDefinition[FaceStringParts.Length - arrayElemsEmptyCount];

			arrayElemsEmptyCount = 0;
			for (int i = 0; i < _Corners.Length; ++i)
			{
				// If this array index has an empty string, skip past it.
				if (string.IsNullOrWhiteSpace(FaceStringParts[i]))
				{
					++arrayElemsEmptyCount;
					continue;
				}

				int currentIndexElement;
				if (firstPartIsFaceDefinition)
					currentIndexElement = i + arrayElemsEmptyCount + 1;

				else
					currentIndexElement = i + arrayElemsEmptyCount;

				_Corners[i] = new FaceCornerDefinition(FaceStringParts[currentIndexElement]);
			}
		}
	}

	internal readonly struct FaceCornerDefinition
	{
		// Don't change any Int into a UInt. The specification allows negative values (offset from end instead of start)
		internal readonly int _VertexIndex;
		internal readonly bool _IsVertexTextureUsed = false;
		internal readonly int _VertexTextureIndex = 0;
		internal readonly bool _IsVertexNormalUsed = false;
		internal readonly int _VertexNormalIndex = 0;

		internal FaceCornerDefinition(string CurrentFaceStringPart)
		{
			string[] indicesInString = CurrentFaceStringPart.Split('/', StringSplitOptions.TrimEntries);

			switch (indicesInString.Length)
			{
				case 0:
					throw new ArgumentException($"The passed in string ({CurrentFaceStringPart}) is empty, which is not allowed.");

				case 1:
					// Only the Vertex parameter is defined.
					_VertexIndex = int.Parse(indicesInString[0], NumberStyles.Integer);
					break;

				case 2:
					// This has the Vertex Texture parameter defined as well.
					_VertexIndex = int.Parse(indicesInString[0], NumberStyles.Integer);
					_IsVertexTextureUsed = true;
					_VertexTextureIndex = int.Parse(indicesInString[1], NumberStyles.Integer);
					break;

				default:
				// More than 3 parts in this string. Only pay attention to the first 3 and ignore the rest.
				// ReSharper disable once RedundantCaseLabel
				case 3:
					// This has the Vertex Normal parameter defined as well, with or without the Texture Normal too.
					_VertexIndex = int.Parse(indicesInString[0], NumberStyles.Integer);
					_IsVertexNormalUsed = true;
					_VertexNormalIndex = int.Parse(indicesInString[2], NumberStyles.Integer);

					if (indicesInString[1].Length > 0)
					{
						_IsVertexTextureUsed = true;
						_VertexTextureIndex = int.Parse(indicesInString[1], NumberStyles.Integer);
					}

					break;
			}
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

		internal MaterialDefinition(string MaterialReference, int StartIndex)
		{
			_MaterialName = MaterialReference;
			_MaterialStartIndex = StartIndex;
		}
	}
}
