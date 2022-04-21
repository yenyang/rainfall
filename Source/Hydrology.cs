
using ColossalFramework;
using ICities;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Threading;

namespace Rainfall
{
    public class Hydrology : ThreadingExtensionBase
    {
        private float _realTimeCount;
        private BuildingManager _buildingManager;
        private WeatherManager _weatherManager;
        private WaterSimulation _waterSimulation;
        private GameAreaManager _gameAreaManager;
        private NetManager _netManager;
        private TerrainManager _terrainManager;
        public bool isRaining;
        System.Random random = new System.Random();
        private StormDistributionIO customStormDistribution;

        private int _capacity;
        public bool terminated;
        public bool purged;
        public bool initialized;
        public static Hydrology instance = null;
        public bool loaded;
        private float stormDuration = 30;
        private decimal stormTime = 0;
        private bool previousStorm = false;
        private string intensityCurveName;
        private SortedList<float, float> stormIntensityCurve;
        private decimal stormIntensityCurveKeyIndex;
        private float intensityTargetLock;
        private float beforeTickCurrentIntensity;
        private const decimal secondsToMinutes = (decimal)(1f / 60f);
        private const float rainStep = 0.0002f;
        private const int ChirpRainTweetChance = 1;
        private string forecasterName;
        private string forecasterChannel;
        public int[] _preRainfallLandvalues;
        private string rainUnlockMilestone = "Milestone3";
        private uint waterSourceMaxQuantity = 1000u;
        private float eightyOneTilesDelay = 5f;
        private float eightyOneTileCheckPeriod = 30f;
        private uint naturalDrainageSourceMaxQuantity = 8000000u;
        public bool cleanUpCycle = false;
        public bool endStorm = false;
        public bool intensityCurveFinished = false;
        public bool holdLandValue = true;

        private float _realTimeCountSinceLastStorm;

        private List<string> mildQuotes;
        private List<string> normalQuotes;
        private List<string> heavyQuotes;
        private List<string> extremeQuotes;
        private List<string> chirperFirstNames;
        private List<string> chirperLastNames;
        private List<string> introductionStatements;
        private List<string> beforeTimeStatements;
        private List<string> afterIntensityAdjectiveStatements;
        private SortedList<float, string> depthAdjectives;
        private List<string> beforeIntensityAdjectiveStatements;
        private SortedList<float, string> intensityAdjectives;
        private List<string> beforeReturnRateStatements;
        private List<string> closingStatements;

        private int initialTileCount = 0;

        public int[] gameAreas;
        public Hydrology()
        {

        }

        public override void OnCreated(IThreading threading)
        {
            InitializeManagers();
            
            _capacity = _buildingManager.m_buildings.m_buffer.Length;
            
            instance = this;

            initialized = false;
            terminated = false;
            purged = false;
            isRaining = false;
            loaded = false;
            _realTimeCount = 0;

            initializeQuotes();
            initializeRainFallForecastStrings();
            customStormDistribution = new StormDistributionIO();

            base.OnCreated(threading);
        }

        public static void deinitialize()
        {

            Hydrology.instance.initialized = false;
            Hydrology.instance.loaded = false;
            Hydrology.instance.terminated = true;
        }
        private void InitializeManagers()
        {
            _buildingManager = Singleton<BuildingManager>.instance;
            _weatherManager = Singleton<WeatherManager>.instance;
            _terrainManager = Singleton<TerrainManager>.instance;
            _waterSimulation = _terrainManager.WaterSimulation;
            _netManager = Singleton<NetManager>.instance;
            _gameAreaManager = Singleton<GameAreaManager>.instance;
        }

        public override void OnBeforeSimulationTick()
        {
            if (terminated) return;

            if (!initialized) return;
            // Debug.Log("[RF].Hydrology  before tick & initialized");

            if (!loaded) return;

            if (isRaining && stormTime < (decimal)stormDuration && intensityTargetLock > 0 && stormIntensityCurveKeyIndex > 0 && intensityCurveFinished && stormIntensityCurve.Count > 0)
            {
                //Debug.Log("[RF]Hydrology.BeforeTick Current Rainfall = " + _weatherManager.m_currentRain.ToString());

                int stormIntensityCurveKeyIndexFloor = Mathf.FloorToInt((float)stormIntensityCurveKeyIndex);
                int stormIntensityCurveKeyIndexCeil = Mathf.CeilToInt((float)stormIntensityCurveKeyIndex);
                float timeRangeMinimum = stormIntensityCurve.Keys[stormIntensityCurveKeyIndexFloor];
                float timeRangeMaximum = stormIntensityCurve.Keys[stormIntensityCurveKeyIndexCeil];

                float interpolationPercentage = (float)stormIntensityCurveKeyIndex - stormIntensityCurveKeyIndexFloor;
                float rangeMinimum = stormIntensityCurve[timeRangeMinimum];
                float rangeMaximum = stormIntensityCurve[timeRangeMaximum];
                beforeTickCurrentIntensity = Mathf.Lerp(rangeMinimum, rangeMaximum, interpolationPercentage);
                //Debug.Log("[RF]Hydrology BeforeTick SICKI = " + stormIntensityCurveKeyIndex.ToString() + " stormTime = " + stormTime.ToString() + " bTCI = " + beforeTickCurrentIntensity.ToString());
                if (beforeTickCurrentIntensity < rainStep)
                {
                    beforeTickCurrentIntensity = rainStep;
                }
                _weatherManager.m_currentRain = beforeTickCurrentIntensity;

            }
            else if (stormTime > (decimal)stormDuration)
            {
                _weatherManager.m_currentRain = 0;
                _weatherManager.m_targetRain = 0;
                stormIntensityCurveKeyIndex = 0;
                intensityCurveFinished = false;
                stormTime = 0;
                stormDuration = 0;
                isRaining = false;
                Debug.Log("[RF]Hydrology BeforeTick Storm Ended StormTime > StormDuration");
            }

            if (endStorm == true)
            {
                _weatherManager.m_currentRain = 0;
                _weatherManager.m_targetRain = 0;
                stormIntensityCurveKeyIndex = 0;
                intensityCurveFinished = false;
                stormTime = 0;
                stormDuration = 0;
                isRaining = false;
                endStorm = false;
            }
            
            base.OnBeforeSimulationTick();

        }

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            bool logging = false;
            if (!terminated && OptionHandler.getSliderSetting("GlobalRunoffScalar") == 0 || _weatherManager.m_enableWeather == false)
            {
                terminated = true;
            }
            else if (terminated && OptionHandler.getSliderSetting("GlobalRunoffScalar") != 0 && purged)
            {
                terminated = false;
                purged = false;
            }

