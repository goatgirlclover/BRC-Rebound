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