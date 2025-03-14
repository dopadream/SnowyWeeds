﻿using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using WeatherRegistry;

namespace SnowyWeeds
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency(WEATHER_REGISTRY, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(LOBBY_COMPATIBILITY, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal const string PLUGIN_GUID = "dopadream.lethalcompany.snowyweeds", PLUGIN_NAME = "SnowyWeeds", PLUGIN_VERSION = "1.4.1", LOBBY_COMPATIBILITY = "BMX.LobbyCompatibility", WEATHER_REGISTRY = "mrov.WeatherRegistry", ARTIFICE_BLIZZARD = "butterystancakes.lethalcompany.artificeblizzard";
        internal static new ManualLogSource Logger;
        internal static Texture weedTexture;
        internal static Material weedMaterial;

        void Awake()
        {
            Logger = base.Logger;

            if (Chainloader.PluginInfos.ContainsKey(LOBBY_COMPATIBILITY))
            {
                LobbyCompatibility.Init();
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Lobby Compatibility detected");
            }

            if (Chainloader.PluginInfos.ContainsKey(WEATHER_REGISTRY))
            {
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Weather Registry detected");
            }

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
            if (Chainloader.PluginInfos.ContainsKey(WEATHER_REGISTRY) && WeatherRegistrySnowCheck())
                return true;

            return StartOfRound.Instance.currentLevel.levelIncludesSnowFootprints && (StartOfRound.Instance.currentLevel.name != "ArtificeLevel" || !Chainloader.PluginInfos.ContainsKey(ARTIFICE_BLIZZARD) || GameObject.Find("/Systems/Audio/BlizzardAmbience") == null);
        }

        private static bool WeatherRegistrySnowCheck()
        {
            return WeatherManager.GetCurrentLevelWeather().name.Equals("Snowfall") || WeatherManager.GetCurrentLevelWeather().name.Equals("Blizzard");
        }

        [HarmonyPatch]
        private class SnowyWeedPatches
        {
            [HarmonyPatch(typeof(MoldSpreadManager), "Start")]
            [HarmonyPostfix]
            private static void PostStart(MoldSpreadManager __instance)
            {
                if (weedMaterial == null && weedTexture != null)
                {
                    Renderer weed = __instance.moldPrefab?.GetComponentsInChildren<Renderer>()?.Where(rend => rend.gameObject.layer != 22)?.FirstOrDefault();
                    if (weed != null)
                    {
                        weedMaterial = Instantiate(weed.sharedMaterial);
                        weedMaterial.mainTexture = weedTexture;
                        Logger.LogDebug("Cached snowy material");
                    }
                    else
                        Logger.LogError("Failed to create snowy weeds material");
                }
            }
            [HarmonyPatch(typeof(RoundManager), "FinishGeneratingNewLevelClientRpc")]
            [HarmonyPostfix]
            private static void PostSpreadMold()
            {
                if (weedMaterial == null || !IsSnowLevel())
                {
                    return;
                }

                Renderer[] componentsInChildren = FindAnyObjectByType<MoldSpreadManager>()?.moldContainer?.GetComponentsInChildren<Renderer>();
                if (componentsInChildren == null || componentsInChildren.Length < 1)
                    return;

                foreach (Renderer renderers in componentsInChildren)
                {
                    renderers.sharedMaterial = weedMaterial;
                    Logger.LogDebug(weedMaterial.mainTexture.name + " applied to shroud");
                }
            }
        }
    }
}