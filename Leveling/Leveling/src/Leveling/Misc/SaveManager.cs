using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private const string SAVE_FILE_NAME = "player_stats.sav";
        private const string BACKUP_FILE_EXTENSION = ".backup";
        private static readonly byte[] MagicHeader = new byte[] { 0xFE, 0xCA, 0xDE, 0xAF, 0x01 };
        private static readonly byte[] MagicFooter = new byte[] { 0x02, 0xEF, 0xCD, 0xBA, 0xFD };

        public static Dictionary<string, bool> GetDefaultOneUseItems()
        {
            return new Dictionary<string, bool>(new PlayerSaveData().OneUseItems);
        }

        private static string BaseDirPath
        {
            get
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dirPath = Path.Combine(appDataPath, "LandCrab", "PEAK", "PEAKLeveling");

                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                return dirPath;
            }
        }

        private static string SaveFilePath => Path.Combine(BaseDirPath, SAVE_FILE_NAME);

        private static string GetSaveFilePath(string fileName) => Path.Combine(BaseDirPath, fileName);

        public static void SaveData(int level, float experience, Dictionary<string, bool> oneUseItems)
        {
            try
            {
                var data = new PlayerSaveData { Level = level, Experience = experience, OneUseItems = oneUseItems };

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

        private static string GetBackupFileName(PlayerSaveData data)
        {
            int expInt = (int)Math.Floor(data.Experience);
            int expFrac = (int)((data.Experience - expInt) * 1000);

            return $"L{data.Level}_E{expInt}_{expFrac:D3}{BACKUP_FILE_EXTENSION}";
        }

        private static string GetBackupFileBasePattern(PlayerSaveData data)
        {
            int expInt = (int)Math.Floor(data.Experience);
            return $"L{data.Level}_E{expInt}_";
        }

        private static PlayerSaveData DecodeBackup(string filePath)
        {
            try
            {
                string encodedData = File.ReadAllText(filePath);
                byte[] finalBytes = Convert.FromBase64String(encodedData);

                int requiredLength = MagicHeader.Length + MagicFooter.Length;

                if (finalBytes.Length < requiredLength) throw new InvalidDataException("Backup too short.");

                for (int i = 0; i < MagicHeader.Length; i++)
                {
                    if (finalBytes[i] != MagicHeader[i]) throw new InvalidDataException("Backup Header mismatch.");
                }

                int footerStartIndex = finalBytes.Length - MagicFooter.Length;
                for (int i = 0; i < MagicFooter.Length; i++)
                {
                    if (finalBytes[footerStartIndex + i] != MagicFooter[i]) throw new InvalidDataException("Backup Footer mismatch.");
                }

                int jsonLength = finalBytes.Length - requiredLength;
                byte[] jsonBytes = new byte[jsonLength];

                Buffer.BlockCopy(finalBytes, MagicHeader.Length, jsonBytes, 0, jsonLength);
                string json = Encoding.UTF8.GetString(jsonBytes);

                return JsonConvert.DeserializeObject<PlayerSaveData>(json);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Could not decode backup file: {Path.GetFileName(filePath)}. Error: {ex.Message}");
                return null;
            }
        }

        public static void CreateBackup(int maxBackups)
        {
            if (!File.Exists(SaveFilePath))
            {
                Plugin.Log.LogInfo("Cannot create backup: No main save file exists yet.");
                return;
            }

            PlayerSaveData currentData = LoadData();
            if (currentData == null)
            {
                Plugin.Log.LogError("Failed to load current save data for backup check.");
                return;
            }

            string targetBackupFileName = GetBackupFileName(currentData);
            string baseMatchPattern = GetBackupFileBasePattern(currentData);

            var existingBackups = GetBackupFilesInternal().ToList();
            var backupsToExamine = existingBackups
                .Where(f => f.Name.StartsWith(baseMatchPattern, StringComparison.OrdinalIgnoreCase))
                .ToList();

            bool shouldSkipCreation = false;

            foreach (var fileInfo in backupsToExamine)
            {
                PlayerSaveData existingData = DecodeBackup(fileInfo.FullName);
                if (existingData != null)
                {
                    bool levelsMatch = currentData.Level == existingData.Level;
                    bool expMatches = currentData.Experience == existingData.Experience;

                    bool itemsMatch = currentData.OneUseItems.Count == existingData.OneUseItems.Count &&
                                      currentData.OneUseItems.All(pair => existingData.OneUseItems.ContainsKey(pair.Key) && existingData.OneUseItems[pair.Key] == pair.Value);

                    if (levelsMatch && expMatches && itemsMatch)
                    {
                        Plugin.Log.LogInfo("Skipping backup: Current save data is identical to an existing backup.");
                        shouldSkipCreation = true;
                        break;
                    }
                }
            }

            if (shouldSkipCreation) return;

            foreach (var fileInfo in backupsToExamine)
            {
                try
                {
                    File.Delete(fileInfo.FullName);
                    Plugin.Log.LogInfo($"Deleted old backup for replacement: {fileInfo.Name}");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"Failed to delete old backup {fileInfo.Name} for replacement: {ex.Message}");
                }
            }

            string newBackupPath = Path.Combine(BaseDirPath, targetBackupFileName);

            try
            {
                File.Copy(SaveFilePath, newBackupPath, true);
                Plugin.Log.LogInfo($"Created new backup: {targetBackupFileName}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to create backup: {ex.Message}");
            }

            CleanupOldBackups(maxBackups);
        }

        private static void CleanupOldBackups(int maxBackups)
        {
            if (maxBackups <= 0) return;

            var existingBackups = GetBackupFilesInternal();

            var backupsToDelete = existingBackups
                .OrderBy(f => f.CreationTime)
                .Skip(maxBackups)
                .ToList();

            foreach (var file in backupsToDelete)
            {
                try
                {
                    File.Delete(file.FullName);
                    Plugin.Log.LogInfo($"Deleted old backup: {file.Name}");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"Failed to delete old backup {file.Name}: {ex.Message}");
                }
            }
        }

        private static IEnumerable<FileInfo> GetBackupFilesInternal()
        {
            var dir = new DirectoryInfo(BaseDirPath);
            return dir.GetFiles($"*{BACKUP_FILE_EXTENSION}")
                      .OrderByDescending(f => f.CreationTime);
        }

        public static string[] GetBackupDataForConfig()
        {
            var backupLabels = new List<string>();
            var backupFiles = GetBackupFilesInternal().ToList();

            if (!backupFiles.Any()) return backupLabels.ToArray();

            int backupIndex = 1;

            foreach (var fileInfo in backupFiles)
            {
                PlayerSaveData data = DecodeBackup(fileInfo.FullName);
                if (data != null)
                {
                    string baseLabel = $"Level: {data.Level} || Experience: {Math.Round(data.Experience, 3)}";

                    string indexedLabel = $"{backupIndex} backup{(backupIndex > 1 ? "s" : "")} ago || {baseLabel}";

                    backupLabels.Add(indexedLabel);
                    backupIndex++;
                }
            }

            return backupLabels.ToArray();
        }

        public static bool LoadBackup(string backupFileName)
        {
            if (backupFileName.Equals("Current Save", StringComparison.OrdinalIgnoreCase))
            {
                Plugin.Log.LogInfo("Attempted to load 'Current Save' backup. No action taken.");
                return true;
            }

            string backupPath = GetSaveFilePath(backupFileName);

            if (!File.Exists(backupPath))
            {
                Plugin.Log.LogError($"Backup file not found at path: {backupPath}");
                return false;
            }

            try
            {
                File.Copy(backupPath, SaveFilePath, true);
                Plugin.Log.LogInfo($"Successfully loaded backup '{backupFileName}' over main save file.");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to copy backup file: {ex.Message}");
                return false;
            }
        }
    }
}