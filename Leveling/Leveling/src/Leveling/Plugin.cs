using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Leveling.Misc;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PhotonPlayer = Photon.Realtime.Player;

namespace Leveling
{
    [BepInAutoPlugin]
    public partial class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; } = null!;
        private Harmony harmony = null!;

        public static ConfigEntry<bool> automaticBackups;
        public static ConfigEntry<int> backupsBeforeDeletion;
        public static ConfigEntry<string> backupToLoad;

        public static ConfigEntry<bool> showExperienceGainUI;
        public static ConfigEntry<bool> showLevelGainUI;
        public static ConfigEntry<bool> showPlayerLevels;

        public static float XPGainedThisRun = 0f;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"Plugin {Name} is loaded!");

            showExperienceGainUI = Config.Bind("UI", "Show Experience Gain", true, "When enabled, you will see when you gain XP, usually shown a +5XP");
            showLevelGainUI = Config.Bind("UI", "Show Level Gain", true, "When enabled, you will see when you level up.");

            automaticBackups = Config.Bind("Backups", "Automatic Backups", true, "Creates backups of your save file automatically on game startup");
            backupsBeforeDeletion = Config.Bind("Backups", "Backups Before Deletion", 6, "How many backups can be created before it starts deleting old backups.");

            string[] backupLabels = SaveManager.GetBackupDataForConfig();

            List<string> displayValues = new List<string> { "Current Save" };
            displayValues.AddRange(backupLabels);

            ConfigDescription configDesc = new ConfigDescription(
                "Select a backup to load. The selection will replace your main save file on game start. Leave as 'Current Save' to skip loading a backup. Game restart is required after selection.",
                new AcceptableValueList<string>(displayValues.ToArray())
            );

            backupToLoad = Config.Bind("Backups", "Backup To Load", "Current Save", configDesc);

            if (automaticBackups.Value)
            {
                SaveManager.CreateBackup(backupsBeforeDeletion.Value);
            }

            string labelToLoad = backupToLoad.Value;

            if (!labelToLoad.Equals("Current Save", StringComparison.OrdinalIgnoreCase))
            {
                string filenameToLoad = ParseLabelToFilename(labelToLoad);

                Log.LogWarning($"Attempting to load selected backup (Label: {labelToLoad}, File: {filenameToLoad})");

                if (SaveManager.LoadBackup(filenameToLoad))
                {
                    backupToLoad.Value = "Current Save";
                    Config.Save();
                }
            }

            Netcode.EnsureInitialized();

            PlayerSaveData savedData = SaveManager.LoadData();
            LevelingAPI.LoadLocalPlayerStats(savedData);

            LevelingAPI.OnRemotePlayerLevelChanged += HandleRemotePlayerLevelChange;
            LevelingAPI.OnLocalPlayerExperienceChanged += ExperienceGain;
            LevelingAPI.OnLocalPlayerLevelChanged += LevelGain;

            harmony = new Harmony(Id);
            harmony.PatchAll();
        }

        private void OnApplicationQuit()
        {
            LevelingAPI.SaveLocalData();
        }

        private string ParseLabelToFilename(string label)
        {
            try
            {
                int levelStart = label.IndexOf("Level: ") + "Level: ".Length;
                int levelEnd = label.IndexOf(" || Experience:");

                if (levelEnd == -1)
                    levelEnd = label.IndexOf(" Experience:");

                if (levelStart < "Level: ".Length || levelEnd == -1 || levelEnd <= levelStart)
                {
                    Log.LogError($"Failed to find Level component in label: {label}");
                    return "Current Save";
                }

                string levelStr = label.Substring(levelStart, levelEnd - levelStart).Trim();

                int expStart = label.IndexOf("Experience: ") + "Experience: ".Length;
                string expStr = label.Substring(expStart).Trim();

                if (!float.TryParse(expStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float experience))
                {
                    Log.LogError($"Failed to parse experience string: {expStr}. Defaulting to Current Save.");
                    return "Current Save";
                }

                int expInt = (int)Math.Floor(experience);
                int expFrac = (int)((experience - expInt) * 1000);

                return $"L{levelStr}_E{expInt}_{expFrac:D3}.backup";
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to parse backup label '{label}' into filename. Error: {ex.Message}");
                return "Current Save";
            }
        }

        private void HandleRemotePlayerLevelChange(PhotonPlayer player, int newLevel)
        {
            if (player == null || player.IsLocal) return;

            string baseName = Patches.RemoveLevelTag(player.NickName);
            player.NickName = $"{baseName} [{newLevel}]";
            Log.LogInfo($"Updated remote player name: {player.NickName}");
        }

        private void ExperienceGain(float newExp)
        {
            Patches.UpdateAccoladesText();
            if (!showExperienceGainUI.Value) { return; }
            Log.LogInfo($"Trying to create ui.");
            CreateExperienceGUI(newExp, out GameObject expTextObj, false);
        }

        private void LevelGain(int newLevel)
        {
            Patches.UpdateAccoladesText();
            if (!showLevelGainUI.Value) { return; }
            Log.LogInfo($"Trying to create ui.");
            CreateExperienceGUI(0, out GameObject expTextObj, true);
        }

        private void CreateExperienceGUI(float xpGain, out GameObject expTextObj, bool xpOrLevel)
        {
            var heroTextComponent = GUIManager.instance.heroDayText;

            if (heroTextComponent == null)
            {
                Log.LogError("Could not find HeroDayText! XP UI cannot be created.");
                expTextObj = null;
                return;
            }

            GameObject xpCanvas = new GameObject("XPGainCanvas");
            xpCanvas.transform.SetParent(GUIManager.instance.transform, false);

            Canvas canvas = xpCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            xpCanvas.AddComponent<CanvasScaler>();
            xpCanvas.AddComponent<GraphicRaycaster>();

            expTextObj = new GameObject("ExperienceText");
            expTextObj.transform.SetParent(xpCanvas.transform, false);

            expTextObj.AddComponent<CanvasRenderer>();
            TextMeshProUGUI expText = expTextObj.AddComponent<TextMeshProUGUI>();

            expText.font = heroTextComponent.font;
            expText.color = heroTextComponent.color;
            expText.alignment = TextAlignmentOptions.Center;
            expText.fontSize = heroTextComponent.fontSize / 2.5f;

            expText.outlineWidth = 0.1f;
            expText.outlineColor = heroTextComponent.color - new Color(0.5f, 0.5f, 0.5f, 0f);

            XPAnimator animator = expTextObj.AddComponent<XPAnimator>();
            animator.text = expText;

            if (xpOrLevel)
            {
                expText.text = $"LEVEL UP! LVL {LevelingAPI.Level}";
                animator.isLevelUp = true;
            }
            else
            {
                XPGainedThisRun += xpGain;
                expText.text = $"+{Math.Round(xpGain, 2)} XP";
            }

            RectTransform rect = expTextObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200f, 80f);

            if (xpOrLevel)
            {
                // Check if we are displaying the XP, if not then we might as well center it.
                if (showExperienceGainUI.Value) { rect.anchoredPosition = new Vector2(100f, -10f); }
                else { rect.anchoredPosition = new Vector2(100f, 5f); }
            }
            else
            {
                rect.anchoredPosition = new Vector2(100f, 5f);
            }

            Log.LogInfo($"Created XP Gain GUI: +{xpGain} XP");
        }
    }

    [HarmonyPatch]
    public class Patches
    {
        private static TextMeshProUGUI? localLevelUIText;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerHandler), nameof(PlayerHandler.RegisterCharacter))]
        public static void Character_Reg_Postfix(Character character)
        {
            if (character && character.photonView)
            {
                PhotonPlayer networkPlayer = character.photonView.Owner;
                if (networkPlayer != null && networkPlayer.IsLocal)
                {
                    int playerLevel = LevelingAPI.GetPlayerLevel(networkPlayer);
                    Netcode.Instance?.SendLevelUpdateRPC(playerLevel);
                    Netcode.Instance?.RequestAllPlayerLevels();
                }
            }
        }

        public static string RemoveLevelTag(string nickname)
        {
            int lastBracket = nickname.LastIndexOf('[');

            if (lastBracket > 0 && nickname.EndsWith("]"))
            {
                string potentialTag = nickname.Substring(lastBracket);
                return nickname.Substring(0, lastBracket).Trim();
            }
            return nickname;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PauseMenuAccoladesPage), nameof(PauseMenuAccoladesPage.Start))]
        public static void Accolades_OnPageEnter_Postfix(PauseMenuAccoladesPage __instance)
        {
            GameObject obj = __instance.gameObject;
            Transform existingLevel = obj.transform.Find("Level");

            if (existingLevel != null)
            {
                localLevelUIText = existingLevel.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject peaks = obj.transform.Find("Peaks").gameObject;
                GameObject levelUI = GameObject.Instantiate(peaks, peaks.transform.parent);
                levelUI.name = "Level";
                levelUI.transform.localPosition = peaks.transform.localPosition + new Vector3(0, -35, 0);
                localLevelUIText = levelUI.GetComponent<TextMeshProUGUI>();
            }

            UpdateAccoladesText();
        }

        public static void UpdateAccoladesText()
        {
            if (localLevelUIText != null && localLevelUIText.gameObject != null)
            {
                localLevelUIText.text = $"Level: {LevelingAPI.Level}  XP: {Math.Round(LevelingAPI.Experience, 2)}/{LevelingAPI.Level * 100}";
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIPlayerNames), nameof(UIPlayerNames.Init))]
        public static void UIPlayerNames_Init_Postfix(UIPlayerNames __instance)
        {
            foreach (PlayerName name in __instance.playerNameText)
            {
                if (name.text == null || name.characterInteractable?.character?.photonView == null) continue;

                PhotonPlayer player = name.characterInteractable.character.photonView.Owner;

                if (player == null || player.IsLocal) continue;

                int playerLevel = LevelingAPI.GetPlayerLevel(player);
                string baseName = RemoveLevelTag(player.NickName);
                name.text.text = $"{baseName} [{playerLevel}]";
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RunManager), nameof(RunManager.StartRun))]
        public static void RunManager_StartRun_Postfix()
        {
            Plugin.XPGainedThisRun = 0f;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EndScreen), nameof(EndScreen.Start))]
        public static void EndScreen_Start_Postfix(EndScreen __instance)
        {
            Transform badges = __instance.transform.Find("Panel/Margin/Layout/Window_BADGES/Title (1)");
            TextMeshProUGUI tmp = badges.gameObject.GetComponent<TextMeshProUGUI>();

            if (tmp.text.Contains("(XP GAINED:")) { return; }

            tmp.text = $"{tmp.text} (XP GAINED: +{Plugin.XPGainedThisRun})";
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoardingPass), nameof(BoardingPass.UpdateAscent))]
        public static void BoardingPass_UpdateAscent_Postfix(BoardingPass __instance)
        {
            var ascent = __instance._ascentIndex;
            var multiplier = 1f;

            if (ascent < 0)
            {
                multiplier = 0.8f;
            }
            else
            {
                multiplier = 1 + (ascent * 0.1f);
            }

            GameObject ascent_title = __instance.transform.Find("BoardingPass/Panel/Ascent/Title").gameObject;
            TextMeshProUGUI tmp = ascent_title.GetComponent<TextMeshProUGUI>();

            tmp.text = $"{tmp.text} (XP: {multiplier}X)";
        }
    }
}