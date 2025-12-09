using Photon.Realtime;
using Photon.Pun;
using System.Collections.Generic;
using PhotonPlayer = Photon.Realtime.Player;
using System;
using Leveling.Misc;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Leveling
{
    public static class LevelingAPI
    {
        public static event Action<int> OnLocalPlayerLevelChanged;
        public static event Action<float> OnLocalPlayerExperienceChanged;
        public static event Action<PhotonPlayer, int> OnRemotePlayerLevelChanged;

        private static int _level = 1;
        private static float _experience = 0;

        private static float ExperienceToNextLevel => _level * 100;

        private static readonly Dictionary<PhotonPlayer, int> PlayerLevels = new Dictionary<PhotonPlayer, int>();

        public static int Level
        {
            get => _level;
            private set
            {
                if (_level != value)
                {
                    _level = value;
                    Netcode.Instance?.SendLevelUpdateRPC(value);
                    SaveManager.SaveData(_level, _experience);
                    OnLocalPlayerLevelChanged?.Invoke(value);
                }
            }
        }

        public static float Experience
        {
            get => _experience;
            private set
            {
                _experience = value;
                CheckLevelUp();
                SaveManager.SaveData(_level, _experience);
            }
        }

        private static void CheckLevelUp()
        {
            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                Level++;
                Plugin.Log.LogInfo($"Player Leveled Up! New Level: {Level}");
            }
        }

        private static float CalculateMultiplier()
        {
            var ascent = Ascents.currentAscent;
            var multiplier = 1f;

            if (ascent < 0)
            {
                return multiplier = 0.8f;
            }

            return multiplier = 1 + (ascent * 0.1f);
        }

        /// <summary>
        /// Adds the specified amount of experience points to the local player's current experience total.
        /// The amount is capped between 1 and 2000. This may trigger a level up if enough experience is gained.
        /// </summary>
        /// <param name="amount">The non-negative float value of experience to add.</param>
        /// <param name="applyAscentMultiplier">Optional param that when set to false will not apply a multiplier depending on the ascent.</param>
        public static void AddExperience(float amount, bool applyAscentMultiplier = true)
        {
            if (amount > 0 && amount <= 2000 && SceneManager.GetActiveScene().name != "Airport" && SceneManager.GetActiveScene().name != "Title")
            {
                // Apply the multiplier if applyAscentMultiplier is true, else default to 1f.
                var multiplier = applyAscentMultiplier ? CalculateMultiplier() : 1f;

                amount = amount * multiplier;
                Experience += amount;
                OnLocalPlayerExperienceChanged?.Invoke(amount);
                Plugin.Log.LogInfo($"Gained {amount} XP. Current XP: {Experience}/{ExperienceToNextLevel}");
            }
        }

        /// <summary>
        /// Initializes the local player's Level and Experience fields from loaded save data.
        /// This method should be called once on plugin load.
        /// </summary>
        /// <param name="data">The PlayerSaveData object containing the saved level and experience values.</param>
        public static void LoadLocalPlayerStats(PlayerSaveData data)
        {
            _level = data.Level;
            _experience = data.Experience;
            Plugin.Log.LogInfo($"Local player stats initialized to Lvl: {_level}, Exp: {_experience}");
        }

        /// <summary>
        /// Sets the internal tracked level for a remote network player.
        /// This method is primarily used internally by the Netcode class when receiving RPCs.
        /// </summary>
        /// <param name="player">The remote PhotonPlayer whose level is being updated.</param>
        /// <param name="level">The new level of the remote player.</param>
        public static void SetRemotePlayerLevel(PhotonPlayer player, int level)
        {
            if (PlayerLevels.TryGetValue(player, out int currentLevel) && currentLevel == level)
            {
                return;
            }

            PlayerLevels[player] = level;
            Plugin.Log.LogInfo($"Internal level set for {player.NickName}: Lvl {level}");

            OnRemotePlayerLevelChanged?.Invoke(player, level);
        }


        /// <summary>
        /// Retrieves the current level of a specified player (local or remote).
        /// </summary>
        /// <param name="player">The PhotonPlayer object representing the target player.</param>
        /// <returns>The current level of the player, or 1 if the remote player's level hasn't been synced yet.</returns>
        public static int GetPlayerLevel(PhotonPlayer player)
        {
            if (player.IsLocal)
            {
                return Level;
            }

            if (PlayerLevels.TryGetValue(player, out int level))
            {
                return level;
            }
            return 1;
        }
    }
}