            if (terminated && purged == true) //Previous checked to see if their were any water source IDs that needed to be removed. 
            {
                return;
            }
            else if (terminated) // see above comment
            {
                purgePreviousWaterSources();
                purged = true;
                return;
            }

            if (!loaded)
                return;


            //_count += simulationTimeDelta;

            if (!initialized && Hydraulics.instance.initialized == true)
            {
                InitializeManagers();

                _preRainfallLandvalues = new int[_capacity];
                _capacity = _buildingManager.m_buildings.m_buffer.Length;
                terminated = false;
                isRaining = false;
                // Debug.Log("[RF].Hydrology  " + _capacity.ToString());




                if (_weatherManager.m_targetRain > 0 && OptionHandler.getDropdownSetting("PreviousStormOption") != 0 && OptionHandler.getDropdownSetting("PreviousStormOption") != 3)
                {

                    if (_weatherManager.m_currentRain > 0.01)
                    {
                        previousStorm = true;
                    }
                }
                if (OptionHandler.getDropdownSetting("PreviousStormOption") == 3)
                {
                    _weatherManager.m_currentRain = 0;
                    _weatherManager.m_targetRain = 0;
                }
                Debug.Log("[RF].Hydrology  Starting Storm Drain Mod!");
                initialized = true;
            }
            else if (!initialized)
            {
                return;
            }

            if (eightyOneTilesDelay > 0f)
            {
                eightyOneTilesDelay -= realTimeDelta;
                if (_gameAreaManager.m_areaGrid.Length == 81)
                {
                    Debug.Log("[RF]Hydrology.OnUpdate eightyOneTilesDelayNeeded = " + (5f - eightyOneTilesDelay).ToString());
                    eightyOneTilesDelay = 0f;
                }
                return;
            } else if (!DrainageBasinGrid.areYouAwake())
            {
                purgePreviousWaterSources();
                DrainageBasinGrid.Awake();
                initialTileCount = _gameAreaManager.m_areaGrid.Length;
                return;
                         
            } else if (_gameAreaManager.m_areaGrid.Length != initialTileCount && DrainageBasinGrid.areYouAwake())
            {
                DrainageBasinGrid.Clear();
                return;
            } else if (eightyOneTileCheckPeriod > 0f)
            {
                eightyOneTileCheckPeriod -= realTimeDelta;
            } else if (_gameAreaManager.m_areaGrid.Length == 81 && DrainageBasinGrid.areYouAwake())
            {
                DrainageBasinGrid.updateDrainageBasinGridForNewTile(logging);
                eightyOneTileCheckPeriod = 15f;
            }

