using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TootTallyGameModifiers
{
    public static class GameModifierManager
    {
        private static bool _isInitialized;
        private static List<GameModifierBase> _modifierTypesToRemove;
        private static Dictionary<string, GameModifiers.ModifierType> _stringModifierDict;
        private static Dictionary<GameModifiers.ModifierType, GameModifierBase> _gameModifierDict;
        private static Dictionary<GameModifiers.ModifierType, ModifierButton> _modifierButtonDict;
        private static string _modifiersBackup;

        [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
        [HarmonyPostfix]
        public static void OnHomeControllerStartInitialize()
        {
            if (!_isInitialized) Initialize();
        }

        static void AddButton(Transform transform, GameModifiers.Metadata mod)
        {
            var active = _gameModifierDict.ContainsKey(mod.ModifierType);
            var button = new ModifierButton(transform, mod, active, new Vector2(32, 32), 3, 8, true, delegate { Toggle(mod.ModifierType); });
            _modifierButtonDict.Add(mod.ModifierType, button);
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.Start))]
        [HarmonyPostfix]
        static void OnLevelSelectControllerStartPostfix(LevelSelectController __instance)
        {
            _modifierButtonDict.Clear();
            var popup = GameModifierFactory.CreateModifiersPopup(__instance.fullpanel.transform, new Vector2(-420, -150), new Vector2(32, 32), __instance.fullpanel.transform, new Vector2(175, 125), 20, new Vector2(20, 20));
            var hContainer = GameModifierFactory.CreatePopupContainer(popup, new Vector2(0, 65), 15, 2);
            AddButton(hContainer.transform, GameModifiers.HIDDEN);
            AddButton(hContainer.transform, GameModifiers.FLASHLIGHT);
            AddButton(hContainer.transform, GameModifiers.BRUTAL);
            AddButton(hContainer.transform, GameModifiers.INSTA_FAIL);
            __instance.sortdrop.transform.SetAsLastSibling();
        }

        public static void Initialize()
        {
            _gameModifierDict = new Dictionary<GameModifiers.ModifierType, GameModifierBase>();
            _modifierButtonDict = new Dictionary<GameModifiers.ModifierType, ModifierButton>();
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

        public static void Toggle(GameModifiers.ModifierType modifierType)
        {
            var button = _modifierButtonDict[modifierType];
            if (!button.canClickButtons) return;
            button.canClickButtons = false;
            if (!_gameModifierDict.ContainsKey(modifierType))
            {
                button.ToggleOn();
                Add(modifierType);
            }
            else
            {
                button.ToggleOff();
                Remove(modifierType);
            }
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
        public static void UpdateBrutalMode(GameController __instance, int whichtext)
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

        public static void ClearAllModifiers()
        {
            _modifierTypesToRemove.AddRange(_gameModifierDict.Values.ToArray());
            _modifierTypesToRemove.Do(mod => mod.Remove());
            _modifierTypesToRemove.Clear();
        }

        public static string GetModifiersString() => _gameModifierDict.Count > 0 ? _gameModifierDict.Values.Join(mod => mod.Metadata.Name, ",") : "None";

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
                    _gameModifierDict.Add(GameModifiers.ModifierType.InstaFail, new GameModifiers.InstaFail());
                    break;
            };
        }

        public static void Remove(GameModifiers.ModifierType modifierType)
        {
            _gameModifierDict.Remove(modifierType);
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
