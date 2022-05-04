// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System.Numerics;

namespace ThreeD_Obj_Converter.Models.OBJ_format
{
	internal readonly struct ObjFileData
	{
		internal readonly string _HeaderComments;
		internal readonly Vector4[] _AllVertices;
		internal readonly Vector3[] _AllVertexNormals;
		internal readonly Vector3[] _AllVertexTextures;
		
	}
}
