using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TootTallyCore.Utils.TootTallyGlobals;
using UnityEngine;

namespace TootTallyGameModifiers
{
    public static class GameModifierManager
    {
        private static bool _isInitialized;
        private static Dictionary<string, GameModifiers.Metadata> _stringModifierDict;
        private static Dictionary<GameModifiers.ModifierType, GameModifierBase> _gameModifierDict;
        private static Dictionary<GameModifiers.ModifierType, ModifierButton> _modifierButtonDict;
        public static bool GetShouldSubmitScore => _gameModifierDict.Values.All(m => m.Metadata.ScoreSubmitEnabled);
        private static string _modifiersBackup;

        [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
        [HarmonyPostfix]
        static void OnHomeControllerStartInitialize()
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
            if (!_isInitialized) Initialize();

            _modifierButtonDict.Clear();
            var popup = GameModifierFactory.CreateModifiersPopup(__instance.fullpanel.transform, new Vector2(-420, -150), new Vector2(32, 32), __instance.fullpanel.transform, new Vector2(175, 125), 20, new Vector2(20, 20));
            var hContainer = GameModifierFactory.CreatePopupContainer(popup, new Vector2(0, 65));
            AddButton(hContainer.transform, GameModifiers.HIDDEN);
            AddButton(hContainer.transform, GameModifiers.FLASHLIGHT);
            AddButton(hContainer.transform, GameModifiers.BRUTAL);
            AddButton(hContainer.transform, GameModifiers.INSTA_FAIL);
            AddButton(hContainer.transform, GameModifiers.EASY_MODE);
            AddButton(hContainer.transform, GameModifiers.STRICT_MODE);
            //AddButton(hContainer.transform, GameModifiers.AUTO_TUNE);
            AddButton(hContainer.transform, GameModifiers.HIDDEN_CURSOR);
            __instance.sortdrop.transform.SetAsLastSibling();
        }

        static void Initialize()
        {
            _gameModifierDict = new Dictionary<GameModifiers.ModifierType, GameModifierBase>();
            _modifierButtonDict = new Dictionary<GameModifiers.ModifierType, ModifierButton>();
            _stringModifierDict = new Dictionary<string, GameModifiers.Metadata>()
            {
                {GameModifiers.HIDDEN.Name, GameModifiers.HIDDEN },
                {GameModifiers.FLASHLIGHT.Name, GameModifiers.FLASHLIGHT },
                {GameModifiers.BRUTAL.Name, GameModifiers.BRUTAL },
                {GameModifiers.INSTA_FAIL.Name, GameModifiers.INSTA_FAIL },
                {GameModifiers.EASY_MODE.Name, GameModifiers.EASY_MODE },
                {GameModifiers.STRICT_MODE.Name, GameModifiers.STRICT_MODE },
                {GameModifiers.AUTO_TUNE.Name, GameModifiers.AUTO_TUNE },
                {GameModifiers.HIDDEN_CURSOR.Name, GameModifiers.HIDDEN_CURSOR },
            };
            _modifiersBackup = "";
            _isInitialized = true;
        }

        private static GameModifierBase _hidden, _flashlight, _brutalMode, _instaFail, _easyMode, _strictMode, _autoTune, _noCursor;

