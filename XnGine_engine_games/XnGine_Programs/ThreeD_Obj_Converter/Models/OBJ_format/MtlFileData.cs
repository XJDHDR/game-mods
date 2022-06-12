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

			StringBuilder commonStringsBuilder = new();

			using (StreamReader mtlDataStreamReader = new(MtlDataStream))
			{
				string? readString;

				// Read the comments in the header.
				for (int i = 0; i < int.MaxValue; ++i)
				{
					readString = mtlDataStreamReader.ReadLine()?.Trim();

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

				// Skip past any empty lines between the header comments and data.
				for (int i = 0; i < int.MaxValue; ++i)
				{
					readString = mtlDataStreamReader.ReadLine()?.Trim();

					// Check if the end of the stream has been reached.
					if (readString == null)
						break;

					// Check if this is an empty line.
					if (string.IsNullOrWhiteSpace(readString))
						continue;

					// Since this line is not empty, move on to reading the other file data.
					break;
				}

				//
			}



			List<MaterialData> allMaterialsList = new();


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
			internal readonly Vector3 _TransmissionFilterColour;
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


			internal MaterialData(Stream MtlDataStream)
			{
			}

			internal void _Write(Stream OutputStream)
			{
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
