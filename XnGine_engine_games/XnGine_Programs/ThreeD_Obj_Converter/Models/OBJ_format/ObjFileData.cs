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
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.Enums;

namespace ThreeD_Obj_Converter.Models.OBJ_format
{
	internal readonly ref struct ObjFileData
	{
		/// <summary> Add your comments here, including newlines and the starting # for each line. </summary>
		internal readonly string _HeaderComments;

		internal readonly string[] _InlineCommentStrings;
		internal readonly int[] _InlineCommentStartIndex;

		internal readonly MaterialLibraryDefinition[] _MaterialLibraries;

		internal readonly Vector4[] _AllVertices;
		internal readonly Vector3[] _AllVertexTextures;
		internal readonly Vector3[] _AllVertexNormals;

		internal readonly ObjectDefinition[] _AllObjects;

		internal readonly GroupDefinition[] _AllGroups;

		internal readonly MaterialDefinition[] _AllMaterials;

		internal readonly SmoothingGroup[] _AllSmoothingGroups;


		internal ObjFileData(Stream ObjDataStream, string FolderContainingObj)
		{
			_HeaderComments = string.Empty;
			// Assign an empty array to these fields until the OBJ stream has been completely read.
			_InlineCommentStrings = Array.Empty<string>();
			_InlineCommentStartIndex = Array.Empty<int>();
			_MaterialLibraries = Array.Empty<MaterialLibraryDefinition>();
			_AllVertices = Array.Empty<Vector4>();
			_AllVertexTextures = Array.Empty<Vector3>();
			_AllVertexNormals = Array.Empty<Vector3>();
			_AllGroups = Array.Empty<GroupDefinition>();
			_AllObjects = Array.Empty<ObjectDefinition>();
			_AllMaterials = Array.Empty<MaterialDefinition>();
			_AllSmoothingGroups = Array.Empty<SmoothingGroup>();

			using (StreamReader objDataStreamReader = new(ObjDataStream))
			{
				// Lists and StringBuilder used for temp storage of arrays and strings that are being constructed
				List<string> inlineCommentStrings = new();
				List<int> inlineCommentStartIndex = new();
				List<MaterialLibraryDefinition> materialLibraryFilenames = new();
				List<Vector4> allVertices = new();
				List<Vector3> allVertexTextures = new();
				List<Vector3> allVertexNormals = new();
				List<GroupDefinition> allGroups = new();
				List<ObjectDefinition> allObjects = new();
				List<SmoothingGroup> allSmoothingGroups = new();
				StringBuilder commonStringsBuilder = new();
				StringBuilder messagesStringBuilder = new();

				List<string> allMaterialStrings = new();
				List<List<string>> allFacesAssociatedWithMaterialsList = new();

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

					int currentMaterial = -1;
					readSubstrings[0] = readSubstrings[0].ToLower();
					switch (readSubstrings[0])
					{
						case "mtllib":
							// This line defines a Material resource file
							for (int j = 1; j < readSubstrings.Length; ++j)
							{
								commonStringsBuilder.Append(readSubstrings[j]);
							}
							materialLibraryFilenames.Add(new MaterialLibraryDefinition(commonStringsBuilder.ToString(), FolderContainingObj));
							commonStringsBuilder.Clear();
							break;

						case "f":
							// This line defines a Face
							if (readSubstrings.Length < 4)
							{
								notEnoughPartsError(messagesStringBuilder, in i, "a Face", "3 or more parts", in readString);
								break;
							}
							for (int j = 1; j < readSubstrings.Length; ++j)
							{
								commonStringsBuilder.Append(readSubstrings[j]);
							}
							allFacesAssociatedWithMaterialsList[currentMaterial].Add(commonStringsBuilder.ToString());
							commonStringsBuilder.Clear();
							break;

						case "g":
							// This line defines a point where all the subsequent entries are part of the same group.
							if (readSubstrings.Length < 2)
							{
								notEnoughPartsError(messagesStringBuilder, in i, "a group", "2 or more parts", in readString);
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
								notEnoughPartsError(messagesStringBuilder, in i, "an object", "2 or more parts", in readString);
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
								notEnoughPartsError(messagesStringBuilder, in i, "a smoothing group", "2 parts", in readString);
								break;
							}
							allSmoothingGroups.Add(new SmoothingGroup(readSubstrings[1], linesOfObjDataRead));
							break;

						case "usemtl":
							// This line defines a point where all the subsequent faces must use the specified material reference.
							if (readSubstrings.Length < 2)
							{
								notEnoughPartsError(messagesStringBuilder, in i, "a material reference", "2 or more parts", in readString);
								break;
							}
							for (int j = 1; j < readSubstrings.Length; ++j)
							{
								commonStringsBuilder.Append(readSubstrings[j]);
							}
							allMaterialStrings.Add(commonStringsBuilder.ToString());
							commonStringsBuilder.Clear();
							allFacesAssociatedWithMaterialsList.Add(new List<string>());
							++currentMaterial;
							break;

						case "v":
							// This line defines a Vertex
							if (readSubstrings.Length is not (4 or 5))
							{
								notEnoughPartsError(messagesStringBuilder, in i, "a Vertex", "3 or 4 parts", in readString);
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
								notEnoughPartsError(messagesStringBuilder, in i, "a Vertex Normal", "4 parts", in readString);
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
								notEnoughPartsError(messagesStringBuilder, in i, "a Vertex Texture",
									"between 2 and 4 parts", in readString);
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

				// With that done, use the gathered Material References and Faces to properly construct the OBJ data for them.
				_AllMaterials = new MaterialDefinition[allMaterialStrings.Count];
				for (int i = 0; i < allMaterialStrings.Count; ++i)
				{
					FaceDefinition[] allFacesForCurrentMaterial = new FaceDefinition[allFacesAssociatedWithMaterialsList[i].Count];
					for (int j = 0; j < allFacesAssociatedWithMaterialsList[i].Count; j++)
					{
						allFacesForCurrentMaterial[j] = new(allFacesAssociatedWithMaterialsList[i].ToArray());
					}
					_AllMaterials[i] = new(allMaterialStrings[i], allFacesForCurrentMaterial);
				}

				// Assign all of the lists created above to the array fields, but check if the lists have anything in them
				// first to prevent excessive heap allocations.
				if (inlineCommentStrings.Count != 0)
					_InlineCommentStrings = inlineCommentStrings.ToArray();

				if (inlineCommentStartIndex.Count != 0)
					_InlineCommentStartIndex = inlineCommentStartIndex.ToArray();

				if (materialLibraryFilenames.Count != 0)
					_MaterialLibraries = materialLibraryFilenames.ToArray();

				if (allVertices.Count != 0)
					_AllVertices = allVertices.ToArray();

				if (allVertexTextures.Count != 0)
					_AllVertexTextures = allVertexTextures.ToArray();

				if (allVertexNormals.Count != 0)
					_AllVertexNormals = allVertexNormals.ToArray();

				if (allGroups.Count != 0)
					_AllGroups = allGroups.ToArray();

				if (allObjects.Count != 0)
					_AllObjects = allObjects.ToArray();

				if (allSmoothingGroups.Count != 0)
					_AllSmoothingGroups = allSmoothingGroups.ToArray();

				if (messagesStringBuilder.Length > 0)
				{
					IMsBoxWindow<ButtonResult>? messageBox = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow("Error in OBJ File Data Constructor",
						messagesStringBuilder.ToString());
					messageBox.Show();
				}
			}
		}

		internal ObjFileData(string HeaderComments, string[] InlineCommentStrings, int[] InlineCommentStartIndex,
			MaterialLibraryDefinition[] MaterialLibraries, Vector4[] AllVertices, Vector3[] AllVertexTextures,
			Vector3[] AllVertexNormals, ObjectDefinition[] AllObjects,
			GroupDefinition[] AllGroups, MaterialDefinition[] AllMaterials, SmoothingGroup[] AllSmoothingGroups)
		{
			_HeaderComments = HeaderComments;
			_InlineCommentStrings = InlineCommentStrings;
			_InlineCommentStartIndex = InlineCommentStartIndex;
			_MaterialLibraries = MaterialLibraries;
			_AllVertices = AllVertices;
			_AllVertexTextures = AllVertexTextures;
			_AllVertexNormals = AllVertexNormals;
			_AllObjects = AllObjects;
			_AllGroups = AllGroups;
			_AllMaterials = AllMaterials;
			_AllSmoothingGroups = AllSmoothingGroups;
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
			bool checkSmoothing = (_AllSmoothingGroups.Length > 0);
			int currentSmoothing = 0;


			ReadOnlySpan<byte> crlf = stackalloc byte[] {0x0d, 0x0a};
			CultureInfo invar = CultureInfo.InvariantCulture;

			// Start off with the header comments.
			int bytesRequiredForCharConversion =  Encoding.UTF8.GetByteCount(_HeaderComments);
			Span<byte> intermediateByteSpan = stackalloc byte[bytesRequiredForCharConversion];
			Encoding.UTF8.GetBytes(_HeaderComments, intermediateByteSpan);
			OutputStream.Write(intermediateByteSpan);
			OutputStream.Write(crlf);

			// Add the material library references after the header
			for (int i = 0; i < _MaterialLibraries.Length; ++i)
			{
				checkForInterDataItemToBeWritten(OutputStream, ref objDataLinesWritten,
					ref checkInlineComments, ref currentInlineComment,
					ref checkObjects, ref currentObject,
					ref checkGroups, ref currentGroup,
					ref checkSmoothing, ref currentSmoothing);

				Encoding.UTF8.GetBytes($"mtllib {_MaterialLibraries[i]._LibName}", intermediateByteSpan);
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
					ref checkSmoothing, ref currentSmoothing);

				Encoding.UTF8.GetBytes(
					$"vn {_AllVertexNormals[i].X.ToString(invar)} {_AllVertexNormals[i].Y.ToString(invar)} {_AllVertexNormals[i].Z.ToString(invar)}"
					, intermediateByteSpan);
				OutputStream.Write(intermediateByteSpan);
				OutputStream.Write(crlf);
				++objDataLinesWritten;
			}
			OutputStream.Write(crlf);

			// Write all faces and their associated Material References to the output Stream.
			for (int i = 0; i < _AllMaterials.Length; ++i)
			{
				checkForInterDataItemToBeWritten(OutputStream, ref objDataLinesWritten,
					ref checkInlineComments, ref currentInlineComment,
					ref checkObjects, ref currentObject,
					ref checkGroups, ref currentGroup,
					ref checkSmoothing, ref currentSmoothing);

				// Write the current Material Reference
				Encoding.UTF8.GetBytes($"usemtl {_AllMaterials[i]._MaterialName}", intermediateByteSpan);
				OutputStream.Write(intermediateByteSpan);
				OutputStream.Write(crlf);
				++objDataLinesWritten;

				// Write all of the corners for the current face.
				for (int j = 0; j < _AllMaterials[i]._FacesAssociatedWithMaterial.Length; ++j)
				{
					checkForInterDataItemToBeWritten(OutputStream, ref objDataLinesWritten,
						ref checkInlineComments, ref currentInlineComment,
						ref checkObjects, ref currentObject,
						ref checkGroups, ref currentGroup,
						ref checkSmoothing, ref currentSmoothing);

					for (int k = 0; k < _AllMaterials[i]._FacesAssociatedWithMaterial[j]._Corners.Length; ++k)
					{
						Encoding.UTF8.GetBytes(
							$"f {_AllMaterials[i]._FacesAssociatedWithMaterial[j]._Corners[k]._VertexIndex.ToString(invar)}" , intermediateByteSpan
						);
						OutputStream.Write(intermediateByteSpan);

						if (_AllMaterials[i]._FacesAssociatedWithMaterial[j]._Corners[k]._IsVertexTextureUsed)
						{
							Encoding.UTF8.GetBytes(
								$"/{_AllMaterials[i]._FacesAssociatedWithMaterial[j]._Corners[k]._VertexTextureIndex.ToString(invar)}", intermediateByteSpan
								);
						}
						else if (_AllMaterials[i]._FacesAssociatedWithMaterial[j]._Corners[k]._IsVertexNormalUsed)
						{
							Encoding.UTF8.GetBytes("/", intermediateByteSpan);
						}
						OutputStream.Write(intermediateByteSpan);

						if (_AllMaterials[i]._FacesAssociatedWithMaterial[j]._Corners[k]._IsVertexNormalUsed)
						{
							Encoding.UTF8.GetBytes(
								$"/{_AllMaterials[i]._FacesAssociatedWithMaterial[j]._Corners[k]._VertexNormalIndex.ToString(invar)}", intermediateByteSpan
								);
							OutputStream.Write(intermediateByteSpan);
						}

						OutputStream.WriteByte(0x20);	// SPACE char
					}
					OutputStream.Write(crlf);
					++objDataLinesWritten;
				}
				OutputStream.Write(crlf);
			}
		}


		// ==== Private methods ====
		private static void notEnoughPartsError(StringBuilder MessageStringBuilder, in int LineNumber,
			in string ElementName, in string NumberOfParts, in string ReadString)
		{
			MessageStringBuilder.Append($"Error: Line {LineNumber} defining {ElementName} in the OBJ stream ");
			MessageStringBuilder.AppendLine($"does not have {NumberOfParts} defined, and will be skipped:");
			MessageStringBuilder.AppendLine($"{ReadString}");
			MessageStringBuilder.AppendLine();
		}

		private void checkForInterDataItemToBeWritten(Stream OutputStream, ref int ObjDataLinesWritten,
			ref bool CheckInlineComments, ref int CurrentInlineComment,
			ref bool CheckObjects, ref int CurrentObject,
			ref bool CheckGroups, ref int CurrentGroup,
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

		internal readonly struct MaterialLibraryDefinition
		{
			internal readonly string _LibName;
			internal readonly MtlFileData _MtlData;

			internal MaterialLibraryDefinition(string LibName, string FolderContainingObj)
			{
				_LibName = LibName;

				// First, check if the LibName is an absolute path.
				// If so, immediately read the contents of the MTL file.
				if (File.Exists(LibName))
				{
					_MtlData = new MtlFileData(new FileStream(LibName, FileMode.Open, FileAccess.Read, FileShare.Read));
					return;
				}

				// Otherwise, check if it is a path relative to the folder containing the OBJ file.
				string libAbsolutePath = $"{FolderContainingObj}/{LibName}";
				if (File.Exists(libAbsolutePath))
				{
					_MtlData = new MtlFileData(new FileStream(libAbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read));
					return;
				}

				// If it's not a relative path either, the MTL data can't be read.
				_MtlData = new MtlFileData();
			}

			internal MaterialLibraryDefinition(string LibName, MtlFileData MtlData)
			{
				_LibName = LibName;
				_MtlData = MtlData;
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
		/// Struct used to record the name of a Material Reference, as well as associated faces.
		/// </summary>
		internal readonly struct MaterialDefinition
		{
			internal readonly string _MaterialName;
			internal readonly FaceDefinition[] _FacesAssociatedWithMaterial;

			internal MaterialDefinition(string MaterialReference, FaceDefinition[] FacesAssociatedWithMaterial)
			{
				_MaterialName = MaterialReference;
				_FacesAssociatedWithMaterial = FacesAssociatedWithMaterial;
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

			internal FaceDefinition(FaceCornerDefinition[] Corners) =>
				_Corners = Corners;
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

			internal FaceCornerDefinition(int VertexIndex, bool IsVertexTextureUsed, int VertexTextureIndex,
				bool IsVertexNormalUsed, int VertexNormalIndex)
			{
				_VertexIndex = VertexIndex;
				_IsVertexTextureUsed = IsVertexTextureUsed;
				_VertexTextureIndex = VertexTextureIndex;
				_IsVertexNormalUsed = IsVertexNormalUsed;
				_VertexNormalIndex = VertexNormalIndex;
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
}
