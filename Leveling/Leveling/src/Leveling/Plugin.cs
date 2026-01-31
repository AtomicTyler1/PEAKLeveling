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
using UnityEngine.SceneManagement;
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
        public static ConfigEntry<bool> showAscentMultiplier;
        public static ConfigEntry<bool> showLevelingUsersOnly;

        public static float XPGainedThisRun = 0f;
        
        public static float XPGained_Climbing = 0f;
        public static float XPGained_Items = 0f;
        public static float XPGained_Winning = 0f;
        public static float XPGained_Badges = 0f;
        public static float XPGained_Luggages = 0f;
        public static float XPGained_Mods = 0f;
        public static float XPGained_Other = 0f;

        public enum XPSource
        {
            Winning,
            Luggages,
            Badges,
            Items,
            Climbing,
            Other
        }

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"Plugin {Name} is loaded!");

            showExperienceGainUI = Config.Bind("UI", "Show Experience Gain", true, "When enabled, you will see when you gain XP, usually shown a +5XP");
            showLevelGainUI = Config.Bind("UI", "Show Level Gain", true, "When enabled, you will see when you level up.");
            showAscentMultiplier = Config.Bind("UI", "Show Ascent Multiplier", true, "When enabled, the ascent multiplier will be shown on the boarding pass screen. Turn it off if incompatible with localization or other mods change it.");
            showLevelingUsersOnly = Config.Bind("UI", "Show Leveling Users Only", false, "When enabled, only players using the leveling mod will have their levels shown, if off then everyone without the mod will have [1]. REQUIRES REJOIN");

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

        public static void IncreaseXPSource(XPSource source, float amount, bool ascentMultiplier = true)
        {
            if (amount > 0 && amount <= 2000)
            {
                if (SceneManager.GetActiveScene().name.ToLower().Contains("airport")) return;
                if (!SceneManager.GetActiveScene().name.ToLower().Contains("level")) return;

                var multiplier = ascentMultiplier ? CalculateMultiplier() : 1f;
                amount *= multiplier;

                if (source == XPSource.Winning)
                {
                    XPGained_Winning += amount;
                }
                else if (source == XPSource.Luggages)
                {
                    XPGained_Luggages += amount;
                }
                else if (source == XPSource.Badges)
                {
                    XPGained_Badges += amount;
                }
                else if (source == XPSource.Items)
                {
                    XPGained_Items += amount;
                }
                else if (source == XPSource.Climbing)
                {
                    XPGained_Climbing += amount;
                }
                else if (source == XPSource.Other)
                {
                    XPGained_Other += amount;
                }
            }
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
            Patches.UpdateAudioSliderText(player, newLevel);
            Patches.UpdatePlayerNameText(player, newLevel);
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
        private static Dictionary<int, PlayerName> playerNames = new Dictionary<int, PlayerName>();
        private static Dictionary<int, TextMeshProUGUI> audioLevelSliderNames = new Dictionary<int, TextMeshProUGUI>();

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

        public static void UpdateAudioSliderText(PhotonPlayer player, int level)
        {
            if (audioLevelSliderNames != null && audioLevelSliderNames.ContainsKey(player.ActorNumber))
            {
                TextMeshProUGUI tmp = audioLevelSliderNames[player.ActorNumber];
                string baseName = RemoveLevelTag(player.NickName);
                tmp.text = $"{baseName} [{level}]";
            }
            else
            {
                Plugin.Log.LogWarning($"Could not find AudioLevelSlider Text for player {player.NickName} to update level display.");
            }
        }

        public static void UpdatePlayerNameText(PhotonPlayer player, int level)
        {
            if (playerNames.TryGetValue(player.ActorNumber, out PlayerName name) && name != null && name.text != null)
            {
                string baseName = RemoveLevelTag(player.NickName);
                name.text.text = $"{baseName} [{level}]";
                return;
            }

            UIPlayerNames uiNames = GameObject.FindFirstObjectByType<UIPlayerNames>();
            if (uiNames != null)
            {
                foreach (PlayerName playerNameComp in uiNames.playerNameText)
                {
                    if (playerNameComp.text == null || playerNameComp.characterInteractable?.character?.photonView == null) continue;

                    PhotonPlayer owner = playerNameComp.characterInteractable.character.photonView.Owner;
                    playerNames[owner.ActorNumber] = playerNameComp;
                    if (owner == player)
                    {
                        string baseName = RemoveLevelTag(player.NickName);
                        playerNameComp.text.text = $"{baseName} [{level}]";
                    }
                }
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

                if (!playerNames.ContainsKey(player.ActorNumber))
                {
                    playerNames.Add(player.ActorNumber, name);
                }
                else
                {
                    playerNames[player.ActorNumber] = name;
                }

                if (!Plugin.showLevelingUsersOnly.Value)
                {
                    int playerLevel = LevelingAPI.GetPlayerLevel(player);
                    string baseName = RemoveLevelTag(player.NickName);
                    name.text.text = $"{baseName} [{playerLevel}]";
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RunManager), nameof(RunManager.StartRun))]
        public static void RunManager_StartRun_Postfix()
        {
            audioLevelSliderNames.Clear();
            playerNames.Clear();
            Plugin.XPGainedThisRun = 0f;
            Plugin.XPGained_Winning = 0f;
            Plugin.XPGained_Luggages = 0f;
            Plugin.XPGained_Badges = 0f;
            Plugin.XPGained_Mods = 0f;
            Plugin.XPGained_Items = 0f;
            Plugin.XPGained_Climbing = 0f;
            Plugin.XPGained_Other = 0f;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EndScreen), nameof(EndScreen.Start))]
        public static void EndScreen_Start_Postfix(EndScreen __instance)
        {
            Transform badges = __instance.transform.Find("Panel/Margin/Layout/Window_BADGES/Title (1)");
            TextMeshProUGUI tmp = badges.gameObject.GetComponent<TextMeshProUGUI>();

            if (tmp.text.Contains("(+")) { return; }

            tmp.text = $"{tmp.text} (LEVEL {LevelingAPI.Level})";

            Plugin.XPGained_Mods = Plugin.XPGainedThisRun - Plugin.XPGained_Winning 
                - Plugin.XPGained_Luggages - Plugin.XPGained_Badges - Plugin.XPGained_Items
                - Plugin.XPGained_Climbing - Plugin.XPGained_Other;

            var obj = __instance.transform.Find("Panel/Margin");
            var mtn = obj.Find("Mtn.");

            Color32 color = __instance.transform.Find("Panel/BG").GetComponent<Image>().color;

            var icon = GameObject.Instantiate(mtn.gameObject, obj);
            icon.transform.localPosition += new Vector3(-310f, 0f, 0f);
            icon.GetComponent<Image>().color = color;
            icon.name = "Leveling_Icon";

            var SCOUTING_REPORT = __instance.transform.Find("Panel/Margin/SCOUTING_REPORT");
            var levelingTitle = GameObject.Instantiate(SCOUTING_REPORT.gameObject, obj);
            levelingTitle.transform.localPosition = icon.transform.localPosition + new Vector3(0f, -30f, 0f);
            GameObject.Destroy(levelingTitle.GetComponent<LocalizedText>());
            levelingTitle.GetComponent<TextMeshProUGUI>().text = "LEVELING";
            levelingTitle.GetComponent<TextMeshProUGUI>().color = color;
            levelingTitle.name = "Leveling_Title";

            var ascent = Ascents.currentAscent;
            var multiplier = 1f;

            if (ascent < 0)
            {
                multiplier = 0.8f;
            }
            else
            {
                multiplier = 1 + (ascent * 0.1f);
            }

            CreateEndScreenSection(__instance, "Winning", Plugin.XPGained_Winning);
            CreateEndScreenSection(__instance, "Luggages", Plugin.XPGained_Luggages);
            CreateEndScreenSection(__instance, "Badges", Plugin.XPGained_Badges);
            CreateEndScreenSection(__instance, "Items", Plugin.XPGained_Items);
            CreateEndScreenSection(__instance, "Climbing", Plugin.XPGained_Climbing);
            CreateEndScreenSection(__instance, "Other", Plugin.XPGained_Other);
            CreateEndScreenSection(__instance, "Mods", Plugin.XPGained_Mods);
            CreateEndScreenSection(__instance, "Ascent", multiplier);
            CreateEndScreenSection(__instance, "Total", Plugin.XPGainedThisRun);
        }

        private static void CreateEndScreenSection(EndScreen __instance, string name, float value)
        {
            if (value <= 0) { return; }

            var margin = __instance.transform.Find("Panel/Margin");
            var titleRef = margin.Find("Leveling_Title");
            if (titleRef == null) return;

            float lineSpacing = 24f;
            float rowWidth = 180f;
            float entryFontSize = 15f;
            int index = CountExistingEntries(__instance);

            Color32 themeColor = titleRef.GetComponent<TextMeshProUGUI>().color;

            GameObject entryGroup = new GameObject($"LevelingEntry_{name}");
            entryGroup.transform.SetParent(margin, false);

            float yPos = -20f - (index * lineSpacing);
            entryGroup.transform.localPosition = titleRef.transform.localPosition + new Vector3(0f, yPos, 0f);

            GameObject lineObj = new GameObject("Separator");
            lineObj.transform.SetParent(entryGroup.transform, false);
            var lineImg = lineObj.AddComponent<Image>();
            lineImg.color = themeColor;

            RectTransform lineRect = lineObj.GetComponent<RectTransform>();
            lineRect.sizeDelta = new Vector2(rowWidth, 2f);
            lineRect.localPosition = new Vector3(0, 10f, 0);

            Action<GameObject, string, TextAlignmentOptions, float> SetupText = (obj, txt, align, xPos) => {
                if (obj.GetComponent<LocalizedText>()) GameObject.Destroy(obj.GetComponent<LocalizedText>());

                var tmp = obj.GetComponent<TextMeshProUGUI>();
                tmp.text = txt;
                tmp.fontSize = entryFontSize;
                tmp.alignment = align;
                tmp.color = themeColor;
                tmp.enableAutoSizing = false;

                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(rowWidth / 2f, 20f);
                obj.transform.localPosition = new Vector3(xPos, 0f, 0f);
            };

            var labelObj = GameObject.Instantiate(titleRef.gameObject, entryGroup.transform);
            SetupText(labelObj, name.ToUpper(), TextAlignmentOptions.Left, -(rowWidth / 2f) + (rowWidth / 4f));
            var amountObj = GameObject.Instantiate(titleRef.gameObject, entryGroup.transform);

            if (name == "Ascent")
            {
                SetupText(amountObj, $"{Math.Round(value, 2)}X", TextAlignmentOptions.Right, (rowWidth / 2f) - (rowWidth / 4f));
                return;
            }

            SetupText(amountObj, $"+{Math.Round(value, 3)}XP", TextAlignmentOptions.Right, (rowWidth / 2f) - (rowWidth / 4f));
        }

        private static int CountExistingEntries(EndScreen __instance)
        {
            var margin = __instance.transform.Find("Panel/Margin");
            int count = 0;
            foreach (Transform child in margin)
            {
                if (child.name.StartsWith("LevelingEntry_"))
                {
                    count++;
                }
            }
            return count;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoardingPass), nameof(BoardingPass.UpdateAscent))]
        public static void BoardingPass_UpdateAscent_Postfix(BoardingPass __instance)
        {
            if (!Plugin.showAscentMultiplier.Value) { return; }

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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AudioLevelSlider), nameof(AudioLevelSlider.Awake))]
        public static void AudioLevelSlider_ShowLevel(AudioLevelSlider __instance)
        {
            PhotonPlayer photonPlayer = __instance.player;
            int level = LevelingAPI.GetPlayerLevel(photonPlayer);

            GameObject name = __instance.transform.Find("Name").gameObject;
            TextMeshProUGUI tmp = name.GetComponent<TextMeshProUGUI>();

            if (!audioLevelSliderNames.ContainsKey(photonPlayer.ActorNumber))
            {
                audioLevelSliderNames.Add(photonPlayer.ActorNumber, tmp);
            }
            else
            {
                audioLevelSliderNames[photonPlayer.ActorNumber] = tmp;
            }
        }
    }
}