using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace Leveling.Misc
{
    public class PlayerSaveData
    {
        public int Level { get; set; } = 1;
        public float Experience { get; set; } = 0;
        public Dictionary<string, bool> OneUseItems { get; set; } = new Dictionary<string, bool>()
        {
            { "BUGLEBBNO", false }
        };
    }

    public static class SaveManager
    {

        // This system is setup to do the following, please do not change the functionality of this code unless you intend to break save compatibility or specifically put systems in place to fix it:
        // This is built to make it more difficult for users to manually edit their save files to cheat levels/experience without the use of external tools.
        // External tools include another mod, manually decoding, editing, and re-encoding the save file, and UnityExplorer.

        private static readonly byte[] MagicHeader = new byte[] { 0xFE, 0xCA, 0xDE, 0xAF, 0x01 };
        private static readonly byte[] MagicFooter = new byte[] { 0x02, 0xEF, 0xCD, 0xBA, 0xFD };

        public static Dictionary<string, bool> GetDefaultOneUseItems()
        {
            return new Dictionary<string, bool>(new PlayerSaveData().OneUseItems);
        }

        private static string SaveFilePath
        {
            get
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dirPath = Path.Combine(appDataPath, "LandCrab", "PEAK", "PEAKLeveling");

                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                return Path.Combine(dirPath, "player_stats.sav");
            }
        }

        public static void SaveData(int level, float experience, Dictionary<string, bool> oneUseItems)
        {
            try
            {
                var data = new PlayerSaveData { Level = level, Experience = experience, OneUseItems = oneUseItems};

                string json = JsonConvert.SerializeObject(data);

                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

                byte[] finalBytes = new byte[MagicHeader.Length + jsonBytes.Length + MagicFooter.Length];

                Buffer.BlockCopy(MagicHeader, 0, finalBytes, 0, MagicHeader.Length);
                Buffer.BlockCopy(jsonBytes, 0, finalBytes, MagicHeader.Length, jsonBytes.Length);
                Buffer.BlockCopy(MagicFooter, 0, finalBytes, MagicHeader.Length + jsonBytes.Length, MagicFooter.Length);

                string encodedData = Convert.ToBase64String(finalBytes);

                File.WriteAllText(SaveFilePath, encodedData);
                Plugin.Log.LogInfo($"Saved data (obfuscated) to: {SaveFilePath}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to save data: {ex.Message}");
            }
        }

        public static PlayerSaveData LoadData()
        {
            if (!File.Exists(SaveFilePath))
            {
                Plugin.Log.LogInfo("Save file not found. Loading default new game data (Lvl 1, Exp 0).");
                return new PlayerSaveData();
            }

            try
            {
                string encodedData = File.ReadAllText(SaveFilePath);

                byte[] finalBytes = Convert.FromBase64String(encodedData);

                int requiredLength = MagicHeader.Length + MagicFooter.Length;

                if (finalBytes.Length < requiredLength)
                {
                    throw new InvalidDataException("Save file too short after decoding. File is corrupt or invalid.");
                }

                for (int i = 0; i < MagicHeader.Length; i++)
                {
                    if (finalBytes[i] != MagicHeader[i])
                    {
                        throw new InvalidDataException("Magic Header mismatch. File is corrupt or modified.");
                    }
                }

                int footerStartIndex = finalBytes.Length - MagicFooter.Length;

                for (int i = 0; i < MagicFooter.Length; i++)
                {
                    if (finalBytes[footerStartIndex + i] != MagicFooter[i])
                    {
                        throw new InvalidDataException("Magic Footer mismatch. File is corrupt or modified.");
                    }
                }

                int jsonLength = finalBytes.Length - requiredLength;
                byte[] jsonBytes = new byte[jsonLength];

                Buffer.BlockCopy(finalBytes, MagicHeader.Length, jsonBytes, 0, jsonLength);

                string json = Encoding.UTF8.GetString(jsonBytes);

                PlayerSaveData loadedData = JsonConvert.DeserializeObject<PlayerSaveData>(json);
                Plugin.Log.LogInfo($"Loaded data (obfuscated) from: {SaveFilePath} (Lvl {loadedData.Level}, Exp {loadedData.Experience})");
                return loadedData;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error loading or deserializing save file. Possible corruption/tampering. Loading default new game data. Error: {ex.Message}");
                return new PlayerSaveData();
            }
        }
    }
}