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
    [BepInPlugin("goatgirl.Rebound", "Rebound", "2.0.0")]
    [BepInProcess("Bomb Rush Cyberfunk.exe")]
    [BepInDependency("com.yuril.MovementPlus", BepInDependency.DependencyFlags.SoftDependency)]
    public class ReboundPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        public static Player player { get { return WorldHandler.instance?.GetCurrentPlayer(); }}
        internal static Harmony Harmony = new Harmony("goatgirl.Rebound");
        public static bool MPlusActive = false; 

        public static float landTime = float.PositiveInfinity;
        public static Vector2 landingVelocity = new Vector2 (0, 0);
        public static float distanceFallenFromPeakOfJump = 0f;
        public static float landingComboMeter = 1f;

        // configurable values - these defaults may not be the same as the configuration defaults 
        public static float reboundVelocityMultiplier = 0.8f;
        public static float launcherBonusMultiplier = 0.5f;
        public static float maxLandingTimeToRebound = 0.15f;
        
        public static float minVelocityToRebound = 0f; // how fast you have to be falling to do a rebound
        public static float minReboundYVelocity = 8f; // 8f = player jump speed
        public static float maxReboundYVelocity = 0f; // zero or below = no cap

        public static bool capBasedOnHeight = true; // intended more for m+
        public static float heightCapGenerosity = 1.5f; // m+ height cap offset
        public static bool alwaysCalculateBasedOnHeight = false; // (bounceCap)

        public static bool refreshCombo = true; // set comboMeter to landingComboMeter on rebound
        public static bool tempDisableBoostAfterRebound = true;

        // non-configurable values, must be set here (New Years Update)
        public const float comboCost = 0.25f; // negative = increase
        public const float boostCost = 0f;
        public const bool allowBoostedRebounds = true;
        //public static bool canCancelCombo = false; // buggy and unfinished. intended more for vanilla

        public static List<string> cancelReboundActions = new List<string>();
        public static List<string> doReboundActions = new List<string>();

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
                    Log.LogInfo($"Rebound: MovementPlus found!");
                }
            }

            RBSettings.UpdateSettings(Config);
        }

        private void FixedUpdate()
        {
            if (!InGame()) { return; }

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


            } else if (player.ability == player.boostAbility && landTime >= maxLandingTimeToRebound && !MPlusActive) {
                player.LandCombo();
            }
        }

        public static bool CanRebound() {
            return ReboundPlugin.landTime < maxLandingTimeToRebound && player.IsGrounded();
        }

        public static void ReboundTrick() {
            bool isBoosting = allowBoostedRebounds ? player.boosting : false;
            float floorAngle = (Vector3.Dot(Vector3.ProjectOnPlane(player.motor.groundNormalVisual, Vector3.up).normalized, player.dir));
            
            // Force rebound animations to properly transition to jump/fall animations
            player.animInfosSets[(int)player.moveStyle][Animator.StringToHash("jumpTrick1")].fadeTo[player.fallHash] = 1f;
            player.animInfosSets[(int)player.moveStyle][Animator.StringToHash("jumpTrick1")].fadeTo[Animator.StringToHash("fallIdle")] = 1f;
            
            PrepForRebound(); // Set up variables, play SFX/voice, particle effects, etc.
            if (refreshCombo) { player.comboTimeOutTimer = landingComboMeter; }

            // Calc initial rebound velocity
            Vector2 reboundVelocity = landingVelocity;
            reboundVelocity.y = -reboundVelocity.y*reboundVelocityMultiplier; 
            
            // Set trick animation and name
            if (player.CheckBoostTrick() && allowBoostedRebounds) {
                player.DoTrick(Player.TrickType.AIR, "Boosted Rebound", 0);
                player.ActivateAbility(player.airTrickAbility);
                reboundVelocity.x = Mathf.Max(reboundVelocity.x, player.GetForwardSpeed());
            } else { 
                if (tempDisableBoostAfterRebound) {
                    player.ActivateAbility(player.airTrickAbility);
                    player.airTrickAbility.duration /= 3f;
                    player.PlayAnim(Animator.StringToHash("jumpTrick1"), true, true, -1f);
                }
                
                string normalTrickName = player.moveStyle == MoveStyle.BMX ? "360 Backflip" : (player.moveStyle == MoveStyle.SKATEBOARD ? "McTwist" : "Corkscrew");
                player.DoTrick(Player.TrickType.AIR, "Rebound " + normalTrickName, 0); 
            }

            // Counteract M+ fast fall exploitation - bounceCap should never be run into in vanilla
            float targetHeight = (distanceFallenFromPeakOfJump*reboundVelocityMultiplier); // how high a rebound SHOULD be taking you
            float reboundCalculatedByHeight = Mathf.Sqrt(2f * targetHeight * player.motor.gravity); // velocity to get to said height
            float bounceCap = (reboundCalculatedByHeight/9f) + heightCapGenerosity; // why divide by nine? i dunno, but it works!

            if (!alwaysCalculateBasedOnHeight && capBasedOnHeight) {
                reboundVelocity.y = Mathf.Min(reboundVelocity.y, bounceCap);
            } else if (alwaysCalculateBasedOnHeight) {
                reboundVelocity.y = bounceCap - heightCapGenerosity;
            }
            
            // Rebound off launcher bonus
            if (player.onLauncher) { 
                if (!RBSettings.config_slopeOnLauncher.Value) { floorAngle = 0f; }
                float launcherBonus = player.onLauncher.parent.gameObject.name.Contains("Super") ? player.jumpSpeedLauncher * 1.4f : player.jumpSpeedLauncher;
                reboundVelocity.y = Mathf.Max(reboundVelocity.y + (launcherBonus*launcherBonusMultiplier), launcherBonus); //launcher bonus multiplier should be configurable
            }

            // Constrain to min and max
            reboundVelocity.y = Mathf.Max(reboundVelocity.y, minReboundYVelocity);

            if (reboundVelocity.y > maxReboundYVelocity && maxReboundYVelocity != 0) {
                reboundVelocity.x = maxReboundYVelocity * (reboundVelocity.x/reboundVelocity.y); // reduce speed to follow same arc
                reboundVelocity.y = maxReboundYVelocity;
            }

            // Adjust velocity based on floor normal
            Vector2 floorAngleXY = new Vector2 (Mathf.Sin(floorAngle)*(RBSettings.config_slopeMultiplierX.Value), Mathf.Cos(floorAngle)*(RBSettings.config_slopeMultiplierY.Value));
            if (floorAngleXY.x < 0f) { floorAngleXY.y += 1f; } // higher instead of lower
            //floorAngleXY.y = Mathf.Lerp(floorAngleXY.y, 1f, 0.5f);

            player.motor.SetVelocityYOneTime(reboundVelocity.y * floorAngleXY.y);
            float newForwardSpeed = reboundVelocity.x + (reboundVelocity.y * floorAngleXY.x);
            player.SetForwardSpeed(newForwardSpeed);
            if (newForwardSpeed < 0f) {
                player.SetRotation(-player.dir);
            }
            
            // Handle combo meter
            //float oldComboTimer = player.comboTimeOutTimer;
            if (comboCost != 0f) { 
                if ((player.comboTimeOutTimer - comboCost) > 1f) {
                    player.ResetComboTimeOut();
                } else {
                    player.DoComboTimeOut(comboCost);
                    player.comboTimeOutTimer = Mathf.Clamp(player.comboTimeOutTimer, 0f, 1f); 
                }
            }

            if (player.comboTimeOutTimer == 0f) { player.LandCombo(); }
            //if (boostCost != 0f) { player.AddBoostCharge(-boostCost); }
        }

        public static void PrepForRebound() {
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
            if (!ComboCanBeCancelled()) {return;}
            ReboundPlugin.landTime = ReboundPlugin.maxLandingTimeToRebound + 1f;
            player.LandCombo();
        }

        public static bool ComboCanBeCancelled() {
            bool mpluscombodone = MPlusActive ? player.comboTimeOutTimer <= 0 : true;
            return mpluscombodone && player.ability != player.slideAbility && !player.IsPerformingManualTrick && !player.slideAbility.locked
            && player.ability != player.ledgeClimbAbility && player.switchMoveStyleAbility != player.ability;
        }

        private void OnDestroy() { Harmony.UnpatchSelf(); }

        public static bool ActionsPressed(List<string> actions, bool checkIfAllPressed) {
            if (actions?.Any() != true) {return false;}
            bool returnValue = checkIfAllPressed;
            foreach (string actionPropertyOrMethod in actions) {
                if (TranslateActionString(actionPropertyOrMethod) == !checkIfAllPressed) 
                { returnValue = !checkIfAllPressed; }
            }
            return returnValue;      
        }

        public static bool TranslateActionString(string action) {
            switch(action) {
                case "jumpRequested": return player.jumpRequested;
                case "trick1ButtonHeld": return player.trick1ButtonHeld;
                case "trick2ButtonHeld": return player.trick2ButtonHeld;
                case "trick3ButtonHeld": return player.trick3ButtonHeld;
                case "slideButtonHeld": return player.slideButtonHeld;
                case "boostButtonHeld": return player.boostButtonHeld;
                case "sprayButtonHeld": return player.sprayButtonHeld;
                case "danceButtonHeld": return player.danceButtonHeld;
                case "switchStyleButtonHeld": return player.switchStyleButtonHeld;
                case "walkButtonHeld": return player.walkButtonHeld;
                default: return false;
            }
        }

        public static bool InGame() {
            return player != null && !player.isDisabled && !Core.Instance.BaseModule.IsInGamePaused;
        }
    }
}