            if (isRaining && stormTime < (decimal)stormDuration && Math.Abs(_weatherManager.m_currentRain - beforeTickCurrentIntensity) <= 0.5)
            {
                _weatherManager.m_currentRain = beforeTickCurrentIntensity;
            }
            if (_weatherManager.m_currentRain == 0 && isRaining == false)
            {

                if (cleanUpCycle == true)
                {
                    _weatherManager.m_targetRain = 1;
                }
                //Debug.Log("[RF].Hydrology  not raining ");
            }
            else if (_weatherManager.m_currentRain > 0 && isRaining == false && simulationTimeDelta > 0 && realTimeDelta > 0)
            {
                MilestoneInfo unlockMilestone = null;
                try
                {
                    if (!Singleton<UnlockManager>.instance.m_allMilestones.TryGetValue(rainUnlockMilestone, out unlockMilestone))
                    {
                        unlockMilestone = null;
                    }
                }
                catch
                {
                    //Debug.Log("Could not read milestone");
                    unlockMilestone = null;
                }
                if (unlockMilestone != null)
                {
                    Singleton<UnlockManager>.instance.CheckMilestone(unlockMilestone, false, false);
                }
                if (!Singleton<UnlockManager>.instance.Unlocked(unlockMilestone) && OptionHandler.getCheckboxSetting("PreventRainBeforeMilestone") == true || OptionHandler.getSliderSetting("BreakBetweenStorms") > _realTimeCountSinceLastStorm)
                {
                    _weatherManager.m_currentRain = 0;
                    _weatherManager.m_targetRain = 0;
                }
                else
                {

                    _preRainfallLandvalues = new int[_capacity];


                    isRaining = true;

                    //Debug.Log("[RF]Hydrology.onUpdate1 Current Rainfall = " + _weatherManager.m_currentRain.ToString());


                    if (_weatherManager.m_targetRain > 0)
                    {
                        bool flag = true;
                        intensityCurveFinished = false;
                        stormDuration = random.Next((int)OptionHandler.getSliderSetting("MinimumStormDuration"), (int)OptionHandler.getSliderSetting("MaximumStormDuration"));
                        stormDuration -= stormDuration % 3f;
                        Debug.Log("[RF]Hydrology.OnUpdate1 stormDuration = " + stormDuration.ToString());
                        stormIntensityCurveKeyIndex = 0;
                        //Debug.Log("[RF]Hydrology.OnUpdate1 cityName = " + cityName.ToString());
                        
                        //Debug.Log("[RF]Hydrology.OnUpdate1 intensity curve name = " + intensityCurveName.ToString());
                        intensityTargetLock = _weatherManager.m_targetRain;
                        //Debug.Log("[RF]Hydrology.OnUpdate1 intensityTargetLock = " + intensityTargetLock.ToString());
                        int MaxStormDuration = 1440;
                        intensityCurveName = "Type IA - Pacific Northwest";
                        SortedList<float, float> initialStormDepthCurve = new SortedList<float, float>();
                        bool flag1 = StormDistributionIO.GetDepthCurve(intensityCurveName, ref initialStormDepthCurve);
                        
                        if (!flag1)
                        {
                            flag = false;
                            if (!flag1)
                                Debug.Log("[RF]Hydrology.OnUpdate Could not find Intensity Curve " + intensityCurveName);
                           
                        }
                        Debug.Log("[RF]Hydrology.OnUpdate1 flag1 = " + flag1.ToString());
                        if (flag)
                        {
                            StormDistributionIO.logCurve(initialStormDepthCurve, "Initial Storm Depth Curve");
                            bool flag3;
                            SortedList<float, float> reducedDepthCurve;
                            if (stormDuration < MaxStormDuration)
                            {
                                reducedDepthCurve = new SortedList<float, float>();
                                flag3 = StormDistributionIO.reduceDuration(stormDuration, initialStormDepthCurve, ref reducedDepthCurve);
                            }
                            else
                            {
                                reducedDepthCurve = initialStormDepthCurve;
                                flag3 = true;
                            }
                            if (flag3)
                            {

                                SortedList<float, float> initialStormIntensityCurve = StormDistributionIO.GetIntensityCurve(reducedDepthCurve);
                                float initialMaxIntensity = StormDistributionIO.GetMaxValue(initialStormIntensityCurve);
                                StormDistributionIO.logCurve(initialStormIntensityCurve, "Initial Storm Intensity Curve");
                                stormIntensityCurve = StormDistributionIO.ScaleDepthCurve(initialStormIntensityCurve, _weatherManager.m_targetRain / initialMaxIntensity);
                                StormDistributionIO.logCurve(stormIntensityCurve, "Storm Intensity Curve");
                                if (StormDistributionIO.GetMaxValue(stormIntensityCurve) <= 0)
                                {
                                    flag = false;
                                }
                                Debug.Log("[RF]Hydrology.OnUpdate1 Starting storm with Max intensity " + StormDistributionIO.GetMaxValue(stormIntensityCurve).ToString());
                                beforeTickCurrentIntensity = rainStep;
                                //Debug.Log("[RF]Hydrology.OnUpdate1 beforeTickCurrentIntensity = " + beforeTickCurrentIntensity.ToString());

                                if (!previousStorm)
                                {
                                    stormTime = (decimal)(realTimeDelta);
                                    stormIntensityCurveKeyIndex = stormTime / (decimal)stormIntensityCurve.Keys[1];
                                    //Debug.Log("[RF]Hydrology.OnUpdate1 stormIntensityCurveKeyIndex = " + stormIntensityCurveKeyIndex.ToString());
                                }
                                else if ((int)OptionHandler.getDropdownSetting("PreviousStormOption") == 1)
                                {
                                    stormTime = (decimal)(realTimeDelta);
                                    float temporaryIntensity = 0;
                                    stormIntensityCurveKeyIndex = stormTime / (decimal)stormIntensityCurve.Keys[1];
                                    decimal stormIntensityCurveKeyIndexDelta = stormIntensityCurveKeyIndex;
                                    while (temporaryIntensity < _weatherManager.m_currentRain && stormTime < (decimal)stormDuration)
                                    {
                                        int stormIntensityCurveKeyIndexFloor = Mathf.FloorToInt((float)stormIntensityCurveKeyIndex);
                                        int stormIntensityCurveKeyIndexCeil = Mathf.CeilToInt((float)stormIntensityCurveKeyIndex);
                                        float timeRangeMinimum = stormIntensityCurve.Keys[stormIntensityCurveKeyIndexFloor];
                                        float timeRangeMaximum = stormIntensityCurve.Keys[stormIntensityCurveKeyIndexCeil];

                                        float interpolationPercentage = (float)stormIntensityCurveKeyIndex - stormIntensityCurveKeyIndexFloor;
                                        float rangeMinimum = stormIntensityCurve[timeRangeMinimum];
                                        float rangeMaximum = stormIntensityCurve[timeRangeMaximum];
                                        temporaryIntensity = Mathf.Lerp(rangeMinimum, rangeMaximum, interpolationPercentage);
                                        stormTime = (decimal)Mathf.Lerp(timeRangeMinimum, timeRangeMaximum, interpolationPercentage);
                                        stormIntensityCurveKeyIndex += stormIntensityCurveKeyIndexDelta;
                                    }
                                }
                                else if ((int)OptionHandler.getDropdownSetting("PreviousStormOption") == 2)
                                {
                                    stormTime = (decimal)(stormDuration);
                                    float temporaryIntensity = 0f;
                                    decimal stormIntensityCurveKeyIndexDelta = (decimal)(simulationTimeDelta) / (decimal)stormIntensityCurve.Keys[1];
                                    stormIntensityCurveKeyIndex = (decimal)(stormIntensityCurve.Keys.Count - 1) - stormIntensityCurveKeyIndexDelta;
                                    while (temporaryIntensity < _weatherManager.m_currentRain && stormTime > 0)
                                    {
                                        int stormIntensityCurveKeyIndexFloor = Mathf.FloorToInt((float)stormIntensityCurveKeyIndex);
                                        int stormIntensityCurveKeyIndexCeil = Mathf.CeilToInt((float)stormIntensityCurveKeyIndex);
                                        float timeRangeMinimum = stormIntensityCurve.Keys[stormIntensityCurveKeyIndexFloor];
                                        float timeRangeMaximum = stormIntensityCurve.Keys[stormIntensityCurveKeyIndexCeil];
                                        float interpolationPercentage = (float)stormIntensityCurveKeyIndex - stormIntensityCurveKeyIndexFloor;
                                        float rangeMinimum = stormIntensityCurve[timeRangeMinimum];
                                        float rangeMaximum = stormIntensityCurve[timeRangeMaximum];
                                        temporaryIntensity = Mathf.Lerp(rangeMinimum, rangeMaximum, interpolationPercentage);
                                        stormTime = (decimal)Mathf.Lerp(timeRangeMinimum, timeRangeMaximum, interpolationPercentage);
                                        stormIntensityCurveKeyIndex -= stormIntensityCurveKeyIndexDelta;
                                    }
                                    if (stormTime < 0)
                                        flag = false;
                                }
                            }
                            else
                            {
                                flag = false;
                                Debug.Log("[RF]Hydrology.OnUpdate1 could not reduce depth curve duration");
                            }

                            //Debug.Log("[RF]Hydrology.OnUpdate1 simulationTimeDelta = " + simulationTimeDelta.ToString());
                            // Debug.Log("[RF]Hydrology.OnUpdate1 timeScale = " + ModSettings.TimeScale.ToString());
                            //Debug.Log("[RF]Hydrology.OnUpdate1 (simulationTimeDelta * ModSettings.TimeScale) = " + (simulationTimeDelta * ModSettings.TimeScale).ToString());

                        }
                        
                        else
                        {
                            Debug.Log("[RF]Hydrology.OnUpdate1 finished before it started");
                        }
                        if (flag)
                        {
                            
                            intensityCurveFinished = true;
                        } else
                        {
                            intensityCurveFinished = false;
                        }

                    }
                    if (OptionHandler.getCheckboxSetting("ChirpForecasts") == true)
                    {
                        ChirpForecast.SendMessage(forecasterName, generateRainFallForecast());
                    }
                }
                // Debug.Log("[RF].Hydrology  Started to Rain with int = " + _weatherManager.m_currentRain.ToString() + " but will rain " + _weatherManager.m_targetRain.ToString());
            }
            else if (_weatherManager.m_currentRain > 0 && isRaining == true && simulationTimeDelta > 0 && realTimeDelta > 0 && stormTime < (decimal)stormDuration)
            {
                if (OptionHandler.getCheckboxSetting("ChirpRainTweets") == true && random.Next(0, 10000) < ChirpRainTweetChance)
                {
                    string tweeterName = randomString(chirperFirstNames) + " " + randomString(chirperLastNames);
                    if (_weatherManager.m_currentRain < 0.45)
                    {
                        ChirpForecast.SendMessage(tweeterName, randomString(mildQuotes) + " #Rainfall");
                    }
                    else if (_weatherManager.m_currentRain < 0.65)
                    {
                        ChirpForecast.SendMessage(tweeterName, randomString(normalQuotes) + " #Rainfall");
                    }
                    else if (_weatherManager.m_currentRain < 0.85)
                    {
                        ChirpForecast.SendMessage(tweeterName, randomString(heavyQuotes) + " #Rainfall");
                    }
                    else
                    {
                        ChirpForecast.SendMessage(tweeterName, randomString(extremeQuotes) + " #Rainfall");
                    }
                }

                //Debug.Log("[RF].Hydrology  is raining ");


                //Debug.Log("[RF]Hydrology.onUpdate2 Current Rainfall = " + _weatherManager.m_currentRain.ToString());
                stormTime += (decimal)realTimeDelta;
                if (stormTime < (decimal)stormDuration)
                {
                    int stormIntensityCurveKeyIndexFloor = (int)Math.Floor(stormIntensityCurveKeyIndex);
                    int stormIntensityCurveKeyIndexCeil = (int)Math.Ceiling(stormIntensityCurveKeyIndex);
                    float timeRangeMinimum = stormIntensityCurve.Keys[stormIntensityCurveKeyIndexFloor];
                    float timeRangeMaximum = stormIntensityCurve.Keys[stormIntensityCurveKeyIndexCeil];
                    float timeRange = timeRangeMaximum - timeRangeMinimum;
                    int attempts = 0;

                    while (stormTime >= (decimal)timeRangeMaximum || stormTime <= (decimal)timeRangeMinimum)
                    {
                        if (stormTime > (decimal)timeRangeMaximum)
                        {
                            stormIntensityCurveKeyIndexFloor++;
                            stormIntensityCurveKeyIndexCeil++;
                        }
                        else if (stormTime < (decimal)timeRangeMinimum)
                        {
                            stormIntensityCurveKeyIndexFloor--;
                            stormIntensityCurveKeyIndexCeil--;
                        }
                        timeRangeMinimum = stormIntensityCurve.Keys[stormIntensityCurveKeyIndexFloor];
                        timeRangeMaximum = stormIntensityCurve.Keys[stormIntensityCurveKeyIndexCeil];
                        attempts++;
                        if (attempts > stormIntensityCurve.Keys.Count)
                            break;
                    }
                    if (attempts > stormIntensityCurve.Keys.Count)
                    {
                        Debug.Log("[RF]Hydrology.OnUpdate2 stormTime = " + stormTime.ToString());
                        Debug.Log("[RF]Hydrology.OnUpdate2 stormIntensityCurveKeyIndexFloor = " + stormIntensityCurveKeyIndexFloor.ToString());
                        Debug.Log("[RF]Hydrology.OnUpdate2 stormIntensityCurveKeyIndexCeil = " + stormIntensityCurveKeyIndexCeil.ToString());
                        Debug.Log("[RF]Hydrology.OnUpdate2 timeRangeMinimum = " + timeRangeMinimum.ToString());
                        Debug.Log("[RF]Hydrology.OnUpdate2 timeRangeMaximum = " + timeRangeMaximum.ToString());
                        Debug.Log("[RF]Hydrology.OnUpdate2 attempts = " + attempts.ToString());
                        Debug.Log("[RF]Hydrology.OnUpdate Could not find time range for stormTime = " + stormTime.ToString());
                    }
                    else
                    {
                        timeRange = timeRangeMaximum - timeRangeMinimum;
                        decimal timePercentage = (stormTime - (decimal)timeRangeMinimum) / (decimal)timeRange;
                        stormIntensityCurveKeyIndex = (decimal)stormIntensityCurveKeyIndexFloor + timePercentage;
                        //Debug.Log("[RF]Hydrology Update stormTime = " + stormTime.ToString() + " of " + stormDuration.ToString() + " rainfall = " + _weatherManager.m_currentRain.ToString() + " max = " + StormDistributionIO.GetMaxValue(stormIntensityCurve) + " total depth = " + StormDistributionIO.GetMaxDepth(stormIntensityCurve));
                    }
                }




            }
            //Debug.Log("[RF].Hydrology  Raining with int = " + _weatherManager.m_currentRain.ToString() + " but will rain " + _weatherManager.m_targetRain.ToString());

