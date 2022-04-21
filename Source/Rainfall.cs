
using ICities;
using System.Collections.Generic;
using ColossalFramework.UI;
using System.Reflection;
using UnityEngine;
using ColossalFramework;
using System;
using Rainfall.UI;
using System.Runtime.Remoting;

namespace Rainfall
{
    public class RainfallMod : IUserMod
    {

        public string Name { get { return "Rainfall"; } }
        public string Description { get { return "Simulates runoff and Includes a Storm Drain AI to manage Storm Drain Assets. By [SSU]yenyang"; } }
        public UISlider DifficultySlider;
        public UISlider SnowmeltFactorSlider;
        public UISlider RefreshRateSlider;
        public UICheckBox ChirpForecastCheckBox;
        public UICheckBox ChirpRainTweetsCheckBox;
        
        public UISlider MinimumStormDurationSlider;
        public UISlider MaximumStormDurationSlider;
        public UISlider MaximumStormIntensitySlider;
        public UISlider TargetRainIntensitySlider;
        public UIButton ResetAllButton;
        public UIDropDown PreviousStormDropDown;
        private readonly string[] previousStormDropDownOptions = new string[] { "Start New Storm", "Continue from First Half", "Continue from Second Half", "End Previous Storm" };
       
        private readonly string[] StormDrainAssetControlDropDownOptions = new string[] { "No Control", "District Control", "ID Control", "District Control with ID Override" };
        private UIDropDown StormDrainAssetControlDropDown;

        private readonly string[] DrainageBasinGridOptions = new string[] {  "None","Owned Tiles", "Adjacent Tiles","Adjacent and Diagonal Tiles","25 Tiles"};
        public UIDropDown DrainageBasinGridOptionsDropDown;
        public UISlider BuildingFloodingToleranceSlider;
        public UISlider BuildingFloodedToleranceSlider;
        public UISlider RoadwayFloodingToleranceSlider;
        public UISlider RoadwayFloodedToleranceSlider;
        public UISlider PedestrianPathFloodingToleranceSlider;
        public UISlider PedestrianPathFloodedToleranceSlider;
        public UISlider TrainTrackFloodingToleranceSlider;
        public UISlider TrainTrackFloodedToleranceSlider;
        public UICheckBox BuildingPadIncreaseOptInCheckbox;
        public UISlider IncreaseBuildingPadHeightSlider;
        public UISlider MaxBuildingPadHeightSlider;
        public UICheckBox AdditionalIncreaseForLowerPadsCheckBox;
        public UICheckBox IncreaseExistingVanillaPadsOnLoadCheckbox;
        public UICheckBox FreezeLandvaluesCheckBox;
        
        public UICheckBox PreventRainBeforeMilestoneCheckBox;
       
        public UICheckBox SimulatePollutionCheckBox;
        private string[] gravityDrainageDropDownOptions = new string[] { "Ignore Gravity", "Simplified (Alpha) Gravity", "Improved (Beta) Gravity" };
        public UIDropDown GravityDrainageDropDown;
        public UIButton DeleteAssetsButton;
        public UIButton CleanUpCycleButton;
        public UIButton EndStormButton;
        public UIButton MakeItRainButton;
        public UIButton RemoveWaterSourcesButton;
        
        public UICheckBox AdditionalToleranceOnSlopeCheckBox;
        public List<OptionsItemBase> PublicBuildingsRunoffCoefficientSliders;
        public List<OptionsItemBase> PrivateBuildingsRunoffCoefficientSliders;
        public List<OptionsItemBase> PrivateBuildingRunoffModifierSliders;

        public UICheckBox AutomaticPipeLateralsCheckBox;
        private StormDistributionIO customStormDistribution;

