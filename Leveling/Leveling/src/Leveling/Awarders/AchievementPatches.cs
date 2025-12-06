using HarmonyLib;

namespace Leveling.Awarders;

[HarmonyPatch]
internal class AchievementPatches
{
    private const float BadgeExp = 10f;

    [HarmonyPatch(typeof(GlobalEvents), nameof(GlobalEvents.OnAchievementThrown))]
    [HarmonyPostfix]
    static void AwardExperienceForGettingAchievement()
    {
        LevelingAPI.AddExperience(BadgeExp);
    }
}
