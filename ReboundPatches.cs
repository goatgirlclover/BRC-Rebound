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
    [HarmonyPatch(typeof(AirTrickAbility))]
    internal class AirTrickPatch {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(AirTrickAbility.SetupBoostTrick))]
        private static bool SetupBoostTrick_Prefix(AirTrickAbility __instance) { 
            if (ReboundPlugin.rebounding && ReboundPlugin.allowBoostedRebounds && RBTrix.boostedReboundsRespectTrickButtons.Value) {
                int trickNum = (int)RBTrix.GetPlayerTrickNumber(ReboundPlugin.player);
                if (trickNum < 0) { trickNum = 0; }
                __instance.curTrick = trickNum;
            }
            ReboundPlugin.Log.LogInfo(__instance.curTrick);
            ReboundPlugin.Log.LogInfo(ReboundPlugin.rebounding.ToString() + ReboundPlugin.allowBoostedRebounds.ToString() + RBTrix.boostedReboundsRespectTrickButtons.Value.ToString());
            return true;
        }    
    }

    [HarmonyPatch(typeof(Player))]
    internal class PlayerPatches
    {
        public static bool wasRequestingJump;
        public static float wasRequestingJumpTime;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.HandleJump))]
        public static bool HandleJumpPrefix_TriggerRebound(Player __instance)
        {
            if (__instance != ReboundPlugin.player || __instance.isDisabled || __instance.isAI) { return true; }
            
            __instance.jumpedThisFrame = false;
            __instance.timeSinceJumpRequested += Reptile.Core.dt;

            // simplified JumpIsAllowed check for Rebound - bypasses any ability checks
            if (__instance.jumpRequested && __instance.IsGrounded()
            && (!__instance.jumpConsumed || __instance.ability == __instance.slideAbility || __instance.ability == __instance.boostAbility)) {            
                if (ReboundPlugin.PlayerAttemptingRebound()) { 
                    ReboundPlugin.ReboundTrick(); 
                } else if (__instance.JumpIsAllowed()) { // kind of inefficient...
                    if (ReboundPlugin.CanRebound()) { ReboundPlugin.CancelRebound(); }
                    __instance.Jump(); 
                    ReboundPlugin.landTime = float.PositiveInfinity;
                }
                
                ReboundPlugin.distanceFallenFromPeakOfJump = 0f;
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.FixedUpdateAbilities))]
        public static bool Postfix_FuckOverQuickTurn(Player __instance) {
            if (!ReboundPlugin.hasQuickTurn) { return true; }
            if (__instance != ReboundPlugin.player || __instance.isDisabled || __instance.isAI) { return true; }
            if (!RBQTHelper.AbilityIsQuickTurn(__instance)) { return true; }

            if (__instance.jumpButtonNew && __instance.IsGrounded()
            && ReboundPlugin.PlayerAttemptingRebound()) { //&& ReboundPlugin.attemptingBoostedRebound) {         
                __instance.jumpButtonNew = false;
                ReboundPlugin.ReboundTrick();
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.OnLanded))]
        public static bool OnLandedPrefix_ReboundDistance(Player __instance) {
            if (__instance != ReboundPlugin.player || __instance.isDisabled || __instance.isAI) { return true; }
            ReboundPlugin.rebounding = false;
                        
            ReboundPlugin.landTime = 0f;
            ReboundPlugin.distanceFallenFromPeakOfJump += __instance.reallyMovedVelocity.y; 
            //ReboundPlugin.groundedPositionY = __instance.tf.position.y;
            if (__instance.comboTimeOutTimer <= 0) { ReboundPlugin.CancelRebound(); }

            wasRequestingJump = __instance.jumpRequested;
            wasRequestingJumpTime = __instance.timeSinceJumpRequested;
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Player.OnLanded))]
        public static void OnLandedPostfix_ReboundSlide(Player __instance) {
            if (__instance != ReboundPlugin.player || __instance.isDisabled || __instance.isAI) { return; }

            if (__instance.slideButtonHeld && !__instance.slideAbility.locked && !__instance.jumpRequested 
            && ((wasRequestingJump && wasRequestingJumpTime <= __instance.JumpPreGroundingGraceTime) || __instance.jumpButtonNew)
            && ReboundPlugin.PlayerAttemptingRebound()) { //&& ReboundPlugin.doReboundActions.Contains("slideButtonHeld")
                //ReboundPlugin.Log.LogInfo("Buffered slide rebound");
                __instance.jumpRequested = true;
                __instance.timeSinceJumpRequested = wasRequestingJumpTime;
                ReboundPlugin.ReboundTrick();
                //ReboundPlugin.ReboundTrick(); 
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.DoTrick))]
        public static bool DoTrickPrefix_FailIfNotRebound(Player.TrickType type, string trickName, int trickNum, Player __instance) {
            if (__instance != ReboundPlugin.player || __instance.isDisabled || __instance.isAI) { return true; }

            // if can rebound and choosing not to, drop combo
            if (ReboundPlugin.CanRebound() && !ReboundPlugin.rebounding) {
                if (ReboundPlugin.landTime > Reptile.Core.dt*3f && !(__instance.currentTrickType == Player.TrickType.SLIDE && type == Player.TrickType.SLIDE)) {
                    ReboundPlugin.CancelRebound();
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.LandCombo))]
        public static bool LandComboPrefix_ExtendForRebound(Player __instance) {
            if (__instance != ReboundPlugin.player || __instance.isDisabled || __instance.isAI) { return true; }

            // don't land combo if the player can still rebound
            return !ReboundPlugin.CanRebound(); //(!(ReboundPlugin.CanRebound() && !__instance.slideAbility.stopDecided));
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.ActivateAbility))]
        public static bool AbilityPrefix_CancelRebound(Ability a, Player __instance) {
            if (__instance != ReboundPlugin.player || __instance.isDisabled || __instance.isAI) { return true; }
            if (!ReboundPlugin.settingUpRebound) { ReboundPlugin.rebounding = false; }

            if ((__instance.ability == null || __instance.ability == __instance.slideAbility) && a != __instance.boostAbility && 
            ReboundPlugin.CanRebound() && ReboundPlugin.landTime > Reptile.Core.dt*3f) {
                ReboundPlugin.CancelRebound();
            }
            
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.UpdateTrails))]
        public static bool UpdateReboundTrail(Player __instance) {
			if (__instance.characterVisual.VFX.boostpackEffect.activeSelf)
			{
				if (!__instance.preBoostpackActive)
				{
					__instance.boostpackTrail.time = (__instance.boostpackTrail.startWidth = 0f);
				}
				__instance.preBoostpackActive = true;
				__instance.characterVisual.VFX.boostpackTrail.SetActive(true);
				__instance.boostTrailTimer += Core.dt * 1.2f;
				if (__instance.boostTrailTimer > __instance.boostpackTrailDefaultTime && !ReboundPlugin.rebounding)
				{
					__instance.boostTrailTimer = __instance.boostpackTrailDefaultTime;
				}
				__instance.trailWidth += Core.dt;
				if (__instance.trailWidth > __instance.boostpackTrailDefaultWidth && !ReboundPlugin.rebounding)
				{
					__instance.trailWidth = __instance.boostpackTrailDefaultWidth;
				}
			}
			else
			{
				__instance.preBoostpackActive = false;
				__instance.boostTrailTimer -= Core.dt * 6f;
				if (__instance.boostTrailTimer < 0f)
				{
					__instance.boostTrailTimer = 0f;
					__instance.characterVisual.VFX.boostpackTrail.SetActive(false);
                    ReboundPlugin.rebounding = false;
				}
				__instance.trailWidth -= Core.dt;
				if (__instance.trailWidth < 0f)
				{
					__instance.trailWidth = 0f;
				}
			}
			__instance.boostpackTrail.time = __instance.boostTrailTimer;
			__instance.boostpackTrail.startWidth = __instance.trailWidth;

            return false;
        }

        /* [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.PlayAnim))]
        public static bool PlayAnimPrefix(int newAnim, bool forceOverwrite, bool instant, float atTime, Player __instance) {
            if (!__instance.gameObject.activeSelf || (newAnim == __instance.curAnim && !forceOverwrite) 
            || !ReboundPlugin.rebounding || RBTrix.forceAnimationBlending.Value <= 0f) { return true; }
            
			if (!instant && atTime == -1f && ReboundPlugin.rebounding && !__instance.animInfos.ContainsKey(__instance.curAnim)
                //&& __instance.curAnim == Animator.StringToHash(RBTrix.GetReboundAnimation())
                && RBTrix.GetAllReboundAnimations().Contains(__instance.curAnim)
                && (newAnim == __instance.fallHash || newAnim == Animator.StringToHash("fallIdle")))
			{
				__instance.anim.CrossFade(newAnim, RBTrix.forceAnimationBlending.Value);
                __instance.curAnimActiveTime = 0f;
                __instance.firstFrameAnim = true;
                __instance.characterVisual.feetIK = (__instance.animInfos.ContainsKey(newAnim) && __instance.animInfos[newAnim].feetIK);
                __instance.curAnim = newAnim;
                return false; 
			}
            
            return true;
        } */
    }

}