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
	internal readonly partial struct MtlFileData
	{
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
				_Name = ThisMaterialName.StartsWith("newmtl ", StringComparison.OrdinalIgnoreCase) ?
					ThisMaterialName.Remove(0, 7) :
					ThisMaterialName;

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
								notEnoughPartsError(MessageStringBuilder, in LineNumber, "Opacity", "2 parts", in readString);
								break;
							}
							_Opacity = float.Parse(readSubstrings[1], NumberStyles.Float, CultureInfo.InvariantCulture);
							break;

						case "illum":
							// This is an Illumination Model
							illumDecoder(MessageStringBuilder, in LineNumber, in readSubstrings, in readString, ref _IlluminationModel);
							break;

						case "ka":
							// This is an Ambient Colour
							if (readSubstrings.Length is not 4)
							{
								notEnoughPartsError(MessageStringBuilder, in LineNumber, "an Ambient Colour", "4 parts", in readString);
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
								notEnoughPartsError(MessageStringBuilder, in LineNumber, "a Diffuse Colour", "4 parts", in readString);
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
								notEnoughPartsError(MessageStringBuilder, in LineNumber, "a Specular Colour", "4 parts", in readString);
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
								notEnoughPartsError(MessageStringBuilder, in LineNumber, "an Optical Density", "2 parts", in readString);
								break;
							}
							_OpticalDensity = float.Parse(readSubstrings[1], NumberStyles.Float, CultureInfo.InvariantCulture);
							break;

						case "ns":
							// This is a Specular Exponent
							if (readSubstrings.Length is not 2)
							{
								notEnoughPartsError(MessageStringBuilder, in LineNumber, "a Specular Exponent", "2 parts", in readString);
								break;
							}
							_SpecularExponent = float.Parse(readSubstrings[1], NumberStyles.Float, CultureInfo.InvariantCulture);
							break;

						case "tf":
							// This is a Transmission Filter Colour.
							tfDecoder(MessageStringBuilder, LineNumber, in readSubstrings, in readString, ref _TransmissionFilterColour);
							break;

						case "tr":
							// This is a Transparency definition. This is an alternative to "d", and appears to be not as popular.
							// Therefore, convert it to an Opacity definition.
							if (readSubstrings.Length is not 2)
							{
								notEnoughPartsError(MessageStringBuilder, in LineNumber, "Transparency", "2 parts", in readString);
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


			// ==== Private methods ====
			private static void notEnoughPartsError(StringBuilder MessageStringBuilder, in int LineNumber,
				in string ElementName, in string NumberOfParts, in string ReadString)
			{
				MessageStringBuilder.Append($"Error: Line {LineNumber} defining {ElementName} in the MTL stream ");
				MessageStringBuilder.AppendLine($"does not have {NumberOfParts} defined, and will be skipped:");
				MessageStringBuilder.AppendLine($"{ReadString}");
				MessageStringBuilder.AppendLine();
			}

			private void illumDecoder(StringBuilder MessageStringBuilder, in int LineNumber, in string[] ReadSubstrings,
				in string ReadString, ref IlluminationModels IlluminationModel)
			{
				if (ReadSubstrings.Length is not 2)
				{
					notEnoughPartsError(MessageStringBuilder, in LineNumber, "an Illumination Model", "2 parts", in ReadString);
					return;
				}

				int illuminationModelNumber = int.Parse(ReadSubstrings[1]);
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
						IlluminationModel = (IlluminationModels) illuminationModelNumber;
						break;

					default:
						IlluminationModel = IlluminationModels.Undefined;
						break;
				}
			}

			private static void tfDecoder(StringBuilder MessageStringBuilder, int LineNumber,
				in string[] ReadSubstrings, in string ReadString, ref TransmissionColourFilter TransmissionColourFilter)
			{
				if (ReadSubstrings.Length is not (4 or 5))
				{
					MessageStringBuilder.Append(
						$"Error: Line {LineNumber} defining a Transmission Filter Colour in the MTL stream ");
					MessageStringBuilder.AppendLine("does not have 4 or 5 parts defined, and will be skipped:");
					MessageStringBuilder.AppendLine($"{ReadString}");
					MessageStringBuilder.AppendLine();
					return;
				}

				if (ReadSubstrings.Length is 4)
				{
					// This is an RGB Transmission Filter Colour.
					TransmissionColourFilter = new TransmissionColourFilter(
						new Vector3(
							float.Parse(ReadSubstrings[1], NumberStyles.Float, CultureInfo.InvariantCulture),
							float.Parse(ReadSubstrings[2], NumberStyles.Float, CultureInfo.InvariantCulture),
							float.Parse(ReadSubstrings[3], NumberStyles.Float, CultureInfo.InvariantCulture)
						),
						false
					);
					return;
				}

				// Otherwise, this is a CIEXYZ Transmission Filter Colour.
				if (ReadSubstrings[1].Equals("xyz", StringComparison.OrdinalIgnoreCase))
				{
					TransmissionColourFilter = new TransmissionColourFilter(
						new Vector3(
							float.Parse(ReadSubstrings[2], NumberStyles.Float,
								CultureInfo.InvariantCulture),
							float.Parse(ReadSubstrings[3], NumberStyles.Float,
								CultureInfo.InvariantCulture),
							float.Parse(ReadSubstrings[4], NumberStyles.Float, CultureInfo.InvariantCulture)
						),
						true
					);
				}
				else
				{
					MessageStringBuilder.Append(
						$"Error: Line {LineNumber} defining a Transmission Filter Colour in the MTL stream ");
					MessageStringBuilder.AppendLine("is not correctly formatted, and will be skipped:");
					MessageStringBuilder.AppendLine($"{ReadString}");
					MessageStringBuilder.AppendLine();
				}
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
