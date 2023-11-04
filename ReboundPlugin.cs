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
    [BepInPlugin("goatgirl.Rebound", "Rebound", "1.0.0")]
    [BepInProcess("Bomb Rush Cyberfunk.exe")]
    [BepInDependency("com.yuril.MovementPlus", BepInDependency.DependencyFlags.SoftDependency)]
    public class ReboundPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        public static Player player;
        internal static Harmony Harmony = new Harmony("goatgirl.Rebound");

        public static float landTime = float.PositiveInfinity;
        public static Vector2 landingVelocity = new Vector2 (0, 0);
        public static float distanceFallenFromPeakOfJump = 0f;
        public static float landingComboMeter = 1f;

        public static bool MPlusActive = false; 

        // configurable values - these defaults may not be the same as the configuration defaults seen far below
        public static float reboundVelocityMultiplier = 0.8f;
        public static float launcherBonusMultiplier = 0.5f;
        public static float maxLandingTimeToRebound = 0.15f;
        
        public static float minimumVelocityToRebound = 8f; // how fast you have to be falling to do a rebound
        public static float minimumReboundYVelocity = 8f; // 8f = player jump speed
        public static float maximumReboundYVelocity = 0f; // zero or below = no cap

        public static float comboCost = 0.15f; // negative = increase
        public static float boostCost = 0f;

        public static bool capBasedOnHeight = true; // intended more for m+
        public static float heightCapGenerosity = 1.5f; // m+ height cap offset

        public static bool alwaysCalculateBasedOnHeight = false; // (bounceCap)
        public static bool canCancelCombo = true; // intended more for vanilla
        public static bool refreshCombo = true; // set comboMeter to landingComboMeter on rebound

        public static bool allowBoostedRebounds = true;
        public static bool tempDisableBoost = true;
        public static bool allowReduceComboOnGround = true; // vanilla only

        public static List<string> cancelReboundActions = new List<string>();
        public static List<string> doReboundActions = new List<string>();

        public static Dictionary<string, bool> playerActionsDictionary = new Dictionary<string, bool> {
            {"jumpRequested", false},
            {"trick1ButtonHeld", false},
            {"trick2ButtonHeld", false},
            {"trick3ButtonHeld", false},
            {"slideButtonHeld", false},
            {"boostButtonHeld", false},
            {"sprayButtonHeld", false},
            {"danceButtonHeld", false},
            {"switchStyleButtonHeld", false},
            {"walkButtonHeld", false}
        };


        private void Awake()
        {
            ReboundPlugin.Log = base.Logger;
            Harmony.PatchAll(); 
            Logger.LogInfo($"Plugin Rebound is loaded!");

            // Check for MovementPlus
            foreach (var plugin in BepInEx.Bootstrap.Chainloader.PluginInfos)
            {
                if (plugin.Value.Metadata.GUID.Equals("com.yuril.MovementPlus"))
                { 
                    MPlusActive = true; 
                    Log.LogInfo($"MovementPlus found!");
                }
            }

            RBSettings.UpdateSettings(Config);
        }

        private void FixedUpdate()
        {
            if (player != null) { // any code that needs to be run every frame
                if (landTime < maxLandingTimeToRebound) { landTime += Reptile.Core.dt; } 
                
                if (!player.IsGrounded()) {
                    landingVelocity = new Vector2 (player.GetForwardSpeed(), player.GetVelocity().y);
                    ReboundPlugin.landingComboMeter = player.comboTimeOutTimer;

                    if (player.GetVelocity().y < 0) {
                        distanceFallenFromPeakOfJump += Mathf.Abs(player.GetVelocity().y);
                        landTime = 0f;
                    } else {
                        distanceFallenFromPeakOfJump = 0; // no longer the peak!
                    }


                } else if (CanRebound(player) && ComboUpForGrabs() && !MPlusActive && allowReduceComboOnGround && player.IsComboing()) {
                    player.comboTimeOutTimer -= Reptile.Core.dt/maxLandingTimeToRebound;
                    //if (player.comboTimeOutTimer <= 0) { CancelRebound(); }
                }

                // this is an AWFUL way of doing this but reflection/getfield() isn't working, so it'll have to do for now...
                playerActionsDictionary = new Dictionary<string, bool> {
                    {"jumpRequested", player.jumpRequested},
                    {"trick1ButtonHeld", player.trick1ButtonHeld},
                    {"trick2ButtonHeld", player.trick2ButtonHeld},
                    {"trick3ButtonHeld", player.trick3ButtonHeld},
                    {"slideButtonHeld", player.slideButtonHeld},
                    {"boostButtonHeld", player.boostButtonHeld},
                    {"sprayButtonHeld", player.sprayButtonHeld},
                    {"danceButtonHeld", player.danceButtonHeld},
                    {"switchStyleButtonHeld", player.switchStyleButtonHeld},
                    {"walkButtonHeld", player.walkButtonHeld}
                };
            }
        }

        public static bool CanRebound(Player player) {
            return ReboundPlugin.landTime < maxLandingTimeToRebound && player.IsGrounded();
        }

        public static bool PlayerIsCancellingRebound(Player player) {
            return actionsAreBeingPressed(cancelReboundActions, RBSettings.config_requireAllCRA.Value, player); 
        }

        public static void ReboundTrick(Player player) {
            bool isBoosting = allowBoostedRebounds ? player.boosting : false;
            
            // Force update animations to properly transition to jump/fall
            player.animInfosSets[(int)player.moveStyle][Animator.StringToHash("jumpTrick1")].fadeTo[player.fallHash] = 1f;
            player.animInfosSets[(int)player.moveStyle][Animator.StringToHash("jumpTrick1")].fadeTo[Animator.StringToHash("fallIdle")] = 1f;
            
            OnStartRebound(player); // Set up variables, play SFX/voice, particle effects
            if (refreshCombo) { player.comboTimeOutTimer = landingComboMeter; }

            // Calc initial rebound velocity
            Vector2 reboundVelocity = landingVelocity;
            reboundVelocity.y = -reboundVelocity.y*reboundVelocityMultiplier; //reboundVelocityMultiplier should be configurable
            
            // Boosted or regular? Do trick animation and name accordingly
            if (player.CheckBoostTrick() && allowBoostedRebounds) {
                player.DoTrick(Player.TrickType.AIR, "Boosted Rebound", 0);
                player.ActivateAbility(player.airTrickAbility);
                reboundVelocity.x = Mathf.Max(reboundVelocity.x, player.GetForwardSpeed());

            } else { 
                if (tempDisableBoost) {
                    player.ActivateAbility(player.airTrickAbility);
                    player.airTrickAbility.duration /= 3f;
                    player.PlayAnim(Animator.StringToHash("jumpTrick1"), true, true, -1f);
                }
                
                string normalTrickName = "Corkscrew";
                if (player.moveStyle == MoveStyle.BMX) { normalTrickName = "360 Backflip"; }
                if (player.moveStyle == MoveStyle.SKATEBOARD) { normalTrickName = "McTwist"; }
                player.DoTrick(Player.TrickType.AIR, "Rebound " + normalTrickName, 0); 
                }

            // Counteract M+ fast fall - bounceCap should not EVER be run into in vanilla
            float targetHeight = (distanceFallenFromPeakOfJump*reboundVelocityMultiplier); // how high a rebound SHOULD be taking you
            float reboundCalculatedByHeight = Mathf.Sqrt(2f * targetHeight * player.motor.gravity); // velocity to get to said height
            float bounceCap = (reboundCalculatedByHeight/9f) + heightCapGenerosity; // why divide by nine? i dunno, but it works!
            //Log.LogInfo(bounceCap + ", " + reboundVelocity.y);
            if (!alwaysCalculateBasedOnHeight && capBasedOnHeight) {
                reboundVelocity.y = Mathf.Min(reboundVelocity.y, bounceCap);
            } else if (alwaysCalculateBasedOnHeight) {
                reboundVelocity.y = bounceCap - heightCapGenerosity;
            }
            
            // Rebound off launcher bonus
            if (player.onLauncher) { 
                float launcherBonus = player.onLauncher.parent.gameObject.name.Contains("Super") ? player.jumpSpeedLauncher * 1.4f : player.jumpSpeedLauncher;
                reboundVelocity.y = Mathf.Max(reboundVelocity.y + (launcherBonus*launcherBonusMultiplier), launcherBonus); //launcher bonus multiplier should be configurable
            }

            // Constrain to min and max
            reboundVelocity.y = Mathf.Max(reboundVelocity.y, minimumReboundYVelocity);

            if (reboundVelocity.y > maximumReboundYVelocity && maximumReboundYVelocity != 0) {
                reboundVelocity.x = maximumReboundYVelocity * (reboundVelocity.x/reboundVelocity.y); // reduce speed to follow same arc
                reboundVelocity.y = maximumReboundYVelocity;
                //Log.LogInfo($"Hit maximum bounce height! " + bounceCap + ", " + groundedPositionY + ", " + positionYDiff);
            }

            player.motor.SetVelocityYOneTime(reboundVelocity.y);
            player.SetForwardSpeed(reboundVelocity.x);

            if (comboCost != 0f) { 
                if ((player.comboTimeOutTimer - comboCost) > 1f) {
                    player.ResetComboTimeOut();
                } else {
                    player.DoComboTimeOut(comboCost);
                    player.comboTimeOutTimer = Mathf.Clamp(player.comboTimeOutTimer, 0f, 1f); 
                }
            }

            if (boostCost != 0f) { player.AddBoostCharge(-boostCost); }
        }

        public static void OnStartRebound(Player player) {
            bool isBoosting = allowBoostedRebounds ? player.boosting : false;

            if (!isBoosting) { player.StopCurrentAbility(); }
            
            player.jumpedThisFrame = true;
            player.isJumping = true;
            player.maintainSpeedJump = true;
            player.jumpConsumed = true;
            player.jumpRequested = false;
            player.jumpedThisFrame = true;
            player.timeSinceLastJump = 0f;
            player.ForceUnground(true);
            player.radialHitbox.SetActive(true);
            
            player.AudioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.jump_special, player.playerOneShotAudioSource, 0f);
            player.PlayVoice(AudioClipID.VoiceJump, VoicePriority.MOVEMENT, true);
            if (!isBoosting) { player.PlayAnim(Animator.StringToHash("jumpTrick1"), true, false, -1f); }
            else { player.PlayAnim(Animator.StringToHash("jump"), true, false, -1f); }
           
            player.DoHighJumpEffects(player.motor.groundNormalVisual * -1f);
            //player.ringParticles.Emit(1);
        }

        public static void CancelRebound() {
            if (!ComboUpForGrabs()) {return;}
            ReboundPlugin.landTime = ReboundPlugin.maxLandingTimeToRebound + 1f;
            player.LandCombo();
        }

        public static bool ComboUpForGrabs() {
            bool mpluscombodone = MPlusActive ? player.comboTimeOutTimer <= 0 : true;
            return mpluscombodone && player.ability != player.slideAbility && canCancelCombo;
        }

        private void OnDestroy() {
            Harmony.UnpatchSelf(); // for scriptengine
        }

        public static bool actionsAreBeingPressed(List<string> actions, bool checkIfAllPressed, Player _p) {
            if (actions?.Any() != true) {return false;}
            bool returnValue = checkIfAllPressed;
            foreach (string actionPropertyOrMethod in actions) {
                if (playerActionsDictionary[actionPropertyOrMethod] == !checkIfAllPressed) 
                { returnValue = !checkIfAllPressed; }
            }
            return returnValue;      
        }
    }

    [HarmonyPatch(typeof(Player))]
    internal class PlayerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.Init))]
        public static bool InitPrefix_SetPluginPlayerReference(Player __instance)
        {
            if (ReboundPlugin.player == null)
            {
                ReboundPlugin.player = __instance;
                //ReboundPlugin.Log.LogDebug($"Set ReboundPlugin player instance");
            }
            //RBSettings.UpdateSettings(ReboundPlugin.Config);
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.HandleJump))]
        public static bool HandleJumpPrefix_HandleRebound(Player __instance)
        {
            PlayerPatches.InitPrefix_SetPluginPlayerReference(__instance); // for testing - quickly get player reference in case we reloaded the plugin with scriptengine and the reference is lost
            RBSettings.SetSettingsInPlugin(); // again, for testing w/ scriptengine
            
            __instance.jumpedThisFrame = false;
            __instance.timeSinceJumpRequested += Reptile.Core.dt;

            // simplified JumpIsAllowed check for rebound - bypasses any ability checks
            if (__instance.jumpRequested && !__instance.jumpConsumed && (__instance.IsGrounded() || __instance.timeSinceLastAbleToJump <= __instance.JumpPostGroundingGraceTime)) {            
                bool doingRBActions = ReboundPlugin.doReboundActions.Any() ? ReboundPlugin.actionsAreBeingPressed(ReboundPlugin.doReboundActions, RBSettings.config_requireAllDRA.Value, __instance) : true;
                ReboundPlugin.Log.LogInfo(doingRBActions);
                
                if (ReboundPlugin.landingVelocity.y < -ReboundPlugin.minimumVelocityToRebound 
                && ReboundPlugin.CanRebound(__instance) 
                && !ReboundPlugin.PlayerIsCancellingRebound(__instance)
                && doingRBActions) { 
                    ReboundPlugin.ReboundTrick(__instance); 

                } else if (__instance.JumpIsAllowed()) { // kind of inefficient...
                    if (ReboundPlugin.CanRebound(__instance)) 
                    { ReboundPlugin.CancelRebound(); }
                    __instance.Jump(); 
                }
                
                ReboundPlugin.distanceFallenFromPeakOfJump = 0f;
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.FixedUpdatePlayer))]
        public static bool FixedUpdatePlayer_UpdateComboTimer(Player __instance)
        {
            if (ReboundPlugin.CanRebound(__instance) && ReboundPlugin.ComboUpForGrabs() && !ReboundPlugin.MPlusActive && ReboundPlugin.allowReduceComboOnGround && __instance.IsComboing()) {
                __instance.comboTimeOutTimer -= Reptile.Core.dt/ReboundPlugin.maxLandingTimeToRebound;
                //if (player.comboTimeOutTimer <= 0) { CancelRebound(); }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.OnLanded))]
        public static bool OnLandedPrefix_ReboundVel(Player __instance) {
            ReboundPlugin.landTime = 0f;
            ReboundPlugin.distanceFallenFromPeakOfJump += __instance.reallyMovedVelocity.y; // adds "velocity" from frame before that probably isn't caught. make sure this works the way we want it to!
            //ReboundPlugin.groundedPositionY = __instance.tf.position.y;
            if (__instance.comboTimeOutTimer <= 0) { ReboundPlugin.CancelRebound(); }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.DoTrick))]
        public static bool DoTrickPrefix_FailIfNotRebound(Player.TrickType type, string trickName, int trickNum, Player __instance) {
            // if can rebound and choosing not to, drop combo
            if (ReboundPlugin.CanRebound(__instance) && !(trickName.Contains("Rebound") && trickNum == 0 && type == Player.TrickType.AIR)) {
                if (ReboundPlugin.landTime > Reptile.Core.dt && !(__instance.currentTrickType == Player.TrickType.SLIDE && type == Player.TrickType.SLIDE)) {
                    ReboundPlugin.CancelRebound();
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.LandCombo))]
        public static bool LandComboPrefix_ExtendForRebound(Player __instance) {
            if (ReboundPlugin.CanRebound(__instance) && ReboundPlugin.canCancelCombo) {
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.ActivateAbility))]
        public static bool AbilityPrefix_CancelRBIfSlide(Ability a, Player __instance) {
            if (a != __instance.ability && a == __instance.slideAbility && ReboundPlugin.CanRebound(__instance) && ReboundPlugin.landTime > Reptile.Core.dt*3f) {
                ReboundPlugin.CancelRebound();
            }
            return true;
        }
    }

    internal class RBSettings {

        public static List<string> PlayerActionTypes = new List<string> { // <name>buttonHeld
            //"jump", // special handler - use jumpRequested instead
            "slide",
            "boost",
            "spray",
            "dance",
            "trick1",
            "trick2",
            "trick3",
            "trickany", // we need to have a special handler for this one
            "switchstyle",
            "walk" // wtf is this??
        };

        // Variables
        private static ConfigEntry<float> config_reboundVelocityMultiplier;
        private static ConfigEntry<float> config_maxLandingTimeToRebound;
        private static ConfigEntry<float> config_minimumVelocityToRebound;
        private static ConfigEntry<float> config_minimumReboundYVelocity;
        private static ConfigEntry<float> config_maximumReboundYVelocity;
        private static ConfigEntry<float> config_launcherBonusMultiplier;
        private static ConfigEntry<float> config_comboCost;
        private static ConfigEntry<float> config_boostCost;
        
        // Options
        private static ConfigEntry<bool> config_capBasedOnHeight;
        private static ConfigEntry<float> config_heightCapGenerosity;
        private static ConfigEntry<bool> config_canCancelCombo;
        private static ConfigEntry<bool> config_refreshCombo;
        private static ConfigEntry<bool> config_allowBoostedRebounds;
        private static ConfigEntry<bool> config_tempDisableBoost;
        private static ConfigEntry<bool> config_alwaysCalculateBasedOnHeight;
        private static ConfigEntry<bool> config_allowReduceComboOnGround;

        private static ConfigEntry<string> config_cancelReboundActions;
        private static ConfigEntry<string> config_doReboundActions;
        
        public static ConfigEntry<bool> config_requireAllDRA;
        public static ConfigEntry<bool> config_requireAllCRA;

        // Input
        // blahblahblah
        

        public static void UpdateSettings(ConfigFile Config) {
            BindSettings(Config);
            SetSettingsInPlugin();
        }

        public static void SetSettingsInPlugin() {
            ReboundPlugin.reboundVelocityMultiplier = config_reboundVelocityMultiplier.Value;
            ReboundPlugin.maxLandingTimeToRebound = config_maxLandingTimeToRebound.Value;
            ReboundPlugin.minimumVelocityToRebound = config_minimumVelocityToRebound.Value;
            ReboundPlugin.minimumReboundYVelocity = config_minimumReboundYVelocity.Value;
            ReboundPlugin.maximumReboundYVelocity = config_maximumReboundYVelocity.Value;
            ReboundPlugin.launcherBonusMultiplier = config_launcherBonusMultiplier.Value;
            ReboundPlugin.comboCost = config_comboCost.Value;
            ReboundPlugin.boostCost = config_boostCost.Value;
            ReboundPlugin.capBasedOnHeight = config_capBasedOnHeight.Value;
            ReboundPlugin.heightCapGenerosity = config_heightCapGenerosity.Value;
            ReboundPlugin.canCancelCombo = config_canCancelCombo.Value;
            ReboundPlugin.refreshCombo = config_refreshCombo.Value;
            ReboundPlugin.alwaysCalculateBasedOnHeight = config_alwaysCalculateBasedOnHeight.Value;
            ReboundPlugin.allowBoostedRebounds = config_allowBoostedRebounds.Value;
            ReboundPlugin.tempDisableBoost = config_tempDisableBoost.Value;
            ReboundPlugin.allowReduceComboOnGround = config_allowReduceComboOnGround.Value;

            ReboundPlugin.cancelReboundActions = ConvertString(config_cancelReboundActions.Value, PlayerActionTypes);
            ReboundPlugin.doReboundActions = ConvertString(config_doReboundActions.Value, PlayerActionTypes);
        }

        public static List<string> ConvertString(string _string, List<string> _list) {
            //_string = _string.ToLower();
            string stringNoSpaces = _string.Replace(" ", "").ToLower();
            List<string> keyList = stringNoSpaces.Split(',').ToList();
            List<string> newList = new List<string>();

            foreach (string _key in keyList) {
                string key = _key;
                if (_list.Contains(key)) {
                    //if (key.Equals("jump")) { key = "jumpRequested"; }
                    if (key.Equals("trickany")) { 
                        key = "trick1ButtonHeld";  //is this working correctly
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
                "How much vertical speed is preserved when rebounding."); // Description of the option 
            
            config_maxLandingTimeToRebound = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Rebound Grace Period",     // The key of the configuration option in the configuration file
                0.15f,    // The default value
                "How much time you have after landing to do a rebound."); // Description of the option 

            config_minimumVelocityToRebound = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Minimum Falling Velocity to Rebound",     // The key of the configuration option in the configuration file
                4f,    // The default value
                "How fast you have to be falling to be able to do a rebound. (For reference, 8 is the player's jump height)"); // Description of the option 

            config_minimumReboundYVelocity = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Minimum Rebound Velocity",     // The key of the configuration option in the configuration file
                8f,    // The default value
                "The minimum upward speed a rebound can send you. (For reference, 8 is the player's jump height)"); // Description of the option 
            
            config_maximumReboundYVelocity = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Maximum Rebound Velocity",     // The key of the configuration option in the configuration file
                0f,    // The default value
                "The maximum upward speed a rebound can send you. If zero or below, there's no upper limit (except the maximum fall speed). Note that reaching this limit will slow down your forward speed as well as your upward speed."); // Description of the option 
            
            config_launcherBonusMultiplier = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Launcher Bonus Multiplier",     // The key of the configuration option in the configuration file
                0.5f,    // The default value
                "How much extra momentum you get from rebounding off a launcher. A rebound off a launcher will never send you lower than the height a launcher sends you by default."); // Description of the option 

            config_comboCost = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Combo Meter Cost",     // The key of the configuration option in the configuration file
                0.15f,    // The default value
                "How much combo meter it costs to do a rebound (ranging from 0 to 1). 0.15 is equivalent to jumping out of a manual. Set to a negative number to gain meter after a rebound."); // Description of the option 
            
            config_boostCost = Config.Bind(
                "1. Variables",          // The section under which the option is shown
                "Boost Meter Cost",     // The key of the configuration option in the configuration file
                0f,    // The default value
                "How much boost meter it costs to do a rebound. Set to a negative number to gain boost after every rebound."); // Description of the option 
            
            
            
            config_capBasedOnHeight = Config.Bind(
                "2. Options",          // The section under which the option is shown
                "Cap Rebound Velocity Based on Height",     // The key of the configuration option in the configuration file
                true,    // The default value
                "Limit your rebound speed based on the peak of your jump, preventing you from going really high really fast by abusing Movement Plus fast falls. Shouldn't ever have an effect on vanilla gameplay, but when playing with Movement Plus, you may be sent lower than you might expect."); // Description of the option 

            config_heightCapGenerosity = Config.Bind(
                "2. Options",          // The section under which the option is shown
                "Height Cap Generosity",     // The key of the configuration option in the configuration file
                1.5f,    // The default value
                "How much extra speed is added to the height-based velocity cap. Note that this is not a multiplier, but an offset added to the height cap."); // Description of the option 
            
            config_canCancelCombo = Config.Bind(
                "2. Options",          // The section under which the option is shown
                "Can Cancel Combo if Rebound Missed",     // The key of the configuration option in the configuration file
                true,    // The default value
                "Toggle whether the plugin is able to end your combo early if you choose not to rebound during the grace period. This option prevents you from artificially extending your combos by using the rebound period, but (ideally) should not mess with your combo in a way that doesn't match the vanilla rules. This option also extends your combo if you run out of meter before the grace period ends. Automatically adjusts to respect Movement Plus's combo tweaks if that mod is installed."); // Description of the option 
            
            config_refreshCombo = Config.Bind(
                "2. Options",          // The section under which the option is shown
                "Refresh Combo Meter",     // The key of the configuration option in the configuration file
                true,    // The default value
                "Toggle whether your combo meter is set to the value it was at before you landed before every rebound. Keeps your combo losses consistent, regardless of when within the grace period you rebounded."); // Description of the option 
            
            config_allowBoostedRebounds = Config.Bind(
                "2. Options",          // The section under which the option is shown
                "Allow Boosted Rebounds",     // The key of the configuration option in the configuration file
                true,    // The default value
                "Allow the player to do a Boosted Rebound (boost trick + rebound) when the boost button is held."); // Description of the option 

            config_tempDisableBoost = Config.Bind(
                "2. Options",          // The section under which the option is shown
                "Can Temporarily Disable Boost",     // The key of the configuration option in the configuration file
                true,    // The default value
                "Toggle whether doing a rebound stops you from boosting until the trick is done. Does not affect Boosted Rebounds."); // Description of the option 
            
            config_alwaysCalculateBasedOnHeight = Config.Bind(
                "2. Options",          // The section under which the option is shown
                "Always Calculate Rebound Velocity Based On Height",     // The key of the configuration option in the configuration file
                false,    // The default value
                "Always set the rebound velocity to the height-based velocity cap, regardless of the player's falling velocity. Note that this method tends to send you slightly higher, and also may be less accurate with mid-jump velocity changes (ex. wall plants or air boosts). However, this does allow you to bypass the fall speed limitations. Highly recommended to leave on false."); // Description of the option 
        
            config_allowReduceComboOnGround = Config.Bind(
                "2. Options",          // The section under which the option is shown
                "Allow Reduce Combo On Ground (Vanilla Only)",     // The key of the configuration option in the configuration file
                true,    // The default value
                "Allow the plugin to slowly reduce your combo meter while on the ground within the rebound grace period. If Refresh Combo Meter is on, this is solely a visual thing. Vanilla only - has no effect if Movement Plus is installed."); // Description of the option 
        
            
            
            config_doReboundActions = Config.Bind(
                "3. Input",          // The section under which the option is shown
                "Rebound Modifier Actions",     // The key of the configuration option in the configuration file
                "",    // The default value
                "A list of what additional player actions need to be pressed/held to do a rebound, separated by commas. Note that jumping is always required. Acceptable values: slide, boost, spray, dance, trick1, trick2, trick3, trickAny, switchStyle, walk"); // Description of the option 
              
            config_requireAllDRA = Config.Bind(
                "3. Input",          // The section under which the option is shown
                "Require ALL Rebound Modifiers to Rebound",     // The key of the configuration option in the configuration file
                false,    // The default value
                "If true, the player must press/hold ALL the rebound modifier actions (as well as jump) to successfully rebound. If false, the player can hold ANY of them (along with jump) to rebound."); // Description of the option 
              
            
            config_cancelReboundActions = Config.Bind(
                "3. Input",          // The section under which the option is shown
                "Cancel Rebound Modifier Actions",     // The key of the configuration option in the configuration file
                "slide",    // The default value
                "A list of what player actions can be pressed/held to cancel a rebound and do a regular jump instead, separated by commas. Acceptable values: slide, boost, spray, dance, trick1, trick2, trick3, trickAny, switchStyle, walk"); // Description of the option 
        
            config_requireAllCRA = Config.Bind(
                "3. Input",          // The section under which the option is shown
                "Require ALL Cancel Rebound Modifiers to Cancel",     // The key of the configuration option in the configuration file
                false,    // The default value
                "If true, the player must press/hold ALL the cancel modifier actions to cancel a rebound. If false, the player can hold ANY of them to cancel a rebound."); // Description of the option 
              
        }
    }

}