using DG.Tweening.Core;
using HarmonyLib;
using UnityEngine;

namespace Leveling.Awarders;

[HarmonyPatch]
class LuaggagePatches
{
    private const float OpenLuggageExp = 15f;
    private const float MinimumDistanceFromLuggage = 7f;

    private static float LocalCharacterDistanceFrom(Vector3 position)
    {
        return Vector3.Distance(position, Character.localCharacter.Center) * CharacterStats.unitsToMeters;
    }

    [HarmonyPatch(typeof(GlobalEvents), nameof(GlobalEvents.TriggerLuggageOpened))]
    [HarmonyPostfix]
    public static void IncrementOpenedLuggages(Luggage luggage, Character character)
    {
        if (LocalCharacterDistanceFrom(luggage.transform.position) > MinimumDistanceFromLuggage)
        {
            return;
        }

        switch (luggage.displayName)
        {
            case "Ancient Luggage":
                Plugin.IncreaseXPSource(Plugin.XPSource.Luggages, OpenLuggageExp + 20);
                LevelingAPI.AddExperience(OpenLuggageExp + 20);
                return;
            case "Explorer's Luggage":
                Plugin.IncreaseXPSource(Plugin.XPSource.Luggages, OpenLuggageExp + 10);
                LevelingAPI.AddExperience(OpenLuggageExp + 10);
                return;
            case "Big Luggage":
                Plugin.IncreaseXPSource(Plugin.XPSource.Luggages, OpenLuggageExp + 5);
                LevelingAPI.AddExperience(OpenLuggageExp + 5);
                return;
            default:
                Plugin.IncreaseXPSource(Plugin.XPSource.Luggages, OpenLuggageExp);
                LevelingAPI.AddExperience(OpenLuggageExp);
                return;
        }
    }
}
