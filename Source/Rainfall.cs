
using ICities;
using System.Collections.Generic;
using ColossalFramework.UI;
using System.Reflection;
using UnityEngine;
using ColossalFramework;
using System;
using Rainfall.UI;

namespace Rainfall
{
    public class RainfallMod : IUserMod
    {

        public string Name { get { return "Rainfall"; } }
        public string Description { get { return "Simulates urban runoff from buildings and Includes a Storm Drain AI to manage Storm Drain Assets. By [SSU]yenyang"; } }
        public UISlider DifficultySlider;
        public UISlider RefreshRateSlider;
        public UICheckBox ChirpForecastCheckBox;
        public UICheckBox ChirpRainTweetsCheckBox;
        public UIDropDown CityNameDropDown;
        public UIDropDown StormDistributionDropDown;
        public UICheckBox AutomaticallyPickStormDistributionCheckBox;
        public UIDropDown TimeScaleDropDown;
        private StormDistributionIO customStormDistribution;
        private DepthDurationFrequencyIO customDepthDurationFrequency;
        private string[] stormDistributionNames;
        private string[] cityNames;
        private List<float> TimeScalesFloat = new List<float> { 0.125f, 0.25f, 0.33f, 0.5f, 1f, 2f, 4f, 8f, 16f };
        private string[] TimeScalesString = new string[] { "0.125", "0.25", "0.33", "0.5", "1", "2", "4", "8", "16" };
        public UISlider MinimumStormDurationSlider;
        public UISlider MaximumStormDurationSlider;
        public UISlider MaximumStormIntensitySlider;
        public UIButton ResetAllButton;
        public UIDropDown PreviousStormDropDown;
        private string[] previousStormDropDownOptions = new string[] { "Start New Storm", "Continue from First Half", "Continue from Second Half", "End Previous Storm" };
        //public UICheckBox DistrictControlCheckBox;
        private string[] StormDrainAssetControlDropDownOptions = new string[] { "No Control", "District Control", "ID Control", "District Control with ID Override" };
        private UIDropDown StormDrainAssetControlDropDown;
        public UISlider BuildingFloodingToleranceSlider;
        public UISlider BuildingFloodedToleranceSlider;
        public UISlider RoadwayFloodingToleranceSlider;
        public UISlider RoadwayFloodedToleranceSlider;
        public UISlider PedestrianPathFloodingToleranceSlider;
        public UISlider PedestrianPathFloodedToleranceSlider;
        public UICheckBox FreezeLandvaluesCheckBox;
        //public UICheckBox ImprovedInletMechanicsCheckBox;
        public UICheckBox PreventRainBeforeMilestoneCheckBox;
        //public UICheckBox EasyModeCheckBox;
        public UICheckBox SimulatePollutionCheckBox;
        private string[] gravityDrainageDropDownOptions = new string[] { "Ignore Gravity", "Simplified (Alpha) Gravity", "Improved (Beta) Gravity" };
        public UIDropDown GravityDrainageDropDown;
        public UIButton DeleteAssetsButton;
        public UIButton CleanUpCycleButton;
        public UIButton EndStormButton;
        public List<OptionsItemBase> PublicBuildingsRunoffCoefficientSliders;
        public List<OptionsItemBase> PrivateBuildingsRunoffCoefficientSliders;
        

