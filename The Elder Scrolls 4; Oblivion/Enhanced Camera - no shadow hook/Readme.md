## Enhanced Camera - toggleable shadow hook
This is my successful attempt to fix the CTD that occurs if you try to run Oblivion with both Enhanced Camera and Oblivion Reloaded v7.0 or later. 

The starting point for this fix comes courtesy of Alenet, the creator of Oblivion Reloaded, who posted his solution here: [https://forums.nexusmods.com/index.php?/topic/1163835-enhanced-first-person-camera/page-79#entry72840463](https://forums.nexusmods.com/index.php?/topic/1163835-enhanced-first-person-camera/page-79#entry72840463)

Basically, the problem was that both Oblivion Reloaded and Enhanced Camera were trying to create a hook in Oblivion.exe at the same place to manipulate the game's shadows. This meant that the two hooks overwrote each other and caused the game to jump to a section of code that was not meant to run in the way it was being made to, which caused unpredictable results and the game crashed.

I initially attempted to create a DLL with just this modification applied. However, it soon became clear that this was not enough as two issues manifested themselves as a result of disabling EC's hook:
1. The player has four arms visible after unsheathing their weapon/ raising their fists.
2. Enhanced Camera is never informed that the player has exited a conversation. I've personally seen this as the Player Character's body disappearing in both First and Third Person mode. I've also seen it reported that head bobbing stops working as well. This happened until the player loaded a save.

After doing further research in EC's source code, I discovered that the function that is supposed to run through that disabled hook had code in it that detects if the player has exited a conversation and hides body parts when in first person. Long story short, I moved all of this code out of the Shadow hook function and into a function that doesn't depend on the shadow hook (specifically, it's all now in the UpdateCamera function).

I also managed to refine the Alenet's shadow hook change so that, instead of completely removing from the DLL, I made the creation of that hook dependant on the *bFirstPersonShadows* setting in EC's INI file. If it is set to 0, the hook isn't created and is if set to 1.

I can confirm that these fixes do work. The game works as it should with this patched DLL and Oblivion Reloaded v7.1 installed. The side-effect of this change is that the Player Character no longer casts a shadow thanks to EC. It is now OR that decides if the player has a shadow or not. Also note that this fix is based on Enhanced Camera v1.4b. If there is a later version available, check if that version has fixed this issue and rather use that if it has.

## Known issues
1. Enhanced Camera's first person shadow rendered the PC's entire body, even if you had certain body parts set to be hidden (such as the head). This is not the case if you use Oblivion Reloaded to create this shadow. Unfortunately, I have no idea what can be done to fix this. I have included screenshots of the difference next to this readme.

## Installation
Download and install the regular version of Enhanced Camera as per it's installation instructions. It can be downloaded from here: [https://www.nexusmods.com/oblivion/mods/44337?tab=files](https://www.nexusmods.com/oblivion/mods/44337?tab=files)

After that, navigate to the location you installed Oblivion. Open the *Data/OBSE/Plugins* folder. Next, download the DLL that is next to this readme and place it in the above folder, overwriting the previous DLL.

Finally, if you are using Oblivion Reloaded v7.0 or later, make sure that *bFirstPersonShadows* in EC's INI file is set to 0. If this setting isn't present, you don't have to do anything else.

## Further development and fixes?
I have very little experience with C programming. As a result, I have no intention to put any further development into this project or fix any bugs that may be present. I am releasing this as is. Please don't give me any requests or suggestions. This is simply because I very likely will have no idea how to fulfil your request. If you would like something to be implemented, please rather ask LogicDragon or find another modder that is willing and able to do what you ask. Alternatively, the source code is available. Maybe you can look into doing it yourself.

## Source code?
The source code for my DLL is exactly the same as the original source for Enhanced Camera v1.4b except with my change applied. That original source can be found here: [https://www.nexusmods.com/oblivion/mods/44337?tab=files](https://www.nexusmods.com/oblivion/mods/44337?tab=files)

The changes I made are as follows:

Line 835 has the following:
```C++
}
```
Replace it with this:
```C++
} else if (g_inDialogueMenu != 0) {    // Since the original conditional is "if (g_inDialogueMenu == 0)", using just "else" here should be fine.
    // Detects if player is exiting dialogue menu
    float dialogZoomPercent = *(float *)0x00B13FCC;
    if (im->IsGameMode() && dialogZoomPercent == 0) {
        g_inDialogueMenu = 0;
    }
}
```
and add the following immediately after the above new code:
```C++
if (g_isThirdPerson == 0) {
	UpdateSkeletonNodes(0);

	ThisStdCall(0x00471F20, proc->animData); // applies animData to skeleton

	// Resets scale to prevent bug with arms/head disappearing
	// when equipping items in the inventory menu
	UpdateSkeletonNodes(1);

	// Fix bug where game uses 3rd person magicNode causing spells to be
	// casted from wrong position. Move it to same position as first person node.
	NiNode * nMagicNodeFirst = FindNode((*g_thePlayer)->firstPersonNiNode, "magicNode");
	NiNode * nMagicNodeThird = FindNode((*g_thePlayer)->niNode, "magicNode");
	nMagicNodeThird->m_worldTranslate.x = nMagicNodeFirst->m_worldTranslate.x;
	nMagicNodeThird->m_worldTranslate.y = nMagicNodeFirst->m_worldTranslate.y;
	nMagicNodeThird->m_worldTranslate.z = nMagicNodeFirst->m_worldTranslate.z;
}
```

Next, scroll down to line 1559 where you will find:
```C++
// Disables visibility of arms/head nodes
UpdateSkeletonNodes(0);

HighProcess * proc = static_cast<HighProcess *>((*g_thePlayer)->process);
ThisStdCall(0x00471F20, proc->animData); // applies animData to skeleton

// Resets scale to prevent bug with arms/head disappearing
// when equipping items in the inventory menu
UpdateSkeletonNodes(1);

// Fix bug where game uses 3rd person magicNode causing spells to be
// casted from wrong position. Move it to same position as first person node.
NiNode * nMagicNodeFirst = FindNode((*g_thePlayer)->firstPersonNiNode, "magicNode");
NiNode * nMagicNodeThird = FindNode((*g_thePlayer)->niNode, "magicNode");
nMagicNodeThird->m_worldTranslate.x = nMagicNodeFirst->m_worldTranslate.x;
nMagicNodeThird->m_worldTranslate.y = nMagicNodeFirst->m_worldTranslate.y;
nMagicNodeThird->m_worldTranslate.z = nMagicNodeFirst->m_worldTranslate.z;
```
Delete it all or comment it all out.

From line 1580 is the following:
```C++
if (toggleNodes == 0) {
	// Detects if player is exiting dialogue menu
	InterfaceManager * im = InterfaceManager::GetSingleton();
	if (g_inDialogueMenu != 0) {
		float dialogZoomPercent = *(float *)0x00B13FCC;
		if (im->IsGameMode() && dialogZoomPercent == 0) {
			g_inDialogueMenu = 0;
		}
	}
}
```
Delete or comment this out as well.

Finally, on line 1671, you will find this:
```C++
WriteRelJump(kShadowsHook,(UInt32)UpdateShadows);
```
Replace it with this:
```C++
if (g_bFirstPersonShadows != 0) {
	WriteRelJump(kShadowsHook,(UInt32)UpdateShadows);
}
```

Just apply the above changes, compile it and your DLL will be the same as mine.
