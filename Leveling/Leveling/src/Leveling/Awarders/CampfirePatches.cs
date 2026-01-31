using HarmonyLib;
using Photon.Pun.Demo.PunBasics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Leveling.Awarders;

[HarmonyPatch]
public class CampfirePatches
{
    private static float xpToAward = 10f;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Campfire), nameof(Campfire.Light_Rpc))]
    public static void Campfire_Light_Rpc_Patch(Campfire __instance)
    {
        float bonusXp = __instance.advanceToSegment switch
        {
            Segment.Tropics => 0f,
            Segment.Alpine => 5f,
            Segment.Caldera => 10f,
            Segment.TheKiln => 15f,
            _ => 0f
        };

        float totalXp = xpToAward + bonusXp;
        Plugin.XPGained_Other += totalXp;
        LevelingAPI.AddExperience(totalXp);
    }
}
