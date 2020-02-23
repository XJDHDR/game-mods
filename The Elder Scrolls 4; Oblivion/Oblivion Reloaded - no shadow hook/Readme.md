## Oblivion Reloaded - no shadow hook
This is my attempt to fix the CTD that occurs if you try to run Oblivion with both Enhanced Camera and Oblivion Reloaded v7.0 or later. 

This fix was inspired by the fix that Alenet, the creator of Oblivion Reloaded, posted here to try fix this issue by modifying Enhanced Camera: [https://forums.nexusmods.com/index.php?/topic/1163835-enhanced-first-person-camera/page-79#entry72840463](https://forums.nexusmods.com/index.php?/topic/1163835-enhanced-first-person-camera/page-79#entry72840463)

I initially created a modified version of Enhanced Camera's DLL with Alenet's fix and uploaded it here: [https://github.com/XJDHDR/game-mods/tree/master/The%20Elder%20Scrolls%204%3B%20Oblivion/Enhanced%20Camera%20-%20no%20shadow%20hook](https://github.com/XJDHDR/game-mods/tree/master/The%20Elder%20Scrolls%204%3B%20Oblivion/Enhanced%20Camera%20-%20no%20shadow%20hook)
However, this fixed DLL caused other problems as noted in the readme found at the above link.

After this, I had an idea. The problem is caused by Oblivion Reloaded and Enhanced Camera both trying to hook the same part of Oblivion's code and the solution proposed was to remove the hook that EC creates. I decided to see what would happen if I instead removed the hook from OR's code. I did that and the result was that this also fixed the crash.

As a result of this testing, I have modified Oblivion Reloaded so that it's creation of this conflicting hook is controlled by the Shadow Mode Enabled setting in OR's INI file. When Shadow Mode is disabled, OR will not create this hook. It will only do so if Shadow Mode is enabled. This does, of course, also mean that you will have to use Oblivion Reloaded without it's enhanced shadows while Enhanced Camera is installed.

## Known issues
- The DLL I have provided here was compiled from the source code for Oblivion Reloaded v7.0, not the latest version (v7.1). This is simply because Alenet has not provided the source code for this version.
- As stated above, my fix means that you can't use OR's enhanced shadows in conjunction with EC.
- I have not extensively tested my modifications. I didn't see any issues in my brief testing but there may be something that my testing didn't reveal. Please do inform me if you spot something.

## Installation
- If you previously installed my Enhanced Camera DLL, delete it and replace it with the original.
- Download and install the regular version of Oblivion Reloaded v7.0 as per it's installation instructions. It can be downloaded from here: [http://tesreloaded.com/thread/2/package](http://tesreloaded.com/thread/2/package)
- After that, navigate to the location you installed Oblivion. Open the *Data/OBSE/Plugins* folder. Next, download the DLL that is next to this readme and place it in the above folder, overwriting the previous DLL.
- Finally, open the *OblivionReloaded.ini* file and find the setting: *ShadowMode->Enabled* and check if it says *= 1*. If it does, you need to change the 1 into a 0 to disable OR's shadow mode. If you leave it at 1, OR will still try to create the hook and the game will crash once again.

## Further development and fixes?
I have very little experience with C programming. I would like to update my DLL to add my fix to OR version 7.1, which can only happen when the source code for that version becomes available. Otherwise, I have no intention to put any further development into this project or fix any bugs that may be present. I am releasing this as is. Please don't give me any requests or suggestions. This is simply because I very likely will have no idea how to fulfil your request. If you would like something to be implemented, please rather ask Alenet or find another modder that is willing and able to do what you ask. Alternatively, the source code is available. Maybe you can look into doing it yourself.

## Source code?
The source code for my DLL is exactly the same as the original source for Oblivion Reloaded v7.0 except with my fix applied That original source can be found here: [https://github.com/Alenett/TES-Reloaded-Source](https://github.com/Alenett/TES-Reloaded-Source)

My fix is as follows:
1. Go to the location you downloaded and/or extracted the OR source code to and open this file in a text editor **TES-Reloaded-Source-master\OR\OblivionReloaded\main.cpp**. 
2. Scroll to line 69, or the line that says: *CreateShadowsHook();*
3. Edit this line to instead say *if (TheSettingManager->SettingsMain.ShadowMode.Enabled) CreateShadowsHook();* and save the file.
4. Install **Install Visual Studios 2015** and the **DirectX SDK**.
5. Open **Visual Studios 2015**, load the Oblivion Reloaded VCProject into it and then build it.