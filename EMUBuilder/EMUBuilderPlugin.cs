using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsDebuggingTools;
using HarmonyLib;
using UnityEngine;

namespace EquinoxsModUtils
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class EMUBuilderPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.equinox.EMUBuilder";
        private const string PluginName = "EMUBuilder";
        private const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        // Unity Functions

        private void Awake() {
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();

            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log = Logger;
        }
    }

    /// <summary>
    /// Contains functions for building machines
    /// </summary>
    public static class EMUBuilder {
        // Members

        /// <summary>
        /// An array of the types of machines that EMUBuilder can currently build
        /// </summary>
        public static MachineTypeEnum[] SupportedMachineTypes = new MachineTypeEnum[] {
            MachineTypeEnum.Assembler,
            MachineTypeEnum.Chest,
            MachineTypeEnum.Conveyor,
            MachineTypeEnum.Drill,
            MachineTypeEnum.Inserter,
            MachineTypeEnum.LightSticks,  
            MachineTypeEnum.Planter,
            MachineTypeEnum.PowerGenerator,
            MachineTypeEnum.Smelter,
            MachineTypeEnum.Thresher,
            MachineTypeEnum.TransitDepot,
            MachineTypeEnum.TransitPole,
            MachineTypeEnum.Accumulator,
            MachineTypeEnum.VoltageStepper,
            MachineTypeEnum.Structure,
            MachineTypeEnum.BlastSmelter,
            MachineTypeEnum.Nexus,
            MachineTypeEnum.Crusher,
            MachineTypeEnum.SandPump,
            //MachineTypeEnum.,
        };

        // Public Functions

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
            if (!EMU.LoadingStates.hasSaveStateLoaded) {
                EMUBuilderPlugin.Log.LogError("BuildMachine() called before SaveState.instance has loaded");
                EMUBuilderPlugin.Log.LogWarning("Try using the event ModUtils.SaveStateLoaded or checking with ModUtils.hasSaveStateLoaded");
                return;
            }

            MachineBuilder.BuildMachine(resId, gridInfo, shouldLog, variationIndex, recipe, chainData, reverseConveyor);
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
            if (!EMU.LoadingStates.hasSaveStateLoaded) {
                EMUBuilderPlugin.Log.LogError("BuildMachine() called before SaveState.instance has loaded");
                EMUBuilderPlugin.Log.LogWarning("Try using the event ModUtils.SaveStateLoaded or checking with ModUtils.hasSaveStateLoaded");
                return;
            }

            int resID = EMU.Resources.GetResourceIDByName(resourceName);
            if (resID == -1) {
                EMUBuilderPlugin.Log.LogError($"Could not build machine '{resourceName}'. Couldn't find a resource matching this name.");
                EMUBuilderPlugin.Log.LogWarning($"Try using the ResourceNames class for a perfect match.");
                return;
            }

            MachineBuilder.BuildMachine(resID, gridInfo, shouldLog, variationIndex, recipe, chainData, reverseConveyor);
        }

    }
}
