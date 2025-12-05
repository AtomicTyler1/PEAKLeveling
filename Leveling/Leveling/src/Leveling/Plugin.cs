using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Peak.Network;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using PhotonPlayer = Photon.Realtime.Player;
using TMPro;
using System.Collections.Generic;
using pworld.Scripts;
using UnityEngine.UI;
using Leveling.Misc;

namespace Leveling
{
    [BepInAutoPlugin]
    public partial class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; } = null!;
        private Harmony harmony = null!;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"Plugin {Name} is loaded!");

            Netcode.EnsureInitialized();

            PlayerSaveData savedData = SaveManager.LoadData();
            LevelingAPI.LoadLocalPlayerStats(savedData);

            LevelingAPI.OnRemotePlayerLevelChanged += HandleRemotePlayerLevelChange;
            LevelingAPI.OnLocalPlayerExperienceChanged += ExperienceGain;
            LevelingAPI.OnLocalPlayerLevelChanged += LevelGain;

            harmony = new Harmony(Id);
            harmony.PatchAll();
        }

        private void FixedUpdate()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                LevelingAPI.AddExperience(50);
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
            Log.LogInfo($"Trying to create ui.");
            CreateExperienceGUI(newExp, out GameObject expTextObj, false);
        }

        private void LevelGain(int newLevel)
        {
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

            XPAnimator animator = expTextObj.AddComponent<XPAnimator>();
            animator.text = expText;

            if (xpOrLevel)
            {
                expText.text = $"LEVEL UP! LVL {LevelingAPI.Level}";
                animator.isLevelUp = true;
            }
            else
            {
                expText.text = $"+{xpGain} XP";
            }

            RectTransform rect = expTextObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200f, 80f);

            if (xpOrLevel)
            {
                rect.anchoredPosition = new Vector2(100f, -10f);
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
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerHandler), nameof(PlayerHandler.RegisterCharacter))]
        public static void Character_Reg_Postfix(Character character)
        {
            if (character && character.photonView)
            {
                PhotonPlayer networkPlayer = character.photonView.Owner;

                if (networkPlayer != null)
                {
                    int playerLevel = LevelingAPI.GetPlayerLevel(networkPlayer);

                    string baseName = RemoveLevelTag(networkPlayer.NickName);

                    if (networkPlayer.IsLocal)
                    {
                        Netcode.Instance?.SendLevelUpdateRPC(playerLevel);
                        Netcode.Instance?.RequestAllPlayerLevels();

                        foreach (PhotonPlayer player in PhotonNetwork.PlayerList)
                        {
                            if (player.IsLocal) continue;

                            int remotePlayerLevel = LevelingAPI.GetPlayerLevel(player);
                            string remoteBaseName = RemoveLevelTag(player.NickName);
                            player.NickName = $"{remoteBaseName} [{remotePlayerLevel}]";
                        }

                        Plugin.Log.LogInfo($"Character Registered (Local): {networkPlayer.NickName} is Lvl {playerLevel}. Broadcasting and Requesting Sync.");
                        return;
                    }
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

            if (obj.transform.Find("Level"))
            {
                obj.transform.Find("Level").GetComponent<TextMeshProUGUI>().text = $"Level: {LevelingAPI.Level}  XP: {LevelingAPI.Experience}/{LevelingAPI.Level * 100}";
                return;
            }

            GameObject peaks = obj.transform.Find("Peaks").gameObject;

            GameObject levelUI = GameObject.Instantiate(peaks, peaks.transform.parent);
            levelUI.name = "Level";
            levelUI.transform.localPosition = peaks.transform.localPosition + new Vector3(0, -35, 0);
            levelUI.GetComponent<TextMeshProUGUI>().text = $"Level: {LevelingAPI.Level}  XP: {LevelingAPI.Experience}/{LevelingAPI.Level * 100}";

            LevelingAPI.OnLocalPlayerExperienceChanged += (newXP) =>
            {
                levelUI.GetComponent<TextMeshProUGUI>().text = $"Level: {LevelingAPI.Level}  XP: {LevelingAPI.Experience}/{LevelingAPI.Level * 100}";
            };
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIPlayerNames), nameof(UIPlayerNames.Init))]
        public static void UIPlayerNames_Init_Postfix(UIPlayerNames __instance)
        {
            foreach (PlayerName name in __instance.playerNameText)
            {
                var text = name.text;

                if (text != null &&
                    name.characterInteractable != null &&
                    name.characterInteractable.character != null &&
                    name.characterInteractable.character.photonView != null)
                {
                    PhotonPlayer player = name.characterInteractable.character.photonView.Owner;

                    int playerLevel = LevelingAPI.GetPlayerLevel(player);
                    string baseName = RemoveLevelTag(player.NickName);

                    text.text = $"{baseName} [{playerLevel}]";

                    LevelingAPI.OnRemotePlayerLevelChanged += (changedPlayer, newLevel) =>
                    {
                        if (text == null) return;

                        if (changedPlayer == player)
                        {
                            string updatedBaseName = RemoveLevelTag(changedPlayer.NickName);
                            text.text = $"{updatedBaseName} [{newLevel}]";
                        }
                    };
                }
            }
        }
    }
}