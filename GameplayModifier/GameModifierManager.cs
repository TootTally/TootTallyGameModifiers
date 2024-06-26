﻿using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TootTallyCore;
using TootTallyCore.Graphics;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Utils.Assets;
using TootTallyCore.Utils.TootTallyNotifs;
using UnityEngine;
using UnityEngine.UI;

namespace TootTallyGameModifiers
{
    public static class GameModifierManager
    {
        private static bool _isInitialized;
        private static Dictionary<GameModifiers.ModifierType, GameModifierBase> _gameModifierDict;
        private static List<GameModifierBase> _modifierTypesToRemove;
        private static Dictionary<string, GameModifiers.ModifierType> _stringModifierDict;
        private static string _modifiersBackup;

        private static GameObject _modifierPanel, _modifierPanelContainer;
        private static GameObject _showModifierPanelButton, _hideModifierPanelButton;
        private static Dictionary<GameModifiers.ModifierType, GameObject> _modifierButtonDict;

        private static TootTallyAnimation _openAnimation, _closeAnimation;
        private static bool _canClickButtons;

        [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
        [HarmonyPostfix]
        public static void OnHomeControllerStartInitialize()
        {
            if (!_isInitialized) Initialize(); 
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.Start))]
        [HarmonyPostfix]
        static void OnLevelSelectControllerStartPostfix(LevelSelectController __instance)
        {
            _modifierButtonDict.Clear();

            _showModifierPanelButton = GameObjectFactory.CreateModifierButton(__instance.fullpanel.transform, AssetManager.GetSprite("ModifierButton.png"), "OpenModifierPanelButton", "", false, ShowModifierPanel);
            _showModifierPanelButton.transform.localScale = Vector2.one;
            _showModifierPanelButton.GetComponent<RectTransform>().pivot = Vector2.one / 2f;
            _showModifierPanelButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(365, -160);

            //good fuck yeah we love that
            GameObject camerapopups = GameObject.Find("Camera-Popups").gameObject;
            GameObject panelBody = camerapopups.transform.Find("LeaderboardCanvas/PanelBody").gameObject;
            panelBody.GetComponent<Image>().color = Theme.colors.leaderboard.panelBody;
            panelBody.transform.Find("scoresbody").gameObject.GetComponent<Image>().color = Theme.colors.leaderboard.scoresBody;

            _modifierPanel = GameObject.Instantiate(panelBody, __instance.fullpanel.transform);
            GameObjectFactory.DestroyFromParent(_modifierPanel, "CloseButton");
            GameObjectFactory.DestroyFromParent(_modifierPanel, "txt_legal");
            GameObjectFactory.DestroyFromParent(_modifierPanel, "txt_leaderboards");
            GameObjectFactory.DestroyFromParent(_modifierPanel, "txt_songname");
            GameObjectFactory.DestroyFromParent(_modifierPanel, "rule");
            GameObjectFactory.DestroyFromParent(_modifierPanel, "HelpBtn");
            GameObjectFactory.DestroyFromParent(_modifierPanel, "loadingspinner_parent");
            GameObjectFactory.DestroyFromParent(_modifierPanel, "scoreboard");
            GameObjectFactory.DestroyFromParent(_modifierPanel, "tabs");
            GameObjectFactory.DestroyFromParent(_modifierPanel, "errors");
            GameObjectFactory.DestroyFromParent(_modifierPanel, "PanelFront");
            _modifierPanel.name = "ModifierPanel";
            RectTransform rectTransform = _modifierPanel.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(-35, 60);
            rectTransform.sizeDelta = new Vector2(300, 200);
            rectTransform.localScale = Vector2.one * .5f;

            _modifierPanel.SetActive(false);
            _modifierPanel.transform.localScale = Vector2.zero;
            _modifierPanelContainer = _modifierPanel.transform.Find("scoresbody").gameObject;
            var rect = _modifierPanelContainer.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, -10);
            rect.sizeDelta = Vector2.one * -20f;

            _modifierPanelContainer.AddComponent<Mask>();
            var gridLayoutGroup = _modifierPanelContainer.AddComponent<GridLayoutGroup>();
            gridLayoutGroup.padding = new RectOffset(30, 30, 30, 30);
            gridLayoutGroup.spacing = new Vector2(5, 5);
            gridLayoutGroup.cellSize = new Vector2(64, 64);
            gridLayoutGroup.childAlignment = TextAnchor.UpperLeft;

