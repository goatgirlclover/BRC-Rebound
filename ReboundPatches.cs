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
    [HarmonyPatch(typeof(Player))]
    internal class PlayerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.HandleJump))]
        public static bool HandleJumpPrefix_TriggerRebound(Player __instance)
        {
            if (__instance != ReboundPlugin.player || __instance.isDisabled || __instance.isAI) { return true; }
            
            __instance.jumpedThisFrame = false;
            __instance.timeSinceJumpRequested += Reptile.Core.dt;
            //ReboundPlugin.rebounding = false;

            // simplified JumpIsAllowed check for rebound - bypasses any ability checks
            if (__instance.jumpRequested && !__instance.jumpConsumed && 
            (__instance.IsGrounded() || __instance.timeSinceLastAbleToJump <= __instance.JumpPostGroundingGraceTime)) {            
                bool doingRBActions = ReboundPlugin.doReboundActions.Any() ? ReboundPlugin.ActionsPressed(ReboundPlugin.doReboundActions, RBSettings.config_requireAllDRA.Value) : true;
                bool cancelRBActions = ReboundPlugin.cancelReboundActions.Any() ? ReboundPlugin.ActionsPressed(ReboundPlugin.cancelReboundActions, RBSettings.config_requireAllCRA.Value) : false; 
                
                if (ReboundPlugin.landingVelocity.y < -ReboundPlugin.minVelocityToRebound 
                && ReboundPlugin.CanRebound() 
                && !cancelRBActions 
                && doingRBActions) { 
                    ReboundPlugin.ReboundTrick(); 
                } else if (__instance.JumpIsAllowed()) { // kind of inefficient...
                    if (ReboundPlugin.CanRebound()) { ReboundPlugin.CancelRebound(); }
                    __instance.Jump(); 
                }
                
                ReboundPlugin.distanceFallenFromPeakOfJump = 0f;
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.OnLanded))]
        public static bool OnLandedPrefix_ReboundDistance(Player __instance) {
            if (__instance != ReboundPlugin.player || __instance.isDisabled || __instance.isAI) { return true; }
            ReboundPlugin.rebounding = false;
            
            //__instance.boostpackTrailDefaultTime = ReboundPlugin.originalTrailTime;
            //__instance.boostpackTrailDefaultWidth = ReboundPlugin.originalTrailWidth;
            
            ReboundPlugin.landTime = 0f;
            ReboundPlugin.distanceFallenFromPeakOfJump += __instance.reallyMovedVelocity.y; // adds "velocity" from frame before that probably isn't caught. make sure this works the way we want it to!
            //ReboundPlugin.groundedPositionY = __instance.tf.position.y;
            if (__instance.comboTimeOutTimer <= 0) { ReboundPlugin.CancelRebound(); }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.DoTrick))]
        public static bool DoTrickPrefix_FailIfNotRebound(Player.TrickType type, string trickName, int trickNum, Player __instance) {
            if (__instance != ReboundPlugin.player || __instance.isDisabled || __instance.isAI) { return true; }

            // if can rebound and choosing not to, drop combo
            if (ReboundPlugin.CanRebound() && !(trickName.Contains("Rebound") && trickNum == 0 && type == Player.TrickType.AIR)) {
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
            return (!(ReboundPlugin.CanRebound() && !__instance.slideAbility.stopDecided));
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.ActivateAbility))]
        public static bool AbilityPrefix_CancelRebound(Ability a, Player __instance) {
            if (__instance != ReboundPlugin.player || __instance.isDisabled || __instance.isAI) { return true; }
            ReboundPlugin.rebounding = false;

            //__instance.boostpackTrailDefaultTime = ReboundPlugin.originalTrailTime;
            //__instance.boostpackTrailDefaultWidth = ReboundPlugin.originalTrailWidth;

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
    }

}