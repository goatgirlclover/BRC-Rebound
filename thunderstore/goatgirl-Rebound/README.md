# Rebound
A Hover-inspired plugin for Bomb Rush Cyberfunk that adds the Rebound, a timing-based trick that offers new air mobility options. Tapping the jump button as soon as you hit the ground will trigger a Rebound, launching you back into the air while preserving momentum and speed. 

**Note:** If playing with MovementPlus 3.0, for better compatibility with the **Wave Dash** move, either:
- lower the Rebound grace period and increase the Wave Dash grace period - tapping quickly results in a Rebound, and tapping slower results in a Wave Dash
    - Rebound overrides Wave Dash, so the Rebound grace period must always be lower than the Wave Dash grace period 
- use a Rebound Modifier Action in the config file to require holding another action to Rebound
    - For example, using "trickAny" as a Rebound Modifier will require holding any of the trick buttons, as well as pressing jump, to Rebound

## Features
* **Rebound**: Tap the jump button as soon as you land to launch right back into the air!
    * Cancellable by holding the slide button (or whatever input settings you prefer)
* **Boosted Rebound**: Hold boost while performing a Rebound to automatically perform a Boost Trick, refiling your meter and launching you forwards!
* **Plenty of configuration options** to allow you to tune the move exactly how you'd like it

## Installation
* If installing using r2modman, just click "Install with Mod Manager"
* If installing manually, extract the .zip and drop the Rebound.dll file into your \BepInEx\plugins\ folder 