// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Windows;
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.Enums;
using ThreeD_Obj_Converter.Models.OBJ_format;
using ThreeD_Obj_Converter.Models.ThreeD_format;

namespace ThreeD_Obj_Converter.Models
{
	internal struct Convert3DToObj
	{
		/// <summary>
		/// Converts a 3D model into an OBJ.
		/// </summary>
		/// <param name="InputPath">The folder which contains the 3D model that needs to be converted.</param>
		/// <param name="ThreeDFileName">The file name (without extension) of the 3D model that needs to be converted.</param>
		/// <param name="OutputPath">The folder where the OBJ and associated MTL files will be saved.</param>
		internal static void _Convert(string InputPath, string ThreeDFileName, string OutputPath)
		{
			ThreeDHeader threeDHeader;
			ThreeDBody threeDBody;
			using (FileStream input3DStream = new($"{InputPath}/{ThreeDFileName}", FileMode.Open))
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

			// Before constructing the OBJ data, group together faces that have a common texture
			// Also, translate all of the model's PlanePoints into OBJ Faces.
			int pointOffsetDivisor;
			switch (threeDHeader._Version)
			{
				case 2.5f:
					pointOffsetDivisor = 4;
					break;

				default:
					pointOffsetDivisor = 12;
					break;
			}
			int loopCounter = 1;
			List<string> uniqueTexturesList = new();
			List<List<ObjFileData.FaceDefinition>> facesAssociatedWithTextureLists = new();
			for (int i = 0; i < threeDHeader._PlaneCount; ++i)
			{
				// Convert the current Plane into an OBJ Face
				ObjFileData.FaceCornerDefinition[] currentFaceCorners = new ObjFileData.FaceCornerDefinition[threeDBody._PlaneLists[i]._PlanePointCount];
				for (int j = 0; j < threeDBody._PlaneLists[i]._PlanePointCount; ++j)
				{
					currentFaceCorners[j] = new(
						(threeDBody._PlaneLists[i]._PlanePoints[j]._PointOffset / pointOffsetDivisor + 1),
						true,
						loopCounter,
						true,
						loopCounter
					);
					++loopCounter;
				}
				ObjFileData.FaceDefinition currentFace = new(currentFaceCorners);

				// Check if the current Plane's texture has already been defined by a previous Plane.
				int currentTextureLocation = uniqueTexturesList.IndexOf($"{threeDBody._PlaneLists[i]._TextureFileIndex}_{threeDBody._PlaneLists[i]._TextureImageIndex}-0");
				if (currentTextureLocation < 0)
				{
					// If not, create a new entry for this texture.
					uniqueTexturesList.Add($"{threeDBody._PlaneLists[i]._TextureFileIndex}_{threeDBody._PlaneLists[i]._TextureImageIndex}-0");
					// Also create a new entry for this Plane and later ones with the same texture.
					facesAssociatedWithTextureLists.Add(new List<ObjFileData.FaceDefinition>());
					facesAssociatedWithTextureLists[^1].Add(currentFace);
					continue;
				}

				// Otherwise, add this plane to the list.
				facesAssociatedWithTextureLists[currentTextureLocation].Add(currentFace);
			}

			// Create the OBJ Material definition
			string[] uniqueTextures = uniqueTexturesList.ToArray();
			ObjFileData.MaterialDefinition[] allMaterials = new ObjFileData.MaterialDefinition[uniqueTextures.Length];
			for (int i = 0; i < allMaterials.Length; ++i)
			{
				allMaterials[i] = new ObjFileData.MaterialDefinition(
					uniqueTextures[i], facesAssociatedWithTextureLists[i].ToArray()
					);
			}

			// Translate all of the textures used by the model into MTL format.
			MtlFileData.MaterialData[] allMaterialData = new MtlFileData.MaterialData[uniqueTextures.Length];
			for (int i = 0; i < uniqueTextures.Length; ++i)
			{
				allMaterialData[i] = new(
					uniqueTextures[i],
					new Vector3(1, 1, 1),
					new Vector3(1, 1, 1),
					new Vector3(0, 0, 0),
					1,
					1,
					new MtlFileData.MaterialData.TransmissionColourFilter(new Vector3(0, 0, 0), false),
					1,
					MtlFileData.MaterialData.IlluminationModels.ColorOnAndAmbientOff,
					$"textures/{uniqueTextures[i]}.png",
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
			Vector4[] allVertices = new Vector4[threeDHeader._PointCount];
			for (int i = 0; i < threeDHeader._PointCount; ++i)
			{
				allVertices[i] = new(threeDBody._PointLists[i]._X, threeDBody._PointLists[i]._Y,
					threeDBody._PointLists[i]._Z, 1);
			}

			// Translate all of the model's PlanePoints into OBJ Texture Coordinates
			List<Vector3> allTextureCoordinatesList = new();
			for (int i = 0; i < threeDHeader._PlaneCount; ++i)
			{
				for (int j = 0; j < threeDBody._PlaneLists[i]._PlanePointCount; ++j)
				{
					allTextureCoordinatesList.Add(new Vector3(threeDBody._PlaneLists[i]._PlanePoints[j]._U,
						threeDBody._PlaneLists[i]._PlanePoints[j]._V, 0));
				}
			}
			Vector3[] allTextureCoordinates = allTextureCoordinatesList.ToArray();

			// Translate all of the model's NormalLists into OBJ Normal coordinates
			Vector3[] allVertexNormals = new Vector3[threeDHeader._PlaneCount];
			for (int i = 0; i < allVertexNormals.Length; ++i)
			{
				allVertexNormals[i].X = threeDBody._NormalLists[i]._X / 256f;
				allVertexNormals[i].Y = threeDBody._NormalLists[i]._Y / 256f;
				allVertexNormals[i].Z = threeDBody._NormalLists[i]._Z / 256f;
			}

			// Finally, gather together all of the above data into a unified OBJ definition.
			ObjFileData objFileData = new(headerComments, Array.Empty<string>(), Array.Empty<int>(), materialLibraries,
				allVertices, allTextureCoordinates, allVertexNormals, Array.Empty<ObjFileData.ObjectDefinition>(),
				Array.Empty<ObjFileData.GroupDefinition>(), allMaterials, Array.Empty<ObjFileData.SmoothingGroup>());

			// Write the OBJ data to file
			try
			{
				using FileStream outputObjStream = new($"{OutputPath}/{ThreeDFileName}.obj", FileMode.Create);
				objFileData._Write(outputObjStream);
			}
			catch (Exception e)
			{
				IMsBoxWindow<ButtonResult>? messageBox = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
					"Exception during File Stream write", $"An Exception occurred while writing out file:\n{e}");
				messageBox.Show();
			}

			// Write all of the MTL data to files
			for (int i = 0; i < objFileData._MaterialLibraries.Length; ++i)
			{
				using FileStream outputMtlStream = new($"{OutputPath}/{objFileData._MaterialLibraries[i]._LibName}.mtl", FileMode.Create);
				objFileData._MaterialLibraries[i]._MtlData._Write(outputMtlStream);
			}
		}
	}
}
