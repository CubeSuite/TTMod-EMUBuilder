using EquinoxsDebuggingTools;
using FIMSpace.Generating.Planning.PlannerNodes.Cells.Actions;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using TriangleNet.Voronoi.Legacy;
using UnityEngine;

namespace EquinoxsModUtils
{
    internal static class MachineBuilder
    {
        // Objects & Variables

        // ToDo: Test if bug still exists
        private static List<string> flowerNames = new List<string>() {
            EMU.Names.Resources.SmallFloorPot,
            EMU.Names.Resources.WallPot,
            EMU.Names.Resources.MediumFloorPot,
            EMU.Names.Resources.CeilingPlant1x1,
            EMU.Names.Resources.CeilingPlant3x3,
            EMU.Names.Resources.WallPlant1x1,
            EMU.Names.Resources.WallPlant3x3,
        };

        internal static void BuildMachine(int resId, GridInfo gridInfo, bool shouldLog, int variationIndex, int recipe, ConveyorBuildInfo.ChainData? chainData, bool reverseConveyor) {
            MachineTypeEnum type = EMU.Resources.GetMachineTypeFromResID(resId);
            if(type == MachineTypeEnum.NONE) {
                ResourceInfo resourceInfo = SaveState.GetResInfoFromId(resId);
                string name = resourceInfo == null ? "Unknown Resource" : resourceInfo.displayName;
                EMUBuilderPlugin.Log.LogError($"Cannot build machine for invalid resID: {resId} - '{name}'");
                return;
            }

            switch (type) {
                case MachineTypeEnum.Accumulator:
                case MachineTypeEnum.BlastSmelter:
                case MachineTypeEnum.Chest:
                case MachineTypeEnum.LightSticks:
                case MachineTypeEnum.Planter:
                case MachineTypeEnum.Smelter:
                case MachineTypeEnum.Thresher:
                case MachineTypeEnum.TransitDepot:
                case MachineTypeEnum.VoltageStepper:
                case MachineTypeEnum.Crusher:
                case MachineTypeEnum.SandPump:
                case MachineTypeEnum.Nexus:
                    DoSimpleBuild(resId, gridInfo, shouldLog); break;
                
                case MachineTypeEnum.Assembler:
                    if (recipe == -1) DoSimpleBuild(resId, gridInfo, shouldLog);
                    else DoSimpleBuildWithRecipe(resId, gridInfo, recipe, shouldLog);
                    break;
                    
                case MachineTypeEnum.Inserter: DoSimpleBuildWithRecipe(resId, gridInfo, recipe, shouldLog); break;
                case MachineTypeEnum.Conveyor: DoConveyorBuild(resId, gridInfo, chainData, reverseConveyor); break;
                case MachineTypeEnum.Drill: DoDrillBuild(resId, gridInfo); break;
                case MachineTypeEnum.ResearchCore: DoResearchCoreBuild(resId, gridInfo); break;
                case MachineTypeEnum.Structure: DoStructureBuild(resId, gridInfo, variationIndex); break;
                case MachineTypeEnum.TransitPole: DoMassTransitBuild(resId, gridInfo, variationIndex); break;

                default:
                    EMUBuilderPlugin.Log.LogError($"Sorry, EMU currently doesn't support building {type}");
                    break;
            }
        }

        // doBuild Functions

        private static void DoSimpleBuild(int resID, GridInfo gridInfo, bool shouldLog) {
            SimpleBuildInfo simpleBuildInfo = new SimpleBuildInfo() {
                machineType = resID,
                rotation = gridInfo.yawRot,
                minGridPos = new GridPos(gridInfo.minPos, GameState.instance.GetStrata())
            };
            BuilderInfo builderInfo = (BuilderInfo)SaveState.GetResInfoFromId(resID);
            StreamedHologramData hologram = GetHologram(builderInfo, gridInfo);

            MachineInstanceDefaultBuilder builder = (MachineInstanceDefaultBuilder)Player.instance.builder.GetBuilderForType(BuilderInfo.BuilderType.MachineInstanceDefaultBuilder);
            builder.newBuildInfo = (SimpleBuildInfo)simpleBuildInfo.Clone();
            builder = (MachineInstanceDefaultBuilder)SetCommonBuilderFields(builder, builderInfo, gridInfo, hologram);

            SetPlayerBuilderPrivateFields(builder, builderInfo);
            DoBuild(builder, builderInfo, -1);

            if (shouldLog) EMUBuilderPlugin.Log.LogInfo($"Built {builderInfo.displayName} at ({gridInfo.minPos}) with yawRotation {gridInfo.yawRot}");
        }