            else if (_weatherManager.m_currentRain == 0 && isRaining == true)
            {
                try
                {
                    purgePreviousWaterSources();
                }
                catch (Exception e)
                {
                    Debug.Log("[RF]Hydrology.OnUpdate Could not remove water sources from buildings encountered exception " + e.ToString());
                }
                isRaining = false;
                cleanUpCycle = false;
                
                //Debug.Log("[RF].Hydrology  No longer Raining. int = " + _weatherManager.m_currentRain.ToString() + " but will rain " + _weatherManager.m_targetRain.ToString());
            }
            if (isRaining == false && simulationTimeDelta > 0)
            {
                _realTimeCountSinceLastStorm += realTimeDelta;
                if (_realTimeCountSinceLastStorm > OptionHandler.getSliderSetting("FreezeLandvaluesTimer")) //repalce with Modsetting Variable
                {
                    _preRainfallLandvalues = new int[_capacity];
                    holdLandValue = false;
                }
                if (_realTimeCountSinceLastStorm > OptionHandler.getSliderSetting("MaxTimeBetweenStorms")) //repalce with Modsetting Variable
                {
                    _weatherManager.m_targetRain = Mathf.Clamp((float)random.NextDouble(),0.2f,1.0f);
                }
            } else if (isRaining == true)
            {
                _realTimeCountSinceLastStorm = 0f;
                holdLandValue = true;
            }
            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }
        
