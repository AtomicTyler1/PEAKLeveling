using HarmonyLib;

namespace Leveling.Awarders;

[HarmonyPatch]
internal class AchievementPatches
{
    private const float BadgeExp = 10f;

    static bool IsAscentAchievement(ACHIEVEMENTTYPE type)
    {
        return type >= ACHIEVEMENTTYPE.Ascent1 && type <= ACHIEVEMENTTYPE.Ascent7;
    }

    [HarmonyPatch(typeof(AchievementManager), nameof(AchievementManager.ThrowAchievement))]
    [HarmonyPrefix]
    static void AddRepeatedAchievements(AchievementManager __instance, ACHIEVEMENTTYPE type)
    {
        if (__instance.runBasedValueData.steamAchievementsPreviouslyUnlocked.Contains(type) || IsAscentAchievement(type))
        {
            return;
        }

        LevelingAPI.AddExperience(BadgeExp);
    }
}
