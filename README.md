# Rebound
A Hover-inspired plugin for Bomb Rush Cyberfunk that adds the Rebound, a timing-based trick that offers new air mobility options. 


![Rebound](https://github.com/scoopds/BRC-Rebound/blob/fec2b2adf0a51ab6a0c0d61dfcf6beeab2e9199a/ref/showcase.gif)
## Features
* **Rebound**: Tap the jump button as soon as you land to launch right back into the air!
    * Cancellable by holding the slide button (or whatever input settings you prefer)
* **Boosted Rebound**: Hold boost while performing a Rebound to automatically perform a Boost Trick, refiling your meter and launching you forwards!
* **Plenty of configuration options** to allow you to tune the move exactly how you'd like it
## Installation
* If installing using r2modman, just click "Install with Mod Manager"
* If installing manually, extract the .zip and drop the Rebound.dll file into your \BepInEx\plugins\ folder 
## Building from Source
This plugin requires the following .dlls to be placed in the \lib\ folder:
* A [publicized](https://github.com/BepInEx/BepInEx.AssemblyPublicizer) version of the game's code (Assembly-CSharp.dll)
* 0Harmony.dll and BepInEx.dll from \BepInEx\core
* [NewTrix.dll](https://thunderstore.io/c/bomb-rush-cyberfunk/p/Woodztock/NewTrix) (for NewTrix + BunchOfEmotes compatibility)

With these files, run "dotnet build" in the project's root folder and the .dll will be placed in the \bin\ folder.