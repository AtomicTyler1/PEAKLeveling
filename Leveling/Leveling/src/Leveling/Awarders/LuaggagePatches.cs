using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace Leveling.Awarders
{
    [HarmonyPatch]
    class LuaggagePatches
    {

        // Doesnt seem to be working properly? - not awarding xp, needs proper testing.

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Luggage), nameof(Luggage.OpenLuggageRPC))]
        static void Luggage_OpenLuggageRPC_Postfix(Luggage __instance)
        {
            Transform luggageTransform = __instance.transform;
            Transform playerTransform = Player.localPlayer.character.transform;

            float distance = Vector3.Distance(luggageTransform.position, playerTransform.position);

            float requiredDistance = 3f;

            if (distance <= requiredDistance)
            {
                int xpAward = 15;
                LevelingAPI.AddExperience(xpAward);
                Plugin.Log.LogInfo($"Awarded {xpAward} XP for opening luggage.");
            }
        }
    }
}
