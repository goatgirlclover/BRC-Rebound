# Rebound
A Hover-inspired plugin for Bomb Rush Cyberfunk that adds the Rebound, a timing-based trick that offers new air mobility options. Tapping the jump button as soon as you hit the ground will trigger a Rebound, launching you back into the air while preserving momentum and speed. 

## Features
* **Rebound**: Tap the jump button as soon as you land to launch right back into the air!
    * Cancellable by holding the slide button (or whatever input settings you prefer)
* **Boosted Rebound**: Hold boost while performing a Rebound to automatically perform a Boost Trick, refiling your meter and launching you farther!
* **(New!) Burst Rebound**: Hold back and boost while Rebounding to do a Burst Rebound, converting your forward speed directly into height
* **Plenty of configuration options** to allow you to tune the move exactly how you'd like it
* **Custom Rebound animations**, configurable for each movestyle

## MovementPlus 2.9 Compatibility
By default, Rebounding only requires pressing the jump button shortly after landing. To avoid overlapping with MovementPlus 2.9's **Wave Dash**, either:
- use a **Rebound Modifier Action** in the config file (recommended)
    - Using a Modifier Action requires you to hold that button while pressing jump to Rebound
    - "**trickAny**" or "**slide**" are the most commonly used Modifier Actions
        - On an Xbox controller, trick1 is X, trick2 is Y, and trick3 is B. "trickAny" detects any of the 3
        - Set to trick3 if using the Project G config - ***if your Rebounds have "stopped working" after installing M+ 2.9, this is why!***
    - Jumping within this window without holding a Modifier Action will result in a Wave Dash instead
- lower the Rebound grace period and increase the Wave Dash grace period 
    - Tapping quickly results in a Rebound, and tapping slower results in a Wave Dash
    - Rebound always overrides Wave Dash, so *the Rebound grace period must be lower than the Wave Dash grace period*
    - **Not recommended** due to the timing being tricky to pull off consistently

## Installation
* If installing using r2modman, just click "Install with Mod Manager"
* If installing manually, extract the .zip and drop the Rebound.dll file into your \BepInEx\plugins\ folder 

## Credits
Thanks to:
- Woodztock and Lazy Duchess for their work on NewTrix, which I referenced to support custom animations
- Fay for helping me test versions 2.0 and onward :3