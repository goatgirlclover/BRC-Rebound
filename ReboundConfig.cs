using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Reptile;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rebound
{   
    internal class RBSettings {
        public static List<string> PlayerActionTypes = new List<string> { 
            //"jump", // special handler - use jumpRequested instead
            "slide",
            "boost",
            "spray",
            "dance",
            "trick1",
            "trick2",
            "trick3",
            "trickany", // special handler
            "switchstyle",
            "walk" // keyboard only
        };

        // Variables
        private static ConfigEntry<float> config_reboundVelocityMultiplier;
        private static ConfigEntry<float> config_maxLandingTimeToRebound;
        private static ConfigEntry<float> config_minVelocityToRebound;
        private static ConfigEntry<float> config_minReboundYVelocity;
        private static ConfigEntry<float> config_maxReboundYVelocity;
        private static ConfigEntry<float> config_launcherBonusMultiplier;
        //private static ConfigEntry<float> config_comboCost; // removed in New Years Update
        //private static ConfigEntry<float> config_boostCost; // removed in New Years Update

        public static ConfigEntry<float> config_slopeMultiplierX;
        public static ConfigEntry<float> config_slopeMultiplierY;
        
        // Options
        private static ConfigEntry<bool> config_capBasedOnHeight;
        private static ConfigEntry<float> config_heightCapGenerosity;
        //private static ConfigEntry<bool> config_canCancelCombo; // removed in New Years Update
        private static ConfigEntry<bool> config_refreshCombo;
        //private static ConfigEntry<bool> config_allowBoostedRebounds; // removed in New Years Update
        private static ConfigEntry<bool> config_tempDisableBoostAfterRebound;
        private static ConfigEntry<bool> config_alwaysCalculateBasedOnHeight;
        //private static ConfigEntry<bool> config_allowReduceComboOnGround; // never implemented properly

        public static ConfigEntry<bool> config_slopeOnLauncher;

        // Options (post-New Years)
        public static ConfigEntry<bool> config_preventComboExtend;
        public static ConfigEntry<bool> config_practiceMode; 

        public static ConfigEntry<bool> config_enableTrail;
        public static ConfigEntry<float> config_trailLength;
        public static ConfigEntry<float> config_trailWidth;

        public static ConfigEntry<bool> config_enableBurstRebound;
        public static ConfigEntry<float> config_burstReboundPower;
        //public static ConfigEntry<float> config_burstReboundTransfer;

        // Input
        private static ConfigEntry<string> config_cancelReboundActions;
        private static ConfigEntry<string> config_doReboundActions;
        
        public static ConfigEntry<int> config_actionHoldTime;
        
        public static ConfigEntry<bool> config_requireAllDRA;
        public static ConfigEntry<bool> config_requireAllCRA;

        public static ConfigEntry<float> config_burstReboundSens; // 0.6
        public static ConfigEntry<float> config_burstReboundDead; // 0.8
        public static float config_burstReboundDeadSquared;
        
        public static void UpdateSettings(ConfigFile Config) {
            BindSettings(Config);
            SetSettingsInPlugin();
        }

        public static void SetSettingsInPlugin() {
            ReboundPlugin.reboundVelocityMultiplier = config_reboundVelocityMultiplier.Value;
            ReboundPlugin.maxLandingTimeToRebound = config_maxLandingTimeToRebound.Value;
            ReboundPlugin.minVelocityToRebound = config_minVelocityToRebound.Value;
            ReboundPlugin.minReboundYVelocity = config_minReboundYVelocity.Value;
            ReboundPlugin.maxReboundYVelocity = config_maxReboundYVelocity.Value;
            ReboundPlugin.launcherBonusMultiplier = config_launcherBonusMultiplier.Value;
            /*ReboundPlugin.comboCost = config_comboCost.Value;
            ReboundPlugin.boostCost = config_boostCost.Value;*/
            ReboundPlugin.capBasedOnHeight = config_capBasedOnHeight.Value;
            ReboundPlugin.heightCapGenerosity = config_heightCapGenerosity.Value;
            //ReboundPlugin.canCancelCombo = config_canCancelCombo.Value;
            ReboundPlugin.refreshCombo = config_refreshCombo.Value;
            ReboundPlugin.alwaysCalculateBasedOnHeight = config_alwaysCalculateBasedOnHeight.Value;
            //ReboundPlugin.allowBoostedRebounds = config_allowBoostedRebounds.Value;
            ReboundPlugin.tempDisableBoostAfterRebound = config_tempDisableBoostAfterRebound.Value;
            //ReboundPlugin.allowReduceComboOnGround = config_allowReduceComboOnGround.Value;

            ReboundPlugin.cancelReboundActions = ConvertString(config_cancelReboundActions.Value, PlayerActionTypes);
            ReboundPlugin.doReboundActions = ConvertString(config_doReboundActions.Value, PlayerActionTypes);

            config_burstReboundDeadSquared = config_burstReboundDead.Value*config_burstReboundDead.Value;
        }

        // make this unnecessary next update
        public static void UpdateSettingsEvent(object sender, EventArgs args) { SetSettingsInPlugin(); }

        public static List<string> ConvertString(string _string, List<string> _list) {
            string stringNoSpaces = _string.Replace(" ", "").ToLower();
            List<string> keyList = stringNoSpaces.Split(',').ToList();
            List<string> newList = new List<string>();

            foreach (string _key in keyList) {
                string key = _key;
                if (_list.Contains(key)) {
                    //if (key.Equals("jump")) { key = "jumpRequested"; }
                    if (key.Equals("trickany")) { 
                        key = "trick1ButtonHeld"; 
                        newList.Add(key);
                        key = "trick2ButtonHeld"; 
                        newList.Add(key);
                        key = "trick3ButtonHeld"; 
                    } else if (key.Equals("switchstyle")) { 
                        key = "switchStyleButtonHeld";
                    } else { key = key + "ButtonHeld"; }
                    newList.Add(key);
                }
            }
            return newList;
        }

        private static void BindSettings(ConfigFile Config) {
            config_reboundVelocityMultiplier = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Rebound Velocity Multiplier",     // The key of the configuration option in the configuration file
                0.8f,    // The default value
                "How much vertical speed is preserved when Rebounding."); // Description of the option 
            
            config_maxLandingTimeToRebound = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Rebound Grace Period",     // The key of the configuration option in the configuration file
                0.2f,    // The default value
                "How much time you have after landing to do a Rebound."); // Description of the option 

            config_minVelocityToRebound = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Minimum Falling Velocity to Rebound",     // The key of the configuration option in the configuration file
                0f,    // The default value
                "How fast you have to be falling to be able to do a Rebound. (For reference, 8 is the player's jump height)"); // Description of the option 

            config_minReboundYVelocity = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Minimum Rebound Velocity",     // The key of the configuration option in the configuration file
                8f,    // The default value
                "The minimum upward speed a Rebound can send you. (For reference, 8 is the player's jump height)"); // Description of the option 
            
            config_maxReboundYVelocity = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Maximum Rebound Velocity",     // The key of the configuration option in the configuration file
                0f,    // The default value
                "The maximum upward speed a Rebound can send you. If zero or below, there's no upper limit (except the maximum fall speed). Note that reaching this limit will slow down your forward speed as well as your upward speed."); // Description of the option 
            
            config_launcherBonusMultiplier = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Launcher Bonus Multiplier",     // The key of the configuration option in the configuration file
                0.5f,    // The default value
                "How much extra momentum you get from Rebounding off a launcher. A Rebound off a launcher will never send you lower than the height a launcher sends you by default."); // Description of the option 

            /*config_comboCost = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Combo Meter Cost",     // The key of the configuration option in the configuration file
                0.15f,    // The default value
                "How much combo meter it costs to do a Rebound (ranging from 0 to 1). 0.15 is equivalent to jumping out of a manual. Set to a negative number to gain meter after a Rebound."); // Description of the option */
            
            /*config_boostCost = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Boost Meter Cost",     // The key of the configuration option in the configuration file
                0f,    // The default value
                "How much boost meter it costs to do a Rebound. Set to a negative number to gain boost after every Rebound."); // Description of the option */

            config_slopeMultiplierX = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Ground Angle Multiplier on Forward Speed",     // The key of the configuration option in the configuration file
                1f,    // The default value
                "How much the angle of the ground you Rebound off affects your forward speed. Speeds up on downward slopes, slows down on upward slopes. Note that you will automatically be flipped over if the slope sends you backwards."); // Description of the option 
            
            config_slopeMultiplierY = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Ground Angle Multiplier on Upward Speed",     // The key of the configuration option in the configuration file
                1f,    // The default value
                "How much the angle of the ground you Rebound off affects your Rebound height. Higher on upward slopes, lower on downward slopes. Not restricted by minimum/maximum Rebound height."); // Description of the option 
            
            config_capBasedOnHeight = Config.Bind(
                "2. Options",          // The section under which the option is shown
                "Cap Rebound Velocity Based on Height",     // The key of the configuration option in the configuration file
                true,    // The default value
                "Limit your Rebound speed based on the peak of your jump, preventing you from going really high really fast by abusing Movement Plus fast falls. Shouldn't ever have an effect on vanilla gameplay, but when playing with Movement Plus, you may be sent lower than you might expect."); // Description of the option 

            config_heightCapGenerosity = Config.Bind(
                "2. Options",          // The section under which the option is shown
                "Height Cap Generosity",     // The key of the configuration option in the configuration file
                1.5f,    // The default value
                "How much extra speed is added to the height-based velocity cap. Note that this is not a multiplier, but an offset added to the height cap."); // Description of the option 
            
            //config_canCancelCombo = Config.Bind(
            //    "2. Options",          // The section under which the option is shown
            //    "Can Cancel Combo if Rebound Missed",     // The key of the configuration option in the configuration file
            //    true,    // The default value
            //    "Toggle whether the plugin is able to end your combo early if you choose not to Rebound during the grace period. This option prevents you from artificially extending your combos by using the Rebound period, but (ideally) should not mess with your combo in a way that doesn't match the vanilla rules. This option also extends your combo if you run out of meter before the grace period ends. Automatically adjusts to respect Movement Plus's combo tweaks if that mod is installed."); // Description of the option 
            
            config_refreshCombo = Config.Bind(
                "2. Options",          // The section under which the option is shown
                "Refresh Combo Meter",     // The key of the configuration option in the configuration file
                true,    // The default value
                "Toggle whether your combo meter is set to the value it was at before you landed before every Rebound. Keeps your combo losses consistent, regardless of when within the grace period you rebounded."); // Description of the option 
            
            /*config_allowBoostedRebounds = Config.Bind(
                "2. Options",          // The section under which the option is shown
                "Allow Boosted Rebounds",     // The key of the configuration option in the configuration file
                true,    // The default value
                "Allow the player to do a Boosted Rebound (boost trick + Rebound) when the boost button is held."); // Description of the option 
            */
            config_tempDisableBoostAfterRebound = Config.Bind(
                "2. Options",          // The section under which the option is shown
                "Temporarily Disable Boost on Rebound",     // The key of the configuration option in the configuration file
                true,    // The default value
                "Toggle whether doing a Rebound stops you from boosting until the trick is done. Does not affect Boosted Rebounds."); // Description of the option 

            config_enableBurstRebound = Config.Bind(
                "2. Options",
                "Enable Burst Rebounds",
                true,
                "Hold back (relative to the player) and boost while Rebounding to do a Burst Rebound, converting your forward speed directly into height."
            );

            config_burstReboundPower = Config.Bind(
                "2. Options", 
                "Burst Rebound Power", 
                0.65f,
                "Adjusts how much forward speed you lose when Burst Rebounding. Higher values preserve more forward speed, converting less of it into Burst Rebound height; however, more vertical momentum is preserved in its place. Recommended to keep between 0 and 1."
            );

            /*config_burstReboundTransfer = Config.Bind(
                "2. Options",
                "Burst Rebound Transfer", 
                0.65f,
                "(IN TESTING - THIS OPTION WILL PROBABLY BE REMOVED ON RELEASE) How much horizontal speed transfers into vertical speed when Burst Rebounding. Remember that this currently stacks on regular Rebound speeds from falling velocity, so a high value (above 0.75 or so) + high speeds = OP as fuck"
            ); */
            
            config_alwaysCalculateBasedOnHeight = Config.Bind(
                "2. Options",          // The section under which the option is shown
                "Always Calculate Rebound Velocity Based On Height",     // The key of the configuration option in the configuration file
                false,    // The default value
                "Always set the Rebound velocity to the height-based velocity cap, regardless of the player's falling velocity. Note that this method tends to send you slightly higher, and also may be less accurate with mid-jump velocity changes (ex. wall plants or air boosts). However, this does prevent the fall speed limitations from capping your Rebound height. Highly recommended to leave on false."); // Description of the option 
        
            // no effect without CanCancelCombo (which is not implemented)
            //config_allowReduceComboOnGround = Config.Bind(
            //    "2. Options",          // The section under which the option is shown
            //    "Allow Reduce Combo On Ground (Vanilla Only)",     // The key of the configuration option in the configuration file
            //    true,    // The default value
            //    "Allow the plugin to slowly reduce your combo meter while on the ground within the Rebound grace period. If Refresh Combo Meter is on, this is solely a visual thing. Vanilla only - has no effect if Movement Plus is installed."); // Description of the option 
        
            config_slopeOnLauncher = Config.Bind(
                "2. Options",          // The section under which the option is shown
                "Apply Slope Physics When Rebounding Off Launcher",     // The key of the configuration option in the configuration file
                false,    // The default value
                "If false, Rebounding off a launcher will act like you have rebounded off of flat ground. If true, Rebounding off a launcher will send you at an angle. Note that this can and will send you flying in directions you may not want - but it also could be used to your advantage to get insane height/speed."); // Description of the option 
        
            config_preventComboExtend = Config.Bind("2. Options", "Prevent Combo Extension", true, "Prevents using the Rebound grace period to artificially extend combos by detecting if an action other than a Rebound is taken during the period and cancelling the combo accordingly. If using MovementPlus, this does not apply until the combo meter runs out while on the ground.");
            config_practiceMode = Config.Bind("2. Options", "Practice Mode", false, "(FLASHING COLORS WARNING) If true, your character will be colored pink when it is possible to Rebound.");

            config_enableTrail = Config.Bind("2. Options", "Enable Rebound Trail", true, "Toggle the extended boost trail effect when Rebounding.");
            config_trailLength = Config.Bind("2. Options", "Trail Time", 3f, "Affects how long the Rebound trail lasts. Note that this time is adjusted depending on the height of the Rebound. The default value reflects the approximate time at the peak of a Rebound.");
            config_trailWidth = Config.Bind("2. Options", "Trail Width", 0.25f, "Affects how big the Rebound trail starts off as.");

            config_doReboundActions = Config.Bind(
                "3. Input",          // The section under which the option is shown
                "Rebound Modifier Actions",     // The key of the configuration option in the configuration file
                "",    // The default value
                "A list of what additional player actions need to be pressed/held to do a Rebound, separated by commas. Note that jumping is always required. Acceptable values: slide, boost, spray, dance, trick1, trick2, trick3, trickAny, switchStyle, walk"); // Description of the option 
            
            config_actionHoldTime = Config.Bind(
                "3. Input",
                "Action Hold Time",
                2,
                "How many frames the player must hold a Rebound Modifier Action to Rebound. Does not include the first frame the actions are pressed. If set to 0 or below, holding the action is not required.");

            config_requireAllDRA = Config.Bind(
                "3. Input",          // The section under which the option is shown
                "Require ALL Rebound Modifiers to Rebound",     // The key of the configuration option in the configuration file
                false,    // The default value
                "If true, the player must press/hold ALL the Rebound modifier actions (as well as jump) to successfully Rebound. If false, the player can hold ANY of them (along with jump) to Rebound."); // Description of the option 
              
            
            config_cancelReboundActions = Config.Bind(
                "3. Input",          // The section under which the option is shown
                "Cancel Rebound Modifier Actions",     // The key of the configuration option in the configuration file
                "",    // The default value
                "A list of what player actions can be pressed/held to prevent a Rebound and do a regular jump instead, separated by commas. Acceptable values: slide, boost, spray, dance, trick1, trick2, trick3, trickAny, switchStyle, walk"); // Description of the option 
        
            config_requireAllCRA = Config.Bind(
                "3. Input",          // The section under which the option is shown
                "Require ALL Cancel Rebound Modifiers to Cancel",     // The key of the configuration option in the configuration file
                false,    // The default value
                "If true, the player must press/hold ALL the cancel modifier actions to cancel a Rebound. If false, the player can hold ANY of them to cancel a Rebound."); // Description of the option 

            config_burstReboundSens = Config.Bind("3. Input", "Burst Rebound Sensitivity", 0.75f, "How precice you have to hold the left stick backwards (relative to the player) to activate a Burst Rebound. Higher values are more precise.");
            config_burstReboundDead = Config.Bind("3. Input", "Burst Rebound Deadzone", 0.8f, "How far back you have to hold the left stick to activate a Burst Rebound. 1.0 is completely backwards, while 0.0 is not holding the stick at all.");
            config_burstReboundDeadSquared = config_burstReboundDead.Value*config_burstReboundDead.Value;
            
              config_reboundVelocityMultiplier.SettingChanged += UpdateSettingsEvent;
              config_maxLandingTimeToRebound.SettingChanged += UpdateSettingsEvent;
              config_minVelocityToRebound.SettingChanged += UpdateSettingsEvent;
              config_minReboundYVelocity.SettingChanged += UpdateSettingsEvent;
              config_maxReboundYVelocity.SettingChanged += UpdateSettingsEvent;
              config_launcherBonusMultiplier.SettingChanged += UpdateSettingsEvent;
              config_slopeMultiplierX.SettingChanged += UpdateSettingsEvent;
              config_slopeMultiplierY.SettingChanged += UpdateSettingsEvent;
              config_capBasedOnHeight.SettingChanged += UpdateSettingsEvent;
              config_heightCapGenerosity.SettingChanged += UpdateSettingsEvent;
              config_refreshCombo.SettingChanged += UpdateSettingsEvent;
              config_tempDisableBoostAfterRebound.SettingChanged += UpdateSettingsEvent;
              config_alwaysCalculateBasedOnHeight.SettingChanged += UpdateSettingsEvent;
              config_slopeOnLauncher.SettingChanged += UpdateSettingsEvent;
              config_cancelReboundActions.SettingChanged += UpdateSettingsEvent;
              config_doReboundActions.SettingChanged += UpdateSettingsEvent;
              config_requireAllDRA.SettingChanged += UpdateSettingsEvent;
              config_requireAllCRA.SettingChanged += UpdateSettingsEvent;
              config_burstReboundDead.SettingChanged += UpdateSettingsEvent; 
        }
    }
}