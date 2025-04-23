using Reptile;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using CrewBoomMono;
// seperate class to avoid compilation errors if CrewBoomMono is not installed
namespace Rebound
{
    public class RBCBHelper {
        public static List<Material> GetPlayerMaterials(Player p) { 
            List<Material> newList = new List<Material>(); 
            CharacterDefinition definition = p.characterVisual.GetComponentInChildren<CharacterDefinition>(true);
            if (definition == null) { return new List<Material>() {p.characterVisual.GetComponentInChildren<SkinnedMeshRenderer>().material}; }
            
            foreach (SkinnedMeshRenderer renderer in definition.Renderers) {
                foreach (Material rendererMaterial in renderer.materials) {
                    newList.Add(rendererMaterial);
                }
            }
            return newList; 
        }
    }
}