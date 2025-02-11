using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;
using WeatherRegistry;

namespace SnowyWeeds
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency(WEATHER_REGISTRY, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        const string PLUGIN_GUID = "dopadream.lethalcompany.snowyweeds", PLUGIN_NAME = "SnowyWeeds", PLUGIN_VERSION = "1.3.4", WEATHER_REGISTRY = "mrov.WeatherRegistry";
        internal static new ManualLogSource Logger;
        internal static Texture weedTexture;

        void Awake()
        {
            Logger = base.Logger;

            try
            {
                AssetBundle weedBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "snowyweeds"));
                Texture snowyWeedTex = weedBundle.LoadAsset("InvasiveWeedSnowy", typeof(Texture)) as Texture;
                weedTexture = snowyWeedTex;
            }
            catch
            {
                Logger.LogError("Encountered some error loading asset bundle. Did you install the plugin correctly?");
                return;
            }


            new Harmony(PLUGIN_GUID).PatchAll();


            Logger.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded");
        }

        private static bool IsSnowLevel()
        {
            if (Chainloader.PluginInfos.ContainsKey(WEATHER_REGISTRY))
            {
                return WeatherRegistrySnowCheck();
            } else
            {
                return StartOfRound.Instance.currentLevel.levelIncludesSnowFootprints;
            }
        }

        private static bool WeatherRegistrySnowCheck()
        {
            return StartOfRound.Instance.currentLevel.levelIncludesSnowFootprints || WeatherManager.GetCurrentLevelWeather().name.Equals("Snowfall") || WeatherManager.GetCurrentLevelWeather().name.Equals("Blizzard");
        }

        [HarmonyPatch]
        private class SnowyWeedPatches
        {
            [HarmonyPatch(typeof(MoldSpreadManager), "GenerateMold")]
            [HarmonyPostfix]
            private static void PostSpreadMold(MoldSpreadManager __instance)
            {
                if (!IsSnowLevel() || StartOfRound.Instance == null)
                {
                    return;
                }
                for (int i = 0; i < __instance.moldContainer.childCount; i++)
                {
                    Renderer[] componentsInChildren = __instance.moldContainer.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderers in componentsInChildren)
                    {
                        renderers.material.mainTexture = weedTexture;
                        Logger.LogDebug(renderers.material.mainTexture.name + " applied to shroud");
                    }
                }
            }
        }
    }
}