## 2.0.0 (New Year Update)
* **New configuration option:** Prevent Combo Extension (enabled by default)
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

# 1.0.0
* Initial release 