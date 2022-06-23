// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System;
using System.IO;
using System.Numerics;
using ThreeD_Obj_Converter.Models.OBJ_format;
using ThreeD_Obj_Converter.Models.ThreeD_format;

namespace ThreeD_Obj_Converter.Models
{
	internal struct Convert3DToObj
	{
		internal static void _Convert(string InputPath, string ThreeDFileName, string OutputPath)
		{
			ThreeDHeader threeDHeader;
			ThreeDBody threeDBody;
			using (FileStream input3DStream = new($"{InputPath}/{ThreeDFileName}.3D", FileMode.Open))
			{
				using (BinaryReader input3DBinaryReader = new(input3DStream))
				{
					threeDHeader = new(input3DBinaryReader, out bool wasSuccessful);

					if (!wasSuccessful)
						return;

					threeDBody = new(input3DBinaryReader, threeDHeader);
				}
			}

			// Build the OBJ file's header
			string headerComments = "OBJ file created using XJDHDR's 3D to OBJ converter";

			// Translate all of the textures used by the model into MTL format.
			MtlFileData.MaterialData[] allMaterialData = new MtlFileData.MaterialData[threeDBody._PlaneLists.Length];
			for (int i = 0; i < threeDBody._PlaneLists.Length; ++i)
			{
				allMaterialData[i] = new(
					$"{threeDBody._PlaneLists[i]._TextureFileIndex}_{threeDBody._PlaneLists[i]._TextureImageIndex}-0",
					new Vector3(1, 1, 1),
					new Vector3(1, 1, 1),
					new Vector3(0, 0, 0),
					1,
					1,
					new MtlFileData.MaterialData.TransmissionColourFilter(new Vector3(0, 0, 0), false),
					1,
					MtlFileData.MaterialData.IlluminationModels.ColorOnAndAmbientOff,
					$"textures/{threeDBody._PlaneLists[i]._TextureFileIndex}_{threeDBody._PlaneLists[i]._TextureImageIndex}-0.png",
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty,
					string.Empty
				);
			}
			MtlFileData mtlFileData = new("MTL file created using XJDHDR's 3D to OBJ converter",
				Array.Empty<string>(), Array.Empty<int>(), allMaterialData);
			ObjFileData.MaterialLibraryDefinition[] materialLibraries = new ObjFileData.MaterialLibraryDefinition[1];
			materialLibraries[0] = new($"{ThreeDFileName}.mtl",mtlFileData);

			// Translate all of the model's vertices into OBJ format.

			ObjFileData objFileData = new(headerComments, Array.Empty<string>(), Array.Empty<int>(), materialLibraries,
				);

			using (FileStream outputObjStream = new(OutputPath, FileMode.Create))
			{
				objFileData._Write(outputObjStream);
			}
		}
	}
}