        public void onEnabled()
        {
            CitiesHarmony.API.HarmonyHelper.EnsureHarmonyInstalled();
        }
        public void OnSettingsUI(UIHelperBase helper)
        {
            customStormDistribution = new StormDistributionIO();


            UIHelperBase group = helper.AddGroup("General Options");
            ChirpForecastCheckBox = group.AddCheckbox("#RainForecast Chirps", ModSettings.ChirpForecasts, OnChirpForecastCheckBoxChanged) as UICheckBox;
            ChirpRainTweetsCheckBox = group.AddCheckbox("#Rainfall Chirps", ModSettings.ChirpRainTweets, OnChirpRainTweetsCheckBoxChanged) as UICheckBox;
            StormDrainAssetControlDropDown = group.AddDropdown("Storm Drain Asset Control", StormDrainAssetControlDropDownOptions, ModSettings.StormDrainAssetControlOption, OnStormDrainAssetControlDropDownChanged) as UIDropDown;
            SimulatePollutionCheckBox = group.AddCheckbox("Simulate Pollution", ModSettings.SimulatePollution, OnSimulatePollutionCheckBoxChanged) as UICheckBox;
            PreventRainBeforeMilestoneCheckBox = group.AddCheckbox("Prevent Rain Before Milestone", ModSettings.PreventRainBeforeMilestone, OnPreventRainBeforeMilestoneCheckBoxChanged) as UICheckBox;
            FreezeLandvaluesCheckBox = group.AddCheckbox("Prevent Building Upgrades from Increased Landvalue due to Rainwater", ModSettings.FreezeLandvalues, OnFreezeLandvaluesCheckBoxChanged) as UICheckBox;
            AutomaticPipeLateralsCheckBox = group.AddCheckbox("Automatic Pipe Laterals", ModSettings.AutomaticPipeLaterals, OnAutomaticPipeLateralsCheckBoxChanged) as UICheckBox;
            PreviousStormDropDown = group.AddDropdown("Previous Storm Options (IE Loading after you saved during a storm)", previousStormDropDownOptions, ModSettings.PreviousStormOption, OnPreviousStormOptionChanged) as UIDropDown;
            GravityDrainageDropDown = group.AddDropdown("Gravity Drainage Options", gravityDrainageDropDownOptions, ModSettings.GravityDrainageOption, OnGravityDrainageOptionChanged) as UIDropDown;
            DifficultySlider = group.AddSlider("Difficulty", 0f, (float)ModSettings._maxDifficulty, (float)ModSettings._difficultyStep, (float)ModSettings.Difficulty, OnDifficultyChanged) as UISlider;
            DifficultySlider.tooltip = ModSettings.Difficulty.ToString() + "%";
            DifficultySlider.width += 100;
            RefreshRateSlider = group.AddSlider("Refresh Rate", 1f, (float)ModSettings._maxRefreshRate, 1f, (float)ModSettings.RefreshRate, OnRefreshRateChanged) as UISlider;
            RefreshRateSlider.tooltip = ModSettings.RefreshRate.ToString() + " seconds";
            RefreshRateSlider.width += 100;
            DrainageBasinGridOptionsDropDown = group.AddDropdown("Drainage Basin Grid Extents", DrainageBasinGridOptions, ModSettings.SelectedDrainageBasinGridOption, onDrainageBasinGridOptionChanged) as UIDropDown;
            TargetRainIntensitySlider = group.AddSlider("Target Rain Intensity", 0.2f, 1f, 0.05f, 0.2f, onTargetRainIntensityChanged) as UISlider;
            TargetRainIntensitySlider.width += 100;
            MakeItRainButton = group.AddButton("Initiate Storm with Target Intensity", makeItRain) as UIButton;


            UIHelperBase PadHeightGroup = helper.AddGroup("Building Pads Height Increases (Use Move It! to fine tune)");
            BuildingPadIncreaseOptInCheckbox = PadHeightGroup.AddCheckbox("Increase Building Pads", ModSettings.PadHeightIncreaseOptIn, OnBuildingPadIncreaseOptInChanged) as UICheckBox;
            IncreaseBuildingPadHeightSlider = PadHeightGroup.AddSlider("Bldg. Pad Increase", ModSettings._minPadIncrease, ModSettings._maxPadIncrease, ModSettings._padIncreaseStep, (float)ModSettings.IncreaseBuildingPadHeight, OnIncreaseBuildingPadHeightChanged) as UISlider;
            IncreaseBuildingPadHeightSlider.tooltip = ((float)ModSettings.IncreaseBuildingPadHeight / 100f).ToString() + " units";
            IncreaseBuildingPadHeightSlider.width += 100;
           
            UIHelperBase FloodingToleranceGroup = helper.AddGroup("Flooding Tolerances");
            BuildingFloodingToleranceSlider = FloodingToleranceGroup.AddSlider("Building Flooding Tol.", (float)ModSettings._minFloodTolerance, (float)ModSettings._maxFloodTolerance, (float)ModSettings._floodToleranceStep, (float)ModSettings.BuildingFloodingTolerance, OnBuildingFloodingToleranceChanged) as UISlider;
            BuildingFloodingToleranceSlider.tooltip = ((float)ModSettings.BuildingFloodingTolerance / 100f).ToString() + " units";
            BuildingFloodingToleranceSlider.width += 100;
            BuildingFloodedToleranceSlider = FloodingToleranceGroup.AddSlider("Building Flooded Tol.", (float)ModSettings._minFloodTolerance, (float)ModSettings._maxFloodTolerance, (float)ModSettings._floodToleranceStep, (float)ModSettings.BuildingFloodedTolerance, OnBuildingFloodedToleranceChanged) as UISlider;
            BuildingFloodedToleranceSlider.tooltip = ((float)ModSettings.BuildingFloodedTolerance/100f).ToString() + " units";
            BuildingFloodedToleranceSlider.width += 100;
            RoadwayFloodingToleranceSlider = FloodingToleranceGroup.AddSlider("Roadway Flooding Tol.", (float)ModSettings._minFloodTolerance, (float)ModSettings._maxFloodTolerance, (float)ModSettings._floodToleranceStep, (float)ModSettings.RoadwayFloodingTolerance, OnRoadwayFloodingToleranceChanged) as UISlider;
            RoadwayFloodingToleranceSlider.tooltip = ((float)ModSettings.RoadwayFloodingTolerance / 100f).ToString() + " units";
            RoadwayFloodingToleranceSlider.width += 100;
            RoadwayFloodedToleranceSlider = FloodingToleranceGroup.AddSlider("Roadway Flooded Tol.", (float)ModSettings._minFloodTolerance, (float)ModSettings._maxFloodTolerance, (float)ModSettings._floodToleranceStep, (float)ModSettings.RoadwayFloodedTolerance, OnRoadwayFloodedToleranceChanged) as UISlider;
            RoadwayFloodedToleranceSlider.tooltip = ((float)ModSettings.RoadwayFloodedTolerance / 100f).ToString() + " units";
            RoadwayFloodedToleranceSlider.width += 100;
            AdditionalToleranceOnSlopeCheckBox = FloodingToleranceGroup.AddCheckbox("Additional Roadway Tolerance based on Slope", ModSettings.AdditionalToleranceOnSlopes, OnAdditionalToleranceCheckBoxChanged) as UICheckBox;

            PedestrianPathFloodingToleranceSlider = FloodingToleranceGroup.AddSlider("Ped. Path Flooding Tol.", (float)ModSettings._minFloodTolerance, (float)ModSettings._maxFloodTolerance, (float)ModSettings._floodToleranceStep, (float)ModSettings.PedestrianPathFloodingTolerance, OnPedestrianPathFloodingToleranceChanged) as UISlider;
            PedestrianPathFloodingToleranceSlider.tooltip = ((float)ModSettings.PedestrianPathFloodingTolerance / 100f).ToString() + " units";
            PedestrianPathFloodingToleranceSlider.width += 100;
            PedestrianPathFloodedToleranceSlider = FloodingToleranceGroup.AddSlider("Ped. Path Flooded Tol.", (float)ModSettings._minFloodTolerance, (float)ModSettings._maxFloodTolerance, (float)ModSettings._floodToleranceStep, (float)ModSettings.PedestrianPathFloodedTolerance, OnPedestrianPathFloodedToleranceChanged) as UISlider;
            PedestrianPathFloodedToleranceSlider.tooltip = ((float)ModSettings.PedestrianPathFloodedTolerance / 100f).ToString() + " units";
            PedestrianPathFloodedToleranceSlider.width += 100;

            TrainTrackFloodingToleranceSlider = FloodingToleranceGroup.AddSlider("Train Track Flooding Tol.", (float)ModSettings._minFloodTolerance, (float)ModSettings._maxFloodTolerance, (float)ModSettings._floodToleranceStep, (float)ModSettings.TrainTrackFloodingTolerance, OnTrainTrackFloodingToleranceChanged) as UISlider;
            TrainTrackFloodingToleranceSlider.tooltip = ((float)ModSettings.TrainTrackFloodingTolerance / 100f).ToString() + " units";
            TrainTrackFloodingToleranceSlider.width += 100;
            TrainTrackFloodedToleranceSlider = FloodingToleranceGroup.AddSlider("Train Track Flooded Tol.", (float)ModSettings._minFloodTolerance, (float)ModSettings._maxFloodTolerance, (float)ModSettings._floodToleranceStep, (float)ModSettings.TrainTrackFloodedTolerance, OnTrainTrackFloodedToleranceChanged) as UISlider;
            TrainTrackFloodedToleranceSlider.tooltip = ((float)ModSettings.TrainTrackFloodedTolerance / 100f).ToString() + " units";
            TrainTrackFloodedToleranceSlider.width += 100;

            UIHelperBase StormWaterSimulationGroup = helper.AddGroup("Stormwater Simulation Settings");
           MinimumStormDurationSlider = StormWaterSimulationGroup.AddSlider("Min. Storm Duration", ModSettings._minStormDuration, ModSettings._maxStormDuration, ModSettings._stormDurationStep, (float)ModSettings.MinimumStormDuration, onMinimumStormDurationChanged) as UISlider;
            MinimumStormDurationSlider.tooltip = convertMinToHourMin((float)ModSettings.MinimumStormDuration);
            MinimumStormDurationSlider.width += 100;
            MaximumStormDurationSlider = StormWaterSimulationGroup.AddSlider("Max. Storm Duration", ModSettings._minStormDuration, ModSettings._maxStormDuration, ModSettings._stormDurationStep, (float)ModSettings.MaximumStormDuration, onMaximumStormDurationChanged) as UISlider;

            MaximumStormDurationSlider.tooltip = convertMinToHourMin((float)ModSettings.MaximumStormDuration);
            MaximumStormDurationSlider.width += 100;
            MaximumStormIntensitySlider = StormWaterSimulationGroup.AddSlider("Max. Intensity", ModSettings._minStormIntensity, ModSettings._maxStormIntensity, ModSettings._stormIntenistyStep, (float)ModSettings.MaximumStormIntensity, onMaximumStormIntensityChanged) as UISlider;
            MaximumStormIntensitySlider.tooltip = ModSettings.MaximumStormIntensity.ToString() + " Units/Hour";
            MaximumStormIntensitySlider.width += 100;

            //SnowmeltFactorSlider = StormWaterSimulationGroup.AddSlider("Snowmelt Factor", 0f, ModSettings._maxSnowmeltFactor, ModSettings._SnowmeltFactorStep, ModSettings.SnowmeltFactor, onSnowmeltFactorChanged) as UISlider;
            //SnowmeltFactorSlider.width += 100;
            //SnowmeltFactorSlider.tooltip = ModSettings.SnowmeltFactor.ToString();

            UIHelperBase PublicBuildingsRunOffCoefficientsGroup = helper.AddGroup("Public Buildings Runoff Coefficient Settings");
            initializeRunoffCoefficientSliders();
            createRunoffCoefficientSliders(PublicBuildingsRunOffCoefficientsGroup, PublicBuildingsRunoffCoefficientSliders);

            UIHelperBase PrivateBuildingsRunoffCoefficientsGroup = helper.AddGroup("Private Buildings Runoff Coefficient Settings");
            createRunoffCoefficientSliders(PrivateBuildingsRunoffCoefficientsGroup, PrivateBuildingsRunoffCoefficientSliders);

            UIHelperBase PrivateBuildingsRunoffModifiersGroup = helper.AddGroup("District Specialization Runoff Modifiers Settings");
            createRunoffCoefficientSliders(PrivateBuildingsRunoffModifiersGroup, PrivateBuildingRunoffModifierSliders);

            UIHelperBase ResetGroup = helper.AddGroup("Reset");
            ResetAllButton = ResetGroup.AddButton("Reset All", resetAllSettings) as UIButton;

            UIHelperBase SafelyRemoveRainfallGroup = helper.AddGroup("Safely Remove Rainfall");
            DeleteAssetsButton = SafelyRemoveRainfallGroup.AddButton("Delete Rainfall Assets", deleteAllAssets) as UIButton;
            CleanUpCycleButton = SafelyRemoveRainfallGroup.AddButton("Clean Up Cycle", cleanUpCycle) as UIButton;
            EndStormButton = SafelyRemoveRainfallGroup.AddButton("End Storm", endStorm) as UIButton;
            RemoveWaterSourcesButton = SafelyRemoveRainfallGroup.AddButton("Remove Water Sources", removeWaterSources) as UIButton;
        }



