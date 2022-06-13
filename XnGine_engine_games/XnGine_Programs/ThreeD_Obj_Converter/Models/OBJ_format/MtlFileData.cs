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

namespace ThreeD_Obj_Converter.Models.OBJ_format
{
	internal readonly struct MtlFileData
	{
		/// <summary> The header's comments, including newlines and the starting # for each line. </summary>
		internal readonly string _HeaderComments;

		internal readonly string[] _InlineCommentStrings;
		internal readonly int[] _InlineCommentStartIndex;

		internal readonly MaterialData[] _AllMaterials;

		internal MtlFileData(Stream MtlDataStream)
		{
			_HeaderComments = string.Empty;
			_InlineCommentStrings = Array.Empty<string>();
			_InlineCommentStartIndex = Array.Empty<int>();
			List<MaterialData> allMaterialsList = new();
			List<string> inlineCommentStrings = new();
			List<int> inlineCommentStartIndex = new();

			int lineNumber = 0;
			StringBuilder commonStringsBuilder = new();
			StringBuilder messageStringBuilder = new();

			using (StreamReader mtlDataStreamReader = new(MtlDataStream))
			{
				string? readString;

				// Read the comments in the header.
				for (int i = 0; i < int.MaxValue; ++i)
				{
					readString = mtlDataStreamReader.ReadLine()?.Trim();
					++lineNumber;

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
						commonStringsBuilder.Append(readString);
						continue;
					}

					// Since this line is not a comment, move on to reading the other file data.
					break;
				}

				// Skip past all lines between the header comments and first material definition.
				while (true)
				{
					readString = mtlDataStreamReader.ReadLine()?.Trim();
					++lineNumber;

					// Check if the end of the stream has been reached.
					if (readString == null)
						break;

					// Check if this is an empty line.
					if (string.IsNullOrWhiteSpace(readString))
						continue;

					// If this line is not a comment, empty, or a material definition, note the presence of it and skip to the next line.
					if (!readString.StartsWith("newmtl ", StringComparison.OrdinalIgnoreCase))
					{
						messageStringBuilder.AppendLine($"Line {lineNumber} contains data that is not part of a material and will be skipped:");
						messageStringBuilder.AppendLine($"{readString}");
						messageStringBuilder.AppendLine();
						continue;
					}

					// Since this line is the start of the material definition, run the material definition constructor.
					allMaterialsList.Add(new MaterialData(
						mtlDataStreamReader, messageStringBuilder, in readString, ref lineNumber, ref inlineCommentStrings,
						ref inlineCommentStartIndex, out readString, out bool endOfFileReached)
					);

					// TODO: Use new read string
					if (endOfFileReached)
						break;
				}
			}

			_AllMaterials = allMaterialsList.ToArray();
		}


		internal void _Write(Stream OutputStream)
		{
			Span<byte> intermediateByteSpan = Span<byte>.Empty;
			ReadOnlySpan<byte> crlf = stackalloc byte[] {0x0d, 0x0a};
			CultureInfo invar = CultureInfo.InvariantCulture;
			StringBuilder outputStringBuilder = new();

			Encoding.UTF8.GetBytes(_HeaderComments, intermediateByteSpan);
			OutputStream.Write(intermediateByteSpan);
			OutputStream.Write(crlf);

			for (int i = 0; i < _AllMaterials.Length; ++i)
			{
				_AllMaterials[i]._Write(OutputStream);
			}
		}

