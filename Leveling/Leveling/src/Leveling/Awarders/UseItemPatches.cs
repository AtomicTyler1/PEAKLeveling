using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace Leveling.Awarders;

internal class TrackedItem
{
    Item item;
    float lastTimeUsed;
}

[HarmonyPatch]
internal class UseItemPatches
{
    private const Rarity FallbackRarity = Rarity.Common;
    private const float CooldownTime = 30f;

    // Names in here will not gain experience when used
    private static List<string> blacklistedItems = new List<string>
    {
        "Passport",
        "Bing Bong",
        "Binoculars",
        "Torn Page",
        "Scroll",
        "Guidebook",
        "Parasol"
    };

    // Names in here will have a 30s cooldown each time they are used between getting experience
    private static List<string> trackedItemNames = new List<string>
    {
        "Faerie Lantern",
        "Lantern",
        "Torch"
    };

    private static readonly Dictionary<string, float> itemCooldowns = new Dictionary<string, float>();

    private static float CalculateExperience(Rarity itemRarity)
    {
        float weight = 100;
        if (LootData.RarityWeights.ContainsKey(itemRarity))
        {
            weight = LootData.RarityWeights[itemRarity];
        }

        return 100 / weight * Mathf.Log10(weight);
    }

    private static bool TryGetItemRarity(GameObject itemObject, out Rarity itemRarity)
    {
        LootData itemData = itemObject.GetComponent<LootData>();
        if (itemData == null)
        {
            // Default to common rarity if none is present
            itemRarity = Rarity.Common;
            return false;
        }

        itemRarity = itemData.Rarity;
        return true;
    }

    [HarmonyPatch(typeof(Item), nameof(Item.FinishCastPrimary))]
    [HarmonyPostfix]
    public static void OnPrimaryUse(Item __instance)
    {
        string itemName = __instance.UIData.itemName;

        if (!__instance.lastHolderCharacter.IsLocal || __instance.OnPrimaryFinishedCast == null || blacklistedItems.Contains(itemName))
        {
            return;
        }

        if (trackedItemNames.Contains(itemName))
        {
            float currentTime = Time.time;
            if (itemCooldowns.TryGetValue(itemName, out float lastTimeUsed))
            {
                if (currentTime < lastTimeUsed + CooldownTime)
                {
                    return;
                }
            }
            itemCooldowns[itemName] = currentTime;
        }

        if (!TryGetItemRarity(__instance.gameObject, out Rarity itemRarity))
        {
            itemRarity = FallbackRarity;
        }

        float usesFactor = 1;
        if (__instance.totalUses > 0)
        {
            usesFactor = __instance.totalUses;
        }

        float expToGive = CalculateExperience(itemRarity) / usesFactor;
        LevelingAPI.AddExperience(expToGive);
    }

    [HarmonyPatch(typeof(Item), nameof(Item.FinishCastSecondary))]
    [HarmonyPostfix]
    public static void OnSecondaryUse(Item __instance)
    {
        string itemName = __instance.UIData.itemName;

        if (!__instance.lastHolderCharacter.IsLocal || __instance.OnSecondaryFinishedCast == null || blacklistedItems.Contains(itemName))
        {
            return;
        }

        if (trackedItemNames.Contains(itemName))
        {
            float currentTime = Time.time;

            if (itemCooldowns.TryGetValue(itemName, out float lastTimeUsed))
            {
                if (currentTime < lastTimeUsed + CooldownTime)
                {
                    return;
                }
            }

            itemCooldowns[itemName] = currentTime;
        }

        Rarity itemRarity = FallbackRarity;
        TryGetItemRarity(__instance.gameObject, out itemRarity); 
        
        float usesFactor = 1;
        if (__instance.totalUses > 0)
        {
            usesFactor = __instance.totalUses;
        }

        float expToGive = CalculateExperience(itemRarity) / usesFactor;
        LevelingAPI.AddExperience(expToGive);
    }
}
