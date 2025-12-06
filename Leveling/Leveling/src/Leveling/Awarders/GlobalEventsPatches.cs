using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace Leveling.Awarders;

[HarmonyPatch]
class GlobalEventsPatches
{
    [HarmonyPatch(typeof(GlobalEvents), nameof(GlobalEvents.TriggerRunEnded))]
    [HarmonyPostfix]
    public static void GlobalEvents_TriggerRunEnded()
    {
        Character local = Character.localCharacter;

        if (local.refs.stats.won)
        {
            int xpAward = 500;
            LevelingAPI.AddExperience(xpAward);
            Plugin.Log.LogInfo($"Awarded {xpAward} XP for winning the game.");
        }
        else if (local.refs.stats.somebodyElseWon)
        {
            int xpAward = 50;
            LevelingAPI.AddExperience(xpAward);
            Plugin.Log.LogInfo($"Awarded {xpAward} XP for teamate winning");
        }
    }
}
