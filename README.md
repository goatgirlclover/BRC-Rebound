<p align="center"> <img src="icon.png" alt="Rebound icon" width="200"/> </p> 
<h1> <p align="center" > Rebound </p> </h1> 
A Hover-inspired plugin for Bomb Rush Cyberfunk that adds the Rebound, a timing-based trick that offers new air mobility options. Installation and more info can be found on [Thunderstore](https://thunderstore.io/c/bomb-rush-cyberfunk/p/goatgirl/Rebound/).
## Features
* **Rebound**: Tap the jump button as soon as you land to launch right back into the air!
    * Cancellable by holding the slide button (or whatever input settings you prefer)
* **Boosted Rebound**: Hold boost while performing a Rebound to automatically perform a Boost Trick, refiling your meter and launching you forwards!
* **Plenty of configuration options** to allow you to tune the move exactly how you'd like it
## Building from Source
This plugin requires the following .dlls to be placed in the \lib\ folder:
* A [publicized](https://github.com/BepInEx/BepInEx.AssemblyPublicizer) version of the game's code (Assembly-CSharp.dll)
* 0Harmony.dll and BepInEx.dll from \BepInEx\core
* [NewTrix.dll](https://thunderstore.io/c/bomb-rush-cyberfunk/p/Woodztock/NewTrix) (for NewTrix + BunchOfEmotes compatibility)

With these files, run "dotnet build" in the project's root folder and the .dll will be placed in the \bin\ folder.
