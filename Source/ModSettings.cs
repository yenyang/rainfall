using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rainfall
{
    internal static class ModSettings
    {
        private static int? _difficulty;
        public const int _maxDifficulty = 200;
        public const int _difficultyStep = 10;
        

        public static int Difficulty
        {
            get
            {
                if (!_difficulty.HasValue)
                {
                    _difficulty = PlayerPrefs.GetInt("RF_Difficulty", 100);
                } 
                return _difficulty.Value;
            }
            set
            {
                if (value > 200)
                    throw new ArgumentOutOfRangeException();
                if (value == _difficulty)
                {
                    return;
                }
                PlayerPrefs.SetInt("RF_Difficulty", value);
                _difficulty = value;
            }
        }
        private static int? _refreshRate;
        public const int _maxRefreshRate = 60;
     
        public static int RefreshRate
        {
            get
            {
                if (!_refreshRate.HasValue)
                {
                    _refreshRate = PlayerPrefs.GetInt("RF_RefreshRate", 5);
                }
                return _refreshRate.Value;
            }
            set
            {
                if (value > _maxRefreshRate)
                    throw new ArgumentOutOfRangeException();
                if (value == _refreshRate)
                {
                    return;
                }
                PlayerPrefs.SetInt("RF_RefreshRate", value);
                
                _refreshRate = value;
            }
        }

        private static int? _previousStormOption;
        private const int _defaultPreviousStormOption = 1;
        public static int PreviousStormOption
        {
            get
            {
                if (_previousStormOption == null)
                {
                    _previousStormOption = PlayerPrefs.GetInt("RF_PreviousStormOption", _defaultPreviousStormOption);
                }
                return (int)_previousStormOption;
            }
            set
            {
                PlayerPrefs.SetInt("RF_PreviousStormOption", value);
                _previousStormOption = value;
            }
        }




        private static bool _chirpForecasts;
        private static int? _chirpForecastsInt;
        public static bool ChirpForecasts
        {
            get
            {
                if (_chirpForecastsInt == null)
                {
                    _chirpForecastsInt = PlayerPrefs.GetInt("RF_ChirpForecasts", 1);
                }
                if (_chirpForecastsInt == 1)
                {
                    _chirpForecasts = true;
                } else
                {
                    _chirpForecasts = false;
                }
                return _chirpForecasts;
            }
            set
            {
                if (value == true)
                {
                    _chirpForecastsInt = 1;
                } else
                {
                    _chirpForecastsInt = 0;
                }
                PlayerPrefs.SetInt("RF_ChirpForecasts", (int)_chirpForecastsInt);


            }
        }

        private static bool _chirpRainTweets;
        private static int? _chirpRainTweetsInt;
        public static bool ChirpRainTweets
        {
            get
            {
                if (_chirpRainTweetsInt == null)
                {
                    _chirpRainTweetsInt = PlayerPrefs.GetInt("RF_ChirpRainTweets", 1);
                }
                if (_chirpRainTweetsInt == 1)
                {
                    _chirpRainTweets = true;
                }
                else
                {
                    _chirpRainTweets = false;
                }
                return _chirpRainTweets;
            }
            set
            {
                if (value == true)
                {
                    _chirpRainTweetsInt = 1;
                }
                else
                {
                    _chirpRainTweetsInt = 0;
                }
                PlayerPrefs.SetInt("RF_ChirpRainTweets", (int)_chirpRainTweetsInt);


            }
        }

        private static bool _automaticStormDistribution;
        private static int? _automaticStormDistributionInt;
        public static bool AutomaticStormDistribution
        {
            get
            {
                if (_automaticStormDistributionInt == null)
                {
                    _automaticStormDistributionInt = PlayerPrefs.GetInt("RF_AutomaticStormDistribution", 1);
                }
                if (_automaticStormDistributionInt == 1)
                {
                    _automaticStormDistribution = true;
                }
                else
                {
                    _automaticStormDistribution = false;
                }
                return _automaticStormDistribution;
            }
            set
            {
                if (value == true)
                {
                    _automaticStormDistributionInt = 1;
                }
                else
                {
                    _automaticStormDistributionInt = 0;
                }
                PlayerPrefs.SetInt("RF_AutomaticStormDistribution", (int)_automaticStormDistributionInt);


            }
        }
        public const string DefaultStormDistributionName = "Default (Unmodded) Simulation";
        private static string _stormDistributionName;
        public static string StormDistributionName
        {
            get
            {
                if (_stormDistributionName == null)
                {
                    _stormDistributionName = PlayerPrefs.GetString("RF_StormDistributionName", DefaultStormDistributionName);
                }
                return _stormDistributionName;
            }
            set
            {
                if (value != "")
                {
                    PlayerPrefs.SetString("RF_StormDistributionName", value);
                    _stormDistributionName = value;
                    Debug.Log("[RF]ModSettings RF_StormDistributionName = " + _stormDistributionName);
                }
            }
        }

        public const string DefaultCityName = "Default (Unmodded) Simulation";
        private static string _cityName;
        public static string CityName
        {
            get
            {
                if (_cityName == null)
                {
                    _cityName = PlayerPrefs.GetString("RF_CityName", DefaultCityName);
                }
                if (!DepthDurationFrequencyIO.HasCity(_cityName))
                    _cityName = DefaultCityName;
                return _cityName;
            }
            set
            {
                if (value != "")
                {
                    PlayerPrefs.SetString("RF_CityName", value);
                    _cityName = value;
                    Debug.Log("[RF]ModSettings RF_CityName = " + _cityName);
                  
                }
            }
        }

        public const float DefaultTimeScale = 1f;
        private static float _timeScale;
        public static float TimeScale
        {
            get
            {
                if (_timeScale == 0f)
                {
                    _timeScale = PlayerPrefs.GetFloat("RF_TimeScale", DefaultTimeScale);

                }
                return _timeScale;
            }
            set
            {
                PlayerPrefs.SetFloat("RF_TimeScale", value);
                _timeScale = value;
            }
        }

        private static int? _userMinimumStormDuration;
        public const float _maxStormDuration = 1440f;
        public const float _minStormDuration = 30f;
        public const float _stormTimeStep = 3f;
        public const float _stormDurationStep = 15f;


        public static int MinimumStormDuration
        {
            get
            {
                if (!_userMinimumStormDuration.HasValue)
                {
                    _userMinimumStormDuration = PlayerPrefs.GetInt("RF_MinimumStormDuration", (int)_minStormDuration);
                }
                return _userMinimumStormDuration.Value;
            }
            set
            {
                if (value > _maxStormDuration || value < _minStormDuration)
                    throw new ArgumentOutOfRangeException();
                if (value == _userMinimumStormDuration)
                {
                    return;
                }
                PlayerPrefs.SetInt("RF_MinimumStormDuration", value);
                _userMinimumStormDuration = value;
            }
        }

        private static int? _userMaximumStormDuration;

        public static int MaximumStormDuration
        {
            get
            {
                if (!_userMaximumStormDuration.HasValue)
                {
                    _userMaximumStormDuration = PlayerPrefs.GetInt("RF_MaximumStormDuration", (int)_maxStormDuration);
                }
                return _userMaximumStormDuration.Value;
            }
            set
            {
                if (value > _maxStormDuration || value < _minStormDuration)
                    throw new ArgumentOutOfRangeException();
                if (value == _userMaximumStormDuration)
                {
                    return;
                }
                PlayerPrefs.SetInt("RF_MaximumStormDuration", value);
                _userMaximumStormDuration = value;
            }
        }

        private static int? _userMaximumStormIntensitySlider;
        public const float _maxStormIntensity = 25f;
        public const float _minStormIntensity = 1f;
        public const float _stormIntenistyStep = 1f;


        public static int MaximumStormIntensity
        {
            get
            {
                if (!_userMaximumStormIntensitySlider.HasValue)
                {
                    _userMaximumStormIntensitySlider = PlayerPrefs.GetInt("RF_MaximumStormIntensity", (int)_maxStormIntensity);
                }
                return _userMaximumStormIntensitySlider.Value;
            }
            set
            {
                if (value > _maxStormIntensity || value < _minStormIntensity)
                    throw new ArgumentOutOfRangeException();
                if (value == _userMaximumStormIntensitySlider)
                {
                    return;
                }
                PlayerPrefs.SetInt("RF_MaximumStormIntensity", value);
                _userMaximumStormIntensitySlider = value;
            }
        }

        public struct runoffStruct
        {
            private string name;
            private float coefficient;

            public runoffStruct(string name, float coefficient)
            {
                this.name = name;
                this.coefficient = coefficient;
            }
            public string Name
            {
                get { return name; }
                set { name = value; }
            }
            public float Coefficient
            {
                get { return coefficient; }
                set { coefficient = value; }
            }
        }

        public static Dictionary<string, float> DefaultRunoffCoefficients = new Dictionary<string, float>()
        {
            {"CemetaryAI", 0.2f }, {"ParkAI", 0.2f }, {"LandfillSiteAI", 0.1f }, {"FirestationAI" , 0.4f }, {"PoliceStationAI", 0.4f },
            {"HospitalAI", 0.6f }, {"PowerPlantAI", 0.8f }, {"DepotAI", 0.4f }, {"CargoStationAI", 0.5f }, {"SaunaAI", 0.3f }, {"SchoolAI", 0.4f },
            {"OtherPlayerBuildingAI", 0.5f }, {"LowDensityResidentialBuildingAI", 0.35f }, {"LowDensityCommercialBuildingAI", 0.5f },
            {"HighDensityResidentialBuildingAI", 0.6f }, {"HighDensityCommercialBuildingAI", 0.7f }, {"FarmingIndustrialBuildingAI", 0.2f },
            {"ForestryIndustrialBuildingAI", 0.2f }, {"OreIndustrialBuildingAI", 0.85f }, {"OilIndustryBuildingAI", 0.9f }, {"GenericIndustryBuildingAI", 0.8f },
            {"OtherPrivateBuildingAI", 0.5f }
        };

        public static Dictionary<string, runoffStruct> PublicBuildingsRunoffCoefficients = new Dictionary<string, runoffStruct>() {
            { "CemetaryAI", new runoffStruct( "Cemetaries" , PlayerPrefs.GetFloat("RF_CemetaryAI", DefaultRunoffCoefficients["CemetaryAI"])) },
            { "ParkAI", new runoffStruct( "Parks" , PlayerPrefs.GetFloat("RF_ParkAI", DefaultRunoffCoefficients["ParkAI"])) },
            { "LandfillSiteAI", new runoffStruct("Landfills", PlayerPrefs.GetFloat("RF_LandfillSiteAI", DefaultRunoffCoefficients["LandfillSiteAI"])) },
            {  "FirestationAI", new runoffStruct( "Fire Stations",  PlayerPrefs.GetFloat("RF_FirestationAI", DefaultRunoffCoefficients["FirestationAI"])) },
            {  "PoliceStationAI", new runoffStruct( "Police Stations",  PlayerPrefs.GetFloat("RF_PoliceStationAI", DefaultRunoffCoefficients["PoliceStationAI"])) },
            {  "HospitalAI", new runoffStruct( "Hospitals" , PlayerPrefs.GetFloat("RF_HospitalAI", DefaultRunoffCoefficients["HospitalAI"])) },
            {  "PowerPlantAI", new runoffStruct( "Power Plants",  PlayerPrefs.GetFloat("RF_PowerPlanAI", DefaultRunoffCoefficients["PowerPlantAI"])) },
            {  "DepotAI", new runoffStruct( "Depots",  PlayerPrefs.GetFloat("RF_DepotAI", DefaultRunoffCoefficients["DepotAI"])) },
            {  "CargoStationAI", new runoffStruct( "Cargo Stations",  PlayerPrefs.GetFloat("RF_CargoStationAI", DefaultRunoffCoefficients["CargoStationAI"]))},
            {  "SaunaAI", new runoffStruct( "Saunas",  PlayerPrefs.GetFloat("RF_SaunaAI", DefaultRunoffCoefficients["SaunaAI"])) },
            {  "SchoolAI", new runoffStruct( "Schools",  PlayerPrefs.GetFloat("RF_SchoolAI", DefaultRunoffCoefficients["SchoolAI"])) },
            {  "OtherPlayerBuildingAI", new runoffStruct( "Other Player Buildings",  PlayerPrefs.GetFloat("RF_OtherPlayerBuildingAI", DefaultRunoffCoefficients["OtherPlayerBuildingAI"])) }

        };
        public static Dictionary<string, runoffStruct> PrivateBuildingsRunoffCoefficients = new Dictionary<string, runoffStruct>()
        {
            {  "LowDensityResidentialBuildingAI", new runoffStruct( "Low Density Residential",  PlayerPrefs.GetFloat("RF_LowDensityResidentialBuildingAI", DefaultRunoffCoefficients["LowDensityResidentialBuildingAI"])) },
            {  "LowDensityCommercialBuildingAI", new runoffStruct( "Low Density Commercial",  PlayerPrefs.GetFloat("RF_LowDensityCommercialBuildingAI", DefaultRunoffCoefficients["LowDensityCommercialBuildingAI"])) },
            {  "HighDensityResidentialBuildingAI", new runoffStruct( "High Density Residentaial",  PlayerPrefs.GetFloat("RF_HighDensityResidentialBuildingAI", DefaultRunoffCoefficients["HighDensityResidentialBuildingAI"])) },
            {  "HighDensityCommercialBuildingAI", new runoffStruct( "High Density Commercial",  PlayerPrefs.GetFloat("RF_HighDensityCommercialBuildingAI", DefaultRunoffCoefficients["HighDensityCommercialBuildingAI"])) },
            {  "FarmingIndustrialBuildingAI", new runoffStruct( "Farming Industry",  PlayerPrefs.GetFloat("RF_FamringIndustrialBuildingAI", DefaultRunoffCoefficients["FarmingIndustrialBuildingAI"])) },
            {  "ForestryIndustrialBuildingAI", new runoffStruct( "Foresty Industry", PlayerPrefs.GetFloat("RF_ForestryIndustrialBuildingAI",  DefaultRunoffCoefficients["ForestryIndustrialBuildingAI"])) },
            {  "OreIndustrialBuildingAI", new runoffStruct( "Ore Industry",  PlayerPrefs.GetFloat("RF_OreIndustrialBuildingAI", DefaultRunoffCoefficients["OreIndustrialBuildingAI"])) },
            {  "OilIndustryBuildingAI", new runoffStruct( "Oil Industry",  PlayerPrefs.GetFloat("RF_OilIndustryBuildingAI", DefaultRunoffCoefficients["OilIndustryBuildingAI"])) },
            {  "GenericIndustryBuildingAI", new runoffStruct( "Generic Industry",  PlayerPrefs.GetFloat("RF_GenericIndustryBuildingAI", DefaultRunoffCoefficients["GenericIndustryBuildingAI"])) },
            {  "OtherPrivateBuildingAI", new runoffStruct( "Other Zoned Buildings", PlayerPrefs.GetFloat("RF_OtherPrivateBuildingAI",  DefaultRunoffCoefficients["OtherPrivateBuildingAI"])) }
        };

        public static bool setRunoffCoefficient(string name, float value)
        {
            if (PublicBuildingsRunoffCoefficients.ContainsKey(name))
            {
                runoffStruct tempRunoffCoefficient = PublicBuildingsRunoffCoefficients[name];
                tempRunoffCoefficient.Coefficient = value;
                PublicBuildingsRunoffCoefficients[name] = tempRunoffCoefficient;
                PlayerPrefs.SetFloat("RF_"+name, value);
                return true;
            } else if (PrivateBuildingsRunoffCoefficients.ContainsKey(name)) {
                runoffStruct tempRunoffCoefficient = PrivateBuildingsRunoffCoefficients[name];
                tempRunoffCoefficient.Coefficient = value;
                PrivateBuildingsRunoffCoefficients[name] = tempRunoffCoefficient;
                PlayerPrefs.SetFloat("RF_"+name, value);
                return true;
            }
            return false;
        }

        public static void resetModSettings()
        {
            ModSettings.Difficulty = 100;
            ModSettings.RefreshRate = 5;
            ModSettings.PreviousStormOption = _defaultPreviousStormOption;
            ModSettings.TimeScale = DefaultTimeScale;
            ModSettings.ChirpForecasts = true;
            ModSettings.ChirpRainTweets = true;
            ModSettings.AutomaticStormDistribution = true;
            ModSettings.StormDistributionName = DefaultStormDistributionName;
            ModSettings.CityName = DefaultCityName;
            ModSettings.MinimumStormDuration = (int)ModSettings._minStormDuration;
            ModSettings.MaximumStormDuration = (int)ModSettings._maxStormDuration;
            ModSettings.MaximumStormIntensity = (int)ModSettings._maxStormIntensity;
            
            foreach(KeyValuePair<string, float> pair in DefaultRunoffCoefficients)
            {
                setRunoffCoefficient(pair.Key, DefaultRunoffCoefficients[pair.Key]);
            }
            

        }

    }
}
