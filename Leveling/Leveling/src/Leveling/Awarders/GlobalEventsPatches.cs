using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace Leveling.Awarders;

[HarmonyPatch]
class GlobalEventsPatches
{
    private static float CalculateEscapeExperience()
    {
        int ascent = Ascents.currentAscent;
        if (ascent < 0)
        {
            return 250f;
        }

        return 500f + ascent * 50f;
    }

    [HarmonyPatch(typeof(GlobalEvents), nameof(GlobalEvents.TriggerRunEnded))]
    [HarmonyPostfix]
    public static void GlobalEvents_TriggerRunEnded()
    {
        Character local = Character.localCharacter;

        if (local.refs.stats.won)
        {
            float xpAward = CalculateEscapeExperience();
            Plugin.IncreaseXPSource(Plugin.XPSource.Winning, xpAward, false);
            LevelingAPI.AddExperience(xpAward, false);
            Plugin.Log.LogInfo($"Awarded {xpAward} XP for winning the game.");
        }
        else if (local.refs.stats.somebodyElseWon)
        {
            float xpAward = 50f;
            Plugin.IncreaseXPSource(Plugin.XPSource.Winning, xpAward, false);
            LevelingAPI.AddExperience(xpAward, false);
            Plugin.Log.LogInfo($"Awarded {xpAward} XP for teamate winning");
        }
    }
}
