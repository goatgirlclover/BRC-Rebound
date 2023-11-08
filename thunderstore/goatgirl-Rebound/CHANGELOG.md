## v1.1.1
* Plugin now tries to ignore AI "players," hopefully fixing weird SlopCrew behavior

### v1.1.0
* Rebound velocity now follows the direction of sloped ground
    * Rebound on downward slopes to gain speed
    * Rebound on upward slopes to gain height
    * Not applied on launchers (by default)
* Rebounding on an empty combo meter now ends your combo
* New config options:
    * Ground Angle Multiplier for Forward Speed (1 by default)
    * Ground Angle Multiplier for Upward Speed (1 by default)
    * Apply Slope Physics when Rebounding on Launcher (false by default)
* Default config adjustment: minimum velocity to rebound is now zero (plays better with slopes)
* Disabled an unused config option
* Removed annoying console logs

### v1.0.0
* Initial release 