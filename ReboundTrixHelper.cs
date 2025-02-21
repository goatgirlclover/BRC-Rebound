using Reptile;
using System.Collections;
using System.Collections.Generic;
// seperate class to avoid compilation errors if NewTrix is not installed
namespace Rebound
{
    public class RBTrixHelper {
        public static int StringToHash(string animationName) {
            return trickyclown.AnimationUtility.GetAnimationByName(animationName);
        }
    }
}