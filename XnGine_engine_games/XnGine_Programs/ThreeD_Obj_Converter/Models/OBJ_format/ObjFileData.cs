// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System.IO;
using System.Numerics;

namespace ThreeD_Obj_Converter.Models.OBJ_format
{
	internal readonly struct ObjFileData
	{
		internal readonly string _HeaderComments;
		internal readonly Vector4[] _AllVertices;
		internal readonly Vector3[] _AllVertexTextures;
		internal readonly Vector3[] _AllVertexNormals;
		internal readonly FaceDefinition[] _AllFaces;

		internal readonly string[] _AllGroupNames;
		internal readonly uint[] _AllGroupStartIndices;

		internal readonly string _MaterialLibraryFilename;
		internal readonly string[] _AllMaterialNames;
		internal readonly uint[] _AllMaterialStartIndices;

		//
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
		internal readonly uint _VertexIndex;
		internal readonly uint _VertexTextureindex;
		internal readonly uint _VertexNormalindex;
	}
}
