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
	internal readonly struct MtlFileData
	{
		/// <summary> The header's comments, including newlines and the starting # for each line. </summary>
		internal readonly string _HeaderComments;

		internal readonly string[] _InlineCommentStrings;
		internal readonly int[] _InlineCommentStartIndex;

		internal readonly string[] _AllMaterialNames;
		internal readonly Vector3[] _AllMaterialAmbientColours;
		internal readonly Vector3[] _AllMaterialDiffuseColours;
		internal readonly Vector3[] _AllMaterialSpecularColours;
		internal readonly float[] _AllMaterialSpecularExponents;
		internal readonly float[] _AllMaterialOpacities;
		internal readonly Vector3[] _AllMaterialTransmissionFilterColours;
		internal readonly float[] _AllMaterialOpticalDensities;

		/// <summary>
		/// Used to define the Illumination Models that are used for each material. The Models are as follows:<para />
		/// 0. Color on and Ambient off<br/>
		/// 1. Color on and Ambient on<br/>
		/// 2. Highlight on<br/>
		/// 3. Reflection on and Ray trace on<br/>
		/// 4. Transparency: Glass on, Reflection: Ray trace on<br/>
		/// 5. Reflection: Fresnel on and Ray trace on<br/>
		/// 6. Transparency: Refraction on, Reflection: Fresnel off and Ray trace on<br/>
		/// 7. Transparency: Refraction on, Reflection: Fresnel on and Ray trace on<br/>
		/// 8. Reflection on and Ray trace off<br/>
		/// 9. Transparency: Glass on, Reflection: Ray trace off<br/>
		/// 10. Casts shadows onto invisible surfaces<br/>
		/// </summary>
		internal readonly byte[] _AllMaterialIlluminationModels;

		internal readonly string[] _AllMaterialAmbientTextureMaps;
		internal readonly string[] _AllMaterialDiffuseTextureMaps;
		internal readonly string[] _AllMaterialSpecularColourTextureMaps;
		internal readonly string[] _AllMaterialSpecularHighlightTextureMaps;
		internal readonly string[] _AllMaterialAlphaTextureMaps;
		internal readonly string[] _AllMaterialBumpMaps;
		internal readonly string[] _AllMaterialDisplacementMaps;
		internal readonly string[] _AllMaterialStencilDecalTextures;


		internal MtlFileData(Stream MtlDataStream)
		{

		}


		internal void _Write(Stream OutputStream)
		{
			StringBuilder outputStringBuilder = new();

			outputStringBuilder.AppendLine(_HeaderComments);
			outputStringBuilder.AppendLine();

			for (int i = 0; i < _AllMaterialNames.Length; ++i)
			{
				writeIndividualMaterialToStringBuilder(outputStringBuilder, i);
			}

			StringBuilder.ChunkEnumerator stringBuilderChunks = outputStringBuilder.GetChunks();
			foreach (ReadOnlyMemory<char> individualStringBuilderChunk in stringBuilderChunks)
			{
				Span<byte> sBChunkBytes = new byte[individualStringBuilderChunk.Length];
				Encoding.ASCII.GetBytes(individualStringBuilderChunk.Span, sBChunkBytes);
				OutputStream.Write(sBChunkBytes);
			}
		}

		private void writeIndividualMaterialToStringBuilder(StringBuilder OutputStringBuilder, int I)
		{
			OutputStringBuilder.AppendLine($"newmtl {_AllMaterialNames[I]}");

			if (I < _AllMaterialAmbientColours.Length)
				OutputStringBuilder.AppendLine(
					$"Ka {_AllMaterialAmbientColours[I].X} {_AllMaterialAmbientColours[I].Y} {_AllMaterialAmbientColours[I].Z}"
				);

			if (I < _AllMaterialDiffuseColours.Length)
				OutputStringBuilder.AppendLine(
					$"Kd {_AllMaterialDiffuseColours[I].X} {_AllMaterialDiffuseColours[I].Y} {_AllMaterialDiffuseColours[I].Z}"
				);

			if (I < _AllMaterialSpecularColours.Length)
				OutputStringBuilder.AppendLine(
					$"Ks {_AllMaterialSpecularColours[I].X} {_AllMaterialSpecularColours[I].Y} {_AllMaterialSpecularColours[I].Z}"
				);

			if (I < _AllMaterialSpecularExponents.Length)
				OutputStringBuilder.AppendLine($"Ns {_AllMaterialSpecularExponents[I]}");

			if (I < _AllMaterialOpacities.Length)
				OutputStringBuilder.AppendLine($"d {_AllMaterialOpacities[I]}");

			if (I < _AllMaterialTransmissionFilterColours.Length)
				OutputStringBuilder.AppendLine(
					$"Tf {_AllMaterialTransmissionFilterColours[I].X} {_AllMaterialTransmissionFilterColours[I].Y} {_AllMaterialTransmissionFilterColours[I].Z}"
				);

			if (I < _AllMaterialOpticalDensities.Length)
				OutputStringBuilder.AppendLine($"Ni {_AllMaterialOpticalDensities[I]}");

			if (I < _AllMaterialIlluminationModels.Length)
				OutputStringBuilder.AppendLine($"illum {_AllMaterialIlluminationModels[I]}");


			if (I < _AllMaterialAmbientTextureMaps.Length)
				OutputStringBuilder.AppendLine($"map_Ka {_AllMaterialAmbientTextureMaps[I]}");

			if (I < _AllMaterialDiffuseTextureMaps.Length)
				OutputStringBuilder.AppendLine($"map_Kd {_AllMaterialDiffuseTextureMaps[I]}");

			if (I < _AllMaterialSpecularColourTextureMaps.Length)
				OutputStringBuilder.AppendLine($"map_Ks {_AllMaterialSpecularColourTextureMaps[I]}");

			if (I < _AllMaterialSpecularHighlightTextureMaps.Length)
				OutputStringBuilder.AppendLine($"map_Ns {_AllMaterialSpecularHighlightTextureMaps[I]}");

			if (I < _AllMaterialAlphaTextureMaps.Length)
				OutputStringBuilder.AppendLine($"map_d {_AllMaterialAlphaTextureMaps[I]}");

			if (I < _AllMaterialBumpMaps.Length)
				OutputStringBuilder.AppendLine($"bump {_AllMaterialBumpMaps[I]}");

			if (I < _AllMaterialDisplacementMaps.Length)
				OutputStringBuilder.AppendLine($"disp {_AllMaterialDisplacementMaps[I]}");

			if (I < _AllMaterialStencilDecalTextures.Length)
				OutputStringBuilder.AppendLine($"decal {_AllMaterialStencilDecalTextures[I]}");

			OutputStringBuilder.AppendLine();
		}
	}
}
