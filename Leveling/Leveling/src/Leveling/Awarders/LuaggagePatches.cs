using HarmonyLib;
using UnityEngine;

namespace Leveling.Awarders;

[HarmonyPatch]
class LuaggagePatches
{
    private const float OpenLuggageExp = 15f;
    private const float MinimumDistanceFromLuggage = 5f;

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

        LevelingAPI.AddExperience(OpenLuggageExp);
    }
}
