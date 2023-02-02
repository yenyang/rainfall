using System;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using UnityEngine;


namespace Rainfall
{
    internal static class OptionHandler
    {
        private static Dictionary<string, List<OptionsItemBase>> allOptions = new Dictionary<string, List<OptionsItemBase>>
        {
            {
                "GEN1", new List<OptionsItemBase>
                {
                    new OptionsCheckbox() {defaultValue = true, uniqueName = "ChirpForecasts",                  readableName = "#RainForecast Chirps"},
                    new OptionsCheckbox() {defaultValue = true, uniqueName = "ChirpRainTweets",                 readableName = "#Rainfall Chirps"},
                    new OptionsCheckbox() {defaultValue = true, uniqueName = "PreventRainBeforeMilestone",      readableName = "Prevent Rain Before Milestone"},
                    new OptionsCheckbox() {defaultValue = true, uniqueName = "AutomaticPipeLaterals",           readableName = "Automatic Pipe Laterals" },
                    new OptionsCheckbox() {defaultValue = true, uniqueName = "OnlyFloodOwnedtiles",             readableName = "Unowned Tiles Do Not Flood"},
                    new OptionsDropdown() {defaultValue = 3,    uniqueName = "StormDrainAssetControlOption",    readableName = "Storm Drain Asset Control",                                             options = new List<string>(){ "No Control", "District Control", "ID Control", "District Control with ID Override" }},
                    new OptionsDropdown() {defaultValue = 2,    uniqueName = "GravityDrainageOption",           readableName = "Gravity Drainage Options",                                              options = new List<string>() { "Ignore Gravity", "Simplified (Alpha) Gravity", "Improved (Beta) Gravity" } },
                    new OptionsSlider()   {defaultValue = 0f,   uniqueName = "IncreaseBuildingPadHeight",       readableName = "Increase Building Pads",                                                units = " units",   tooltipFormat = "F1"},
                    new OptionsSlider()   {defaultValue = 5f,   uniqueName = "InletRateMultiplier",             readableName = "Inlet Rate Multiplier",                                                units = "X",   tooltipFormat = "F1", min=0.2f,  max=20f, step=0.2f},
                    new OptionsSlider()   {defaultValue = 5f,   uniqueName = "OutletRateMultiplier",            readableName = "Outlet Rate Multiplier",                                                units = "X",   tooltipFormat = "F1", min=0.2f,  max=20f, step=0.2f},
                    new OptionsSlider()   {defaultValue = 0.5f, uniqueName = "MakeItRainIntensity",             readableName = "Make it Rain! Intensity",                                                units = " units",   tooltipFormat = "F2", min=0.2f,  max=1f, step=0.05f},
                    new OptionsButton()   {                     uniqueName = "MakeItRain",                      readableName = "Make it Rain!",                                                         onButtonClicked = Hydrology.MakeItRain},
                }
            },
            { "GEN2", new List<OptionsItemBase>
                {
                    new OptionsCheckbox() {defaultValue = true,  uniqueName = "FreezeLandvalues",                readableName = "Prevent Building Upgrades from Increased Landvalue due to Rainwater"},
                    new OptionsSlider()   {defaultValue = 120f,  uniqueName = "FreezeLandvaluesTimer",           readableName = "Prev. Upgrade Add. Time",   units = " min",     tooltipFormat = "F0",     max = 600f,   step = 60f, tooltipMultiplier = 1f/60f},
                    new OptionsSlider()   {defaultValue = 180f,  uniqueName = "BreakBetweenStorms",              readableName = "Min. time btw. storms",     units = " min",     tooltipFormat = "F0",     max = 3600f,  step = 60f, tooltipMultiplier = 1f/60f},
                    new OptionsSlider()   {defaultValue = 3600f, uniqueName = "MaxTimeBetweenStorms",            readableName = "Max. time btw. storms",     units = " min",     tooltipFormat = "F0",     max = 3600f,  step = 60f, tooltipMultiplier = 1f/60f},
                    new OptionsCheckbox() {defaultValue = true,  uniqueName = "SimulatePollution",               readableName = "Simulate Pollution"},

                }
            },
            {
                "RO", new List<OptionsItemBase>
                {
                    new OptionsSlider()   {defaultValue = 25f,  uniqueName = "GlobalRunoffScalar",              readableName = "Global Runoff Scalar",                                                 units = " X",       tooltipFormat = "F2",              max = 200f, step = 2.5f, tooltipMultiplier = 0.04f},
                    //new OptionsDropdown() {defaultValue = 1,    uniqueName = "PreviousStormOption",             readableName = "Previous Storm Options (IE Loading after you saved during a storm)",    options = new List<string>() { "Start New Storm", "Continue from First Half", "Continue from Second Half", "End Previous Storm" } },
                    //new OptionsSlider()   {defaultValue = 180f,   uniqueName = "MinimumStormDuration",          readableName = "Min. Storm Duration",                                               units = " min",     tooltipFormat = "F0",     min = 60f,   max = 1440f,  step = 60f, tooltipMultiplier=1f/60f},
                    //new OptionsSlider()   {defaultValue = 480f,  uniqueName = "MaximumStormDuration",           readableName = "Max. Storm Duration",                                               units = " min",     tooltipFormat = "F0",     min = 60f,   max = 1440f,  step = 60f, tooltipMultiplier=1f/60f},
                    new OptionsSlider()   {defaultValue = 0.0001f, uniqueName = "IntensityRateOfChange",        readableName = "Intensity Rate of Change",                                              units = " units", tooltipFormat = "F5", min = 0.00001f, max = 0.0004f, step = 0.00001f},
                    new OptionsSlider()   {defaultValue = 2f,   uniqueName = "MinimumDrainageAreaRunoff",       readableName = "Min. Drainage Area RO",                                                 units = " units",   tooltipFormat = "F0",                 max = 100f, step = 1f},
                    new OptionsSlider()   {defaultValue = 500f, uniqueName = "MaximumDrainageAreaRunoff",       readableName = "Max. Drainage Area RO",                                                 units = " units",   tooltipFormat = "F0",                 max = 500f, step = 10f},
                    new OptionsDropdown() {defaultValue = 1,    uniqueName = "SelectedGridOption",              readableName = "Simulation Tiles",                          options = new List<string>(){ "None", "Owned Tiles", "Adjacent Tiles", "Adjacent and Diagonal Tiles","25 Tiles", "49 Tiles", "81 Tiles" }},
                    new OptionsButton()   {                     uniqueName = "ApplyGridOption",                 readableName = "Apply Simulation Tiles Choice",                                                             onButtonClicked = DrainageAreaGrid.Clear},
                    new OptionsSlider()   {defaultValue = 0.2f, uniqueName = "UndevelopedRunoffCoefficient",    readableName = "Vacant Runoff Coeff.",                 tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                    new OptionsSlider()   {defaultValue = 0.5f, uniqueName = "DefaultRunoffCoefficient",        readableName = "Default Runoff Coeff.",                 tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                    new OptionsSlider()   {defaultValue = 1.0f, uniqueName = "FloodSpawnerScalar",              readableName = "Flood Spawner Scalar",                  units = " X", tooltipFormat = "F1", min = 0.1f, max = 10f, step = 0.1f}
                }
            },
            {
                "PUB", new List<OptionsItemBase>
                {
                     new OptionsSlider()     {defaultValue = 0.8f,    uniqueName = "Electricity",               readableName = "Electricity",                 tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.6f,    uniqueName = "Healthcare",                readableName = "Healthcare",                  tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.6f,    uniqueName = "EmergencyServices",         readableName = "Emergency Services",          tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.6f,    uniqueName = "PoliceDepartments",         readableName = "Police & Banks",              tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.4f,    uniqueName = "EducationAndCampus",        readableName = "Education & Campus",          tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.6f,    uniqueName = "Transport",                 readableName = "Transport",                   tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.2f,    uniqueName = "ParksAndPlaza",             readableName = "Parks and Plaza",             tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.6f,    uniqueName = "UniqueBuildings",          readableName = "Unique Buildings",            tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.6f,    uniqueName = "Monuments",                 readableName = "Monuments",                   tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                }
            },
            {
                "PVT", new List<OptionsItemBase>
                {
                     new OptionsSlider()     {defaultValue = 0.4f,    uniqueName = "LowDensityResidential",       readableName = "Low Density Residential",     tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.6f,   uniqueName = "HighDensityResidential",      readableName = "High Density Residential",    tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.5f,    uniqueName = "LowDensityCommercial",        readableName = "Low Density Commercial",      tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.7f,    uniqueName = "HighDensityCommercial",       readableName = "High Density Commercial",     tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.5f,   uniqueName = "Office",                      readableName = "Office",                      tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = -0.1f,    uniqueName = "SelfSufficientResidential",   readableName = "Self Sufficient Resi. Mod.",  tooltipFormat = "F2",   min = -0.3f,  max = 0.3f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.0f,    uniqueName = "TouristCommercial",           readableName = "Tourist/Leisure Mod.",     tooltipFormat = "F2",     min = -0.3f,  max = 0.3f,    step = 0.05f},
                     //new OptionsSlider()     {defaultValue = 0.0f,    uniqueName = "LeisureCommerical",           readableName = "Lesiure Commercial Mod.",     tooltipFormat = "F2",     min = -0.3f,  max = 0.3f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = -0.1f,    uniqueName = "OrganicCommerical",           readableName = "Organic Commercial Mod.",     tooltipFormat = "F2",     min = -0.3f,  max = 0.3f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = -0.1f,    uniqueName = "HighTechOffice",              readableName = "High Tech Office Mod.",       tooltipFormat = "F2",     min = -0.3f,  max = 0.3f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = -0.05f,   uniqueName = "FinancialOffice",             readableName = "Financial Office Mod.",       tooltipFormat = "F2",     min = -0.3f,  max = 0.3f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.1f,   uniqueName = "WallToWall",                    readableName = "Wall-To-Wall Mod.",          tooltipFormat = "F2",     min = -0.3f,  max = 0.3f,    step = 0.05f},
                }
            },
            {
                "IND", new List<OptionsItemBase>
                {
                     new OptionsSlider()     {defaultValue = 0.85f,   uniqueName = "IndustryGeneral",           readableName = "Industry - General",          tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.2f,    uniqueName = "IndustryForest",            readableName = "Industry - Forest",           tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.2f,    uniqueName = "IndustryAgriculture",       readableName = "Industry - Agriculture",      tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.85f,   uniqueName = "IndustryOre",               readableName = "Industry - Ore",              tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.9f,    uniqueName = "IndustryOil",               readableName = "Industry - Oil",              tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.85f,   uniqueName = "IndustryWarehouse",         readableName = "Warehouse",                   tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     //new OptionsSlider()     {defaultValue = 0.85f,   uniqueName = "IndustryFactory",           readableName = "Factory",                     tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.85f,   uniqueName = "Garbage",                   readableName = "Garbage",                     tooltipFormat = "F2",     max = 1f,    step = 0.05f},

                }
            },
            {
                "NTWK", new List<OptionsItemBase>
                {
                     new OptionsSlider()     {defaultValue = 0.9f,   uniqueName = "Road",                     readableName = "Roads",                      tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.6f,   uniqueName = "Pathway",                  readableName = "Pathways",                      tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.9f,   uniqueName = "Runway",                   readableName = "Runway",                        tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.9f,   uniqueName = "Taxiway",                  readableName = "Taxiway",                       tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.6f,   uniqueName = "TrainTrack",                readableName = "Train Track",                   tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.6f,   uniqueName = "MetroTrack",                readableName = "Metro Track",                   tooltipFormat = "F2",     max = 1f,    step = 0.05f},
                     new OptionsSlider()     {defaultValue = 0.6f,   uniqueName = "Monorail",                  readableName = "Monorail Track",                tooltipFormat = "F2",     max = 1f,    step = 0.05f},

                }
            },
            {
                "BLDG", new List<OptionsItemBase>
                {
                    new OptionsCheckbox()   {defaultValue = true,   uniqueName = "BuildingSufferFlooding",      readableName = "Buildings can be Flooding"},
                    new OptionsSlider()     {defaultValue = 1f,     uniqueName = "BuildingFloodingTolerance",   readableName = "Building Flooding Tol.",    units = " units",   tooltipFormat = "F2"},
                    new OptionsSlider()     {defaultValue = 30f,    uniqueName = "BuildingFloodingTimer",       readableName = "Building Flooding Timer",   units = " seconds", tooltipFormat = "F0",     max = 300f,    step = 5f},
                    new OptionsSlider()     {defaultValue = 100f,   uniqueName = "BuildingFloodingEfficiency",  readableName = "Flooding Bldg Efficiency",  units = " %",       tooltipFormat = "F0",     max = 100f,      step = 5f, tooltipMultiplier = 1f},
                    new OptionsCheckbox()   {defaultValue = true,   uniqueName = "BuildingSufferFlooded",       readableName = "Buildings can be Flooded"},
                    new OptionsSlider()     {defaultValue = 2f,     uniqueName = "BuildingFloodedTolerance",    readableName = "Building Flooded Tol.",     units = " units",   tooltipFormat = "F2"},
                    new OptionsSlider()     {defaultValue = 60f,    uniqueName = "BuildingFloodedTimer",        readableName = "Building Flooded Timer",    units = " seconds", tooltipFormat = "F0",     max = 300f, step = 5f},
                    new OptionsSlider()     {defaultValue = 50f,    uniqueName = "BuildingFloodedEfficiency",   readableName = "Flooded Bldg Efficiency",   units = " %",       tooltipFormat = "F0",     max = 100f,      step = 5f, tooltipMultiplier = 1f},
                }
            },
            {
                "RDS", new List<OptionsItemBase>
                {
                    new OptionsCheckbox()   {defaultValue = true,   uniqueName = "RoadwaySufferFlooding",      readableName = "Roadways can be Flooding"},
                    new OptionsSlider()     {defaultValue = 1f,     uniqueName = "RoadwayFloodingTolerance",   readableName = "Roadway Flooding Tol.",    units = " units",   tooltipFormat = "F2"},
                    new OptionsSlider()     {defaultValue = 30f,    uniqueName = "RoadwayFloodingTimer",       readableName = "Roadway Flooding Timer",   units = " seconds", tooltipFormat = "F0",     max = 300f,    step = 5f},
                    new OptionsCheckbox()   {defaultValue = true,   uniqueName = "RoadwaySufferFlooded",       readableName = "Roadways can be Flooded"},
                    new OptionsSlider()     {defaultValue = 2f,     uniqueName = "RoadwayFloodedTolerance",    readableName = "Roadway Flooded Tol.",     units = " units",   tooltipFormat = "F2"},
                    new OptionsSlider()     {defaultValue = 60f,    uniqueName = "RoadwayFloodedTimer",        readableName = "Roadway Flooded Timer",    units = " seconds", tooltipFormat = "F0",     max = 300f, step = 5f},
                }
            },
            {
                "RLS", new List<OptionsItemBase>
                {
                    new OptionsCheckbox()   {defaultValue = true,   uniqueName = "RailwaySufferFlooding",      readableName = "Railways can be Flooding"},
                    new OptionsSlider()     {defaultValue = 1f,     uniqueName = "RailwayFloodingTolerance",   readableName = "Railway Flooding Tol.",    units = " units",   tooltipFormat = "F2"},
                    new OptionsSlider()     {defaultValue = 30f,    uniqueName = "RailwayFloodingTimer",       readableName = "Railway Flooding Timer",   units = " seconds", tooltipFormat = "F0",     max = 300f,    step = 5f},
                    new OptionsCheckbox()   {defaultValue = true,   uniqueName = "RailwaySufferFlooded",       readableName = "Railways can be Flooded"},
                    new OptionsSlider()     {defaultValue = 2f,     uniqueName = "RailwayFloodedTolerance",    readableName = "Railway Flooded Tol.",     units = " units",   tooltipFormat = "F2"},
                    new OptionsSlider()     {defaultValue = 60f,    uniqueName = "RailwayFloodedTimer",        readableName = "Railway Flooded Timer",    units = " seconds", tooltipFormat = "F0",     max = 300f, step = 5f},
                    new OptionsCheckbox()   {defaultValue = true,   uniqueName = "MetroSufferFlooding",        readableName = "Metro can be Flooding"},
                    new OptionsSlider()     {defaultValue = 1f,     uniqueName = "MetroFloodingTolerance",     readableName = "Metro Flooding Tol.",      units = " units",   tooltipFormat = "F2"},
                    new OptionsSlider()     {defaultValue = 30f,    uniqueName = "MetroFloodingTimer",         readableName = "Metro Flooding Timer",     units = " seconds", tooltipFormat = "F0",     max = 300f,    step = 5f},
                    new OptionsCheckbox()   {defaultValue = true,   uniqueName = "MetroSufferFlooded",         readableName = "Metro can be Flooded"},
                    new OptionsSlider()     {defaultValue = 2f,     uniqueName = "MetroFloodedTolerance",      readableName = "Metro Flooded Tol.",       units = " units",   tooltipFormat = "F2"},
                    new OptionsSlider()     {defaultValue = 60f,    uniqueName = "MetroFloodedTimer",          readableName = "Metro Flooded Timer",      units = " seconds", tooltipFormat = "F0",     max = 300f, step = 5f},
                }
            },
            {
                "PTH", new List<OptionsItemBase>
                {
                    new OptionsCheckbox()   {defaultValue = true,   uniqueName = "PathwaySufferFlooding",      readableName = "Pathways can be Flooding"},
                    new OptionsSlider()     {defaultValue = 1f,     uniqueName = "PathwayFloodingTolerance",   readableName = "Pathway Flooding Tol.",    units = " units",   tooltipFormat = "F2"},
                    new OptionsSlider()     {defaultValue = 30f,    uniqueName = "PathwayFloodingTimer",       readableName = "Pathway Flooding Timer",   units = " seconds", tooltipFormat = "F0",     max = 300f,    step = 5f},
                    new OptionsCheckbox()   {defaultValue = true,   uniqueName = "PathwaySufferFlooded",       readableName = "Pathways can be Flooded"},
                    new OptionsSlider()     {defaultValue = 2f,     uniqueName = "PathwayFloodedTolerance",    readableName = "Pathway Flooded Tol.",     units = " units",   tooltipFormat = "F2"},
                    new OptionsSlider()     {defaultValue = 60f,    uniqueName = "PathwayFloodedTimer",        readableName = "Pathway Flooded Timer",    units = " seconds", tooltipFormat = "F0",     max = 300f, step = 5f},
                }
            },
            {
                "RMV", new List<OptionsItemBase>
                {
                    new OptionsButton() {uniqueName = "EndStorm",                        readableName = "End Storm",                    onButtonClicked = Hydrology.EndStorm},
                    new OptionsButton() {uniqueName = "DeleteAllAssets",                 readableName = "Delete All Assets",            onButtonClicked = Hydraulics.deleteAllAssets},
                    //new OptionsButton() {uniqueName = "ReleaseWaterSource",              readableName = "Release Next Clicked Water Source", onButtonClicked = },
                    new OptionsButton() {uniqueName = "PurgeWaterSources",               readableName = "Purge RF Water Sources",       onButtonClicked = Hydrology.purgePreviousWaterSources},
                    new OptionsButton() {uniqueName = "PurgeFacilityWaterSources",       readableName = "Purge Water Sources",          onButtonClicked = Hydrology.PurgeFacilityWaterSources},
                    new OptionsButton() {uniqueName = "Terminate",                       readableName = "Terminate",                    onButtonClicked = Hydrology.Terminate},
                }
            }
        };

        private static Dictionary<string, string> fullOptionGroupNames = new Dictionary<string, string>()
        {
            {"GEN1",    "General Settings 1" },
            {"GEN2",    "General Settings 2" },
            {"RO",    "Runoff Settings" },
            {"BLDG",   "Building Flood Settings" },
            {"RDS",    "Roadway Flood Settings" },
            {"RLS",    "Railway Flood Settings" },
            {"PTH",   "Pathway Flood Settings" },
            {"PUB",    "Public Building Runoff Coefficients" },
            {"PVT",    "Private Building Runoff Coefficients" },
            {"IND",    "Industrial Building Runoff Coefficients" },
            {"NTWK",   "Network Runoff Coefficients" },
            {"RMV",    "Safely Remove Rainfall" },
        };
        private static List<string> noResetButtonGroupNames = new List<string>() { "RMV" };

        public static void SetUpOptions(UIHelperBase helper)
        {
            UIHelper actualHelper = helper as UIHelper;
            UIComponent container = actualHelper.self as UIComponent;

            UITabstrip tabStrip = container.AddUIComponent<UITabstrip>();
            tabStrip.relativePosition = new Vector3(0, 0);
            tabStrip.size = new Vector2(container.width - 20, 40);

            UITabContainer tabContainer = container.AddUIComponent<UITabContainer>();
            tabContainer.relativePosition = new Vector3(0, 40);
            tabContainer.size = new Vector2(container.width - 20, container.height - tabStrip.height - 20);
            tabStrip.tabPages = tabContainer;

            int currentIndex = 0;
            foreach (KeyValuePair<string, List<OptionsItemBase>> optionGroup in allOptions)
            {
                UIButton settingsButton = AddOptionTab(tabStrip, optionGroup.Key);
                settingsButton.textPadding = new RectOffset(10, 10, 10, 10);
                settingsButton.autoSize = true;
                String panelHelperGroupName = optionGroup.Key;
                String resetButtonUniqueName = optionGroup.Key + " Reset Button";
                if (fullOptionGroupNames.ContainsKey(optionGroup.Key))
                {
                    settingsButton.tooltip = fullOptionGroupNames[optionGroup.Key];
                    panelHelperGroupName = fullOptionGroupNames[optionGroup.Key];
                    resetButtonUniqueName = "Reset " + fullOptionGroupNames[optionGroup.Key];

                }
                else
                {
                    settingsButton.tooltip = optionGroup.Key;
                }
                tabStrip.selectedIndex = currentIndex;
                UIPanel currentPanel = tabStrip.tabContainer.components[currentIndex++] as UIPanel;
                currentPanel.autoLayout = true;
                currentPanel.autoLayoutDirection = LayoutDirection.Vertical;
                currentPanel.autoLayoutPadding.top = 5;
                currentPanel.autoLayoutPadding.left = 10;
                currentPanel.autoLayoutPadding.right = 10;
                UIHelper panelHelper = new UIHelper(currentPanel);
                UIHelperBase panelHelperGroup = CreateOptions(panelHelper, optionGroup.Value, panelHelperGroupName);
                if (!noResetButtonGroupNames.Contains(optionGroup.Key))
                {
                    OptionResetButton optionGroupResetButton = new OptionResetButton() { uniqueName = resetButtonUniqueName, optionsToReset = optionGroup.Value };
                    optionGroupResetButton.Create(panelHelperGroup);
                }
            }
        }
        private static UIButton AddOptionTab(UITabstrip tabStrip, string caption)
        {
            UIButton tabButton = tabStrip.AddTab(caption);

            tabButton.normalBgSprite = "SubBarButtonBase";
            tabButton.disabledBgSprite = "SubBarButtonBaseDisabled";
            tabButton.focusedBgSprite = "SubBarButtonBaseFocused";
            tabButton.hoveredBgSprite = "SubBarButtonBaseHovered";
            tabButton.pressedBgSprite = "SubBarButtonBasePressed";

            tabButton.textPadding = new RectOffset(10, 10, 10, 10);
            tabButton.autoSize = true;
            tabButton.tooltip = caption;

            return tabButton;
        }
        private static UIHelperBase CreateOptions(UIHelperBase helper, List<OptionsItemBase> options, string groupName)
        {
            UIHelperBase optionGroup = helper.AddGroup(groupName);
            foreach (OptionsItemBase option in options)
            {
                option.Create(optionGroup);
            }
            return optionGroup;
        }

        public const string PlayerPrefPrefix = "RF2_";
        //public static List<string> checkboxes = new List<string>();
        //public static List<string> dropdowns = new List<string>();
        //public static List<string> sliders = new List<string>();
        public static Dictionary<string, bool> checkboxValues = new Dictionary<string, bool>();
        public static Dictionary<string, int> dropdownValues = new Dictionary<string, int>();
        public static Dictionary<string, float> sliderValues = new Dictionary<string, float>();

        public static bool getCheckboxSetting(string uniqueName)
        {
            
            if (checkboxValues.ContainsKey(uniqueName))
            {
                return checkboxValues[uniqueName];
            }
            else
            {
                Debug.Log("[RF].OptionHandler Could not find Checkbox " + uniqueName);
            }
            return false;
        }
        public static int getDropdownSetting(string uniqueName)
        {
           
            if (dropdownValues.ContainsKey(uniqueName))
            {
                return dropdownValues[uniqueName];
            }
            else
            {
                Debug.Log("[RF].OptionHandler Could not find dropDown " + uniqueName);
            }
            return 0;
        }
        public static float getSliderSetting(string uniqueName)
        {
            
            if (sliderValues.ContainsKey(uniqueName))
            {
                return sliderValues[uniqueName];
            }
            else
            {
                Debug.Log("[RF].OptionHandler Could not find slider " + uniqueName);
            }
            return 0f;
        }

        public static readonly Dictionary<Type, string> SegmentAIRunoffCatalog = new Dictionary<Type, string>() {
            { typeof(RoadAI),               "Road"},
            { typeof(PedestrianPathAI),     "Pathway"},
            { typeof(PedestrianWayAI),      "Pathway"},
            { typeof(RunwayAI),             "Runway"},
            { typeof(TaxiwayAI),            "Taxiway" },
            { typeof(TrainTrackBaseAI),     "TrainTrack"},
            { typeof(MetroTrackAI),         "MetroTrack"},
            { typeof(MonorailTrackAI),      "Monorail"},
            { typeof(AirportAreaTaxiwayAI), "Taxiway"},
            { typeof(AirportAreaRunwayAI),  "Runway"},
            { typeof(PedestrianZoneRoadAI), "Pathway" }
        };
        public static Dictionary<Type, string> PublicBuildingAICatalog = new Dictionary<Type, string>()
        {
            {typeof(CemeteryAI),                    "Healthcare" },
            {typeof(ParkAI),                        "ParksAndPlaza" },
            {typeof(ParkGateAI),                    "ParksAndPlaza" },
            {typeof(LandfillSiteAI),                "Garbage" },
            {typeof(FireStationAI),                 "EmergencyServices"},
            {typeof(FirewatchTowerAI),              "EmergencyServices" },
            {typeof(PoliceStationAI),               "PoliceDepartments" },
            {typeof(BankOfficeAI),                  "PoliceDepartments" },    
            {typeof(HospitalAI),                    "Healthcare" },
            {typeof(PowerPlantAI),                  "Electricity" },
            {typeof(HeatingPlantAI),                "Electricity" },
            {typeof(MonumentAI),                    "Monuments" },
            {typeof(HadronColliderAI),              "UniqueBuildings" },
            {typeof(MuseumAI),                      "EducationAndCampus" },
            {typeof(LibraryAI),                     "EducationAndCampus" },
            {typeof(SpaceElevatorAI),               "UniqueBuildings" },
            {typeof(DepotAI),                       "Transport" },
            {typeof(MaintenanceDepotAI),            "Transport" },
            {typeof(PostOfficeAI),                  "Transport" },
            {typeof(CargoStationAI),                "Transport" },
            {typeof(SaunaAI),                       "Healthcare" },
            {typeof(SchoolAI),                      "EducationAndCampus" },
            {typeof(MainCampusBuildingAI),          "EducationAndCampus" },
            {typeof(CampusBuildingAI),              "EducationAndCampus" },
            {typeof(TourBuildingAI),                "Transport" },
            {typeof(DisasterResponseBuildingAI),    "EmergencyServices" },
            {typeof(DoomsdayVaultAI),               "UniqueBuildings" },
            {typeof(HelicopterDepotAI),             "EmergencyServices" },
            {typeof(ShelterAI),                     "EmergencyServices" },
            {typeof(ParkBuildingAI),                "ParksAndPlaza" },
            {typeof(AuxiliaryBuildingAI),           "IndustryGeneral"},
            {typeof(AirportBuildingAI),             "Transport" },
            {typeof(AirportAuxBuildingAI),          "Transport" },
            {typeof(AirportEntranceAI),             "Transport" },
            {typeof(AirportGateAI),                 "Transport" },
        };
        public static List<Type> PublicBuildingAISpecialCatalog = new List<Type>()
        {
            typeof(IndustryBuildingAI),
            typeof(MainIndustryBuildingAI)
        };

       

        public const int _NoControlOption = 0;
        public const int _DistrictControlOption = 1;
        public const int _IDControlOption = 2;
        public const int _IDOverrideOption = 3;
        public const int _ImprovedGravityDrainageOption = 2;
        public const int _SimpleGravityDrainageOption = 1;
        public const int _IgnoreGravityDrainageOption = 0;

        
        
    }
}
