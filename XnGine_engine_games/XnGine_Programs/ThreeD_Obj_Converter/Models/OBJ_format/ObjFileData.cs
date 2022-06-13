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
using System.Numerics;
using System.Text;
using System.Windows;

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

		internal readonly ObjectDefinition[] _AllObjects;

		internal readonly GroupDefinition[] _AllGroups;

		internal readonly MaterialDefinition[] _AllMaterials;

		internal readonly SmoothingGroup[] _AllSmoothingGroups;


		internal ObjFileData(Stream ObjDataStream)
		{
			_HeaderComments = string.Empty;
			// Assign an empty array to these fields until the OBJ stream has been completely read.
			_InlineCommentStrings = Array.Empty<string>();
			_InlineCommentStartIndex = Array.Empty<int>();
			_MaterialLibraryFilenames = Array.Empty<string>();
			_AllVertices = Array.Empty<Vector4>();
			_AllVertexTextures = Array.Empty<Vector3>();
			_AllVertexNormals = Array.Empty<Vector3>();
			_AllFaces = Array.Empty<FaceDefinition>();
			_AllGroups = Array.Empty<GroupDefinition>();
			_AllObjects = Array.Empty<ObjectDefinition>();
			_AllMaterials = Array.Empty<MaterialDefinition>();
			_AllSmoothingGroups = Array.Empty<SmoothingGroup>();

			using (StreamReader objDataStreamReader = new(ObjDataStream))
			{
				// Lists and StringBuilder used for temp storage of arrays and strings that are being constructed
				List<string> inlineCommentStrings = new();
				List<int> inlineCommentStartIndex = new();
				List<string> materialLibraryFilenames = new();
				List<Vector4> allVertices = new();
				List<Vector3> allVertexTextures = new();
				List<Vector3> allVertexNormals = new();
				List<FaceDefinition> allFaces = new();
				List<GroupDefinition> allGroups = new();
				List<ObjectDefinition> allObjects = new();
				List<MaterialDefinition> allMaterials = new();
				List<SmoothingGroup> allSmoothingGroups = new();
				StringBuilder commonStringsBuilder = new();
				StringBuilder messagesStringBuilder = new();

				// Fields that indicate the reading status for comments
				bool havePassedHeader = false;
				bool isBusyReadingHeader = true;
				bool isBusyReadingInlineComment = false;
				int linesOfObjDataRead = 0;

				char[] whitespaceChars = {' ', '	'};

				for (int i = 1; i < int.MaxValue; ++i)
				{
					string? readString = objDataStreamReader.ReadLine()?.Trim();

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

					// Every line in OBJ data that is not a comment or empty line has at least two parts: The data element declaration,
					// and the data associated with that element. If there is less than this, the read data is not valid.
					// Thus, trigger an error and skip to the next line.
					if (readSubstrings.Length < 2)
					{
						messagesStringBuilder.AppendLine($"Error: Line {i} in the OBJ stream does not contain recognised info and will be skipped:");
						messagesStringBuilder.AppendLine($"{readString}");
						messagesStringBuilder.AppendLine();
						continue;
					}

					readSubstrings[0] = readSubstrings[0].ToLower();
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

						case "g":
							// This line defines a point where all the subsequent entries are part of the same group.
							if (readSubstrings.Length < 2)
							{
								messagesStringBuilder.Append($"Error: Line {i} defining a group in the OBJ stream has ");
								messagesStringBuilder.AppendLine("less than 2 parts defined, and will be skipped:");
								messagesStringBuilder.AppendLine($"{readString}");
								messagesStringBuilder.AppendLine();
								break;
							}
							for (int j = 1; j < readSubstrings.Length; ++j)
							{
								commonStringsBuilder.Append(readSubstrings[j]);
							}
							allGroups.Add(new GroupDefinition(commonStringsBuilder.ToString(), linesOfObjDataRead));
							commonStringsBuilder.Clear();
							break;

						case "o":
							// This line defines a point where all the subsequent entries are part of the same object.
							if (readSubstrings.Length < 2)
							{
								messagesStringBuilder.Append($"Error: Line {i} defining an object in the OBJ stream has ");
								messagesStringBuilder.AppendLine("less than 2 parts defined, and will be skipped:");
								messagesStringBuilder.AppendLine($"{readString}");
								messagesStringBuilder.AppendLine();
								break;
							}
							for (int j = 1; j < readSubstrings.Length; ++j)
							{
								commonStringsBuilder.Append(readSubstrings[j]);
							}
							allObjects.Add(new ObjectDefinition(commonStringsBuilder.ToString(), linesOfObjDataRead));
							commonStringsBuilder.Clear();
							break;

						case "s":
							// This line defines a smoothing group, or disables smoothing.
							if (readSubstrings.Length is not 2)
							{
								messagesStringBuilder.Append($"Error: Line {i} defining a smoothing group in the OBJ stream ");
								messagesStringBuilder.AppendLine("does not have 2 parts defined, and will be skipped:");
								messagesStringBuilder.AppendLine($"{readString}");
								messagesStringBuilder.AppendLine();
								break;
							}
							allSmoothingGroups.Add(new SmoothingGroup(readSubstrings[1], linesOfObjDataRead));
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
							allMaterials.Add(new MaterialDefinition(readSubstrings[1], linesOfObjDataRead));
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

				if (allGroups.Count != 0)
					_AllGroups = allGroups.ToArray();

				if (allObjects.Count != 0)
					_AllObjects = allObjects.ToArray();

				if (allMaterials.Count != 0)
					_AllMaterials = allMaterials.ToArray();

				if (allSmoothingGroups.Count != 0)
					_AllSmoothingGroups = allSmoothingGroups.ToArray();

				if (messagesStringBuilder.Length > 0)
					MessageBox.Show(messagesStringBuilder.ToString());
			}
		}


		internal void _Write(Stream OutputStream)
		{
			int objDataLinesWritten = 0;
			bool checkInlineComments = (_InlineCommentStrings.Length > 0);
			int currentInlineComment = 0;
			bool checkObjects = (_AllObjects.Length > 0);
			int currentObject = 0;
			bool checkGroups = (_AllGroups.Length > 0);
			int currentGroup = 0;
			bool checkMaterials = (_AllMaterials.Length > 0);
			int currentMaterial = 0;
			bool checkSmoothing = (_AllSmoothingGroups.Length > 0);
			int currentSmoothing = 0;


			Span<byte> intermediateByteSpan = Span<byte>.Empty;
			ReadOnlySpan<byte> crlf = stackalloc byte[] {0x0d, 0x0a};
			CultureInfo invar = CultureInfo.InvariantCulture;


			// Start off with the header comments.
			Encoding.UTF8.GetBytes(_HeaderComments, intermediateByteSpan);
			OutputStream.Write(intermediateByteSpan);
			OutputStream.Write(crlf);

			// Add the material library references after the header
			for (int i = 0; i < _MaterialLibraryFilenames.Length; ++i)
			{
				checkForInterDataItemToBeWritten(OutputStream, ref objDataLinesWritten,
					ref checkInlineComments, ref currentInlineComment,
					ref checkObjects, ref currentObject,
					ref checkGroups, ref currentGroup,
					ref checkMaterials, ref currentMaterial,
					ref checkSmoothing, ref currentSmoothing);

				Encoding.UTF8.GetBytes($"mtllib {_MaterialLibraryFilenames[i]}", intermediateByteSpan);
				OutputStream.Write(intermediateByteSpan);
				OutputStream.Write(crlf);
				++objDataLinesWritten;
			}
			OutputStream.Write(crlf);

			// Add every vertex definition to the Stream.
			for (int i = 0; i < _AllVertices.Length; ++i)
			{
				checkForInterDataItemToBeWritten(OutputStream, ref objDataLinesWritten,
					ref checkInlineComments, ref currentInlineComment,
					ref checkObjects, ref currentObject,
					ref checkGroups, ref currentGroup,
					ref checkMaterials, ref currentMaterial,
					ref checkSmoothing, ref currentSmoothing);

				// Is the W parameter equal to 1 (default value)? If not, don't include it in the final output stream.
				Vector4 aVert = _AllVertices[i];
				Encoding.UTF8.GetBytes(
					Math.Abs(aVert.W - 1) < 0.00000000001 ?
						$"v {aVert.X.ToString(invar)} {aVert.Y.ToString(invar)} {aVert.Z.ToString(invar)} {aVert.W.ToString(invar)}" :
						$"v {aVert.X.ToString(invar)} {aVert.Y.ToString(invar)} {aVert.Z.ToString(invar)}"
					, intermediateByteSpan);

				OutputStream.Write(intermediateByteSpan);
				OutputStream.Write(crlf);
				++objDataLinesWritten;
			}
			OutputStream.Write(crlf);

			// Write all texture coordinates to the Stream.
			for (int i = 0; i < _AllVertexTextures.Length; ++i)
			{
				checkForInterDataItemToBeWritten(OutputStream, ref objDataLinesWritten,
					ref checkInlineComments, ref currentInlineComment,
					ref checkObjects, ref currentObject,
					ref checkGroups, ref currentGroup,
					ref checkMaterials, ref currentMaterial,
					ref checkSmoothing, ref currentSmoothing);

				// Is the Z parameter equal to 0 (default value)? If not, don't include it in the final output stream.
				if (Math.Abs(_AllVertexTextures[i].Z) > 0.00000000001)
					Encoding.UTF8.GetBytes(
						$"vt {_AllVertexTextures[i].X.ToString(invar)} {_AllVertexTextures[i].Y.ToString(invar)} {_AllVertexTextures[i].Z.ToString(invar)}"
						, intermediateByteSpan);

				// Is the Y parameter also equal to 0 (default value)? If not, don't include it in the final output stream either.
				else if (Math.Abs(_AllVertexTextures[i].Y) > 0.00000000001)
					Encoding.UTF8.GetBytes(
						$"vt {_AllVertexTextures[i].X.ToString(invar)} {_AllVertexTextures[i].Y.ToString(invar)}"
						, intermediateByteSpan);

				else
					Encoding.UTF8.GetBytes($"vt {_AllVertexTextures[i].X.ToString(invar)}" , intermediateByteSpan);

				OutputStream.Write(intermediateByteSpan);
				OutputStream.Write(crlf);
				++objDataLinesWritten;
			}
			OutputStream.Write(crlf);

			// Write all vertex normals to the output Stream.
			for (int i = 0; i < _AllVertexNormals.Length; ++i)
			{
				checkForInterDataItemToBeWritten(OutputStream, ref objDataLinesWritten,
					ref checkInlineComments, ref currentInlineComment,
					ref checkObjects, ref currentObject,
					ref checkGroups, ref currentGroup,
					ref checkMaterials, ref currentMaterial,
					ref checkSmoothing, ref currentSmoothing);

				Encoding.UTF8.GetBytes(
					$"vn {_AllVertexNormals[i].X.ToString(invar)} {_AllVertexNormals[i].Y.ToString(invar)} {_AllVertexNormals[i].Z.ToString(invar)}"
					, intermediateByteSpan);
				OutputStream.Write(intermediateByteSpan);
				OutputStream.Write(crlf);
				++objDataLinesWritten;
			}
			OutputStream.Write(crlf);

			// Write all faces to the output Stream.
			for (int i = 0; i < _AllFaces.Length; ++i)
			{
				checkForInterDataItemToBeWritten(OutputStream, ref objDataLinesWritten,
					ref checkInlineComments, ref currentInlineComment,
					ref checkObjects, ref currentObject,
					ref checkGroups, ref currentGroup,
					ref checkMaterials, ref currentMaterial,
					ref checkSmoothing, ref currentSmoothing);

				// Write all of the corners for the current face.
				for (int j = 0; j < _AllFaces[i]._Corners.Length; ++j)
				{
					Encoding.UTF8.GetBytes($"f {_AllFaces[i]._Corners[j]._VertexIndex.ToString(invar)}" , intermediateByteSpan);
					OutputStream.Write(intermediateByteSpan);

					if (_AllFaces[i]._Corners[j]._IsVertexTextureUsed)
					{
						Encoding.UTF8.GetBytes($"/{_AllFaces[i]._Corners[j]._VertexTextureIndex.ToString(invar)}", intermediateByteSpan);
					}
					else if (_AllFaces[i]._Corners[j]._IsVertexNormalUsed)
					{
						Encoding.UTF8.GetBytes("/", intermediateByteSpan);
					}
					OutputStream.Write(intermediateByteSpan);

					if (_AllFaces[i]._Corners[j]._IsVertexNormalUsed)
					{
						Encoding.UTF8.GetBytes($"/{_AllFaces[i]._Corners[j]._VertexNormalIndex.ToString(invar)}", intermediateByteSpan);
						OutputStream.Write(intermediateByteSpan);
					}

					OutputStream.WriteByte(0x20);	// SPACE char
				}

				OutputStream.Write(crlf);
				++objDataLinesWritten;
			}
			OutputStream.Write(crlf);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="OutputStream"></param>
		/// <param name="ObjDataLinesWritten"></param>
		/// <param name="CheckInlineComments"></param>
		/// <param name="CurrentInlineComment"></param>
		/// <param name="CheckObjects"></param>
		/// <param name="CurrentObject"></param>
		/// <param name="CheckGroups"></param>
		/// <param name="CurrentGroup"></param>
		/// <param name="CheckMaterialsRef"></param>
		/// <param name="CurrentMaterialRef"></param>
		/// <param name="CheckSmoothing"></param>
		/// <param name="CurrentSmoothing"></param>
		private void checkForInterDataItemToBeWritten(Stream OutputStream, ref int ObjDataLinesWritten,
			ref bool CheckInlineComments, ref int CurrentInlineComment,
			ref bool CheckObjects, ref int CurrentObject,
			ref bool CheckGroups, ref int CurrentGroup,
			ref bool CheckMaterialsRef, ref int CurrentMaterialRef,
			ref bool CheckSmoothing, ref int CurrentSmoothing)
		{
			Span<byte> intermediateByteSpan = Span<byte>.Empty;
			ReadOnlySpan<byte> crlf = stackalloc byte[] {0x0d, 0x0a};

			if (CheckInlineComments)
			{
				if (ObjDataLinesWritten >= _InlineCommentStartIndex[CurrentInlineComment])
				{
					Encoding.UTF8.GetBytes(_InlineCommentStrings[CurrentInlineComment], intermediateByteSpan);
					OutputStream.Write(intermediateByteSpan);
					OutputStream.Write(crlf);
					++ObjDataLinesWritten;
					++CurrentInlineComment;

					// If the last inline comment has just been processed, indicate that this code block should be skipped.
					if (CurrentInlineComment >= _InlineCommentStartIndex.Length)
						CheckInlineComments = false;
				}
			}

			if (CheckObjects)
			{
				if (ObjDataLinesWritten >= _AllObjects[CurrentObject]._ObjectStartIndex)
				{
					Encoding.UTF8.GetBytes(_AllObjects[CurrentObject]._ObjectName, intermediateByteSpan);
					OutputStream.Write(intermediateByteSpan);
					OutputStream.Write(crlf);
					++ObjDataLinesWritten;
					++CurrentObject;

					// If the last Object has just been processed, indicate that this code block should be skipped.
					if (CurrentObject >= _AllObjects.Length)
						CheckObjects = false;
				}
			}

			if (CheckGroups)
			{
				if (ObjDataLinesWritten >= _AllGroups[CurrentGroup]._GroupStartIndex)
				{
					Encoding.UTF8.GetBytes(_AllGroups[CurrentGroup]._GroupName, intermediateByteSpan);
					OutputStream.Write(intermediateByteSpan);
					OutputStream.Write(crlf);
					++ObjDataLinesWritten;
					++CurrentGroup;

					// If the last Group has just been processed, indicate that this code block should be skipped.
					if (CurrentGroup >= _AllGroups.Length)
						CheckGroups = false;
				}
			}

			if (CheckMaterialsRef)
			{
				if (ObjDataLinesWritten >= _AllMaterials[CurrentMaterialRef]._MaterialStartIndex)
				{
					Encoding.UTF8.GetBytes(_AllMaterials[CurrentMaterialRef]._MaterialName, intermediateByteSpan);
					OutputStream.Write(intermediateByteSpan);
					OutputStream.Write(crlf);
					++ObjDataLinesWritten;
					++CurrentMaterialRef;

					// If the last Material Reference has just been processed, indicate that this code block should be skipped.
					if (CurrentMaterialRef >= _AllMaterials.Length)
						CheckMaterialsRef = false;
				}
			}

			if (CheckSmoothing)
			{
				if (ObjDataLinesWritten >= _AllSmoothingGroups[CurrentSmoothing]._SmoothingStartIndex)
				{
					Encoding.UTF8.GetBytes(_AllSmoothingGroups[CurrentSmoothing]._SmoothShadingGroup, intermediateByteSpan);
					OutputStream.Write(intermediateByteSpan);
					OutputStream.Write(crlf);
					++ObjDataLinesWritten;
					++CurrentSmoothing;

					// If the last Smoothing Group has just been processed, indicate that this code block should be skipped.
					if (CurrentSmoothing >= _AllSmoothingGroups.Length)
						CheckSmoothing = false;
				}
			}
		}
	}

	/// <summary>
	/// Defines all of the corners of a single face.
	/// </summary>
	internal readonly struct FaceDefinition
	{
		/// <summary>
		/// Array of all the Face Corners that make up an individual face.
		/// </summary>
		internal readonly FaceCornerDefinition[] _Corners;

		/// <summary>
		/// Construct a Face from a Face definition string extracted from an OBJ file.
		/// </summary>
		/// <param name="FaceStringParts">The entire string from an OBJ that defines an individual face.</param>
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

	/// <summary>
	/// Used to hold all the information that defines a single corner in a face.
	/// </summary>
	internal readonly struct FaceCornerDefinition
	{
		// Don't change any Int into a UInt. The specification allows negative values (offset from end instead of start)
		/// <summary> The index number for the vertex used for this corner. </summary>
		internal readonly int _VertexIndex;

		/// <summary> Does this vertex have a texture coordinate attached? </summary>
		internal readonly bool _IsVertexTextureUsed = false;
		/// <summary> The index number for the vertex texture coordinate used for this corner. </summary>
		internal readonly int _VertexTextureIndex = 0;

		/// <summary> Does this vertex have a normal attached? </summary>
		internal readonly bool _IsVertexNormalUsed = false;
		/// <summary> The index number for the vertex normal used for this corner. </summary>
		internal readonly int _VertexNormalIndex = 0;

		/// <summary>
		/// Construct a Face Corner from a correctly formatted string extracted from an OBJ file.
		/// </summary>
		/// <param name="CurrentFaceStringPart">The part of the string from the Face definition that defines the indexes used.</param>
		/// <exception cref="ArgumentException">Thrown if the <paramref name="CurrentFaceStringPart"/> is empty, null, or only contain whitespaces.</exception>
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

	/// <summary>
	/// Struct used to record the name and position of a Group definition.
	/// </summary>
	internal readonly struct GroupDefinition
	{
		internal readonly string _GroupName;
		internal readonly int _GroupStartIndex;

		internal GroupDefinition(string Name, int StartIndex)
		{
			_GroupName = Name;
			_GroupStartIndex = StartIndex;
		}
	}

	/// <summary>
	/// Struct used to record the name and position of a Material Reference.
	/// </summary>
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

	/// <summary>
	/// Struct used to record the name and position of a Object definition.
	/// </summary>
	internal readonly struct ObjectDefinition
	{
		internal readonly string _ObjectName;
		internal readonly int _ObjectStartIndex;

		internal ObjectDefinition(string Name, int StartIndex)
		{
			_ObjectName = Name;
			_ObjectStartIndex = StartIndex;
		}
	}

	/// <summary>
	/// Struct used to record the name and position of a Smoothing Group.
	/// </summary>
	internal readonly struct SmoothingGroup
	{
		internal readonly string _SmoothShadingGroup;
		internal readonly int _SmoothingStartIndex;

		internal SmoothingGroup(string SmoothShadingGroup, int StartIndex)
		{
			_SmoothShadingGroup = SmoothShadingGroup;
			_SmoothingStartIndex = StartIndex;
		}
	}
}
