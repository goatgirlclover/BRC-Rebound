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
using System.IO;

namespace Rebound
{   
    [BepInPlugin("goatgirl.Rebound", "Rebound", "2.3.1")]
    [BepInProcess("Bomb Rush Cyberfunk.exe")]
    [BepInDependency("com.yuril.MovementPlus", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("ConfigTrixAirTricks", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Dragsun.BunchOfEmotes", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.yuril.QuickTurn", BepInDependency.DependencyFlags.SoftDependency)]
    public class ReboundPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        public static Player player { get { return WorldHandler.instance?.GetCurrentPlayer(); }}
        internal static Harmony Harmony = new Harmony("goatgirl.Rebound");
        
        public static bool MPlusActive = false; 
        public static bool hasCBMono = false;
        public static bool hasQuickTurn = false; 

        public static float landTime = float.PositiveInfinity;
        public static Vector2 landingVelocity = new Vector2(0, 0);
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
        public const float comboCost = 0.215f; // negative = increase
        public const float boostCost = 0f;
        public const bool allowBoostedRebounds = true;
        public static bool canCancelCombo = true; // buggy and unfinished. intended more for vanilla

        public static List<string> cancelReboundActions = new List<string>();
        public static List<string> doReboundActions = new List<string>();
        
        public static float reboundTrailTime { get { return RBSettings.config_trailLength.Value + 0.000125f; } }
        public static float reboundTrailWidth { get { return RBSettings.config_trailWidth.Value + 0.000125f; } }
        public static bool rebounding = false;
        public static bool settingUpRebound = false;

        public static Shader unlitShader { get { return Shader.Find("Unlit/Normal"); }}
        public static bool hasUnlitCharacter = false; 

        public static bool attemptingBoostedRebound { get { return player.CheckBoostTrick() && allowBoostedRebounds; } }
        public static bool attemptingBurstRebound { get { return attemptingBoostedRebound && Vector3.Dot(player.dir, player.moveInput) <= -RBSettings.config_burstReboundSens.Value
                                                                && player.moveInputPlain.sqrMagnitude >= (RBSettings.config_burstReboundDeadSquared); } }

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
                else if (plugin.Value.Metadata.GUID.Equals("com.yuril.QuickTurn"))
                { 
                    hasQuickTurn = true; 
                    Log.LogInfo($"Rebound: QuickTurn found!");
                }
                else if (plugin.Value.Metadata.GUID.Equals("CrewBoom"))
                { 
                    hasCBMono = true; 
                    Log.LogInfo($"Rebound: CrewBoomMono found!");
                }
            }

