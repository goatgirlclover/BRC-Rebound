using Reptile;
// seperate class to avoid compilation errors if QuickTurn is not installed
namespace Rebound
{
    public class RBQTHelper {
        public static float QuickTurnSavedSpeed() { return QuickTurn.NewQuickTurnAbility.savedSpeed; }
        public static bool AbilityIsQuickTurn(Player p) { return p.ability is QuickTurn.NewQuickTurnAbility; }
    }
}