        private void OnDifficultyChanged(float val)
        {
            ModSettings.Difficulty = (int)val;
            DifficultySlider.tooltip = ModSettings.Difficulty.ToString() + "%";
            DifficultySlider.tooltipBox.Show();
            DifficultySlider.RefreshTooltip();
        }
        private void onSnowmeltFactorChanged(float val)
        {
            ModSettings.SnowmeltFactor = (int)val;
            SnowmeltFactorSlider.tooltip = ModSettings.SnowmeltFactor.ToString();
            SnowmeltFactorSlider.tooltipBox.Show();
            SnowmeltFactorSlider.RefreshTooltip();
        }
        private void OnBuildingFloodingToleranceChanged(float val)
        {
            if (val < ModSettings.BuildingFloodedTolerance)
                ModSettings.BuildingFloodingTolerance = (int)val;
            else
            {
                ModSettings.BuildingFloodingTolerance = ModSettings.BuildingFloodedTolerance-ModSettings._floodToleranceStep;
                BuildingFloodingToleranceSlider.value = ModSettings.BuildingFloodingTolerance;
            }
            BuildingFloodingToleranceSlider.tooltip = ((float)ModSettings.BuildingFloodingTolerance / 100f).ToString() + " units";
            BuildingFloodingToleranceSlider.tooltipBox.Show();
            BuildingFloodingToleranceSlider.RefreshTooltip();
        }
        private void OnBuildingFloodedToleranceChanged(float val)
        {
            if (val > ModSettings.BuildingFloodingTolerance)
                ModSettings.BuildingFloodedTolerance = (int)val;
            else
            {
                ModSettings.BuildingFloodedTolerance = ModSettings.BuildingFloodingTolerance + ModSettings._floodToleranceStep;
                BuildingFloodedToleranceSlider.value =  ModSettings.BuildingFloodedTolerance;
            }
            BuildingFloodedToleranceSlider.tooltip = ((float)ModSettings.BuildingFloodedTolerance / 100f).ToString() + " units";
            BuildingFloodedToleranceSlider.tooltipBox.Show();
            BuildingFloodedToleranceSlider.RefreshTooltip();
        }
        private void OnRoadwayFloodingToleranceChanged(float val)
        {
            if (val < ModSettings.RoadwayFloodedTolerance)
                ModSettings.RoadwayFloodingTolerance = (int)val;
            else
            {
                ModSettings.RoadwayFloodingTolerance = ModSettings.RoadwayFloodedTolerance - ModSettings._floodToleranceStep;
                RoadwayFloodingToleranceSlider.value = ModSettings.RoadwayFloodingTolerance;
            }
            RoadwayFloodingToleranceSlider.tooltip = ((float)ModSettings.RoadwayFloodingTolerance / 100f).ToString() + " units";
            RoadwayFloodingToleranceSlider.tooltipBox.Show();
            RoadwayFloodingToleranceSlider.RefreshTooltip();
        }
        private void OnRoadwayFloodedToleranceChanged(float val)
        {
            if (val > ModSettings.RoadwayFloodingTolerance)
                ModSettings.RoadwayFloodedTolerance = (int)val;
            else
            {
                ModSettings.RoadwayFloodedTolerance = ModSettings.RoadwayFloodingTolerance + ModSettings._floodToleranceStep;
                RoadwayFloodedToleranceSlider.value = ModSettings.RoadwayFloodedTolerance;
            }
            RoadwayFloodedToleranceSlider.tooltip = ((float)ModSettings.RoadwayFloodedTolerance / 100f).ToString() + " units";
            RoadwayFloodedToleranceSlider.tooltipBox.Show();
            RoadwayFloodedToleranceSlider.RefreshTooltip();
        }

