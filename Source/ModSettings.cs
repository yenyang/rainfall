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
        public const string UnmoddedStormDistributionName = "Unmodded Simulation (Not Recommended)";
        public const string DefaultStormDistributionName = "Type II - Noncoastal US";
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
        public const string UnmoddedCityName = "Unmodded Simulation (Not Recommended)";
        public const string DefaultCityName = "Boise. Idaho. USA";
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
            {"NaturalDrainageAI", 0.2f }, {"OtherPrivateBuildingAI", 0.5f }
        };

        public static Dictionary<string, runoffStruct> PublicBuildingsRunoffCoefficients = new Dictionary<string, runoffStruct>() {
            { "CemetaryAI", new runoffStruct( "Cemetaries" , PlayerPrefs.GetFloat("RF_CemetaryAI", DefaultRunoffCoefficients["CemetaryAI"])) },
            { "ParkAI", new runoffStruct( "Parks" , PlayerPrefs.GetFloat("RF_ParkAI", DefaultRunoffCoefficients["ParkAI"])) },
            { "NaturalDrainageAI", new runoffStruct("Natural Drainage Assets", PlayerPrefs.GetFloat("RF_NaturalDrainageAI", DefaultRunoffCoefficients["NaturalDrainageAI"])) },
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
   
        /*
        private static bool _districtControl;
        private static int? _districtControlInt;
        public static bool DistrictControl
        {
            get
            {
                if (_districtControlInt == null)
                {
                    _districtControlInt = PlayerPrefs.GetInt("RF_DistrictControl", 1);
                }
                if (_districtControlInt == 1)
                {
                    _districtControl = true;
                }
                else
                {
                    _districtControl = false;
                }
                return _districtControl;
            }
            set
            {
                if (value == true)
                {
                    _districtControlInt = 1;
                }
                else
                {
                    _districtControlInt = 0;
                }
                PlayerPrefs.SetInt("RF_DistrictControl", (int)_districtControlInt);


            }
        }
        */
        private static int? _buildingFloodingTolerance;
        public const int _defaultBuildingFloodingTolerance = 50;
        public const int _minFloodTolerance = 0;
        public const int _maxFloodTolerance = 200;
        public const int _floodToleranceStep = 10;

        public static int BuildingFloodingTolerance
        {
            get
            {
                if (!_buildingFloodingTolerance.HasValue)
                {
                    _buildingFloodingTolerance = PlayerPrefs.GetInt("RF_BuildingFloodingTolerance", (int)_defaultBuildingFloodingTolerance);
                }
                return _buildingFloodingTolerance.Value;
            }
            set
            {
                if (value >= _buildingFloodedTolerance || value < _minFloodTolerance)
                    throw new ArgumentOutOfRangeException();
                if (value == _buildingFloodingTolerance)
                {
                    return;
                }
                PlayerPrefs.SetInt("RF_BuildingFloodingTolerance", value);
                _buildingFloodingTolerance = value;
            }
        }

        private static int? _buildingFloodedTolerance;
        public const int _defaultBuildingFloodedTolerance = 100;

        public static int BuildingFloodedTolerance
        {
            get
            {
                if (!_buildingFloodedTolerance.HasValue)
                {
                    _buildingFloodedTolerance = PlayerPrefs.GetInt("RF_BuildingFloodedTolerance", (int)_defaultBuildingFloodedTolerance);
                }
                return _buildingFloodedTolerance.Value;
            }
            set
            {
                if (value > _maxFloodTolerance || value <= _buildingFloodingTolerance)
                    throw new ArgumentOutOfRangeException();
                if (value == _buildingFloodedTolerance)
                {
                    return;
                }
                PlayerPrefs.SetInt("RF_BuildingFloodedTolerance", value);
                _buildingFloodedTolerance = value;
            }
        }

        private static int? _roadwayFloodingTolerance;
        public const int _defaultRoadwayFloodingTolerance = 50;

        public static int RoadwayFloodingTolerance
        {
            get
            {
                if (!_roadwayFloodingTolerance.HasValue)
                {
                    _roadwayFloodingTolerance = PlayerPrefs.GetInt("RF_RoadwayFloodingTolerance", (int)_defaultRoadwayFloodingTolerance);
                }
                return _roadwayFloodingTolerance.Value;
            }
            set
            {
                if (value >= _roadwayFloodedTolerance || value < _minFloodTolerance)
                    throw new ArgumentOutOfRangeException();
                if (value == _roadwayFloodingTolerance)
                {
                    return;
                }
                PlayerPrefs.SetInt("RF_RoadwayFloodingTolerance", value);
                _roadwayFloodingTolerance = value;
            }
        }

        private static int? _roadwayFloodedTolerance;
        public const int _defaultRoadwayFloodedTolerance = 100;

        public static int RoadwayFloodedTolerance
        {
            get
            {
                if (!_roadwayFloodedTolerance.HasValue)
                {
                    _roadwayFloodedTolerance = PlayerPrefs.GetInt("RF_RoadwayFloodedTolerance", (int)_defaultRoadwayFloodedTolerance);
                }
                return _roadwayFloodedTolerance.Value;
            }
            set
            {
                if (value > _maxFloodTolerance || value <= _roadwayFloodingTolerance)
                    throw new ArgumentOutOfRangeException();
                if (value == _roadwayFloodedTolerance)
                {
                    return;
                }
                PlayerPrefs.SetInt("RF_RoadwayFloodedTolerance", value);
                _roadwayFloodedTolerance = value;
            }
        }

        private static int? _pedestrianPathFloodingTolerance;
        public const int _defaultPedestrianPathFloodingTolerance = 50;

        public static int PedestrianPathFloodingTolerance
        {
            get
            {
                if (!_pedestrianPathFloodingTolerance.HasValue)
                {
                    _pedestrianPathFloodingTolerance = PlayerPrefs.GetInt("RF_PedestrianPathFloodingTolerance", (int)_defaultPedestrianPathFloodingTolerance);
                }
                return _pedestrianPathFloodingTolerance.Value;
            }
            set
            {
                if (value >= _pedestrianPathFloodedTolerance || value < _minFloodTolerance)
                    throw new ArgumentOutOfRangeException();
                if (value == _pedestrianPathFloodingTolerance)
                {
                    return;
                }
                PlayerPrefs.SetInt("RF_PedestrianPathFloodingTolerance", value);
                _pedestrianPathFloodingTolerance = value;
            }
        }

        private static int? _IncreaseBuildingPadHeight;
        public const int _defaultIncreaseBuildingPadHeight = 50;
        public const int _minPadIncrease = 0;
        public const int _maxPadIncrease = 500;
        public const int _padIncreaseStep = 10;

        public static int IncreaseBuildingPadHeight
        {
            get
            {
                if (!_IncreaseBuildingPadHeight.HasValue)
                {
                    _IncreaseBuildingPadHeight = PlayerPrefs.GetInt("RF_IncreaseBuildingPadHeight", (int)_defaultIncreaseBuildingPadHeight);
                }
                return _IncreaseBuildingPadHeight.Value;
            }
            set
            {
                if (value > 200f || value < 0f)
                    throw new ArgumentOutOfRangeException();
                if (value == _IncreaseBuildingPadHeight)
                {
                    return;
                }
                PlayerPrefs.SetInt("RF_IncreaseBuildingPadHeight", value);
                _IncreaseBuildingPadHeight = value;
            }

        }
        private static int? _MaxBuildingPadHeight;
        public const int _defaultMaxBuildingPadHeight = 200;

        public static int MaxBuildingPadHeight
        {
            get
            {
                if (!_MaxBuildingPadHeight.HasValue)
                {
                    _MaxBuildingPadHeight = PlayerPrefs.GetInt("RF_MaxBuildingPadHeight", (int)_defaultMaxBuildingPadHeight);
                }
                return _MaxBuildingPadHeight.Value;
            }
            set
            {
                if (value > (float)_maxPadIncrease || value < 0f)
                    throw new ArgumentOutOfRangeException();
                if (value == _MaxBuildingPadHeight)
                {
                    return;
                }
                PlayerPrefs.SetInt("RF_MaxBuildingPadHeight", value);
                _MaxBuildingPadHeight = value;
            }
        }
        private static bool _IncreaseExistingVanillaPadsOnLoad;
        private static int? _IncreaseExistingVanillaPadsOnLoadInt;
        public static bool IncreaseExistingVanillaPadsOnLoad
        {
            get
            {
                if (_IncreaseExistingVanillaPadsOnLoadInt == null)
                {
                    _IncreaseExistingVanillaPadsOnLoadInt = PlayerPrefs.GetInt("RF_IncreaseExistingVanillaPadsOnLoad", 0);
                }
                if (_IncreaseExistingVanillaPadsOnLoadInt == 1)
                {
                    _IncreaseExistingVanillaPadsOnLoad = true;
                }
                else
                {
                    _IncreaseExistingVanillaPadsOnLoad = false;
                }
                return _IncreaseExistingVanillaPadsOnLoad;
            }
            set
            {
                if (value == true)
                {
                    _IncreaseExistingVanillaPadsOnLoadInt = 1;
                }
                else
                {
                    _IncreaseExistingVanillaPadsOnLoadInt = 0;
                }
                PlayerPrefs.SetInt("RF_IncreaseExistingVanillaPadsOnLoad", (int)_IncreaseExistingVanillaPadsOnLoadInt);


            }
        }

        private static int? _pedestrianPathFloodedTolerance;
        public const int _defaultPedestrianPathFloodedTolerance = 100;

        public static int PedestrianPathFloodedTolerance
        {
            get
            {
                if (!_pedestrianPathFloodedTolerance.HasValue)
                {
                    _pedestrianPathFloodedTolerance = PlayerPrefs.GetInt("RF_PedestrianPathFloodedTolerance", (int)_defaultPedestrianPathFloodedTolerance);
                }
                return _pedestrianPathFloodedTolerance.Value;
            }
            set
            {
                if (value > _maxFloodTolerance || value <= _pedestrianPathFloodingTolerance)
                    throw new ArgumentOutOfRangeException();
                if (value == _pedestrianPathFloodedTolerance)
                {
                    return;
                }
                PlayerPrefs.SetInt("RF_PedestrianPathFloodedTolerance", value);
                _pedestrianPathFloodedTolerance = value;
            }
        }
        private static int? _TrainTrackFloodingTolerance;
        public const int _defaultTrainTrackFloodingTolerance = 50;

        public static int TrainTrackFloodingTolerance
        {
            get
            {
                if (!_TrainTrackFloodingTolerance.HasValue)
                {
                    _TrainTrackFloodingTolerance = PlayerPrefs.GetInt("RF_TrainTrackFloodingTolerance", (int)_defaultTrainTrackFloodingTolerance);
                }
                return _TrainTrackFloodingTolerance.Value;
            }
            set
            {
                if (value >= _TrainTrackFloodedTolerance || value < _minFloodTolerance)
                    throw new ArgumentOutOfRangeException();
                if (value == _TrainTrackFloodingTolerance)
                {
                    return;
                }
                PlayerPrefs.SetInt("RF_TrainTrackFloodingTolerance", value);
                _TrainTrackFloodingTolerance = value;
            }
        }

        private static int? _TrainTrackFloodedTolerance;
        public const int _defaultTrainTrackFloodedTolerance = 100;

        public static int TrainTrackFloodedTolerance
        {
            get
            {
                if (!_TrainTrackFloodedTolerance.HasValue)
                {
                    _TrainTrackFloodedTolerance = PlayerPrefs.GetInt("RF_TrainTrackFloodedTolerance", (int)_defaultTrainTrackFloodedTolerance);
                }
                return _TrainTrackFloodedTolerance.Value;
            }
            set
            {
                if (value > _maxFloodTolerance || value <= _TrainTrackFloodingTolerance)
                    throw new ArgumentOutOfRangeException();
                if (value == _TrainTrackFloodedTolerance)
                {
                    return;
                }
                PlayerPrefs.SetInt("RF_TrainTrackFloodedTolerance", value);
                _TrainTrackFloodedTolerance = value;
            }
        }
        private static bool _freezeLandvalues;
        private static int? _freezeLandvaluesInt;
        public static bool FreezeLandvalues
        {
            get
            {
                if (_freezeLandvaluesInt == null)
                {
                    _freezeLandvaluesInt = PlayerPrefs.GetInt("RF_FreezeLandvalues", 1);
                }
                if (_freezeLandvaluesInt == 1)
                {
                    _freezeLandvalues = true;
                }
                else
                {
                    _freezeLandvalues = false;
                }
                return _freezeLandvalues;
            }
            set
            {
                if (value == true)
                {
                    _freezeLandvaluesInt = 1;
                }
                else
                {
                    _freezeLandvaluesInt = 0;
                }
                PlayerPrefs.SetInt("RF_FreezeLandvalues", (int)_freezeLandvaluesInt);


            }
        }
        
        private static int? _gravityDrainageOption;
        public const int _ImprovedGravityDrainageOption = 2;
        public const int _SimpleGravityDrainageOption = 1;
        public const int _IgnoreGravityDrainageOption = 0;
        private const int _defaultGravityDrainageOption = _ImprovedGravityDrainageOption;
        
        public static int GravityDrainageOption 
        {
            get
            {
                if (_gravityDrainageOption == null)
                {
                    _gravityDrainageOption = PlayerPrefs.GetInt("RF_GravityDrainageOption", _defaultGravityDrainageOption);
                }
                return (int)_gravityDrainageOption;
            }
            set
            {
                PlayerPrefs.SetInt("RF_GravityDrainageOption", value);
                _gravityDrainageOption = value;
            }
        }

        private static int? _stormDrainAssetControlOption;
        public const int _NoControlOption = 0;
        public const int _DistrictControlOption = 1;
        public const int _IDControlOption = 2;
        public const int _IDOverrideOption = 3;
        private const int _defaultStormDrainAssetControlOption = _IDOverrideOption;

        public static int StormDrainAssetControlOption
        {
            get
            {
                if (_stormDrainAssetControlOption == null)
                {
                    _stormDrainAssetControlOption = PlayerPrefs.GetInt("RF_StormDrainAssetControlOption", _defaultStormDrainAssetControlOption);
                }
                return (int)_stormDrainAssetControlOption;
            }
            set
            {
                PlayerPrefs.SetInt("RF_StormDrainAssetControlOption", value);
                _stormDrainAssetControlOption = value;
            }
        }

        /*
        private static bool _easyMode;
        private static int? _easyModeInt;
        public static bool EasyMode
        {
            get
            {
                if (_easyModeInt == null)
                {
                    _easyModeInt = PlayerPrefs.GetInt("RF_EasyMode", 0);
                }
                if (_easyModeInt == 1)
                {
                    _easyMode = true;
                }
                else
                {
                    _easyMode = false;
                }
                return _easyMode;
            }
            set
            {
                if (value == true)
                {
                    _easyModeInt = 1;
                }
                else
                {
                    _easyModeInt = 0;
                }
                PlayerPrefs.SetInt("RF_EasyMode", (int)_easyModeInt);


            }
        }
        */
        private static bool _simulatePollution;
        private static int? _simulatePollutionInt;
        public static bool SimulatePollution
        {
            get
            {
                if (_simulatePollutionInt == null)
                {
                    _simulatePollutionInt = PlayerPrefs.GetInt("RF_SimulatePollution", 1);
                }
                if (_simulatePollutionInt == 1)
                {
                    _simulatePollution = true;
                }
                else
                {
                    _simulatePollution = false;
                }
                return _simulatePollution;
            }
            set
            {
                if (value == true)
                {
                    _simulatePollutionInt = 1;
                }
                else
                {
                    _simulatePollutionInt = 0;
                }
                PlayerPrefs.SetInt("RF_SimulatePollution", (int)_simulatePollutionInt);


            }
        }
        /*
        private static bool _improvedInletMechanics;
        private static int? _improvedInletMechanicsInt;
        public static bool ImprovedInletMechanics
        {
            get
            {
                if (_improvedInletMechanicsInt == null)
                {
                    _improvedInletMechanicsInt = PlayerPrefs.GetInt("RF_SimulatePollution", 1);
                }
                if (_improvedInletMechanicsInt == 1)
                {
                    _improvedInletMechanics = true;
                }
                else
                {
                    _improvedInletMechanics = false;
                }
                return _improvedInletMechanics;
            }
            set
            {
                if (value == true)
                {
                    _improvedInletMechanicsInt = 1;
                }
                else
                {
                    _improvedInletMechanicsInt = 0;
                }
                PlayerPrefs.SetInt("RF_ImprovedInletMechanics", (int)_improvedInletMechanicsInt);


            }
        }
        */
        private static bool _preventRainBeforeMilestone;
        private static int? _preventRainBeforeMilestoneInt;
        public static bool PreventRainBeforeMilestone
        {
            get
            {
                if (_preventRainBeforeMilestoneInt == null)
                {
                    _preventRainBeforeMilestoneInt = PlayerPrefs.GetInt("RF_PreventRainBeforeMilestone", 1);
                }
                if (_preventRainBeforeMilestoneInt == 1)
                {
                    _preventRainBeforeMilestone = true;
                }
                else
                {
                    _preventRainBeforeMilestone = false;
                }
                return _preventRainBeforeMilestone;
            }
            set
            {
                if (value == true)
                {
                    _preventRainBeforeMilestoneInt = 1;
                }
                else
                {
                    _preventRainBeforeMilestoneInt = 0;
                }
                PlayerPrefs.SetInt("RF_PreventRainBeforeMilestone", (int)_preventRainBeforeMilestoneInt);


            }
        }
        private static bool _AdditionalIncreaseForLowerPads;
        private static int? _AdditionalIncreaseForLowerPadsInt;
        public static bool AdditionalIncreaseForLowerPads
        {
            get
            {
                if (_AdditionalIncreaseForLowerPadsInt == null)
                {
                    _AdditionalIncreaseForLowerPadsInt = PlayerPrefs.GetInt("RF_AdditionalIncreaseForLowerPads", 1);
                }
                if (_AdditionalIncreaseForLowerPadsInt == 1)
                {
                    _AdditionalIncreaseForLowerPads = true;
                }
                else
                {
                    _AdditionalIncreaseForLowerPads = false;
                }
                return _AdditionalIncreaseForLowerPads;
            }
            set
            {
                if (value == true)
                {
                    _AdditionalIncreaseForLowerPadsInt = 1;
                }
                else
                {
                    _AdditionalIncreaseForLowerPadsInt = 0;
                }
                PlayerPrefs.SetInt("RF_AdditionalIncreaseForLowerPads", (int)_AdditionalIncreaseForLowerPadsInt);


            }
        }

        private static bool _AdditionalToleranceOnSlopes;
        private static int? _AdditionalToleranceOnSlopesInt;
        public static bool AdditionalToleranceOnSlopes
        {
            get
            {
                if (_AdditionalToleranceOnSlopesInt == null)
                {
                    _AdditionalToleranceOnSlopesInt = PlayerPrefs.GetInt("RF_AdditionalToleranceOnSlopes", 1);
                }
                if (_AdditionalToleranceOnSlopesInt == 1)
                {
                    _AdditionalToleranceOnSlopes = true;
                }
                else
                {
                    _AdditionalToleranceOnSlopes = false;
                }
                return _AdditionalToleranceOnSlopes;
            }
            set
            {
                if (value == true)
                {
                    _AdditionalToleranceOnSlopesInt = 1;
                }
                else
                {
                    _AdditionalToleranceOnSlopesInt = 0;
                }
                PlayerPrefs.SetInt("RF_AdditionalToleranceOnSlopes", (int)_AdditionalToleranceOnSlopesInt);


            }
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
            //ModSettings.DistrictControl = true;
            ModSettings.BuildingFloodedTolerance = _defaultBuildingFloodedTolerance;
            ModSettings.BuildingFloodingTolerance = _defaultBuildingFloodingTolerance;
            ModSettings.RoadwayFloodedTolerance = _defaultRoadwayFloodedTolerance;
            ModSettings.RoadwayFloodingTolerance = _defaultRoadwayFloodingTolerance;
            ModSettings.PedestrianPathFloodedTolerance = _defaultPedestrianPathFloodedTolerance;
            ModSettings.PedestrianPathFloodingTolerance = _defaultPedestrianPathFloodingTolerance;
            ModSettings.TrainTrackFloodedTolerance = _defaultTrainTrackFloodedTolerance;
            ModSettings.TrainTrackFloodingTolerance = _defaultTrainTrackFloodingTolerance;
            ModSettings.IncreaseBuildingPadHeight = _defaultIncreaseBuildingPadHeight;
            ModSettings.IncreaseExistingVanillaPadsOnLoad = false;
            ModSettings.MaxBuildingPadHeight = _defaultMaxBuildingPadHeight;
            ModSettings.AdditionalIncreaseForLowerPads = true;
            ModSettings.FreezeLandvalues = true;
            //ModSettings.EasyMode = false;
            ModSettings.SimulatePollution = true;
            ModSettings.PreventRainBeforeMilestone = true;
            ModSettings.GravityDrainageOption = _defaultGravityDrainageOption;
            ModSettings.StormDrainAssetControlOption = _defaultStormDrainAssetControlOption;
            ModSettings.AdditionalToleranceOnSlopes = true;
            //ModSettings.ImprovedInletMechanics = true;
            foreach (KeyValuePair<string, float> pair in DefaultRunoffCoefficients)
            {
                setRunoffCoefficient(pair.Key, DefaultRunoffCoefficients[pair.Key]);
            }
            

        }

    }
}