        public override void OnAfterSimulationTick()
        {
            if (isRaining)
            {
                //Debug.Log("[RF]Hydrology.onAfterSimulationTick Current Rainfall = " + _weatherManager.m_currentRain.ToString());

                if (_weatherManager.m_targetRain != intensityTargetLock && intensityTargetLock > 0)
                {
                    _weatherManager.m_targetRain = intensityTargetLock;
                    _weatherManager.m_currentRain = beforeTickCurrentIntensity;

                }
                if (stormTime < (decimal)stormDuration && Mathf.Abs(_weatherManager.m_currentRain - beforeTickCurrentIntensity) <= 0.5)
                {
                    _weatherManager.m_currentRain = beforeTickCurrentIntensity;
                }
                else if (stormTime >= (decimal)stormDuration && (Mathf.Abs(_weatherManager.m_currentRain - beforeTickCurrentIntensity) <= 0.01))
                {
                    Debug.Log("[RF]Hydrology.OnAfterTick Storm completed naturally.");
                    _weatherManager.m_currentRain = 0;
                }
                else
                {
                    Debug.Log("[RF]Hydrology.OnAfterTick stormTime < StormDuration " + (stormTime < (decimal)stormDuration).ToString());
                    Debug.Log("[RF]Hydrology.OnAfterTick CurrentRain-BTCI = " + (Mathf.Abs(_weatherManager.m_currentRain - beforeTickCurrentIntensity)).ToString() + " CR-CTCI<0.01 = " + (Mathf.Abs(_weatherManager.m_currentRain - beforeTickCurrentIntensity) <= 0.01).ToString());

                    Debug.Log("[RF]Hydrology.OnAfterTick Ended improved simulation stormTime = " + stormTime.ToString() + " stormDuration = " + stormDuration.ToString() + " currentRain = " + _weatherManager.m_currentRain + " bTCI = " + beforeTickCurrentIntensity);
                }
                    

                
            }
            base.OnAfterSimulationTick();
        }

        
        public string randomString(List<string> strings)
        {
            int stringNum = random.Next(0, strings.Count - 1);
            return strings[stringNum];
        }
        public void initializeQuotes()
        {
            mildQuotes = new List<string>();
            normalQuotes = new List<string>();
            heavyQuotes = new List<string>();
            extremeQuotes = new List<string>();
            chirperFirstNames = new List<string>();
            chirperLastNames = new List<string>();

            mildQuotes.Add("Is that Rain?");
            mildQuotes.Add("I think it might be sprinkling outside.");
            mildQuotes.Add("It's only a shower.");
            mildQuotes.Add("A light rain is beginning to fall.");
            mildQuotes.Add("Rain drops keep fallin' on my head.");
            mildQuotes.Add("Everybody wants happiness, nobody wants pain, but you can't have a rainbow without a little rain.");
            mildQuotes.Add("Upon us all, a little rain must fall.");
            normalQuotes.Add("I love it when it rains!");
            normalQuotes.Add("The way I see it, if you want the rainbow, you gotta put up with the rain.");
            normalQuotes.Add("A single gentle rain makes the grass many shades greener.");
            normalQuotes.Add("It looks like it's going to rain.");
            normalQuotes.Add("Let the rain wash away all the pain of yesterday.");
            normalQuotes.Add("Rain, rain, go away, come again another day.");
            normalQuotes.Add("It's supposed to clear up later.");
            normalQuotes.Add("When life gives you rainy days, wear cute boots and jump in the puddles.");
            normalQuotes.Add("Life isn't about waiting for the storm to pass. It's about learning to dance in the rain.");
            normalQuotes.Add("Rain showers my spirit and waters my soul.");
            normalQuotes.Add("Every storm runs out of rain.");
            heavyQuotes.Add("Bring an umbrealla and a jacket.");
            heavyQuotes.Add("I heard it is going to pour down buckets of rain.");
            heavyQuotes.Add("The nicest thing about the rain is that it always stops. Eventually.");
            heavyQuotes.Add("I always like walking in the rain, so no one can see me crying.");
            heavyQuotes.Add("Upcoming heavy rains may cause flooding.");
            heavyQuotes.Add("Thank God I remembered my umbrella.");
            extremeQuotes.Add("Better get inside. This is going to be a huge storm!");
            extremeQuotes.Add("It's going to be storm of the century!");
            extremeQuotes.Add("This is the storm of a lifetime!");
            extremeQuotes.Add("Expect major flooding from today's storm.");
            extremeQuotes.Add("Don't drown! Turn around!");
            extremeQuotes.Add("Flash flood warning!");

            chirperFirstNames.Add("Anna");
            chirperFirstNames.Add("Bernie");
            chirperFirstNames.Add("Catherine");
            chirperFirstNames.Add("David");
            chirperFirstNames.Add("Erik");
            chirperFirstNames.Add("Frank");
            chirperFirstNames.Add("George");
            chirperFirstNames.Add("Heather");
            chirperFirstNames.Add("Igor");
            chirperFirstNames.Add("Jodi");
            chirperFirstNames.Add("Kelly");
            chirperFirstNames.Add("Lynn");
            chirperFirstNames.Add("Matt");
            chirperFirstNames.Add("Megan");
            chirperFirstNames.Add("Natalie");
            chirperFirstNames.Add("Olivia");
            chirperFirstNames.Add("Philip");
            chirperFirstNames.Add("Quinton");
            chirperFirstNames.Add("Regis");
            chirperFirstNames.Add("Sam");
            chirperFirstNames.Add("Thomas");
            chirperFirstNames.Add("Trudy");
            chirperFirstNames.Add("Ulrick");
            chirperFirstNames.Add("Valerie");
            chirperFirstNames.Add("Whitney");
            chirperFirstNames.Add("Xander");
            chirperFirstNames.Add("Yadira");
            chirperFirstNames.Add("Zack");

            chirperLastNames.Add("Adams");
            chirperLastNames.Add("Betancourt");
            chirperLastNames.Add("Caltabiano");
            chirperLastNames.Add("Dizon");
            chirperLastNames.Add("Estrada");
            chirperLastNames.Add("Farrell");
            chirperLastNames.Add("Gutierrez");
            chirperLastNames.Add("Hayden");
            chirperLastNames.Add("Ising");
            chirperLastNames.Add("Jones");
            chirperLastNames.Add("Kelley");
            chirperLastNames.Add("Lerma");
            chirperLastNames.Add("Miller");
            chirperLastNames.Add("Nason");
            chirperLastNames.Add("Ortiz");
            chirperLastNames.Add("Pederson");
            chirperLastNames.Add("Roth");
            chirperLastNames.Add("Swartz");
            chirperLastNames.Add("Torres");
            chirperLastNames.Add("Ulmer");
            chirperLastNames.Add("Vanderbilt");
            chirperLastNames.Add("Walker");
            chirperLastNames.Add("Yoldi");
            chirperLastNames.Add("Zuniga");

        }
        public void initializeRainFallForecastStrings()
        {
            introductionStatements = new List<string>();
            beforeTimeStatements = new List<string>();
            afterIntensityAdjectiveStatements = new List<string>();
            depthAdjectives = new SortedList<float, string>();
            beforeIntensityAdjectiveStatements = new List<string>();
            intensityAdjectives = new SortedList<float, string>();
            beforeReturnRateStatements = new List<string>();
            closingStatements = new List<string>();
            string forecasterFirestName = randomString(chirperFirstNames);
            string forecasterLastName = randomString(chirperLastNames);

            chirperFirstNames.Remove(forecasterFirestName);
            chirperLastNames.Remove(forecasterLastName);
            forecasterName = forecasterFirestName + " " + forecasterLastName;
            forecasterChannel = random.Next(1, 99).ToString();
            introductionStatements.Add("Hello Viewers! My name is ");
            introductionStatements.Add("This is ");
            introductionStatements.Add("Welcome! It's time for your weather forecast by ");
            introductionStatements.Add("Today's forecast brought to you by ");
            introductionStatements.Add("Back again folks. ");
            introductionStatements.Add("This just in! Hello, it's ");

            beforeTimeStatements.Add("In the next ");
            beforeTimeStatements.Add("Rain is expected to fall for the next ");
            beforeTimeStatements.Add("A storm will last for ");
            beforeTimeStatements.Add("Upcoming showers that may be around for the next ");
            beforeTimeStatements.Add("Bring a rainjacket, If you are going out during the next ");

            beforeIntensityAdjectiveStatements.Add("We are anticipating a max intensity of");
            beforeIntensityAdjectiveStatements.Add("We are expecting a peak intensity of");
            beforeIntensityAdjectiveStatements.Add("This storm will drop a");
            beforeIntensityAdjectiveStatements.Add("The climax of of the storm is forecasted to be a");

            

            intensityAdjectives.Add(0.2f, " measly");
            intensityAdjectives.Add(0.25f, " mild");
            intensityAdjectives.Add(0.3f, " small");
            intensityAdjectives.Add(0.4f, " normal");
            intensityAdjectives.Add(0.45f, "n average");
            intensityAdjectives.Add(0.5f, "n above average");
            intensityAdjectives.Add(0.55f, " significant");
            intensityAdjectives.Add(0.6f, " large");
            intensityAdjectives.Add(0.65f, " heavy");
            intensityAdjectives.Add(0.70f, " whopping");
            intensityAdjectives.Add(0.75f, " formidable");
            intensityAdjectives.Add(0.80f, " extreme");
            intensityAdjectives.Add(0.85f, " insane");
            intensityAdjectives.Add(0.90f, " godly");
            intensityAdjectives.Add(0.95f, " astronomical");
            intensityAdjectives.Add(1.0f, " ark-worthy");

            closingStatements.Add("Stay Dry!");
            closingStatements.Add("See you next time on Channel " + forecasterChannel + ".");
            closingStatements.Add("And that's your forecast. Good bye.");
            closingStatements.Add("That concludes our forecast for today.");
        }
        public string generateRainFallForecast()
        {
            StringBuilder fullForecast = new StringBuilder();
            fullForecast.Append(randomString(introductionStatements));
            fullForecast.Append(forecasterName + " from Channel " + forecasterChannel + ". ");
            fullForecast.Append(randomString(beforeTimeStatements));
            fullForecast.Append(convertSecToMinSec(stormDuration - Mathf.Round((float)stormTime)) + " ");
           
            fullForecast.Append(randomString(beforeIntensityAdjectiveStatements));
            float maxIntensity = StormDistributionIO.GetMaxValue(stormIntensityCurve);
            foreach (KeyValuePair<float, string> pair in intensityAdjectives)
            {
                if (maxIntensity <= pair.Key)
                {
                    fullForecast.Append(pair.Value + " " + (Mathf.Floor(maxIntensity * 10f) / 10f).ToString() +  " units/time. ");
                    break;
                }
            }
            
            fullForecast.Append(". " + randomString(closingStatements));
            fullForecast.Append(" #RainForecast");
            return fullForecast.ToString();

        }
        private string convertSecToMinSec(float time)
        {
            string sec = ((int)time % 60).ToString();
            string min = ((int)time / 60).ToString();
            if (((int)time / 60) > 0)
                return (min + " min. and " + sec + " sec.");
            return (sec + " sec.");
        }
        