        public void OnSettingsUI(UIHelperBase helper)
        {
            customStormDistribution = new StormDistributionIO();
            customDepthDurationFrequency = new DepthDurationFrequencyIO();


            UIHelperBase group = helper.AddGroup("General Options");
            ChirpForecastCheckBox = group.AddCheckbox("#RainForecast Chirps", ModSettings.ChirpForecasts, OnChirpForecastCheckBoxChanged) as UICheckBox;
            ChirpRainTweetsCheckBox = group.AddCheckbox("#Rainfall Chirps", ModSettings.ChirpRainTweets, OnChirpRainTweetsCheckBoxChanged) as UICheckBox;
            //EasyModeCheckBox = group.AddCheckbox("Easy/Casual Mode", ModSettings.EasyMode, OnEasyModeCheckBoxChanged) as UICheckBox;
            //DistrictControlCheckBox = group.AddCheckbox("District Control", ModSettings.DistrictControl, OnDistrictControlCheckBoxChanged) as UICheckBox;
            StormDrainAssetControlDropDown = group.AddDropdown("Storm Drain Asset Control", StormDrainAssetControlDropDownOptions, ModSettings.StormDrainAssetControlOption, OnStormDrainAssetControlDropDownChanged) as UIDropDown;
            SimulatePollutionCheckBox = group.AddCheckbox("Simulate Pollution", ModSettings.SimulatePollution, OnSimulatePollutionCheckBoxChanged) as UICheckBox;
            //ImprovedInletMechanicsCheckBox = group.AddCheckbox("Improved Inlet Mechanics", ModSettings.ImprovedInletMechanics, OnImprovedInletMechanicsCheckBoxChanged) as UICheckBox;
            PreventRainBeforeMilestoneCheckBox = group.AddCheckbox("Prevent Rain Before Milestone", ModSettings.PreventRainBeforeMilestone, OnPreventRainBeforeMilestoneCheckBoxChanged) as UICheckBox;

            FreezeLandvaluesCheckBox = group.AddCheckbox("Prevent Building Upgrades from Increased Landvalue due to Rainwater", ModSettings.FreezeLandvalues, OnFreezeLandvaluesCheckBoxChanged) as UICheckBox;
            PreviousStormDropDown = group.AddDropdown("Previous Storm Options (IE Loading after you saved during a storm)", previousStormDropDownOptions, ModSettings.PreviousStormOption, OnPreviousStormOptionChanged) as UIDropDown;
            GravityDrainageDropDown = group.AddDropdown("Gravity Drainage Options", gravityDrainageDropDownOptions, ModSettings.GravityDrainageOption, OnGravityDrainageOptionChanged) as UIDropDown;
            DifficultySlider = group.AddSlider("Difficulty", 0f, (float)ModSettings._maxDifficulty, (float)ModSettings._difficultyStep, (float)ModSettings.Difficulty, OnDifficultyChanged) as UISlider;
            DifficultySlider.tooltip = ModSettings.Difficulty.ToString() + "%";
            DifficultySlider.width += 100;
            RefreshRateSlider = group.AddSlider("Refresh Rate", 1f, (float)ModSettings._maxRefreshRate, 1f, (float)ModSettings.RefreshRate, OnRefreshRateChanged) as UISlider;
            RefreshRateSlider.tooltip = ModSettings.RefreshRate.ToString() + " seconds";
            RefreshRateSlider.width += 100;
            BuildingFloodingToleranceSlider = group.AddSlider("Building Flooding Tol.", (float)ModSettings._minFloodTolerance, (float)ModSettings._maxFloodTolerance, (float)ModSettings._floodToleranceStep, (float)ModSettings.BuildingFloodingTolerance, OnBuildingFloodingToleranceChanged) as UISlider;
            BuildingFloodingToleranceSlider.tooltip = ((float)ModSettings.BuildingFloodingTolerance / 100f).ToString() + " units";
            BuildingFloodingToleranceSlider.width += 100;
            BuildingFloodedToleranceSlider = group.AddSlider("Building Flooded Tol.", (float)ModSettings._minFloodTolerance, (float)ModSettings._maxFloodTolerance, (float)ModSettings._floodToleranceStep, (float)ModSettings.BuildingFloodedTolerance, OnBuildingFloodedToleranceChanged) as UISlider;
            BuildingFloodedToleranceSlider.tooltip = ((float)ModSettings.BuildingFloodedTolerance/100f).ToString() + " units";
            BuildingFloodedToleranceSlider.width += 100;
            RoadwayFloodingToleranceSlider = group.AddSlider("Roadway Flooding Tol.", (float)ModSettings._minFloodTolerance, (float)ModSettings._maxFloodTolerance, (float)ModSettings._floodToleranceStep, (float)ModSettings.RoadwayFloodingTolerance, OnRoadwayFloodingToleranceChanged) as UISlider;
            RoadwayFloodingToleranceSlider.tooltip = ((float)ModSettings.RoadwayFloodingTolerance / 100f).ToString() + " units";
            RoadwayFloodingToleranceSlider.width += 100;
            RoadwayFloodedToleranceSlider = group.AddSlider("Roadway Flooded Tol.", (float)ModSettings._minFloodTolerance, (float)ModSettings._maxFloodTolerance, (float)ModSettings._floodToleranceStep, (float)ModSettings.RoadwayFloodedTolerance, OnRoadwayFloodedToleranceChanged) as UISlider;
            RoadwayFloodedToleranceSlider.tooltip = ((float)ModSettings.RoadwayFloodedTolerance / 100f).ToString() + " units";
            RoadwayFloodedToleranceSlider.width += 100;

            PedestrianPathFloodingToleranceSlider = group.AddSlider("Ped. Path Flooding Tol.", (float)ModSettings._minFloodTolerance, (float)ModSettings._maxFloodTolerance, (float)ModSettings._floodToleranceStep, (float)ModSettings.PedestrianPathFloodingTolerance, OnPedestrianPathFloodingToleranceChanged) as UISlider;
            PedestrianPathFloodingToleranceSlider.tooltip = ((float)ModSettings.PedestrianPathFloodingTolerance / 100f).ToString() + " units";
            PedestrianPathFloodingToleranceSlider.width += 100;
            PedestrianPathFloodedToleranceSlider = group.AddSlider("Ped. Path Flooded Tol.", (float)ModSettings._minFloodTolerance, (float)ModSettings._maxFloodTolerance, (float)ModSettings._floodToleranceStep, (float)ModSettings.PedestrianPathFloodedTolerance, OnPedestrianPathFloodedToleranceChanged) as UISlider;
            PedestrianPathFloodedToleranceSlider.tooltip = ((float)ModSettings.PedestrianPathFloodedTolerance / 100f).ToString() + " units";
            PedestrianPathFloodedToleranceSlider.width += 100;
            UIHelperBase StormWaterSimulationGroup = helper.AddGroup("Stormwater Simulation Settings");
            AutomaticallyPickStormDistributionCheckBox = StormWaterSimulationGroup.AddCheckbox("Automatically Select Storm Distribution", ModSettings.AutomaticStormDistribution, OnAutomaticStormDistributionCheckBoxChanged) as UICheckBox;
            cityNames = getCityNamesDropDownOptions();
            CityNameDropDown = StormWaterSimulationGroup.AddDropdown("City used for Depth-Duration-Frequency Data", cityNames, getCityNameIndex(ModSettings.CityName), OnCityNameDropDownChanged) as UIDropDown;
            stormDistributionNames = getStormDistributionDropDownOptions();
            StormDistributionDropDown = StormWaterSimulationGroup.AddDropdown("Storm Distribution", stormDistributionNames, getStormDistributionIndex(ModSettings.StormDistributionName), OnStormDistributionDropDownChanged) as UIDropDown;
            TimeScaleDropDown = StormWaterSimulationGroup.AddDropdown("Storm Simulation Time Scale Multiplier", TimeScalesString, getTimeScaleIndex(ModSettings.TimeScale), OnTimeScaleChanged) as UIDropDown;
            MinimumStormDurationSlider = StormWaterSimulationGroup.AddSlider("Min. Storm Duration", ModSettings._minStormDuration, ModSettings._maxStormDuration, ModSettings._stormDurationStep, (float)ModSettings.MinimumStormDuration, onMinimumStormDurationChanged) as UISlider;
            MinimumStormDurationSlider.tooltip = convertMinToHourMin((float)ModSettings.MinimumStormDuration);
            MinimumStormDurationSlider.width += 100;
            MaximumStormDurationSlider = StormWaterSimulationGroup.AddSlider("Max. Storm Duration", ModSettings._minStormDuration, ModSettings._maxStormDuration, ModSettings._stormDurationStep, (float)ModSettings.MaximumStormDuration, onMaximumStormDurationChanged) as UISlider;

            MaximumStormDurationSlider.tooltip = convertMinToHourMin((float)ModSettings.MaximumStormDuration);
            MaximumStormDurationSlider.width += 100;
            MaximumStormIntensitySlider = StormWaterSimulationGroup.AddSlider("Max. Intensity", ModSettings._minStormIntensity, ModSettings._maxStormIntensity, ModSettings._stormIntenistyStep, (float)ModSettings.MaximumStormIntensity, onMaximumStormIntensityChanged) as UISlider;
            MaximumStormIntensitySlider.tooltip = ModSettings.MaximumStormIntensity.ToString() + " Units/Hour";
            MaximumStormIntensitySlider.width += 100;
            UIHelperBase PublicBuildingsRunOffCoefficientsGroup = helper.AddGroup("Public Buildings Runoff Coefficient Settings");
            initializeRunoffCoefficientSliders();
            createRunoffCoefficientSliders(PublicBuildingsRunOffCoefficientsGroup, PublicBuildingsRunoffCoefficientSliders);

            UIHelperBase PrivateBuildingsRunoffCoefficientsGroup = helper.AddGroup("Private Buildings Runoff Coefficient Settings");
            createRunoffCoefficientSliders(PrivateBuildingsRunoffCoefficientsGroup, PrivateBuildingsRunoffCoefficientSliders);

            UIHelperBase ResetGroup = helper.AddGroup("Reset");
            ResetAllButton = ResetGroup.AddButton("Reset All", resetAllSettings) as UIButton;

            UIHelperBase SafelyRemoveRainfallGroup = helper.AddGroup("Safely Remove Rainfall");
            DeleteAssetsButton = SafelyRemoveRainfallGroup.AddButton("Delete Rainfall Assets", deleteAllAssets) as UIButton;
            CleanUpCycleButton = SafelyRemoveRainfallGroup.AddButton("Clean Up Cycle", cleanUpCycle) as UIButton;
            EndStormButton = SafelyRemoveRainfallGroup.AddButton("End Storm", endStorm) as UIButton;
        }