        private void OnPedestrianPathFloodingToleranceChanged(float val)
        {
            if (val < ModSettings.PedestrianPathFloodedTolerance)
                ModSettings.PedestrianPathFloodingTolerance = (int)val;
            else
            {
                ModSettings.PedestrianPathFloodingTolerance = ModSettings.PedestrianPathFloodedTolerance - ModSettings._floodToleranceStep;
                PedestrianPathFloodingToleranceSlider.value = ModSettings.PedestrianPathFloodingTolerance;
            }
            PedestrianPathFloodingToleranceSlider.tooltip = ((float)ModSettings.PedestrianPathFloodingTolerance / 100f).ToString() + " units";
            PedestrianPathFloodingToleranceSlider.tooltipBox.Show();
            PedestrianPathFloodingToleranceSlider.RefreshTooltip();
        }
        private void OnPedestrianPathFloodedToleranceChanged(float val)
        {
            if (val > ModSettings.PedestrianPathFloodingTolerance)
                ModSettings.PedestrianPathFloodedTolerance = (int)val;
            else
            {
                ModSettings.PedestrianPathFloodedTolerance = ModSettings.PedestrianPathFloodingTolerance + ModSettings._floodToleranceStep;
                PedestrianPathFloodedToleranceSlider.value = ModSettings.PedestrianPathFloodedTolerance;
            }
            PedestrianPathFloodedToleranceSlider.tooltip = ((float)ModSettings.PedestrianPathFloodedTolerance / 100f).ToString() + " units";
            PedestrianPathFloodedToleranceSlider.tooltipBox.Show();
            PedestrianPathFloodedToleranceSlider.RefreshTooltip();
        }