            _hideModifierPanelButton = GameObjectFactory.CreateCustomButton(_modifierPanelContainer.transform, Vector2.zero, new Vector2(32, 32), AssetManager.GetSprite("Close64.png"), "CloseModifierPanelButton", HideModifierPanel).gameObject;
            var layout = _hideModifierPanelButton.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            _modifierButtonDict.Add(GameModifiers.ModifierType.Hidden,
                GameObjectFactory.CreateModifierButton(_modifierPanelContainer.transform, AssetManager.GetSprite("HD.png"), "HiddenButton", "Hidden: Notes will disappear as they\n approach the left", _gameModifierDict.ContainsKey(GameModifiers.ModifierType.Hidden),
                delegate { Toggle(GameModifiers.ModifierType.Hidden); }));

            _modifierButtonDict.Add(GameModifiers.ModifierType.Flashlight,
                GameObjectFactory.CreateModifierButton(_modifierPanelContainer.transform, AssetManager.GetSprite("FL.png"), "FlashlightButton", "Flashlight: Only a small circle around the\n cursor is visible", _gameModifierDict.ContainsKey(GameModifiers.ModifierType.Flashlight),
                delegate { Toggle(GameModifiers.ModifierType.Flashlight); }));

            _modifierButtonDict.Add(GameModifiers.ModifierType.Brutal,
                GameObjectFactory.CreateModifierButton(_modifierPanelContainer.transform, AssetManager.GetSprite("BT.png"), "BrutalButton", "Brutal: Game will speed up if you do good and\n slow down when you are bad", _gameModifierDict.ContainsKey(GameModifiers.ModifierType.Brutal),
                delegate { Toggle(GameModifiers.ModifierType.Brutal); }));

            _modifierButtonDict.Add(GameModifiers.ModifierType.InstaFail,
                GameObjectFactory.CreateModifierButton(_modifierPanelContainer.transform, AssetManager.GetSprite("IF.png"), "InstaFailButton", "Insta Fail: Restart the song as soon as you miss.", _gameModifierDict.ContainsKey(GameModifiers.ModifierType.InstaFail),
                delegate { Toggle(GameModifiers.ModifierType.InstaFail); }));

