using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using TootTallyCore.Utils.Assets;
using TootTallyCore.Utils.TootTallyModules;
using TootTallySettings.TootTallySettingsObjects;

namespace TootTallyGameModifiers
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("TootTallyCore", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private Harmony _harmony;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get; set; }

        //Change this name to whatever you want
        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }

        public static TootTallySettingSlider StartFadeoutInput, EndFadeoutInput;

        public static void LogInfo(string msg) => Instance.Logger.LogInfo(msg);
        public static void LogError(string msg) => Instance.Logger.LogError(msg);

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;
            _harmony = new Harmony(Info.Metadata.GUID);

            GameInitializationEvent.Register(Info, TryInitialize);
        }

        private void TryInitialize()
        {
            // Bind to the TTModules Config for TootTally
            ModuleConfigEnabled = TootTallyCore.Plugin.Instance.Config.Bind("Modules", "GameModifiers", true, "Enable GameModifiers");
            TootTallyModuleManager.AddModule(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + "Hidden.cfg", true);
            StartFade = config.Bind("Hidden", "StartFade", 3.5f, "Position at which the fade Starts for hidden.");
            EndFade = config.Bind("Hidden", "EndFade", -1.6f, "Position at which the fade Ends for hidden.");


            AssetBundleManager.LoadAssets(Path.Combine(Path.GetDirectoryName(Instance.Info.Location), "layoutassetbundle"));
            AssetManager.LoadAssets(Path.Combine(Path.GetDirectoryName(Instance.Info.Location), "Assets"));
            /*StartFadeoutInput = TootTallySettings.Plugin.MainTootTallySettingPage.AddSlider("Start Fadeout", -25, 25, 500, "HD StartFade", StartFade, false);
            EndFadeoutInput = TootTallySettings.Plugin.MainTootTallySettingPage.AddSlider("End Fadeout", -25, 25, 500, "HD EndFade", EndFade, false);*/
            _harmony.PatchAll(typeof(GameModifierManager));
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            _harmony.UnpatchSelf();
            LogInfo($"Module unloaded!");
        }
        ConfigEntry<float> StartFade, EndFade;
    }
}