        private void OnTrainTrackFloodingToleranceChanged(float val)
        {
            if (val < ModSettings.TrainTrackFloodedTolerance)
                ModSettings.TrainTrackFloodingTolerance = (int)val;
            else
            {
                ModSettings.TrainTrackFloodingTolerance = ModSettings.TrainTrackFloodedTolerance - ModSettings._floodToleranceStep;
                TrainTrackFloodingToleranceSlider.value = ModSettings.TrainTrackFloodingTolerance;
            }
            TrainTrackFloodingToleranceSlider.tooltip = ((float)ModSettings.TrainTrackFloodingTolerance / 100f).ToString() + " units";
            TrainTrackFloodingToleranceSlider.tooltipBox.Show();
            TrainTrackFloodingToleranceSlider.RefreshTooltip();
        }
        private void OnTrainTrackFloodedToleranceChanged(float val)
        {
            if (val > ModSettings.TrainTrackFloodingTolerance)
                ModSettings.TrainTrackFloodedTolerance = (int)val;
            else
            {
                ModSettings.TrainTrackFloodedTolerance = ModSettings.TrainTrackFloodingTolerance + ModSettings._floodToleranceStep;
                TrainTrackFloodedToleranceSlider.value = ModSettings.TrainTrackFloodedTolerance;
            }
            TrainTrackFloodedToleranceSlider.tooltip = ((float)ModSettings.TrainTrackFloodedTolerance / 100f).ToString() + " units";
            TrainTrackFloodedToleranceSlider.tooltipBox.Show();
            TrainTrackFloodedToleranceSlider.RefreshTooltip();
        }

