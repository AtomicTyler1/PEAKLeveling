using HarmonyLib;
using Photon.Pun.Demo.PunBasics;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Leveling.Awarders
{
    [HarmonyPatch]
    class CharacterPatches
    {

        private static float moraleBoostCooldown = 60f;
        private static float lastMoraleBoostXPTime = -Mathf.Infinity;

        private static float climbSpamPreventionTime = -Mathf.Infinity;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), nameof(Character.Zombify))]
        static void Character_Zombify_Postfix(Character __instance)
        {
            if (__instance.IsLocal)
            {
                int xpAward = 50;
                LevelingAPI.AddExperience(xpAward);
                Plugin.Log.LogInfo($"Awarded {xpAward} XP for zombifying.");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), nameof(Character.GetFedItemRPC))]
        static void Character_GetFedItemRPC_Postfix(Character __instance)
        {
            if (__instance.IsLocal)
            {
                int xpAward = 10;
                LevelingAPI.AddExperience(xpAward);
                Plugin.Log.LogInfo($"Awarded {xpAward} XP for being fed.");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), nameof(Character.RPCA_Die))]
        static void Character_Die_Postfix(Character __instance)
        {
            if (__instance.IsLocal)
            {
                int xpAward = 5;
                LevelingAPI.AddExperience(xpAward);
                Plugin.Log.LogInfo($"Awarded {xpAward} XP for dying.");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), nameof(Character.MoraleBoost))]
        static void Character_MoraleBoost_Postfix(Character __instance)
        {
            if (__instance.IsLocal)
            {
                if (Time.time - lastMoraleBoostXPTime < moraleBoostCooldown)
                {
                    return;
                }

                lastMoraleBoostXPTime = Time.time;

                int xpAward = 10;
                LevelingAPI.AddExperience(xpAward);
                Plugin.Log.LogInfo($"Awarded {xpAward} XP for morale boost.");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), nameof(Character.OnStartClimb))]
        static void Character_OnStartClimb_Postfix(Character __instance)
        {
            if (__instance.IsLocal && SceneManager.GetActiveScene().name != "Airport")
            {
                if (Time.time - climbSpamPreventionTime < 15f)
                {
                    return;
                }

                int randomNumber = UnityEngine.Random.Range(0, 100);

                if (randomNumber > 5)
                {
                    return;
                }

                climbSpamPreventionTime = Time.time;

                int xpAward = 15;
                LevelingAPI.AddExperience(xpAward);
                Plugin.Log.LogInfo($"Awarded {xpAward} XP for climbing.");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), nameof(Character.RPCA_Revive))]
        static void Character_RPCA_Revive_Postfix(Character __instance)
        {
            if (__instance.IsLocal)
            {
                int xpAward = 25;
                LevelingAPI.AddExperience(xpAward);
                Plugin.Log.LogInfo($"Awarded {xpAward} XP for being revived.");
            }
            else
            {
                int xpAward = 50;
                LevelingAPI.AddExperience(xpAward);
                Plugin.Log.LogInfo($"Awarded {xpAward} XP for reviving someone.");
            }
        }
    }
}