            RBSettings.UpdateSettings(Config);
        }

        private void FixedUpdate()
        {
            if (!InGame()) { return; }
            if (landTime < maxLandingTimeToRebound) { landTime += Reptile.Core.dt; } 
            
            if (!player.IsGrounded()) {
                landingVelocity = new Vector2(player.GetForwardSpeed(), player.GetVelocity().y);
                ReboundPlugin.landingComboMeter = player.comboTimeOutTimer;

                if (player.GetVelocity().y < 0) {
                    distanceFallenFromPeakOfJump += Mathf.Abs(player.GetVelocity().y);
                    landTime = 0f;
                } else { distanceFallenFromPeakOfJump = 0; } // no longer the peak!

            } else if (player.ability == player.boostAbility && landTime >= maxLandingTimeToRebound && !MPlusActive) {
                player.LandCombo();
            }

            //if (player.ability == player.slideAbility) { landTime = float.PositiveInfinity; }

            /*if (player.boostpackTrailDefaultTime == reboundTrailTime && (player.boostTrailTimer == 0f)) {
                player.boostpackTrailDefaultTime = originalTrailTime;
                player.boostpackTrailDefaultWidth = originalTrailWidth;
            } else if (player.boostpackTrailDefaultWidth == reboundTrailWidth) {*/

            if (rebounding && player.boostTrailTimer <= 0f) { rebounding = false; }
            else if (rebounding && RBSettings.config_enableTrail.Value) {
                player.boostTrailTimer += Mathf.Min(Core.dt, player.GetVelocity().y/500f)*6f; 
                player.trailWidth += Mathf.Min(Core.dt, player.GetVelocity().y/500f); 
            }

            if (RBSettings.config_practiceMode.Value) {
                if (CanReboundDetailed() && !hasUnlitCharacter) {
                    hasUnlitCharacter = true;
                    List<Material> playerMaterials = (hasCBMono ? RBCBHelper.GetPlayerMaterials(player)
                                : new List<Material>() {player.characterVisual.GetComponentInChildren<SkinnedMeshRenderer>().material});
                    foreach (Material material in playerMaterials) {
                        material.shader = unlitShader; 
                        if (material.HasColor("Main Color")) {
                            material.SetColor("Main Color", new Color(255, 246, 143, 255)); // doesn't work due to unlit shader not being loaded 
                        }
                    }
                } else if (!CanReboundDetailed() && hasUnlitCharacter) {
                    hasUnlitCharacter = false; 
                    player.SetOutfit(Reptile.Core.Instance.SaveManager.CurrentSaveSlot.GetCharacterProgress(player.character).outfit);
                }
            }
        }

        public static bool CanRebound() {
            return ReboundPlugin.landTime < maxLandingTimeToRebound && player.IsGrounded();
        }

        public static bool CanReboundDetailed() { // full requirements cross referenced from PlayerPatches
            return CanRebound() && (!player.jumpConsumed || player.ability == player.slideAbility || player.ability == player.boostAbility);
        }

        public static bool PlayerAttemptingRebound() {
            bool doingRBActions = ReboundPlugin.doReboundActions.Any() ? ReboundPlugin.ActionsPressed(ReboundPlugin.doReboundActions, RBSettings.config_requireAllDRA.Value) : true;
            bool cancelRBActions = ReboundPlugin.cancelReboundActions.Any() ? ReboundPlugin.ActionsPressed(ReboundPlugin.cancelReboundActions, RBSettings.config_requireAllCRA.Value) : false; 
            
            return ReboundPlugin.landingVelocity.y < -ReboundPlugin.minVelocityToRebound 
                && ReboundPlugin.landTime < ReboundPlugin.maxLandingTimeToRebound
                && !cancelRBActions && doingRBActions;
        }

        public static void ReboundTrick() {
            bool doingBoostedRebound = attemptingBoostedRebound;
            bool doingBurstRebound = attemptingBurstRebound || (attemptingBoostedRebound && RBQTHelper.AbilityIsQuickTurn(player));
            float floorAngle = Vector3.Dot(Vector3.ProjectOnPlane(player.motor.groundNormalVisual, Vector3.up).normalized, player.dir);
            
            // Force (default) Rebound animations to properly transition to jump/fall animations
            player.animInfosSets[(int)player.moveStyle][Animator.StringToHash("jumpTrick1")].fadeTo[player.fallHash] = 1f;
            player.animInfosSets[(int)player.moveStyle][Animator.StringToHash("jumpTrick1")].fadeTo[Animator.StringToHash("fallIdle")] = 1f;
            
            PrepForRebound(doingBoostedRebound); // Set up variables, play SFX/voice, particle effects, etc.
            if (refreshCombo) { player.comboTimeOutTimer = landingComboMeter; }
            rebounding = true;
            settingUpRebound = true;

            // Calc initial rebound velocity
            Vector2 reboundVelocity = landingVelocity;
            reboundVelocity.y = -reboundVelocity.y*reboundVelocityMultiplier; 
            
            // Set trick animation and name
            if (doingBoostedRebound) {
                player.DoTrick(Player.TrickType.AIR, (doingBurstRebound ? "Burst Rebound" : "Boosted Rebound"), 0);
                player.ActivateAbility(player.airTrickAbility);
                reboundVelocity.x = Mathf.Max(reboundVelocity.x, player.GetForwardSpeed());
            } else { 
                if (tempDisableBoostAfterRebound) {
                    player.ActivateAbility(player.airTrickAbility);
                    player.airTrickAbility.duration /= 3f;
                }

                player.DoTrick(Player.TrickType.AIR, RBTrix.GetReboundTrickName(), 0); 
                player.PlayAnim(RBTrix.StringToHash(RBTrix.GetReboundAnimation()), true, true, -1f);
            }

            // Counteract M+ fast fall exploitation - bounceCap should never be run into in vanilla
            float targetHeight = distanceFallenFromPeakOfJump * reboundVelocityMultiplier; // how high a rebound SHOULD be taking you
            float reboundCalculatedByHeight = Mathf.Sqrt(2f * targetHeight * player.motor.gravity); // velocity to get to said height
            float bounceCap = (reboundCalculatedByHeight/9f) + heightCapGenerosity; // why divide by nine? i dunno, but it works well!

            if (alwaysCalculateBasedOnHeight) { reboundVelocity.y = bounceCap - heightCapGenerosity; }
            else if (capBasedOnHeight) { reboundVelocity.y = Mathf.Min(reboundVelocity.y, bounceCap); }
            
            // Rebound off launcher bonus
            if (player.onLauncher) { 
                if (!RBSettings.config_slopeOnLauncher.Value) { floorAngle = 0f; }
                float launcherBonus = player.onLauncher.parent.gameObject.name.Contains("Super") ? 
                    player.jumpSpeedLauncher * 1.4f : player.jumpSpeedLauncher;
                reboundVelocity.y = Mathf.Max(reboundVelocity.y + (launcherBonus*launcherBonusMultiplier), launcherBonus);
            }

            // Constrain to min and max
            reboundVelocity.y = Mathf.Max(reboundVelocity.y, minReboundYVelocity);
            if (reboundVelocity.y > maxReboundYVelocity && maxReboundYVelocity != 0) {
                reboundVelocity.x = maxReboundYVelocity * (reboundVelocity.x/reboundVelocity.y); // reduce speed to follow same arc
                reboundVelocity.y = maxReboundYVelocity;
            }

            // (NEW!) Boosted Rebound changes
            if (doingBoostedRebound) {
                reboundVelocity *= 1.1f; 
                if (doingBurstRebound) {
                    Vector2 oldReboundVelocity = reboundVelocity;
                    reboundVelocity.y *= (1f - RBSettings.config_burstReboundPower.Value)*(1f + (0.65f));
                    reboundVelocity.x = Mathf.Pow(reboundVelocity.x, RBSettings.config_burstReboundPower.Value); 
                    reboundVelocity.y += (oldReboundVelocity.x - reboundVelocity.x)*0.65f;
                    player.audioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.launcher_woosh,
                    player.playerOneShotAudioSource, 0f);
                }
            }
            
            // Adjust velocity based on floor normal
            Vector2 floorAngleXY = new Vector2(
                Mathf.Sin(floorAngle)*(RBSettings.config_slopeMultiplierX.Value), 
                Mathf.Cos(floorAngle)*(RBSettings.config_slopeMultiplierY.Value));
            if (floorAngleXY.x < 0f) { floorAngleXY.y += 1f; } // higher instead of lower

            // Apply Rebound velocity
            player.motor.SetVelocityYOneTime(reboundVelocity.y * floorAngleXY.y);
            float newForwardSpeed = reboundVelocity.x + (reboundVelocity.y * floorAngleXY.x);
            player.SetForwardSpeed(newForwardSpeed);
            if (newForwardSpeed < 0f) { player.SetRotation(-player.dir); } // turn player around if going backwards
            
            // Handle combo meter
            if (comboCost != 0f) { 
                if ((player.comboTimeOutTimer - comboCost) > 1f) { player.ResetComboTimeOut(); } 
                else {
                    //float oldComboTime = player.comboTimeOutTimer;
                    player.DoComboTimeOut(comboCost);
                    //bool withinBuffer = (oldComboTime > (comboCost*0.6f) && player.comboTimeOutTimer < 0f) || player.comboTimeOutTimer == 0f;
                    player.comboTimeOutTimer = Mathf.Clamp(player.comboTimeOutTimer, 0f, 1f); 
                        //Mathf.Clamp(player.comboTimeOutTimer, (withinBuffer ? 0.000001f : 0f), 1f); 
                }
            }
            
            if (player.comboTimeOutTimer == 0f) { player.LandCombo(); }
            //if (boostCost != 0f) { player.AddBoostCharge(-boostCost); }

            if (RBSettings.config_enableTrail.Value) {
                player.boostTrailTimer = reboundTrailTime;
                player.trailWidth = reboundTrailWidth;
            }

            rebounding = true;
            settingUpRebound = false;
        }

        public static void PrepForRebound(bool isBoosting) {
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
            
            player.AudioManager.PlaySfxGameplay(SfxCollectionID.GenericMovementSfx, AudioClipID.jump_special, 
                player.playerOneShotAudioSource, 0f);
            player.PlayVoice(AudioClipID.VoiceJump, VoicePriority.MOVEMENT, true);
            if (!isBoosting) { player.PlayAnim(RBTrix.StringToHash(RBTrix.GetReboundAnimation()), true, false, -1f); }
            else { player.PlayAnim(Animator.StringToHash("jump"), true, false, -1f); }

            player.DoHighJumpEffects(player.motor.groundNormalVisual * -1f);
            //player.ringParticles.Emit(1);
        }

        public static void CancelRebound() {
            if (!ComboCanBeCancelled() || !RBSettings.config_preventComboExtend.Value || rebounding) { return; }
            ReboundPlugin.landTime = ReboundPlugin.maxLandingTimeToRebound + 1f;
            player.LandCombo();
        }

        public static bool ComboCanBeCancelled() { 
            if (!canCancelCombo) { return false; }
            bool mpluscombodone = MPlusActive ? player.comboTimeOutTimer <= 0 : true;
            return mpluscombodone && player.ability != player.slideAbility && //&& !player.IsPerformingManualTrick
            !player.slideAbility.locked && player.ability != player.ledgeClimbAbility && player.switchMoveStyleAbility != player.ability;
        }

        private void OnDestroy() { Harmony.UnpatchSelf(); }

        public static bool ActionsPressed(List<string> actions, bool checkIfAllPressed) {
            if (actions?.Any() != true) { return false; }
            bool returnValue = checkIfAllPressed;
            foreach (string actionPropertyOrMethod in actions) {
                if (TranslateActionString(actionPropertyOrMethod) == !checkIfAllPressed) 
                { returnValue = !checkIfAllPressed; }
            } return returnValue;      
        }

        public static bool TranslateActionString(string action) {
            switch(action) {
                case "jumpRequested": return player.jumpRequested;
                case "trick1ButtonHeld": return player.trick1ButtonHeld || player.trick1ButtonNew;
                case "trick2ButtonHeld": return player.trick2ButtonHeld || player.trick2ButtonNew;
                case "trick3ButtonHeld": return player.trick3ButtonHeld || player.trick3ButtonNew;
                case "slideButtonHeld": return player.slideButtonHeld || player.slideButtonNew || player.ability == player.slideAbility;
                case "boostButtonHeld": return player.boostButtonHeld || player.boostButtonNew;
                case "sprayButtonHeld": return player.sprayButtonHeld || player.sprayButtonNew;
                case "danceButtonHeld": return player.danceButtonHeld || player.danceButtonNew;
                case "switchStyleButtonHeld": return player.switchStyleButtonHeld || player.switchStyleButtonNew;
                case "walkButtonHeld": return player.walkButtonHeld || player.walkButtonNew;
                default: return false;
            }
        }

        public static bool InGame() {
            return player != null && !player.isDisabled && !Core.Instance.BaseModule.IsInGamePaused;
        }
    }
}