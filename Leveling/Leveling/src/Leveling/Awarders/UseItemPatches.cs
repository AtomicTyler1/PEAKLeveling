using HarmonyLib;
using UnityEngine;

namespace Leveling.Awarders;

[HarmonyPatch]
internal class UseItemPatches
{
    private const Rarity FallbackRarity = Rarity.Common;

    private static float CalculateExperience(Rarity itemRarity)
    {
        float weight = LootData.RarityWeights[itemRarity];

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
        if (!__instance.lastHolderCharacter.IsLocal || __instance.OnPrimaryFinishedCast == null)
        {
            return;
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
        if (!__instance.lastHolderCharacter.IsLocal || __instance.OnSecondaryFinishedCast == null)
        {
            return;
        }

        if (TryGetItemRarity(__instance.gameObject, out Rarity itemRarity))
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
}
