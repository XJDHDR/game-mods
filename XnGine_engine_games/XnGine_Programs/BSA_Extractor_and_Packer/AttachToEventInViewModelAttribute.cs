// This file is or was originally a part of the Game Mods project by XJDHDR, which can be found here:
// https://github.com/XJDHDR/game-mods
//
// The license for it may be found here:
// https://github.com/XJDHDR/game-mods/blob/master/LICENSE.md
//

using System;
using JetBrains.Annotations;

namespace BSA_Extractor_and_Packer;


[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
internal sealed class AttachToEventInViewModelAttribute : Attribute
{
	internal string EventName;
	internal string InstanceFieldName;

	public AttachToEventInViewModelAttribute(string EventName, string InstanceFieldName)
	{
		this.EventName = EventName;
		this.InstanceFieldName = InstanceFieldName;
	}
}