        private void OnIncreaseBuildingPadHeightChanged(float val)
        {
            
            IncreaseBuildingPadHeightSlider.tooltip = ((float)ModSettings.IncreaseBuildingPadHeight / 100f).ToString() + " units";
            IncreaseBuildingPadHeightSlider.tooltipBox.Show();
            IncreaseBuildingPadHeightSlider.RefreshTooltip();
        }
       
       
        private void OnRefreshRateChanged(float val)
        {
            ModSettings.RefreshRate = (int)val;
            RefreshRateSlider.tooltip = ModSettings.RefreshRate.ToString() + " seconds";
            RefreshRateSlider.tooltipBox.Show();
            RefreshRateSlider.RefreshTooltip();
        }

        private void OnChirpForecastCheckBoxChanged(bool val)
        {
            ModSettings.ChirpForecasts = (bool)val;
        }
        private void OnAdditionalToleranceCheckBoxChanged(bool val)
        {
            ModSettings.ChirpForecasts = (bool)val;
        }
        private void OnAutomaticPipeLateralsCheckBoxChanged(bool val)
        {
            ModSettings.AutomaticPipeLaterals = (bool)val;
        }
        
        private void OnBuildingPadIncreaseOptInChanged(bool val)
        {
            ModSettings.PadHeightIncreaseOptIn = (bool)val;
        }
        
        
        private void OnSimulatePollutionCheckBoxChanged(bool val)
        {
            ModSettings.SimulatePollution = (bool)val;
        }
     
        private void OnPreventRainBeforeMilestoneCheckBoxChanged(bool val)
        {
            ModSettings.PreventRainBeforeMilestone = (bool)val;
        }
       
        private void OnFreezeLandvaluesCheckBoxChanged(bool val)
        {
            ModSettings.FreezeLandvalues = (bool)val;
        }
        
        private void OnPreviousStormOptionChanged(int sel)
        {
            ModSettings.PreviousStormOption = (int)sel;
        }

        private void OnGravityDrainageOptionChanged(int sel)
        {
            ModSettings.GravityDrainageOption = (int)sel;
        }

        private void OnChirpRainTweetsCheckBoxChanged(bool val)
        {
            ModSettings.ChirpRainTweets = (bool)val;
        }

        private void onDrainageBasinGridOptionChanged(int sel)
        {
            if (sel != ModSettings.SelectedDrainageBasinGridOption)
            {
                ModSettings.SelectedDrainageBasinGridOption = sel;
                if (DrainageBasinGrid.areYouAwake())
                {
                    DrainageBasinGrid.generateDraiangeBasinGrid();
                }
            }
        }
        private void OnStormDrainAssetControlDropDownChanged(int sel)
        {
            ModSettings.StormDrainAssetControlOption = (int)sel;
        }
       
        private string convertMinToHourMin(float time)
        {
            string min = ((int)time % 60).ToString();
            string hour = ((int)time/60).ToString();
            return (hour + " Hours " + min + " Mins");
        }
        