        private static void DoSimpleBuildWithRecipe(int resID, GridInfo gridInfo, int recipe, bool shouldLog) {
            SimpleBuildInfo info = new SimpleBuildInfo() {
                machineType = resID,
                rotation = gridInfo.yawRot,
                minGridPos = new GridPos(gridInfo.minPos, GameState.instance.GetStrata()),
                recipeId = recipe
            };
            BuilderInfo builderInfo = (BuilderInfo)SaveState.GetResInfoFromId(resID);

            StreamedHologramData hologram = GetHologram(builderInfo, gridInfo);
            MachineInstanceDefaultBuilder builder = (MachineInstanceDefaultBuilder)Player.instance.builder.GetBuilderForType(BuilderInfo.BuilderType.MachineInstanceDefaultBuilder);
            builder.newBuildInfo = info;
            builder = (MachineInstanceDefaultBuilder)SetCommonBuilderFields(builder, builderInfo, gridInfo, hologram);
            
            SetPlayerBuilderPrivateFields(builder, builderInfo);
            DoBuild(builder, builderInfo, recipe);

            if (shouldLog) EMUBuilderPlugin.Log.LogInfo($"Built {builderInfo.displayName} with recipe {recipe} at {gridInfo.minPos} with yawRotation {gridInfo.yawRot}");
        }

        private static void DoConveyorBuild(int resID, GridInfo gridInfo, ConveyorBuildInfo.ChainData? nullableChainData, bool reverseConveyor) {
            if(nullableChainData == null) {
                EMUBuilderPlugin.Log.LogError($"You cannot build a conveyor with null ChainData. Aborting build attempt.");
                return;
            }

            EDT.Log("Conveyor Building", $"Attempting to build conveyor at {gridInfo.minPos} - S{GameState.instance.GetStrata()}");

            ConveyorBuildInfo.ChainData chainData = (ConveyorBuildInfo.ChainData)nullableChainData;

            EDT.Log("Conveyor Building", $"chainData.count: {chainData.count}");
            EDT.Log("Conveyor Building", $"chainData.rotation: {chainData.rotation}");
            EDT.Log("Conveyor Building", $"chainData.shape: {chainData.shape}");
            EDT.Log("Conveyor Building", $"chainData.start: {chainData.start}");
            EDT.Log("Conveyor Building", $"chainData.height: {chainData.height}");

            ConveyorBuildInfo conveyorBuildInfo = new ConveyorBuildInfo() {
                machineType = resID,
                chainData = new List<ConveyorBuildInfo.ChainData>() { chainData },
                isReversed = reverseConveyor,
                machineIds = new List<uint>(),
                autoHubsEnabled = false,
                strata = GameState.instance.GetStrata()
            };
            BuilderInfo builderInfo = (BuilderInfo)SaveState.GetResInfoFromId(resID);
            StreamedHologramData hologram = GetHologram(builderInfo, gridInfo, -1, chainData);

            ConveyorBuilder builder = (ConveyorBuilder)Player.instance.builder.GetBuilderForType(BuilderInfo.BuilderType.ConveyorBuilder);
            builder.beltBuildInfo = conveyorBuildInfo;
            builder = (ConveyorBuilder)SetCommonBuilderFields(builder, builderInfo, gridInfo, hologram);

            SetPlayerBuilderPrivateFields(builder, builderInfo);

            builder.BuildFromNetworkData(conveyorBuildInfo, false);
            Player.instance.inventory.TryRemoveResources(resID, 1);

            EDT.Log("Conveyor Building", $"Built {builderInfo.displayName} at {gridInfo.minPos} with yawRotation {gridInfo.yawRot}");
        }

