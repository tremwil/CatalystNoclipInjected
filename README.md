# CatalystNoclipInjected
![alt tag](https://raw.githubusercontent.com/tremwil/CatalystNoclipInjected/master/app_snapshot.PNG)

### NOTE: This project is no longer maintained. Feel free to continue reporting any issues! If you know a fix for them, I'll include it in the readme, but I won't work on the code or release new versions.


An injected version of my original CatalystNoclip program. It is recommended to switch to this version since not only it comes with greatly improved performance and CPU usage but also with many bugfixes.

# Known bugs and their solution
- If you don't have the right version of Visual C++, the injected DLL will not behave properly. A known fix is to include some DLLs within the game files folder (alongside the game executable). They can be downloaded [here](https://github.com/tremwil/CatalystNoclipInjected/raw/master/DLLs.7z).

# Download
[CatalystNoclipInjected-V-1.0.zip](https://github.com/tremwil/CatalystNoclipInjected/releases/download/v1.0/CatalystNoclipInjected-V-1.0.zip)


# How to use it
To use noclip, you must inject it inside the game. This can be done manually by pressing the "Inject" button or by activating the "Auto Inject" option which will automatically inject the DLL when the game starts.

There are two noclip modes: Real-time (RT) and Frozen-Time (FT). The first one allows you to move your camera around, but suffers from graphical instability. The trick to use this mode without any hassle is to enable it while performing the crouch glitch. As for FT noclip, it freezes the game completely so it is much more stable. The disadvantages of this mode is that not only will the camera be frozen, but the game will not load unloaded areas as you approach them. If you want to change your speed, you can use the "Move Slower" and "Move Faster" hotkeys. To move around, use the keybindings you defined for the game (usually WASD). Use the key mapped to jump to go up and the one mapped to crouch to go down. To change the current noclip mode, use the "Switch Mode" hotkey. To set noclip on/off, use the "Toggle Noclip" one. 

There is another feature called NoStumble. Once its corresponding hotkey is pressed, You will not be able to die or stumble from fall damage. There is also a "Kill Yourself" button, which is there to quickly fix noclip as it only works every two deaths. Note that the KYS button is a work-in-progress and requires you to activate it in mid-air.