        private void RelocateBuilding(ushort building, ref Building data, Vector3 position, float angle)
        {
            BuildingInfo info = data.Info;
            RemoveFromGrid(building, ref data);
            if (info.m_hasParkingSpaces != VehicleInfo.VehicleType.None)
            {
                BuildingManager.instance.UpdateParkingSpaces(building, ref data);
            }
            data.m_position = position;
            data.m_angle = angle;

            AddToGrid(building, ref data);
            data.CalculateBuilding(building);
            BuildingManager.instance.UpdateBuildingRenderer(building, true);
        }

        private static void AddToGrid(ushort building, ref Building data)
        {
            int num = Mathf.Clamp((int)(data.m_position.x / 64f + 135f), 0, 269);
            int num2 = Mathf.Clamp((int)(data.m_position.z / 64f + 135f), 0, 269);
            int num3 = num2 * 270 + num;
            while (!Monitor.TryEnter(BuildingManager.instance.m_buildingGrid, SimulationManager.SYNCHRONIZE_TIMEOUT))
            {
            }
            try
            {
                BuildingManager.instance.m_buildings.m_buffer[(int)building].m_nextGridBuilding = BuildingManager.instance.m_buildingGrid[num3];
                BuildingManager.instance.m_buildingGrid[num3] = building;
            }
            finally
            {
                Monitor.Exit(BuildingManager.instance.m_buildingGrid);
            }
        }