        private static void DoDrillBuild(int resID, GridInfo gridInfo) {
            SimpleBuildInfo simpleBuildInfo = new SimpleBuildInfo() {
                machineType = resID,
                rotation = gridInfo.yawRot,
                minGridPos = new GridPos(gridInfo.minPos, GameState.instance.GetStrata())
            };
            BuilderInfo builderInfo = (BuilderInfo)SaveState.GetResInfoFromId(resID);
            StreamedHologramData hologram = GetHologram(builderInfo, gridInfo);

            DrillBuilder builder = (DrillBuilder)Player.instance.builder.GetBuilderForType(BuilderInfo.BuilderType.DrillBuilder);
            builder.newBuildInfo = simpleBuildInfo;
            builder = (DrillBuilder)SetCommonBuilderFields(builder, builderInfo, gridInfo, hologram);

            SetPlayerBuilderPrivateFields(builder, builderInfo);
            builder.BuildFromNetworkData(simpleBuildInfo, false);
            Player.instance.inventory.TryRemoveResources(resID, 1);
        }

        private static void DoResearchCoreBuild(int resID, GridInfo gridInfo) {
            ResearchCoreBuildInfo researchCoreBuildInfo = new ResearchCoreBuildInfo() {
                machineType = resID,
                startCorner = gridInfo.minPos,
                endCorner = gridInfo.minPos,
                rotation = gridInfo.yawRot
            };
            BuilderInfo builderInfo = (BuilderInfo)SaveState.GetResInfoFromId(resID);
            StreamedHologramData hologram = GetHologram(builderInfo, gridInfo);

            ResearchCoreBuilder builder = (ResearchCoreBuilder)Player.instance.builder.GetBuilderForType(BuilderInfo.BuilderType.ResearchCoreBuilder);
            builder.coreBuildInfo = researchCoreBuildInfo;
            builder = (ResearchCoreBuilder)SetCommonBuilderFields(builder, builderInfo, gridInfo, hologram);

            SetPlayerBuilderPrivateFields(builder, builderInfo);
            builder.BuildFromNetworkData(researchCoreBuildInfo, false);
            Player.instance.inventory.TryRemoveResources(resID, 1);
        }

        private static void DoStructureBuild(int resID, GridInfo gridInfo, int variationIndex) {
            SimpleBuildInfo simpleBuildInfo = new SimpleBuildInfo() {
                machineType = resID,
                rotation = gridInfo.yawRot,
                minGridPos = new GridPos(gridInfo.minPos, GameState.instance.GetStrata())
            };
            BuilderInfo builderInfo = (BuilderInfo)SaveState.GetResInfoFromId(resID);
            StreamedHologramData hologram = GetHologram(builderInfo, gridInfo, variationIndex);

            StructureBuilder builder = (StructureBuilder)Player.instance.builder.GetBuilderForType(BuilderInfo.BuilderType.StructureBuilder);
            builder.newBuildInfo = simpleBuildInfo;
            builder.currentVariationIndex = variationIndex;
            builder = (StructureBuilder)SetCommonBuilderFields(builder, builderInfo, gridInfo, hologram);

            SetPlayerBuilderPrivateFields(builder, builderInfo);
            DoBuild(builder, builderInfo, -1);
        }

        private static void DoMassTransitBuild(int resID, GridInfo gridInfo, int variationIndex) {
            MassTransitBuildInfo info = new MassTransitBuildInfo() {
                machineType = resID,
                rotation = gridInfo.yawRot,
                minGridPos = new GridPos(gridInfo.minPos, GameState.instance.GetStrata()),
                overrideHeight = variationIndex + 4
            };

            EDT.Log("Transit Pole Building", $"Building TransitPole with height {info.overrideHeight}");
            BuilderInfo builderInfo = (BuilderInfo)SaveState.GetResInfoFromId(resID);
            StreamedHologramData hologram = GetHologram(builderInfo, gridInfo, variationIndex);

            MassTransitBuilder builder = (MassTransitBuilder)Player.instance.builder.GetBuilderForType(BuilderInfo.BuilderType.MassTransitBuilder);
            EMU.SetPrivateField("newBuildInfo", builder, info);
            builder.currentVariationIndex = variationIndex;
            builder.curPoleHeight = info.overrideHeight;
            builder = (MassTransitBuilder)SetCommonBuilderFields(builder, builderInfo, gridInfo, hologram);

            SetPlayerBuilderPrivateFields(builder, builderInfo);
            DoBuild(builder, builderInfo, -1);
        }

        // Private Functions

