## Enhanced Camera - no shadow hook
This is my attempt to fix the CTD that occurs if you try to run Oblivion with both Enhanced Camera and Oblivion Reloaded v7.0 or later. 

The fix comes courtesy of Alenet, the creator of Oblivion Reloaded, who posted his solution here: [https://forums.nexusmods.com/index.php?/topic/1163835-enhanced-first-person-camera/page-79#entry72840463](https://forums.nexusmods.com/index.php?/topic/1163835-enhanced-first-person-camera/page-79#entry72840463)

All I did was apply his suggestion to the EC source code then compile it. I can confirm that the proposed fix does work. The game works as it should with this patched DLL and Oblivion Reloaded v7.1 installed. The side-effect of this change is that the Player Character no longer casts a shadow thanks to EC. It is now OR that decides if the player has a shadow or not. Also note that this fix is based on Enhanced Camera v1.4b. If there is a later version available, check if that version has fixed this issue and rather use that if it has.

## Installation
Download and install the regular version of Enhanced Camera as per it's installation instructions. It can be downloaded from here: [https://www.nexusmods.com/oblivion/mods/44337?tab=files](https://www.nexusmods.com/oblivion/mods/44337?tab=files)
After that, navigate to the location you installed EC. Open the *OBSE/Plugins* folder. Next, download the DLL that is next to this readme and place it in the above folder, overwriting the previous DLL.

## Further development and fixes?
I have no intention to put any further development into this project or fix any bugs that may be present. I am releasing this as is. Please don't give me any requests or suggestions. If you would like something to be implemented, please rather ask LogicDragon or find another modder that is willing to do what you ask. Alternatively, the source code is available. Maybe you can look into doing it yourself.

## Source code?
The source code for my DLL is exactly the same as the original source for Enhanced Camera v1.4b except with Alenet's proposed change applied. That original source can be found here: [https://www.nexusmods.com/oblivion/mods/44337?tab=files](https://www.nexusmods.com/oblivion/mods/44337?tab=files)
Just apply the suggested change, compile it and your DLL will be the same as mine.