        private void onMinimumStormDurationChanged(float val)
        {
            if (val < ModSettings.MaximumStormDuration)
                ModSettings.MinimumStormDuration = (int)val;
            else
            {
                ModSettings.MinimumStormDuration = ModSettings.MaximumStormDuration;
                MinimumStormDurationSlider.value = ModSettings.MaximumStormDuration;
            }
            MinimumStormDurationSlider.tooltip = convertMinToHourMin(ModSettings.MinimumStormDuration);
            MinimumStormDurationSlider.tooltipBox.Show();
            MinimumStormDurationSlider.RefreshTooltip();
        }
        private void onMaximumStormDurationChanged(float val)
        {
            if (val > ModSettings.MinimumStormDuration)
                ModSettings.MaximumStormDuration = (int)val;
            else
            {
                ModSettings.MaximumStormDuration = ModSettings.MinimumStormDuration;
                MaximumStormDurationSlider.value = ModSettings.MinimumStormDuration;
            }
            MaximumStormDurationSlider.tooltip = convertMinToHourMin(ModSettings.MaximumStormDuration);
            MaximumStormDurationSlider.tooltipBox.Show();
            MaximumStormDurationSlider.RefreshTooltip();
        }
        private void onMaximumStormIntensityChanged(float val)
        {
            ModSettings.MaximumStormIntensity = (int)val;
            if (val < ModSettings._maxStormIntensity)
                MaximumStormIntensitySlider.tooltip = val.ToString() + " Units/Hour";
            else
                MaximumStormIntensitySlider.tooltip = "Unlimited";
            MaximumStormIntensitySlider.tooltipBox.Show();
            MaximumStormIntensitySlider.RefreshTooltip();
        }
        
        private void initializeRunoffCoefficientSliders()
        {
            PublicBuildingsRunoffCoefficientSliders = new List<OptionsItemBase>();
            foreach (KeyValuePair<string, ModSettings.runoffStruct> pair in ModSettings.PublicBuildingsRunoffCoefficients)
            {
                PublicBuildingsRunoffCoefficientSliders.Add(new OptionsRunoffCoefficientSlider() { value = pair.Value.Coefficient, min = 0f, max = 1f, step = 0.05f, uniqueName = pair.Key, readableName = pair.Value.Name});
            }
            PrivateBuildingsRunoffCoefficientSliders = new List<OptionsItemBase>();
            foreach (KeyValuePair<string, ModSettings.runoffStruct> pair in ModSettings.PrivateBuildingsRunoffCoefficients)
            {
                PrivateBuildingsRunoffCoefficientSliders.Add(new OptionsRunoffCoefficientSlider() { value = pair.Value.Coefficient, min = 0f, max = 1f, step = 0.05f, uniqueName = pair.Key, readableName = pair.Value.Name});
            }
            PrivateBuildingRunoffModifierSliders = new List<OptionsItemBase>();
            foreach (KeyValuePair<string, ModSettings.runoffStruct> pair in ModSettings.PrivateBuildingRunoffModifiers)
            {
                PrivateBuildingRunoffModifierSliders.Add(new OptionsRunoffCoefficientSlider() { value = pair.Value.Coefficient, min = -0.25f, max = 0.25f, step = 0.05f, uniqueName = pair.Key, readableName = pair.Value.Name });
            }
        }
        private void createRunoffCoefficientSliders(UIHelperBase helper, List<OptionsItemBase> sliderList)
        {
            foreach(OptionsItemBase slider in sliderList)
            {
                slider.enabled = true;
                slider.Create(helper);
            }
        }
        