        private static StreamedHologramData GetHologram(BuilderInfo builderInfo, GridInfo gridInfo, int variationIndex = -1, ConveyorBuildInfo.ChainData? nullableChainData = null) {
            StreamedHologramData hologram = null;
            Vector3 thisHologramPos = gridInfo.BottomCenter;
            MachineTypeEnum type = builderInfo.GetInstanceType();

            if(type == MachineTypeEnum.Conveyor) {
                ConveyorBuildInfo.ChainData chainData = (ConveyorBuildInfo.ChainData)nullableChainData;

                ConveyorInstance conveyor = MachineManager.instance.Get<ConveyorInstance, ConveyorDefinition>(0, type);
                ConveyorHologramData conveyorHologram = conveyor.myDef.GenerateUnbuiltHologramData() as ConveyorHologramData;
                conveyorHologram.buildBackwards = false;
                conveyorHologram.curShape = chainData.shape;
                conveyorHologram.numBelts = 1;

                thisHologramPos.x += conveyor.gridInfo.dims.x / 2.0f;
                thisHologramPos.z += conveyor.gridInfo.dims.z / 2.0f;

                Quaternion conveyorRotation = Quaternion.Euler(0, gridInfo.yawRot, 0);
                conveyorHologram.SetTransform(thisHologramPos, conveyorRotation);
                conveyorHologram.type = builderInfo;
                return conveyorHologram;
            }

            if(type != MachineTypeEnum.Inserter) {
                thisHologramPos.x += gridInfo.dims.x / 2.0f;
                thisHologramPos.z += gridInfo.dims.z / 2.0f;
            }

            switch (type) {
                case MachineTypeEnum.Assembler: hologram = ((AssemblerDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.Chest: hologram = ((ChestDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.Drill: hologram = ((DrillDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.Inserter: hologram = ((InserterDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.LightSticks: hologram = ((LightStickDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.Planter: hologram = ((PlanterDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.PowerGenerator: hologram = ((PowerGeneratorDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.ResearchCore: hologram = ((ResearchCoreDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.Smelter: hologram = ((SmelterDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.Thresher: hologram = ((ThresherDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.TransitDepot: hologram = ((TransitDepotDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.TransitPole: hologram = ((TransitPoleDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.WaterWheel: hologram = ((WaterWheelDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.Accumulator: hologram = ((AccumulatorDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.HighVoltageCable: hologram = ((HighVoltageCableDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.VoltageStepper: hologram = ((VoltageStepperDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.Structure: hologram = ((StructureDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.BlastSmelter: hologram = ((BlastSmelterDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.Crusher: hologram = ((CrusherDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.SandPump: hologram = ((SandPumpDefinition)builderInfo).GenerateUnbuiltHologramData(); break;
                case MachineTypeEnum.Nexus: hologram = ((NexusDefinition)builderInfo).GenerateUnbuiltHologramData(); break;

                default:
                    EMUBuilderPlugin.Log.LogWarning($"Skipped rendering hologram for unknown type: {type}");
                    break;
            }

            if (variationIndex != -1) hologram.variationNum = variationIndex;

            Quaternion rotation = Quaternion.Euler(0, gridInfo.yawRot, 0);
            hologram.SetTransform(thisHologramPos, rotation);
            hologram.type = builderInfo;
            return hologram;
        }

        private static ProceduralBuilder SetCommonBuilderFields(ProceduralBuilder builder, BuilderInfo builderInfo, GridInfo gridInfo, StreamedHologramData hologram) {
            builder.curBuilderInfo = builderInfo;
            builder.myNewGridInfo = gridInfo;
            builder.myHolo = hologram;
            builder.recentlyBuilt = true;
            builder.OnShow();
            return builder;
        }

        private static void SetPlayerBuilderPrivateFields(ProceduralBuilder builder, BuilderInfo builderInfo) {
            EMU.SetPrivateField("_currentBuilder", Player.instance.builder, builder);
            EMU.SetPrivateField("_lastBuilderInfo", Player.instance.builder, builderInfo);
            EMU.SetPrivateField("_lastBuildPos", Player.instance.builder, builder.curGridPlacement.MinInt);
        }

        private static void DoBuild(ProceduralBuilder builder, BuilderInfo builderInfo, int recipeID) {
            BuildMachineAction action = builder.GenerateNetworkData();
            action.recipeId = recipeID;
            action.resourceCostID = builderInfo.uniqueId;
            action.resourceCostAmount = 1;
            NetworkMessageRelay.instance.SendNetworkAction(action);
        }
    }
}