		internal readonly struct MaterialData
		{
			internal readonly string _Name;
			internal readonly Vector3 _AmbientColour;
			internal readonly Vector3 _DiffuseColour;
			internal readonly Vector3 _SpecularColour;
			internal readonly float _SpecularExponent;
			internal readonly float _Opacity;
			internal readonly TransmissionColourFilter _TransmissionFilterColour;
			internal readonly float _OpticalDensity;

			/// <summary> Used to define the material's Illumination Model. </summary>
			internal readonly IlluminationModels _IlluminationModel;

			internal readonly string _AmbientTextureMap;
			internal readonly string _DiffuseTextureMap;
			internal readonly string _SpecularColourTextureMap;
			internal readonly string _SpecularHighlightTextureMap;
			internal readonly string _AlphaTextureMap;
			internal readonly string _BumpMap;
			internal readonly string _DisplacementMap;
			internal readonly string _StencilDecalTexture;


			internal MaterialData(StreamReader MtlDataStreamReader, StringBuilder MessageStringBuilder, in string ThisMaterialName,
				ref int LineNumber, ref List<string> InlineCommentStrings, ref List<int> InlineCommentStartIndex,
				out string NextMaterialName, out bool EndOfFileReached)
			{
				// Fill the Name field with the string passed to the constructor.
				if (ThisMaterialName.StartsWith("newmtl ", StringComparison.OrdinalIgnoreCase))
					_Name = ThisMaterialName.Remove(0, 7);

				else
					_Name = ThisMaterialName;

				// Pre-populate the other fields with empty/invalid data, just in case they don't get filled below.
				Vector3 invalidVector3 = new Vector3(-1,-1,-1);
				_AmbientColour = invalidVector3;
				_DiffuseColour = invalidVector3;
				_SpecularColour = invalidVector3;
				_SpecularExponent = -1;
				_Opacity = -1;
				_TransmissionFilterColour = new TransmissionColourFilter(invalidVector3, false);
				_OpticalDensity = -1;
				_IlluminationModel = IlluminationModels.Undefined;
				_AmbientTextureMap = string.Empty;
				_DiffuseTextureMap = string.Empty;
				_SpecularColourTextureMap = string.Empty;
				_SpecularHighlightTextureMap = string.Empty;
				_AlphaTextureMap = string.Empty;
				_BumpMap = string.Empty;
				_DisplacementMap = string.Empty;
				_StencilDecalTexture = string.Empty;
				NextMaterialName = string.Empty;

				// Read the Stream's data line-by-line until either the end of file or next material declaration is found.
				char[] whitespaceChars = {' ', '	'};
				bool mustBreakOutOfLoop = false;
				while (true)
				{
					string? readString = MtlDataStreamReader.ReadLine()?.Trim();
					++LineNumber;

					// Check if the end of the stream has been reached.
					if (readString == null)
					{
						EndOfFileReached = true;
						break;
					}

					// Check if this is an empty line.
					if (string.IsNullOrWhiteSpace(readString))
						continue;

					// Check if this is a comment
					if (readString.StartsWith("#"))
					{
						InlineCommentStrings.Add(readString);
						InlineCommentStartIndex.Add(LineNumber);
						continue;
					}

					// Split this string into the MTL element identifier and associated data.
					string[] readSubstrings = readString.Split(whitespaceChars,
						StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
						);

					// Every line in MTL data that is not a comment or empty line has at least two parts: The data element declaration,
					// and the data associated with that element. If there is less than this, the read data is not valid.
					// Thus, report the error and skip to the next line.
					if (readSubstrings.Length < 2)
					{
						MessageStringBuilder.AppendLine($"Error: Line {LineNumber} in the MTL stream does not contain recognised info and will be skipped:");
						MessageStringBuilder.AppendLine($"{readString}");
						MessageStringBuilder.AppendLine();
						continue;
					}

					readSubstrings[0] = readSubstrings[0].ToLower();
					switch (readSubstrings[0])
					{
						case "bump":
							// This is an alternative Bump Texture Map
							_BumpMap = readString.Remove(0, 5);
							break;

						case "decal":
							// This is a Stencil Decal Texture Map
							_StencilDecalTexture = readString.Remove(0, 6);
							break;

						case "disp":
							// This is a Displacement Texture Map
							_DisplacementMap = readString.Remove(0, 5);
							break;

						case "d":
							// This is an Opacity definition
							if (readSubstrings.Length is not 2)
							{
								MessageStringBuilder.Append($"Error: Line {LineNumber} defining Opacity in the MTL stream ");
								MessageStringBuilder.AppendLine("does not have 2 parts defined, and will be skipped:");
								MessageStringBuilder.AppendLine($"{readString}");
								MessageStringBuilder.AppendLine();
								break;
							}
							_Opacity = float.Parse(readSubstrings[1], NumberStyles.Float, CultureInfo.InvariantCulture);
							break;

						case "illum":
							// This is an Illumination Model
							if (readSubstrings.Length is not 2)
							{
								MessageStringBuilder.Append($"Error: Line {LineNumber} defining an Illumination Model in the MTL stream ");
								MessageStringBuilder.AppendLine("does not have 2 parts defined, and will be skipped:");
								MessageStringBuilder.AppendLine($"{readString}");
								MessageStringBuilder.AppendLine();
								break;
							}
							int illuminationModelNumber = int.Parse(readSubstrings[1]);
							switch (illuminationModelNumber)
							{
								case 0:
								case 1:
								case 2:
								case 3:
								case 4:
								case 5:
								case 6:
								case 7:
								case 8:
								case 9:
								case 10:
									_IlluminationModel = (IlluminationModels)illuminationModelNumber;
									break;

								default:
									_IlluminationModel = IlluminationModels.Undefined;
									break;
							}
							break;

						case "ka":
							// This is an Ambient Colour
							if (readSubstrings.Length is not 4)
							{
								MessageStringBuilder.Append($"Error: Line {LineNumber} defining an Ambient Colour in the MTL stream ");
								MessageStringBuilder.AppendLine("does not have 4 parts defined, and will be skipped:");
								MessageStringBuilder.AppendLine($"{readString}");
								MessageStringBuilder.AppendLine();
								break;
							}
							_AmbientColour = new Vector3(
								float.Parse(readSubstrings[1], NumberStyles.Float, CultureInfo.InvariantCulture),
								float.Parse(readSubstrings[2], NumberStyles.Float, CultureInfo.InvariantCulture),
								float.Parse(readSubstrings[3], NumberStyles.Float, CultureInfo.InvariantCulture)
							);
							break;

						case "kd":
							// This is a Diffuse Colour
							if (readSubstrings.Length is not 4)
							{
								MessageStringBuilder.Append($"Error: Line {LineNumber} defining an Diffuse Colour in the MTL stream ");
								MessageStringBuilder.AppendLine("does not have 4 parts defined, and will be skipped:");
								MessageStringBuilder.AppendLine($"{readString}");
								MessageStringBuilder.AppendLine();
								break;
							}
							_DiffuseColour = new Vector3(
								float.Parse(readSubstrings[1], NumberStyles.Float, CultureInfo.InvariantCulture),
								float.Parse(readSubstrings[2], NumberStyles.Float, CultureInfo.InvariantCulture),
								float.Parse(readSubstrings[3], NumberStyles.Float, CultureInfo.InvariantCulture)
							);
							break;

						case "ks":
							// This is a Specular Colour
							if (readSubstrings.Length is not 4)
							{
								MessageStringBuilder.Append($"Error: Line {LineNumber} defining an Specular Colour in the MTL stream ");
								MessageStringBuilder.AppendLine("does not have 4 parts defined, and will be skipped:");
								MessageStringBuilder.AppendLine($"{readString}");
								MessageStringBuilder.AppendLine();
								break;
							}
							_SpecularColour = new Vector3(
								float.Parse(readSubstrings[1], NumberStyles.Float, CultureInfo.InvariantCulture),
								float.Parse(readSubstrings[2], NumberStyles.Float, CultureInfo.InvariantCulture),
								float.Parse(readSubstrings[3], NumberStyles.Float, CultureInfo.InvariantCulture)
							);
							break;

						case "map_bump":
							// This is a Bump Texture Map
							_BumpMap = readString.Remove(0, 9);
							break;

						case "map_d":
							// This is an Alpha Texture Map
							_AlphaTextureMap = readString.Remove(0, 6);
							break;

						case "map_ka":
							// This is an Ambient Texture Map
							_AmbientTextureMap = readString.Remove(0, 7);
							break;

						case "map_kd":
							// This is a Diffuse Texture Map
							_DiffuseTextureMap = readString.Remove(0, 7);
							break;

						case "map_ks":
							// This is a Specular Colour Texture Map
							_SpecularColourTextureMap = readString.Remove(0, 7);
							break;

						case "map_ns":
							// This is a Specular Highlight Texture Map
							_SpecularHighlightTextureMap = readString.Remove(0, 7);
							break;

						case "newmtl":
							// This is the start of a new material definition. Therefore, finish constructing this one.
							mustBreakOutOfLoop = true;
							NextMaterialName = readString;
							break;

						case "ni":
							// This is an Optical Density
							if (readSubstrings.Length is not 2)
							{
								MessageStringBuilder.Append($"Error: Line {LineNumber} defining an Optical Density in the MTL stream ");
								MessageStringBuilder.AppendLine("does not have 2 parts defined, and will be skipped:");
								MessageStringBuilder.AppendLine($"{readString}");
								MessageStringBuilder.AppendLine();
								break;
							}
							_OpticalDensity = float.Parse(readSubstrings[1], NumberStyles.Float, CultureInfo.InvariantCulture);
							break;

						case "ns":
							// This is a Specular Exponent
							if (readSubstrings.Length is not 2)
							{
								MessageStringBuilder.Append($"Error: Line {LineNumber} defining an Specular Exponent in the MTL stream ");
								MessageStringBuilder.AppendLine("does not have 2 parts defined, and will be skipped:");
								MessageStringBuilder.AppendLine($"{readString}");
								MessageStringBuilder.AppendLine();
								break;
							}
							_SpecularExponent = float.Parse(readSubstrings[1], NumberStyles.Float, CultureInfo.InvariantCulture);
							break;

						case "tf":
							// This is a Transmission Filter Colour.
							if (readSubstrings.Length is not (4 or 5))
							{
								MessageStringBuilder.Append($"Error: Line {LineNumber} defining a Transmission Filter Colour in the MTL stream ");
								MessageStringBuilder.AppendLine("does not have 4 or 5 parts defined, and will be skipped:");
								MessageStringBuilder.AppendLine($"{readString}");
								MessageStringBuilder.AppendLine();
								break;
							}
							if (readSubstrings.Length is 4)
							{
								// This is an RGB Transmission Filter Colour.
								_TransmissionFilterColour = new TransmissionColourFilter(
									new Vector3(
										float.Parse(readSubstrings[1], NumberStyles.Float, CultureInfo.InvariantCulture),
										float.Parse(readSubstrings[2], NumberStyles.Float, CultureInfo.InvariantCulture),
										float.Parse(readSubstrings[3], NumberStyles.Float, CultureInfo.InvariantCulture)
									),
									false
								);
								break;
							}
							// Otherwise, this is a CIEXYZ Transmission Filter Colour.
							if (readSubstrings[1].Equals("xyz", StringComparison.OrdinalIgnoreCase))
							{
								_TransmissionFilterColour = new TransmissionColourFilter(
									new Vector3(
										float.Parse(readSubstrings[2], NumberStyles.Float,
											CultureInfo.InvariantCulture),
										float.Parse(readSubstrings[3], NumberStyles.Float,
											CultureInfo.InvariantCulture),
										float.Parse(readSubstrings[4], NumberStyles.Float, CultureInfo.InvariantCulture)
									),
									true
								);
							}
							else
							{
								MessageStringBuilder.Append($"Error: Line {LineNumber} defining a Transmission Filter Colour in the MTL stream ");
								MessageStringBuilder.AppendLine("is not correctly formatted, and will be skipped:");
								MessageStringBuilder.AppendLine($"{readString}");
								MessageStringBuilder.AppendLine();
							}
							break;

						case "tr":
							// This is a Transparency definition. This is an alternative to "d", and appears to be not as popular.
							// Therefore, convert it to an Opacity definition.
							if (readSubstrings.Length is not 2)
							{
								MessageStringBuilder.Append($"Error: Line {LineNumber} defining Transparency in the MTL stream ");
								MessageStringBuilder.AppendLine("does not have 2 parts defined, and will be skipped:");
								MessageStringBuilder.AppendLine($"{readString}");
								MessageStringBuilder.AppendLine();
								break;
							}
							_Opacity = 1 - float.Parse(readSubstrings[1], NumberStyles.Float, CultureInfo.InvariantCulture);
							break;
					}

					if (mustBreakOutOfLoop)
						break;
				}

				EndOfFileReached = false;
			}

			internal void _Write(Stream OutputStream)
			{
			}

			internal struct TransmissionColourFilter
			{
				internal readonly Vector3 _Values;
				internal readonly bool _IsCiexyz;

				internal TransmissionColourFilter(Vector3 Values, bool IsCiexyz)
				{
					_Values = Values;
					_IsCiexyz = IsCiexyz;
				}
			}

			internal enum IlluminationModels
			{
				Undefined													= -1,
				ColorOnAndAmbientOff										=  0,
				ColorOnAndAmbientOn											=  1,
				HighlightOn													=  2,
				ReflectionOnAndRayTraceOn									=  3,
				TransparencyGlassOnReflectionRayTraceOn						=  4,
				ReflectionFresnelOnAndRayTraceOn							=  5,
				TransparencyRefractionOnReflectionFresnelOffAndRayTraceOn	=  6,
				TransparencyRefractionOnReflectionFresnelOnAndRayTraceOn	=  7,
				ReflectionOnAndRayTraceOff									=  8,
				TransparencyGlassOnReflectionRayTraceOff					=  9,
				CastsShadowsOntoInvisibleSurfaces							= 10
			}
		}
	}
}