        private void OnDifficultyChanged(float val)
        {
            ModSettings.Difficulty = (int)val;
            DifficultySlider.tooltip = ModSettings.Difficulty.ToString() + "%";
            DifficultySlider.tooltipBox.Show();
            DifficultySlider.RefreshTooltip();
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
        private void OnSimulatePollutionCheckBoxChanged(bool val)
        {
            ModSettings.SimulatePollution = (bool)val;
        }
        /*private void OnImprovedInletMechanicsCheckBoxChanged(bool val)
        {
            ModSettings.ImprovedInletMechanics = (bool)val;
        }*/
        private void OnPreventRainBeforeMilestoneCheckBoxChanged(bool val)
        {
            ModSettings.PreventRainBeforeMilestone = (bool)val;
        }
        /*
        private void OnEasyModeCheckBoxChanged(bool val)
        {
            ModSettings.EasyMode = (bool)val;
        }*/
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
        private void OnStormDistributionDropDownChanged(int sel)
        {
            ModSettings.StormDistributionName = stormDistributionNames[sel];
            if (sel == 0)
            {
                CityNameDropDown.selectedIndex = 0;
                ModSettings.CityName = cityNames[0];
            }
            if (CityNameDropDown.selectedIndex == 0 )
            {
                foreach(string city in cityNames)
                {
                    if (DepthDurationFrequencyIO.GetStormDistributionForCity(city) == ModSettings.StormDistributionName && DepthDurationFrequencyIO.GetStormDistributionForCity(city) != "")
                    {
                        ModSettings.CityName = city;
                        CityNameDropDown.selectedIndex = getCityNameIndex(city);
                    }
                }
            }

        }
        private void OnStormDrainAssetControlDropDownChanged(int sel)
        {
            ModSettings.StormDrainAssetControlOption = (int)sel;
        }

        private void OnAutomaticStormDistributionCheckBoxChanged(bool val)
        {
            ModSettings.AutomaticStormDistribution = (bool)val;
        }
        private string[] getStormDistributionDropDownOptions()
        {
            string[] modStormDistributionNames = StormDistributionIO.GetStormDistributionNames();
            string[] fullStormDistributionNames = new string[modStormDistributionNames.Length + 1];
            fullStormDistributionNames[0] = ModSettings.UnmoddedStormDistributionName;
            Array.Copy(modStormDistributionNames, 0, fullStormDistributionNames, 1, modStormDistributionNames.Length);
            return fullStormDistributionNames;
        }
        private int getStormDistributionIndex(string curveName)
        {
            for (int i=0; i<= stormDistributionNames.GetUpperBound(0); i++)
            {
                if (stormDistributionNames[i] == curveName)
                {
                    return i;
                }
            }
            return 0;
        }
        private void OnCityNameDropDownChanged(int sel)
        {
            ModSettings.CityName = cityNames[sel];
            if (sel != 0 && ModSettings.AutomaticStormDistribution)
            {
                if (DepthDurationFrequencyIO.GetStormDistributionForCity(ModSettings.CityName) != "")
                {
                    ModSettings.StormDistributionName = DepthDurationFrequencyIO.GetStormDistributionForCity(ModSettings.CityName);
                    StormDistributionDropDown.selectedIndex = getStormDistributionIndex(ModSettings.StormDistributionName);
                }
            }
                
            if (sel == 0)
                StormDistributionDropDown.selectedIndex = 0;
        }
        private string[] getCityNamesDropDownOptions()
        {
            try {
                string[] modCityNames = DepthDurationFrequencyIO.GetCityNames();
                string[] fullCityNames = new string[modCityNames.Length + 1];
                fullCityNames[0] = ModSettings.UnmoddedCityName;
                Array.Copy(modCityNames, 0, fullCityNames, 1, modCityNames.Length);
                return fullCityNames;
            } catch (Exception e)
            {
                Debug.Log("[RF]GetCityNamesDropDownOptions Could not get city names encountered exception " + e);
                string[] fullCityNames = new string[1];
                fullCityNames[0] = ModSettings.UnmoddedCityName;
                return fullCityNames;
            }
        }
        private int getCityNameIndex(string cityName)
        {
            for (int i = 0; i <= cityNames.GetUpperBound(0); i++)
            {
                if (cityNames[i] == cityName)
                {
                    return i;
                }
            }
            return 0;
        }
        private int getTimeScaleIndex(float timeScale)
        {
            for (int i = 0; i < TimeScalesFloat.Count-1; i++)
            {
                if (TimeScalesFloat[i] == timeScale)
                {
                    return i;
                }
            }
            return 4;
        }
        private void OnTimeScaleChanged(int sel)
        {
            ModSettings.TimeScale = TimeScalesFloat[sel];
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
        }
        private void createRunoffCoefficientSliders(UIHelperBase helper, List<OptionsItemBase> sliderList)
        {
            foreach(OptionsItemBase slider in sliderList)
            {
                slider.enabled = true;
                slider.Create(helper);
            }
        }
        /*private void OnDistrictControlCheckBoxChanged(bool val)
        {
            ModSettings.DistrictControl = (bool)val;
        }*/

        private void resetAllSettings()
        {
            ModSettings.resetModSettings();
            DifficultySlider.value = ModSettings.Difficulty;
            RefreshRateSlider.value = ModSettings.RefreshRate;
            ChirpForecastCheckBox.isChecked = ModSettings.ChirpForecasts;
            CityNameDropDown.selectedIndex = getCityNameIndex(ModSettings.CityName);
            StormDistributionDropDown.selectedIndex = getStormDistributionIndex(ModSettings.StormDistributionName);
            TimeScaleDropDown.selectedIndex = getTimeScaleIndex(ModSettings.TimeScale);
            MinimumStormDurationSlider.value = ModSettings.MinimumStormDuration;
            MaximumStormDurationSlider.value = ModSettings.MaximumStormDuration;
            MaximumStormIntensitySlider.value = ModSettings.MaximumStormIntensity;
            //DistrictControlCheckBox.isChecked = ModSettings.DistrictControl;
            BuildingFloodedToleranceSlider.value = ModSettings._defaultBuildingFloodedTolerance;
            BuildingFloodingToleranceSlider.value = ModSettings._defaultBuildingFloodingTolerance;
            RoadwayFloodedToleranceSlider.value = ModSettings._defaultRoadwayFloodedTolerance;
            RoadwayFloodingToleranceSlider.value = ModSettings._defaultRoadwayFloodingTolerance;
            PedestrianPathFloodedToleranceSlider.value = ModSettings._defaultRoadwayFloodedTolerance;
            PedestrianPathFloodingToleranceSlider.value = ModSettings._defaultRoadwayFloodingTolerance;
            FreezeLandvaluesCheckBox.isChecked = ModSettings.FreezeLandvalues;
            //ImprovedInletMechanicsCheckBox.isChecked = ModSettings.ImprovedInletMechanics;
            PreventRainBeforeMilestoneCheckBox.isChecked = ModSettings.PreventRainBeforeMilestone;
            StormDistributionDropDown.selectedIndex = ModSettings.StormDrainAssetControlOption;
            //EasyModeCheckBox.isChecked = ModSettings.EasyMode;
            SimulatePollutionCheckBox.isChecked = ModSettings.SimulatePollution;
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

    }
    

}



    
 