        private void resetAllSettings()
        {
            ModSettings.resetModSettings();
            DifficultySlider.value = ModSettings.Difficulty;
            //SnowmeltFactorSlider.value = ModSettings.SnowmeltFactor;
            
            RefreshRateSlider.value = ModSettings.RefreshRate;
            ChirpForecastCheckBox.isChecked = ModSettings.ChirpForecasts;
            ChirpRainTweetsCheckBox.isChecked = ModSettings.ChirpRainTweets;
           
            AutomaticPipeLateralsCheckBox.isChecked = ModSettings.AutomaticPipeLaterals;
            PreviousStormDropDown.selectedIndex = ModSettings.PreviousStormOption;
            DrainageBasinGridOptionsDropDown.selectedIndex = ModSettings.SelectedDrainageBasinGridOption;
       
            MinimumStormDurationSlider.value = ModSettings.MinimumStormDuration;
            MaximumStormDurationSlider.value = ModSettings.MaximumStormDuration;
            MaximumStormIntensitySlider.value = ModSettings.MaximumStormIntensity;
            
            BuildingFloodedToleranceSlider.value = ModSettings._defaultBuildingFloodedTolerance;
            BuildingFloodingToleranceSlider.value = ModSettings._defaultBuildingFloodingTolerance;
            RoadwayFloodedToleranceSlider.value = ModSettings._defaultRoadwayFloodedTolerance;
            RoadwayFloodingToleranceSlider.value = ModSettings._defaultRoadwayFloodingTolerance;
            PedestrianPathFloodedToleranceSlider.value = ModSettings._defaultRoadwayFloodedTolerance;
            PedestrianPathFloodingToleranceSlider.value = ModSettings._defaultRoadwayFloodingTolerance;
            TrainTrackFloodedToleranceSlider.value = ModSettings._defaultRoadwayFloodedTolerance;
            TrainTrackFloodingToleranceSlider.value = ModSettings._defaultRoadwayFloodingTolerance;
            FreezeLandvaluesCheckBox.isChecked = ModSettings.FreezeLandvalues;
           
            PreventRainBeforeMilestoneCheckBox.isChecked = ModSettings.PreventRainBeforeMilestone;
            StormDrainAssetControlDropDown.selectedIndex = ModSettings.StormDrainAssetControlOption;
           
            SimulatePollutionCheckBox.isChecked = ModSettings.SimulatePollution;
            IncreaseBuildingPadHeightSlider.value = ModSettings.IncreaseBuildingPadHeight;
            
            BuildingPadIncreaseOptInCheckbox.isChecked = ModSettings.PadHeightIncreaseOptIn;
          
            AdditionalToleranceOnSlopeCheckBox.isChecked = ModSettings.AdditionalToleranceOnSlopes;
         
            GravityDrainageDropDown.selectedIndex = ModSettings.GravityDrainageOption;
            foreach (OptionsItemBase sliderOIB in PublicBuildingsRunoffCoefficientSliders)
            {
                OptionsRunoffCoefficientSlider sliderORCS = sliderOIB as OptionsRunoffCoefficientSlider;
                sliderORCS.slider.value = ModSettings.PublicBuildingsRunoffCoefficients[sliderOIB.uniqueName].Coefficient;
            }
            foreach (OptionsItemBase sliderOIB in PrivateBuildingsRunoffCoefficientSliders)
            {
                OptionsRunoffCoefficientSlider sliderORCS = sliderOIB as OptionsRunoffCoefficientSlider;
                sliderORCS.slider.value = ModSettings.PrivateBuildingsRunoffCoefficients[sliderOIB.uniqueName].Coefficient;
            }
            foreach (OptionsItemBase sliderOIB in PrivateBuildingRunoffModifierSliders)
            {
                OptionsRunoffCoefficientSlider sliderORCS = sliderOIB as OptionsRunoffCoefficientSlider;
                sliderORCS.slider.value = ModSettings.PrivateBuildingRunoffModifiers[sliderOIB.uniqueName].Coefficient;
            }
        }
        private void deleteAllAssets()
        {
            if (Hydraulics.instance.initialized == true && Hydraulics.instance.loaded == true)
                Hydraulics.deleteAllAssets();
        }
        private void cleanUpCycle()
        {
            if (Hydrology.instance.initialized == true && Hydrology.instance.loaded == true)
            {
                Hydrology.instance.cleanUpCycle = true;

            }
        }
        private void endStorm()
        {
            if (Hydrology.instance.initialized == true && Hydrology.instance.loaded == true)
            {
                Hydrology.instance.endStorm = true;

            }
        }

        private void makeItRain()
        {
            if (Hydrology.instance.initialized == true && Hydrology.instance.loaded == true && Hydrology.instance.isRaining == false && Singleton<WeatherManager>.instance.m_targetRain == 0)
            {
                Singleton<WeatherManager>.instance.m_targetFog = 0;
                Singleton<WeatherManager>.instance.m_currentFog = 0;
                if (TargetRainIntensitySlider.value == 0.2f)
                    Singleton<WeatherManager>.instance.m_targetRain = (float)Singleton<SimulationManager>.instance.m_randomizer.Int32(2500, 10000) * 0.0001f;
                else if (TargetRainIntensitySlider.value >= 0.25f && TargetRainIntensitySlider.value <= 1f)
                    Singleton<WeatherManager>.instance.m_targetRain = TargetRainIntensitySlider.value;
            }
        }

        private void onTargetRainIntensityChanged(float val)
        {
            //if (Hydrology.instance.initialized == true && Hydrology.instance.loaded == true)
            //{
                if (val >= 0.25f && val <= 1f)
                {
                    TargetRainIntensitySlider.tooltip = val.ToString();
                    TargetRainIntensitySlider.tooltipBox.Show();
                    TargetRainIntensitySlider.RefreshTooltip();
                } else if (val == 0.2f)
                {
                    TargetRainIntensitySlider.tooltip = "Random";
                    TargetRainIntensitySlider.tooltipBox.Show();
                    TargetRainIntensitySlider.RefreshTooltip();
                }
            /*}
            else {
                TargetRainIntensitySlider.tooltip = "Only Available In Game!";
                TargetRainIntensitySlider.tooltipBox.Show();
                TargetRainIntensitySlider.RefreshTooltip();
            }*/
            
        }

        private void removeWaterSources()
        {
            if (Hydrology.instance.initialized && Hydraulics.instance.initialized && Hydrology.instance.loaded)
            {
                //Hydrology.removeWaterSourcesFromBuildings();
            }
        }

    }
    

}



    
 
