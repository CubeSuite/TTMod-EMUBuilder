using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsDebuggingTools;
using EquinoxsModUtils;
using HarmonyLib;
using UnityEngine;

namespace EMUBuilder
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class EMUBuilderPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.equinox.EMUBuilder";
        private const string PluginName = "EMUBuilder";
        private const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        private void Awake() {
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();

            // ToDo: Apply Patches

            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log = Logger;
        }

        private void Update() {
            // ToDo: Delete If Not Needed
        }
    }

    public static class EMUBuilder 
    {
        /// <summary>
        /// Builds a machine that corresponds to the resID argument at the position and rotation given in gridInfo.
        /// </summary>
        /// <param name="resId">The resource ID of the type of machine you would like to build.</param>
        /// <param name="gridInfo">A GridInfo instance that contains the minPos and yawRotation of the machine you would like to build.</param>
        /// <param name="shouldLog">Whether EMU Info messages should be logged for this call</param>
        /// <param name="variationIndex">Optional - The variation to use for structure builds.</param>
        /// <param name="recipe">Optional - The recipe or filter that you would like to the machine to have selected.</param>
        /// <param name="chainData">Optional - The ChainData to use for building a conveyor belt</param>
        /// <param name="reverseConveyor">Optional - The value to use for ConveyorBuildInfo.isReversed</param>
        public static void BuildMachine(int resId, GridInfo gridInfo, bool shouldLog = false, int variationIndex = -1, int recipe = -1, ConveyorBuildInfo.ChainData? chainData = null, bool reverseConveyor = false) {
            if (!ModUtils.hasSaveStateLoaded) {
                EMUBuilderPlugin.Log.LogError("BuildMachine() called before SaveState.instance has loaded");
                EMUBuilderPlugin.Log.LogWarning("Try using the event ModUtils.SaveStateLoaded or checking with ModUtils.hasSaveStateLoaded");
                return;
            }

            MachineBuilder.buildMachine(resId, gridInfo, shouldLog, variationIndex, recipe, chainData, reverseConveyor);
        }

        /// <summary>
        /// Builds a machine that corresponds to the resourceName argument at the position and rotation given in gridInfo.
        /// </summary>
        /// <param name="resourceName">The name of the machine that you would like to build</param>
        /// <param name="gridInfo">A GridInfo instance that contains the minPos and yawRotation of the machine you would like to build.</param>
        /// <param name="shouldLog">Whether EMU Info messages should be logged for this call</param>
        /// <param name="variationIndex">Optional - The variation to use for structure builds.</param>
        /// <param name="recipe">Optional - The recipe or filter that you would like to the machine to have selected.</param>
        /// <param name="chainData">Optional - The ChainData to use for building a conveyor belt</param>
        /// <param name="reverseConveyor">Optional - The value to use for ConveyorBuildInfo.isReversed</param>
        public static void BuildMachine(string resourceName, GridInfo gridInfo, bool shouldLog = false, int variationIndex = -1, int recipe = -1, ConveyorBuildInfo.ChainData? chainData = null, bool reverseConveyor = false) {
            if (!ModUtils.hasSaveStateLoaded) {
                EMUBuilderPlugin.Log.LogError("BuildMachine() called before SaveState.instance has loaded");
                EMUBuilderPlugin.Log.LogWarning("Try using the event ModUtils.SaveStateLoaded or checking with ModUtils.hasSaveStateLoaded");
                return;
            }

            int resID = ModUtils.GetResourceIDByName(resourceName);
            if (resID == -1) {
                EMUBuilderPlugin.Log.LogError($"Could not build machine '{resourceName}'. Couldn't find a resource matching this name.");
                EMUBuilderPlugin.Log.LogWarning($"Try using the ResourceNames class for a perfect match.");
                return;
            }

            MachineBuilder.buildMachine(resID, gridInfo, shouldLog, variationIndex, recipe, chainData, reverseConveyor);
        }

    }
}
