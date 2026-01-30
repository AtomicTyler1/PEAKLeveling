using Photon.Realtime;
using Photon.Pun;
using System.Collections.Generic;
using PhotonPlayer = Photon.Realtime.Player;
using System;
using Leveling.Misc;
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
        private static Dictionary<string, bool> _oneUseItems = new Dictionary<string, bool>();

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
            }
        }

        public static Dictionary<string, bool> OneUseItems
        {
            get => _oneUseItems;
            private set
            {
                _oneUseItems = value;
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

            return multiplier = Math.Clamp((1 + (ascent * 0.1f)), 1, 2);
        }

        public static void SaveLocalData()
        {
            SaveManager.SaveData(_level, _experience, _oneUseItems);
        }

        /// <summary>
        /// Adds the specified amount of experience points to the local player's current experience total.
        /// The amount is capped between 1 and 2000. This may trigger a level up if enough experience is gained.
        /// </summary>
        /// <param name="amount">The non-negative float value of experience to add.</param>
        /// <param name="applyAscentMultiplier">Optional param that when set to false will not apply a multiplier depending on the ascent.</param>
        /// <param name="allowAwardInAirport">Optional param that lets people gain Experience whilst in the airport.</param>
        public static void AddExperience(float amount, bool applyAscentMultiplier = true, bool allowAwardInAirport = false)
        {
            if (amount > 0 && amount <= 2000)
            {
                if (SceneManager.GetActiveScene().name.ToLower().Contains("airport") && !allowAwardInAirport) return;
                if (!SceneManager.GetActiveScene().name.ToLower().Contains("level") && !allowAwardInAirport) return;

                // Apply the multiplier if applyAscentMultiplier is true, else default to 1f.
                var multiplier = applyAscentMultiplier ? CalculateMultiplier() : 1f;

                amount *= multiplier;
                Experience += amount;
                OnLocalPlayerExperienceChanged?.Invoke(amount);
                Plugin.Log.LogInfo($"Gained {amount} XP. Current XP: {Experience}/{ExperienceToNextLevel}");
            }
        }

        /// <summary>
        /// Adds an item to the dictionary of one time use items.
        /// </summary>
        /// <param name="itemName">The item.UIData.itemName that will be checked for one time use.</param>
        public static void AddOneUseItem(string itemName)
        {
            if (string.IsNullOrEmpty(itemName) || _oneUseItems.ContainsKey(itemName) ) { return; }

            _oneUseItems.Add(itemName, false);

            SaveManager.SaveData(_level, _experience, _oneUseItems);
        }

        /// <summary>
        /// Sets an item as used for the One time use system.
        /// </summary>
        /// <param name="itemName">The item.UIData.itemName that will be set for one time use.</param>
        public static void SetOneUseItem(string itemName)
        {
            if (string.IsNullOrEmpty(itemName) || !_oneUseItems.ContainsKey(itemName) || !SceneManager.GetActiveScene().name.ToLower().Contains("level")) { return; }

            _oneUseItems[itemName] = true;

            SaveManager.SaveData(_level, _experience, _oneUseItems);

            Plugin.Log.LogMessage($"Set {itemName} to true for being a one use item.");
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
            _oneUseItems = data.OneUseItems;

            Dictionary<string, bool> defaultItems = SaveManager.GetDefaultOneUseItems();

            foreach (var item in defaultItems)
            {
                if (!_oneUseItems.ContainsKey(item.Key))
                {
                    _oneUseItems.Add(item.Key, item.Value);
                    Plugin.Log.LogMessage($"DefaultItems was missing {item}, this has now been added.");
                }
            }

            SaveManager.SaveData(_level, _experience, _oneUseItems);

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