        private static void RemoveFromGrid(ushort building, ref Building data)
        {
            BuildingManager buildingManager = BuildingManager.instance;

            BuildingInfo info = data.Info;
            int num = Mathf.Clamp((int)(data.m_position.x / 64f + 135f), 0, 269);
            int num2 = Mathf.Clamp((int)(data.m_position.z / 64f + 135f), 0, 269);
            int num3 = num2 * 270 + num;
            while (!Monitor.TryEnter(buildingManager.m_buildingGrid, SimulationManager.SYNCHRONIZE_TIMEOUT))
            {
            }
            try
            {
                ushort num4 = 0;
                ushort num5 = buildingManager.m_buildingGrid[num3];
                int num6 = 0;
                while (num5 != 0)
                {
                    if (num5 == building)
                    {
                        if (num4 == 0)
                        {
                            buildingManager.m_buildingGrid[num3] = data.m_nextGridBuilding;
                        }
                        else
                        {
                            BuildingManager.instance.m_buildings.m_buffer[(int)num4].m_nextGridBuilding = data.m_nextGridBuilding;
                        }
                        break;
                    }
                    num4 = num5;
                    num5 = BuildingManager.instance.m_buildings.m_buffer[(int)num5].m_nextGridBuilding;
                    if (++num6 > 49152)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
                data.m_nextGridBuilding = 0;
            }
            finally
            {
                Monitor.Exit(buildingManager.m_buildingGrid);
            }
            if (info != null)
            {
                Singleton<RenderManager>.instance.UpdateGroup(num * 45 / 270, num2 * 45 / 270, info.m_prefabDataLayer);
            }
        }
        private void elevateBuildingPad(ushort id)
        {
            Building building = _buildingManager.m_buildings.m_buffer[id];
            _buildingManager.m_buildings.m_buffer[id].m_flags = _buildingManager.m_buildings.m_buffer[id].m_flags | Building.Flags.FixedHeight;
            Vector3 newPosition = building.m_position;

            //Debug.Log("[RF]Hydrology.elevateBuildingPad miny = " + ((float)miny/64f).ToString() + " avgy = " + ((float)avgy / 64f).ToString() + " maxy = " + ((float)maxy / 64f).ToString());

            Vector3 sidewalkPosition1 = Building.CalculateSidewalkPosition(building.m_position, building.m_angle, building.m_length, building.Width * 4, 0);
            Vector3 sidewalkPosition2 = Building.CalculateSidewalkPosition(building.m_position, building.m_angle, building.m_length, -building.Width * 4, 0);
            Vector3 sidewalkPosition = building.CalculateSidewalkPosition();
            Vector3[] positions = new Vector3[] { sidewalkPosition, sidewalkPosition1, sidewalkPosition2 };
            float highestSidewalkPosition = 0;
            foreach (Vector3 position in positions)
            {
                int miny;
                int avgy;
                int maxy;
                _terrainManager.CalculateAreaHeight(position.x, position.y, position.x, position.y, out miny, out avgy, out maxy);

                if (maxy > highestSidewalkPosition)
                    highestSidewalkPosition = (float)maxy / 64f;
                //Debug.Log("[RF].Hydrology.ElevateBuildingPad currentSidewalkElevation = " + ((float)maxy/64f).ToString());
            }




            //Debug.Log("[RF]Hydrology.elevateBuildingPad differenceFromSidewalk = " + differenceFromTerrainToSidewalk.ToString());

            newPosition.y += (float)OptionHandler.getSliderSetting("IncreaseBuildingPadHeight");

            //Debug.Log("[RF]Hydrology.onUpdate " + _buildingManager.m_buildings.m_buffer[id].m_flags.ToString());
            //Debug.Log("[RF]Hydrology.onUpdate tried to elevate building " + id.ToString() + " from elevation " + _buildingManager.m_buildings.m_buffer[id].m_position.y.ToString() + " to elevation " + newPosition.y.ToString());
            RelocateBuilding(id, ref _buildingManager.m_buildings.m_buffer[id], newPosition, _buildingManager.m_buildings.m_buffer[id].m_angle);
            //Debug.Log("[RF]Hydrology.onUpdate building " + id.ToString() + " is at elevation " + _buildingManager.m_buildings.m_buffer[id].m_position.y.ToString());
        }

        private void purgePreviousWaterSources()
        {
            List<ushort> previousStormWaterSourceIDs = new List<ushort>();

            for (int i = 0; i < _waterSimulation.m_waterSources.m_size - 1; i++)
            {
                WaterSource ws = _waterSimulation.m_waterSources.m_buffer[i];
                if (ws.m_inputRate == 0u && ws.m_type == 2 && ws.m_water <= naturalDrainageSourceMaxQuantity && !Hydraulics.instance._previousFacilityWaterSources.Contains((ushort)(i + 1)))
                {

                    previousStormWaterSourceIDs.Add((ushort)(i + 1));
                }
            }
            foreach (ushort id in previousStormWaterSourceIDs)
            {
                _waterSimulation.ReleaseWaterSource(id);
            }
        }
        
        public static void CleanUpCycle()
        {
            if (Hydrology.instance.initialized == true && Hydrology.instance.loaded == true)
            {
                Hydrology.instance.cleanUpCycle = true;

            }
        }
        public static void EndStorm()
        {
            if (Hydrology.instance.initialized == true && Hydrology.instance.loaded == true)
            {
                Hydrology.instance.endStorm = true;
            }
        }

        public static void MakeItRain()
        {
            if (Hydrology.instance.initialized == true && Hydrology.instance.loaded == true && Hydrology.instance.isRaining == false && Singleton<WeatherManager>.instance.m_targetRain == 0)
            {
                Singleton<WeatherManager>.instance.m_targetFog = 0;
                Singleton<WeatherManager>.instance.m_currentFog = 0;
                Singleton<WeatherManager>.instance.m_targetRain = OptionHandler.getSliderSetting("MakeItRainIntensity");
                Hydrology.instance._realTimeCountSinceLastStorm = 3601f;
            }
        }
        public static void Terminate()
        {
            Hydrology.instance.terminated = true;
            DrainageBasinGrid.Clear();
        }
        
    }
}