        [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
        [HarmonyPostfix]
        static void InitializeModifers(GameController __instance)
        {
            if (!_isInitialized) return;

            var modifiers = GetModifiersString();
            Plugin.LogInfo("Active modifiers: " + modifiers);
            foreach (GameModifierBase mod in _gameModifierDict.Values)
                mod.Initialize(__instance);

            _gameModifierDict.TryGetValue(GameModifiers.ModifierType.Hidden, out _hidden);
            _gameModifierDict.TryGetValue(GameModifiers.ModifierType.Flashlight, out _flashlight);
            _gameModifierDict.TryGetValue(GameModifiers.ModifierType.Brutal, out _brutalMode);
            _gameModifierDict.TryGetValue(GameModifiers.ModifierType.InstaFail, out _instaFail);
            _gameModifierDict.TryGetValue(GameModifiers.ModifierType.EasyMode, out _easyMode);
            _gameModifierDict.TryGetValue(GameModifiers.ModifierType.StrictMode, out _strictMode);
            _gameModifierDict.TryGetValue(GameModifiers.ModifierType.AutoTune, out _autoTune);
            _gameModifierDict.TryGetValue(GameModifiers.ModifierType.HiddenCursor, out _noCursor);

            if (_flashlight == null)
                __instance.gameplayppp.vignette.enabled = false;
        }

        [HarmonyPatch(typeof(TootTallyPatches), nameof(TootTallyPatches.OnGameControllerStartSetTitleWithSpeed))]
        [HarmonyPostfix]
        public static void OnSetTitleAddModifiers(GameController __instance)
        {
            if (__instance.freeplay) return;

            //Kinda scuffed but string gets set by TTCore if speed is 0
            var modifiers = GetModifiersString();
            if (modifiers != "")
                AddModifiersToTitle(__instance, modifiers);
        }

        public static void AddModifiersToTitle(GameController __instance, string modifiers)
        {
            var modifiersText = $" [{modifiers}]";
            __instance.songtitle.text += modifiersText;
            __instance.songtitleshadow.text += modifiersText;
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.Update))]
        [HarmonyPostfix]
        static void UpdateModifiers(GameController __instance)
        {
            if (!_isInitialized) return;

            _hidden?.Update(__instance);
            _flashlight?.Update(__instance);
            _brutalMode?.Update(__instance);
            _strictMode?.Update(__instance);
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.doScoreText))]
        [HarmonyPostfix]
        static void UpdateEasyAndHardMode(GameController __instance, int whichtext)
        {
            if (!_isInitialized) return;

            _brutalMode?.SpecialUpdate(__instance);

            if (whichtext <= 2)
                _instaFail?.SpecialUpdate(__instance);
        }

        [HarmonyPatch(typeof(GameController), nameof(GameController.getScoreAverage))]
        [HarmonyPrefix]
        static void UpdateBrutalMode(GameController __instance)
        {
            if (!_isInitialized) return;

            _easyMode?.SpecialUpdate(__instance);
            _strictMode?.SpecialUpdate(__instance);
        }

        public static void Toggle(GameModifiers.ModifierType modifierType)
        {
            var button = _modifierButtonDict[modifierType];
            if (button.onCooldown) return;
            button.onCooldown = true;
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

        public static void ClearAllModifiers() => _gameModifierDict.Clear();

        public static string GetModifiersString() => _gameModifierDict.Count > 0 ? _gameModifierDict.Values.Join(mod => mod.Metadata.Name, ",") : "";

        internal static void Add(GameModifiers.ModifierType modifierType)
        {
            if (_gameModifierDict.ContainsKey(modifierType))
            {
                Plugin.LogInfo($"Modifier of type {modifierType} is already in the modifier list.");
                return;
            }
            _gameModifierDict.Add(modifierType, CreateGameModifier(modifierType));
        }

        private static GameModifierBase CreateGameModifier(GameModifiers.ModifierType modifierType) => modifierType switch
        {
            GameModifiers.ModifierType.Hidden => new GameModifiers.Hidden(),
            GameModifiers.ModifierType.Flashlight => new GameModifiers.Flashlight(),
            GameModifiers.ModifierType.Brutal => new GameModifiers.Brutal(),
            GameModifiers.ModifierType.InstaFail => new GameModifiers.InstaFail(),
            GameModifiers.ModifierType.EasyMode => new GameModifiers.EasyMode(),
            GameModifiers.ModifierType.StrictMode => new GameModifiers.StrictMode(),
            GameModifiers.ModifierType.AutoTune => new GameModifiers.AutoTune(),
            GameModifiers.ModifierType.HiddenCursor => new GameModifiers.HiddenCursor(),
            _ => throw new System.NotImplementedException(),
        };

        internal static void Remove(GameModifiers.ModifierType modifierType)
        {
            _gameModifierDict.Remove(modifierType);
        }

        public static void LoadModifiersFromString(string replayModifierString)
        {
            _modifiersBackup = GetModifiersString();
            ClearAllModifiers();
            var modifierTypes = GetModifierSet(replayModifierString);
            foreach (var modType in modifierTypes) Add(modType.ModifierType);
        }

        public static HashSet<GameModifiers.Metadata> GetModifierSet(string replayModifierString)
        {
            if (replayModifierString == null) return new HashSet<GameModifiers.Metadata>();
            return new HashSet<GameModifiers.Metadata>(replayModifierString.Split(',')
                .Where(_stringModifierDict.ContainsKey)
                .Select(modName => _stringModifierDict[modName]));
        }

        public static void LoadBackedupModifiers() => LoadModifiersFromString(_modifiersBackup);
    }
}
