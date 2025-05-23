## 3.1.0
* Improved Burst Rebound math - Burst Rebound Power options are more balanced
* Burst Rebounds can now be disabled in their own configuration option, independent from Burst Rebound power

## 3.0.3 
* Fixed an issue where Boost Rebounding without QuickTurn installed would cause a crash

## 3.0.2
* (Outdated) Burst Rebounds are now disablable by setting Burst Rebound Power to zero
* All Rebounds (not just Boosted/Burst) can now interrupt Quick Turn
* (3.0.1) Corrected plugin version number

## 3.0.0
* Added **Burst Rebounds**: Hold back (relative to the player) and boost while Rebounding to do a Burst Rebound, converting your forward speed directly into height
    * Can also be activated during a Quick Turn (but must still fit within the grace period!)
    * New configuration options: 
        * **Burst Rebound Power**: Adjust how much forward speed you lose; higher values preserve more forward speed, but reduce Burst Rebound height
        * Sensitivity and deadzone settings
* New configuration option: Practice Mode
    * When enabled, your character will be colored pink when it is possible to Rebound
* Boosted Rebounds now send you slightly higher and farther 
* Slightly increased default Rebound grace period to 0.2 
* New Rebound.NewTrix configuration options:
    * **Directional Tricks**: If enabled, holding a specific direction with the left stick can activate custom Rebound animations, as an alternative to holding the trick buttons
        * Left maps to Trick 0, Up maps to Trick 1, and Right maps to Trick 2
        * Holding down or no direction maps to the default
        * Also includes deadzone settings
    * **Required Inputs for Custom Animations**: A list of ALL the additional player actions that need to be held to activate a custom Rebound animation (in addition to trick buttons, or directions if Directional Tricks is enabled)
        * Blank by default - only trick buttons / directions are required
        * Without pressing these buttons, the default animation will play


## 2.3.1
* New Rebound.NewTrix configuration option: Force Animation Blending
    * The speed at which custom Rebound animations blend into the fall animation (larger numbers are slower transitions)
        * Allows custom Rebound animations to blend smoothly
        * May cause jank - many BOE animations weren't built to blend like this!
    * Set to 0 by default (no effect) due to aformentioned jank - 0.75 recommended if you can deal with it
* BunchOfEmotes animations can be configured by name - **z-codes are no longer needed**
    * Calls NewTrix methods directly; naturally, requires both NewTrix and BunchOfEmotes to be installed

## 2.3.0 
* **Added custom Rebound animations**
    * Rebound animations for each movestyle and each trick button can now be configured in a new "goatgirl.Rebound.NewTrix.cfg" configuration file
    * Animation names follow the same rules as the NewTrix updates
        * **BunchOfEmotes animations are supported** 
    * New Rebound.NewTrix configuration option: Multiple Tricks for Boosted Rebounds (enabled by default)
        * Holding a trick button while doing a Boosted Rebound will perform the boost trick associated with that button
* Reverted a change that made Rebounds too sensitive (could be triggered when jumping off rails or billboards)

## 2.2.0 
* Rebounding while holding slide is now much more consistent and easier to pull off
    * Adjusted timing to match non-sliding Rebounds
    * Preserved jump buffering for Rebounds
    * Consecutive Rebounds are much easier to pull off when using slide as a Modifier Action
* Reduced sensitivity of the "Prevent Combo Extension" configuration option, preventing wrongly dropped combos
* Adjusted input detection to be more consistent
* Clarified MovementPlus 3.0 compatibility on the mod page

## 2.1.0
* Reverted some changes from 2.0.0 to make Rebounds more consistent
    * Intended to fix random Rebounds and the grace period being extended occasionally; however, these issues may have stuck around, so let me know if there's still any weird behavior!
* Fixed the Rebound trail breaking the boost trail after being activated
* The Rebound trail can now be disabled in the configuration file
* (2.1.1) Fixed a syntax error that made some patches apply to AI players


## 2.0.0 (New Year Update)
* New configuration option: Prevent Combo Extension (enabled by default)
    * Prevents using a long grace period to artificially extend combos
    * Detects if an action other than a Rebound is taken during the grace period and cancels the combo accordingly
    * If using MovementPlus, this does not apply until the combo meter runs out while on the ground
* A new trail effect for Rebounds with configurable size and length
* General code cleanup for more reliable Rebounds and better performance
    * Fixed an issue where boosting could prevent a combo from ending in vanilla gameplay
    * Fixed an issue where a combo could be incorrectly dropped by Rebounding in certain situations
    * Consistent player detection backported from my newer mods
* Added notes on MovementPlus 3.0 compatibility on the mod's home page
* Removed configuration options: Combo Meter Cost, Boost Meter Cost, Allow Boosted Rebounds
    * Boosted Rebounds are now always allowed
    * **Combo meter cost has been slightly increased** from the original default (no boost cost)
* Updated mod icon
* (2.0.1) Fixed a regression that broke using slide as a Rebound Modifier Action

## 1.1.1
* Plugin now tries to ignore AI "players," hopefully fixing weird SlopCrew behavior

## 1.1.0
* Rebound velocity now follows the direction of sloped ground
    * Rebound on downward slopes to gain speed
    * Rebound on upward slopes to gain height
    * Not applied on launchers (by default)
* Rebounding on an empty combo meter now ends your combo instantly
* New config options:
    * Ground Angle Multiplier for Forward Speed (1 by default)
    * Ground Angle Multiplier for Upward Speed (1 by default)
    * Apply Slope Physics when Rebounding on Launcher (false by default)
* Default config adjustment: minimum velocity to rebound is now zero (plays better with slopes)
* Disabled an unused config option
* Removed annoying console logs

## 1.0.0
* Initial release 