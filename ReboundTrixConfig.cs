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
    [BepInPlugin("goatgirl.Rebound.NewTrix", "Rebound.NewTrix", "1.0.0")]
    [BepInProcess("Bomb Rush Cyberfunk.exe")]
    [BepInDependency("ConfigTrixAirTricks", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Dragsun.BunchOfEmotes", BepInDependency.DependencyFlags.SoftDependency)]
    public class ReboundTrixPlugin : BaseUnityPlugin {
        private void Awake() { 
            RBTrix.UpdateSettings(Config); 

            // Check for NewTrix + BOE 
            bool hasTrix = false;
            bool hasBOE = false;
            foreach (var plugin in BepInEx.Bootstrap.Chainloader.PluginInfos)
            {
                if (plugin.Value.Metadata.GUID.Equals("ConfigTrixAirTricks")) { hasTrix = true; }
                else if (plugin.Value.Metadata.GUID.Equals("com.Dragsun.BunchOfEmotes")) { hasBOE = true; }
            }
            RBTrix.TrixBOEActive = hasTrix && hasBOE;
            if (RBTrix.TrixBOEActive) { Logger.LogInfo("NewTrix + BOE active!"); }
        }
    }

    internal class RBTrix {
        public static ConfigEntry<bool> boostedReboundsRespectTrickButtons;
        public static ConfigEntry<float> forceAnimationBlending;
        
        public static ConfigEntry<string> reboundTrickAnimFootDefault;
        public static ConfigEntry<string> reboundTrickNameFootDefault;
        public static ConfigEntry<string> reboundTrickAnimFoot0;
        public static ConfigEntry<string> reboundTrickNameFoot0;
        public static ConfigEntry<string> reboundTrickAnimFoot1;
        public static ConfigEntry<string> reboundTrickNameFoot1;
        public static ConfigEntry<string> reboundTrickAnimFoot2;
        public static ConfigEntry<string> reboundTrickNameFoot2;

        public static ConfigEntry<string> reboundTrickAnimBMXDefault;
        public static ConfigEntry<string> reboundTrickNameBMXDefault;
        public static ConfigEntry<string> reboundTrickAnimBMX0;
        public static ConfigEntry<string> reboundTrickNameBMX0;
        public static ConfigEntry<string> reboundTrickAnimBMX1;
        public static ConfigEntry<string> reboundTrickNameBMX1;
        public static ConfigEntry<string> reboundTrickAnimBMX2;
        public static ConfigEntry<string> reboundTrickNameBMX2;

        public static ConfigEntry<string> reboundTrickAnimInlineDefault;
        public static ConfigEntry<string> reboundTrickNameInlineDefault;
        public static ConfigEntry<string> reboundTrickAnimInline0;
        public static ConfigEntry<string> reboundTrickNameInline0;
        public static ConfigEntry<string> reboundTrickAnimInline1;
        public static ConfigEntry<string> reboundTrickNameInline1;
        public static ConfigEntry<string> reboundTrickAnimInline2;
        public static ConfigEntry<string> reboundTrickNameInline2;

        public static ConfigEntry<string> reboundTrickAnimBoardDefault;
        public static ConfigEntry<string> reboundTrickNameBoardDefault;
        public static ConfigEntry<string> reboundTrickAnimBoard0;
        public static ConfigEntry<string> reboundTrickNameBoard0;
        public static ConfigEntry<string> reboundTrickAnimBoard1;
        public static ConfigEntry<string> reboundTrickNameBoard1;
        public static ConfigEntry<string> reboundTrickAnimBoard2;
        public static ConfigEntry<string> reboundTrickNameBoard2;

        /*public static ConfigEntry<string> reboundTrickAnimFootDefaultBoost;
        public static ConfigEntry<string> reboundTrickAnimBMXDefaultBoost;
        public static ConfigEntry<string> reboundTrickAnimInlineDefaultBoost;
        public static ConfigEntry<string> reboundTrickAnimBoardDefaultBoost; */

        public static bool TrixBOEActive = false;
        
        public static void UpdateSettings(ConfigFile Config) { BindSettings(Config); }

        private static void BindSettings(ConfigFile Config) {
            boostedReboundsRespectTrickButtons = Config.Bind("1. Options", "Multiple Tricks for Boosted Rebounds", true, "If enabled, holding a trick button while doing a Boosted Rebound will perform the boost trick associated with that button.");
            forceAnimationBlending = Config.Bind("1. Options", "Force Smooth Animation Blending", 0f, "The speed at which the Rebound animation blends into the fall animation (bigger is slower). If set to 0 or below, many custom Rebound animations will not smoothly transition into the fall animation.");

            reboundTrickAnimFootDefault = Config.Bind("2. On-Foot Rebound Tricks", "On-Foot Trick (Default)", "jumpTrick1", "Default Rebound trick, holding no trick button");
            reboundTrickNameFootDefault = Config.Bind("2. On-Foot Rebound Tricks", "On-Foot Trick (Default) Name", "Rebound Corkscrew", "Default Rebound trick name, holding no trick button");
            reboundTrickAnimFoot0 = Config.Bind("2. On-Foot Rebound Tricks", "On-Foot Trick 0", "jumpTrick1", "Button 1 Rebound trick");
            reboundTrickNameFoot0 = Config.Bind("2. On-Foot Rebound Tricks", "On-Foot Trick 0 Name", "Rebound Corkscrew", "Button 1 Rebound trick name");
            reboundTrickAnimFoot1 = Config.Bind("2. On-Foot Rebound Tricks", "On-Foot Trick 1", "jumpTrick1", "Button 2 Rebound trick");
            reboundTrickNameFoot1 = Config.Bind("2. On-Foot Rebound Tricks", "On-Foot Trick 1 Name", "Rebound Corkscrew", "Button 2 Rebound trick name");
            reboundTrickAnimFoot2 = Config.Bind("2. On-Foot Rebound Tricks", "On-Foot Trick 2", "jumpTrick1", "Button 3 Rebound trick");
            reboundTrickNameFoot2 = Config.Bind("2. On-Foot Rebound Tricks", "On-Foot Trick 2 Name", "Rebound Corkscrew", "Button 3 Rebound trick name");

            reboundTrickAnimBoardDefault = Config.Bind("3. Skateboard Rebound Tricks", "Skateboard Trick (Default)", "jumpTrick1", "Default Rebound trick, holding no trick button");
            reboundTrickNameBoardDefault = Config.Bind("3. Skateboard Rebound Tricks", "Skateboard Trick (Default) Name", "Rebound McTwist", "Default Rebound trick name, holding no trick button");
            reboundTrickAnimBoard0 = Config.Bind("3. Skateboard Rebound Tricks", "Skateboard Trick 0", "jumpTrick1", "Button 1 Rebound trick");
            reboundTrickNameBoard0 = Config.Bind("3. Skateboard Rebound Tricks", "Skateboard Trick 0 Name", "Rebound McTwist", "Button 1 Rebound trick name");
            reboundTrickAnimBoard1 = Config.Bind("3. Skateboard Rebound Tricks", "Skateboard Trick 1", "jumpTrick1", "Button 2 Rebound trick");
            reboundTrickNameBoard1 = Config.Bind("3. Skateboard Rebound Tricks", "Skateboard Trick 1 Name", "Rebound McTwist", "Button 2 Rebound trick name");
            reboundTrickAnimBoard2 = Config.Bind("3. Skateboard Rebound Tricks", "Skateboard Trick 2", "jumpTrick1", "Button 3 Rebound trick");
            reboundTrickNameBoard2 = Config.Bind("3. Skateboard Rebound Tricks", "Skateboard Trick 2 Name", "Rebound McTwist", "Button 3 Rebound trick name");

            reboundTrickAnimInlineDefault = Config.Bind("4. Inline Rebound Tricks", "Inline Trick (Default)", "jumpTrick1", "Default Rebound trick, holding no trick button");
            reboundTrickNameInlineDefault = Config.Bind("4. Inline Rebound Tricks", "Inline Trick (Default) Name", "Rebound Corkscrew", "Default Rebound trick name, holding no trick button");
            reboundTrickAnimInline0 = Config.Bind("4. Inline Rebound Tricks", "Inline Trick 0", "jumpTrick1", "Button 1 Rebound trick");
            reboundTrickNameInline0 = Config.Bind("4. Inline Rebound Tricks", "Inline Trick 0 Name", "Rebound Corkscrew", "Button 1 Rebound trick name");
            reboundTrickAnimInline1 = Config.Bind("4. Inline Rebound Tricks", "Inline Trick 1", "jumpTrick1", "Button 2 Rebound trick");
            reboundTrickNameInline1 = Config.Bind("4. Inline Rebound Tricks", "Inline Trick 1 Name", "Rebound Corkscrew", "Button 2 Rebound trick name");
            reboundTrickAnimInline2 = Config.Bind("4. Inline Rebound Tricks", "Inline Trick 2", "jumpTrick1", "Button 3 Rebound trick");
            reboundTrickNameInline2 = Config.Bind("4. Inline Rebound Tricks", "Inline Trick 2 Name", "Rebound Corkscrew", "Button 3 Rebound trick name");

            reboundTrickAnimBMXDefault = Config.Bind("5. BMX Rebound Tricks", "BMX Trick (Default)", "jumpTrick1", "Default Rebound trick, holding no trick button");
            reboundTrickNameBMXDefault = Config.Bind("5. BMX Rebound Tricks", "BMX Trick (Default) Name", "Rebound 360 Backflip", "Default Rebound trick name, holding no trick button");
            reboundTrickAnimBMX0 = Config.Bind("5. BMX Rebound Tricks", "BMX Trick 0", "jumpTrick1", "Button 1 Rebound trick");
            reboundTrickNameBMX0 = Config.Bind("5. BMX Rebound Tricks", "BMX Trick 0 Name", "Rebound 360 Backflip", "Button 1 Rebound trick name");
            reboundTrickAnimBMX1 = Config.Bind("5. BMX Rebound Tricks", "BMX Trick 1", "jumpTrick1", "Button 2 Rebound trick");
            reboundTrickNameBMX1 = Config.Bind("5. BMX Rebound Tricks", "BMX Trick 1 Name", "Rebound 360 Backflip", "Button 2 Rebound trick name");
            reboundTrickAnimBMX2 = Config.Bind("5. BMX Rebound Tricks", "BMX Trick 2", "jumpTrick1", "Button 3 Rebound trick");
            reboundTrickNameBMX2 = Config.Bind("5. BMX Rebound Tricks", "BMX Trick 2 Name", "Rebound 360 Backflip", "Button 3 Rebound trick name");
        }

        public static string GetReboundAnimation(MoveStyle moveStyle, int trickButton = -1) {
            string trickString = trickButton == -1 ? "Default" : trickButton.ToString(); 
            string moveStyleString = moveStyle == MoveStyle.BMX ? "BMX" : 
                    (moveStyle == MoveStyle.SKATEBOARD || moveStyle == MoveStyle.SPECIAL_SKATEBOARD ? "Board" : 
                        (moveStyle == MoveStyle.INLINE ? "Inline" : "Foot")
                    );
            
            try {
                string fieldName = "reboundTrick" + "Anim" + moveStyleString + trickString;
                ConfigEntry<string> configField = (ConfigEntry<string>)typeof(RBTrix).GetField(fieldName).GetValue(null);
                return configField.Value;
            } 
            catch (System.Exception ex) {
                ReboundPlugin.Log.LogError("GetReboundAnimation exception: " + (string)ex.Message);
                return "jumpTrick1";
            }
            
        }

        public static string GetReboundTrickName(MoveStyle moveStyle, int trickButton = -1) {
            string trickString = trickButton == -1 ? "Default" : trickButton.ToString(); 
            string moveStyleString = moveStyle == MoveStyle.BMX ? "BMX" : 
                    (moveStyle == MoveStyle.SKATEBOARD || moveStyle == MoveStyle.SPECIAL_SKATEBOARD ? "Board" : 
                        (moveStyle == MoveStyle.INLINE ? "Inline" : "Foot")
                    );
            
            try {
                string fieldName = "reboundTrick" + "Name" + moveStyleString + trickString;
                ConfigEntry<string> configField = (ConfigEntry<string>)typeof(RBTrix).GetField(fieldName).GetValue(null);
                return configField.Value;
            } 
            catch (System.Exception ex) {
                ReboundPlugin.Log.LogError("GetReboundTrickName exception: " + (string)ex.Message);
                return moveStyle == MoveStyle.BMX ? "Rebound 360 Backflip" : 
                    (moveStyle == MoveStyle.SKATEBOARD || moveStyle == MoveStyle.SPECIAL_SKATEBOARD ? "Rebound McTwist" : 
                        (moveStyle == MoveStyle.INLINE ? "Rebound Corkscrew" : "Rebound Corkscrew")
                    );
            }
            
        }

        public static string GetReboundAnimation() {
            Player p = ReboundPlugin.player;
            return GetReboundAnimation(p.moveStyle, GetPlayerTrickNumber(p)); 
        }

        public static List<int> GetAllReboundAnimations() {
            Player p = ReboundPlugin.player;
            return new List<int> { 
                RBTrix.StringToHash(GetReboundAnimation(p.moveStyle, -1)),
                RBTrix.StringToHash(GetReboundAnimation(p.moveStyle, 0)),
                RBTrix.StringToHash(GetReboundAnimation(p.moveStyle, 1)),
                RBTrix.StringToHash(GetReboundAnimation(p.moveStyle, 2)),
                };
        }

        public static string GetReboundTrickName() {
            Player p = ReboundPlugin.player;
            return GetReboundTrickName(p.moveStyle, GetPlayerTrickNumber(p)); 
        }

        public static int GetPlayerTrickNumber(Player p) {
			if (p.trick1ButtonNew || p.trick1ButtonHeld) { return 0; }
			if (p.trick2ButtonNew || p.trick2ButtonHeld) { return 1; }
			if (p.trick3ButtonNew || p.trick3ButtonHeld) { return 2; }
			return -1;
        }

        public static int StringToHash(string name) {
            if (TrixBOEActive) { return Rebound.RBTrixHelper.StringToHash(name); }
            else { return Animator.StringToHash(name); }
        }
    }
}