            __instance.sortdrop.transform.SetAsLastSibling();
        }

        public static void Initialize()
        {
            _gameModifierDict = new Dictionary<GameModifiers.ModifierType, GameModifierBase>();
            _modifierButtonDict = new Dictionary<GameModifiers.ModifierType, GameObject>();
            _stringModifierDict = new Dictionary<string, GameModifiers.ModifierType>()
            {
                {"HD", GameModifiers.ModifierType.Hidden },
                {"FL", GameModifiers.ModifierType.Flashlight },
                {"BT", GameModifiers.ModifierType.Brutal },
                {"IF", GameModifiers.ModifierType.InstaFail },
            };
            _modifierTypesToRemove = new List<GameModifierBase>();
            _modifiersBackup = "None";
            _isInitialized = true;
        }

        private static void ShowModifierPanel()
        {
            if (_modifierPanel == null) return;
            if (_modifierPanel.activeSelf)
                TootTallyNotifManager.DisplayNotif("Stop trying to breaking my stuff... -_-");
            _canClickButtons = false;

            _modifierPanel.SetActive(true);

            _closeAnimation?.Dispose();

            _showModifierPanelButton.transform.localScale = Vector2.zero;
            _openAnimation = TootTallyAnimationManager.AddNewScaleAnimation(_modifierPanel, Vector2.one / 2f, 0.75f, new SecondDegreeDynamicsAnimation(2.5f, 1f, 0f));
            TootTallyAnimationManager.AddNewScaleAnimation(_modifierPanel, Vector2.one, 0.1f, new SecondDegreeDynamicsAnimation(0f, 0f, 0f), sender =>
            {
                _canClickButtons = true;
                _modifierButtonDict.Values.Do(b => TootTallyAnimationManager.AddNewScaleAnimation(b, Vector2.one, 0.75f, new SecondDegreeDynamicsAnimation(2.5f, 1f, 0f)));
            });
        }

        private static void HideModifierPanel()
        {
            if (_modifierPanel == null) return;

            _openAnimation?.Dispose();
            _canClickButtons = false;
            TootTallyAnimationManager.AddNewScaleAnimation(_showModifierPanelButton, Vector2.one, 0.5f, new SecondDegreeDynamicsAnimation(3.5f, 1f, 0f));

            _closeAnimation = TootTallyAnimationManager.AddNewScaleAnimation(_modifierPanel, Vector2.zero, 0.35f, new SecondDegreeDynamicsAnimation(2.5f, 1f, 0f), (sender) =>
            {
                _modifierPanel.SetActive(false);
                _modifierButtonDict.Values.Do(b => b.transform.localScale = Vector2.zero);
            });
        }

        public static void Toggle(GameModifiers.ModifierType modifierType)
        {
            if (!_canClickButtons) return;
            _canClickButtons = false;
            if (!_gameModifierDict.ContainsKey(modifierType))
            {
                TootTallyAnimationManager.AddNewEulerAngleAnimation(_modifierButtonDict[modifierType], new Vector3(0, 0, 8), 0.15f, new SecondDegreeDynamicsAnimation(2.5f, 1f, 2.5f), sender => { _canClickButtons = true; });
                _modifierButtonDict[modifierType].transform.Find("glow").gameObject.SetActive(true);
                Add(modifierType);
                return;
            }

            TootTallyAnimationManager.AddNewEulerAngleAnimation(_modifierButtonDict[modifierType], Vector3.zero, 0.15f, new SecondDegreeDynamicsAnimation(2.5f, 1f, 2.5f), sender => { _canClickButtons = true; });
            _modifierButtonDict[modifierType].transform.Find("glow").gameObject.SetActive(false);
            Remove(modifierType);
        }


        [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
        [HarmonyPostfix]
        public static void InitializeModifers(GameController __instance)
        {
            if (!_isInitialized) return;

            Plugin.LogInfo("Active modifiers: " + GetModifiersString());
            foreach (GameModifierBase mod in _gameModifierDict.Values)
            {
                mod.Initialize(__instance);
            }

            if (!_gameModifierDict.ContainsKey(GameModifiers.ModifierType.Flashlight))
                __instance.gameplayppp.vignette.enabled = false;
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.Update))]
        [HarmonyPostfix]
        public static void UpdateModifiers(GameController __instance)
        {
            if (!_isInitialized) return;

            foreach (GameModifierBase mod in _gameModifierDict.Values)
            {
                mod.Update(__instance);
            }
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.doScoreText))]
        [HarmonyPostfix]
        public static void UpdateBurtalMode(GameController __instance, int whichtext)
        {
            if (!_isInitialized) return;

            _gameModifierDict.TryGetValue(GameModifiers.ModifierType.Brutal, out GameModifierBase brutal);
            brutal?.SpecialUpdate(__instance);

            if (whichtext <= 2)
            {
                _gameModifierDict.TryGetValue(GameModifiers.ModifierType.InstaFail, out GameModifierBase instaFail);
                instaFail?.SpecialUpdate(__instance);
            }
            
        }

        public static void Remove(GameModifiers.ModifierType modifierType)
        {
            _gameModifierDict.Remove(modifierType);
        }

        public static void ClearAllModifiers()
        {
            _modifierTypesToRemove.AddRange(_gameModifierDict.Values.ToArray());
            _modifierTypesToRemove.Do(mod => mod.Remove());
            _modifierTypesToRemove.Clear();
        }

        public static string GetModifiersString() => _gameModifierDict.Count > 0 ? _gameModifierDict.Values.Join(mod => mod.Name, ",") : "None";

        public static void Add(GameModifiers.ModifierType modifierType)
        {
            if (_gameModifierDict.ContainsKey(modifierType))
            {
                Plugin.LogInfo($"Modifier of type {modifierType} is already in the modifier list.");
                return;
            }
            switch (modifierType)
            {
                case GameModifiers.ModifierType.Hidden:
                    _gameModifierDict.Add(GameModifiers.ModifierType.Hidden, new GameModifiers.Hidden());
                    break;
                case GameModifiers.ModifierType.Flashlight:
                    _gameModifierDict.Add(GameModifiers.ModifierType.Flashlight, new GameModifiers.Flashlight());
                    break;
                case GameModifiers.ModifierType.Brutal:
                    _gameModifierDict.Add(GameModifiers.ModifierType.Brutal, new GameModifiers.Brutal());
                    break;
                case GameModifiers.ModifierType.InstaFail:
                    _gameModifierDict.Add(GameModifiers.ModifierType.InstaFail, new GameModifiers.InstaFails());
                    break;
            };
        }

        public static void LoadModifiersFromString(string replayModifierString)
        {
            _modifiersBackup = GetModifiersString();
            ClearAllModifiers();
            if (replayModifierString == null) return;

            var replayModifierStringArray = replayModifierString.Split(',');
            if (replayModifierStringArray.Length <= 0)
            {
                Plugin.LogInfo("No modifiers detected.");
                return;
            }

            Plugin.LogInfo($"Loading {replayModifierString} modifiers.");
            foreach (string modName in replayModifierString.Split(','))
            {
                if (_stringModifierDict.ContainsKey(modName))
                    Add(_stringModifierDict[modName]);
            }
        }

        public static void LoadBackedupModifiers() => LoadModifiersFromString(_modifiersBackup);

    }
}
