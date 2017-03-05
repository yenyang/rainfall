
using ColossalFramework;
using ICities;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace Rainfall
{
    public class Hydrology : ThreadingExtensionBase
    {
        private float _realTimeCount;
        private BuildingManager _buildingManager;
        private WeatherManager _weatherManager;
        private WaterSimulation _waterSimulation;
        private TerrainManager _terrainManager;
        public bool isRaining;
        System.Random random = new System.Random();

        private HashSet<ushort> _buildingIDs;
        private HashSet<ushort> _newBuildingIDs;
        private HashSet<ushort> _removeBuildingIDs;

        private HashSet<WaterSource> _waterSources;
        private ushort[] _waterSourceIDs;
        private HashSet<ushort>[] _buildingIDChunks;
        private int currentChunk;
        private int finishedChunks;
        private int _capacity;
        public bool terminated;
        public bool initialized;
        public static Hydrology instance = null;
        public bool loaded;
        private bool improvedSimulation;
        private float stormDuration = 30;
        private decimal stormTime = 0;
        private bool previousStorm = false;
        private float stormDepth;
        private string cityName;
        private string intensityCurveName;
        private SortedList<float, float> stormIntensityCurve;
        private SortedList<float, float> stormDepthCurve;
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
        private uint waterSourceMaxQuantitiy = 100000000u;
        public bool cleanUpCycle = false;
        public bool endStorm = false;

        private List<string> mildQuotes;
        private List<string> normalQuotes;
        private List<string> heavyQuotes;
        private List<string> extremeQuotes;
        private List<string> chirperFirstNames;
        private List<string> chirperLastNames;
        private List<string> introductionStatements;
        private List<string> beforeTimeStatements;
        private List<string> beforeDepthAdjectiveStatements;
        private SortedList<float, string> depthAdjectives;
        private List<string> beforeIntensityAdjectiveStatements;
        private SortedList<float, string> intensityAdjectives;
        private List<string> beforeReturnRateStatements;
        private List<string> closingStatements;
        

        public Hydrology()
        {
            
        }

        public override void OnCreated(IThreading threading)
        {
            InitializeManagers(); 
            _buildingIDs = new HashSet<ushort>();
            _newBuildingIDs = new HashSet<ushort>();
            _removeBuildingIDs = new HashSet<ushort>();
            _waterSources = new HashSet<WaterSource>();
            _capacity = _buildingManager.m_buildings.m_buffer.Length;
            _waterSourceIDs = new ushort[_capacity];
            _buildingIDChunks = new HashSet<ushort>[ModSettings._maxRefreshRate];

            for (int chunks=0; chunks<ModSettings._maxRefreshRate; chunks++)
            {
                _buildingIDChunks[chunks] = new HashSet<ushort>();
            }

            improvedSimulation = false;
            instance = this;
            finishedChunks = 0;
            initialized = false;
            terminated = false;
            isRaining = false;
            loaded = false;
            _realTimeCount = 0;
            finishedChunks = 0;
     
            initializeQuotes();
            initializeRainFallForecastStrings();
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
        }

        public override void OnBeforeSimulationTick()
        {
            if (terminated) return;

            if (!initialized) return;
            // Debug.Log("[RF].Hydrology  before tick & initialized");

            if (!loaded) return;

            if (isRaining && improvedSimulation && stormTime < (decimal)stormDuration && stormDepth > 0 && intensityTargetLock > 0) {
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
                } else if (beforeTickCurrentIntensity > ModSettings.MaximumStormIntensity && ModSettings.MaximumStormIntensity < ModSettings._maxStormIntensity)
                {
                    beforeTickCurrentIntensity = ModSettings.MaximumStormIntensity;
                }
                _weatherManager.m_currentRain = beforeTickCurrentIntensity;

            } else if (stormTime > (decimal)stormDuration)
            {
                _weatherManager.m_currentRain = 0;
                _weatherManager.m_targetRain = 0;

                stormTime = 0;
                improvedSimulation = false;
                Debug.Log("[RF]Hydrology BeforeTick Storm Ended StormTime > StormDuration");
            } else if (improvedSimulation == true && (stormDepth <= 0 || intensityTargetLock <= 0))
            {
                improvedSimulation = false;
                Debug.Log("[RF]Hydrology Before Tick Storm Ended Depth or Intensity <= 0");
            }

            if (!_buildingManager.m_buildingsUpdated) return;
            //Debug.Log("[RF].Hydrology  before tick & building Updated");

            for (int i = 0; i < _buildingManager.m_updatedBuildings.Length; i++)
            {
                ulong ub = _buildingManager.m_updatedBuildings[i];
               // Debug.Log("[RF].Hydrology  before tick & building Updated no:" + ub.ToString() );
                if (ub != 0)
                {
                    for (int j = 0; j < 64; j++)
                    {
                        if ((ub & (ulong)1 << j) != 0)
                        {
                            ushort id = (ushort)(i << 6 | j);
                           // Debug.Log("[RF].Hydrology  before tick & building Updated id:" + id.ToString());
                            if (reviewBuilding(id))
                            {
                                _newBuildingIDs.Add(id);
                               //Debug.Log("[RF].Hydrology  Added _newBuildingID " + id.ToString());
                            }
                            else if (_buildingIDs.Contains(id)) {
                                _removeBuildingIDs.Add(id);
                                //Debug.Log("[RF].Hydrology  Added _RemoveBuildingID " + id.ToString());
                            }

                        }
                    }
                }
            }
            if (endStorm == true)
            {
                improvedSimulation = false;
                _weatherManager.m_currentRain = 0;
                _weatherManager.m_targetRain = 0;
                endStorm = false;
            }
            base.OnBeforeSimulationTick();

        }

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (!terminated && ModSettings.Difficulty == 0 || _weatherManager.m_enableWeather == false)
            {
                terminated = true; 
            } else if (terminated && ModSettings.Difficulty != 0)
            {
                terminated = false;
            }
           
            if (terminated && _waterSources.Count == 0)
            {
                return;
            }
            else if (terminated)
            {
                foreach (ushort id in _buildingIDs)
                {
                    if (_waterSourceIDs[id] != 0)
                        removeWaterSourceAtBuilding(id);
                   // Debug.Log("[RF].Hydrology  Removed Water for building " + id.ToString());
                }
                return;
            }

            if (!loaded)
                return;

            //_count += simulationTimeDelta;

            if (!initialized && Hydraulics.instance.initialized == true)
            {
                InitializeManagers();
                _buildingIDs.Clear();
                _newBuildingIDs.Clear();
                _waterSources.Clear();
                _removeBuildingIDs.Clear();
                _waterSourceIDs = new ushort[_capacity];
                _preRainfallLandvalues = new int[_capacity];
                _capacity = _buildingManager.m_buildings.m_buffer.Length;
                terminated = false;
                improvedSimulation = false;
                isRaining = false;
               // Debug.Log("[RF].Hydrology  " + _capacity.ToString());
                for (ushort id = 0; id < _capacity; id++)
                {
                    if (reviewBuilding(id))
                    {
                        _buildingIDs.Add(id);
                        //Debug.Log("[RF].Hydrology  Added " + id.ToString() + " to _buildingID");
                    }
                }
                List<ushort> previousStormWaterSourceIDs = new List<ushort>();
                
                for (int i =0; i<_waterSimulation.m_waterSources.m_size-1; i++)
                {
                    WaterSource ws = _waterSimulation.m_waterSources.m_buffer[i];
                    if (ws.m_inputRate == 0u && ws.m_type == 2 && ws.m_water <= waterSourceMaxQuantitiy && !Hydraulics.instance._previousFacilityWaterSources.Contains((ushort)(i+1)))
                    {

                        previousStormWaterSourceIDs.Add((ushort)(i + 1));
                    }
                }
                foreach(ushort id in previousStormWaterSourceIDs)
                {
                    _waterSimulation.ReleaseWaterSource(id);
                }

                if (ModSettings.CityName != ModSettings.UnmoddedCityName && ModSettings.StormDistributionName != ModSettings.UnmoddedStormDistributionName && _weatherManager.m_targetRain > 0 && ModSettings.PreviousStormOption != 0 && ModSettings.PreviousStormOption != 3)
                {
                    
                    if (_weatherManager.m_currentRain > 0.01)
                    {
                        previousStorm = true;
                    }
                }
                if (ModSettings.PreviousStormOption == 3)
                {
                    _weatherManager.m_currentRain = 0;
                    _weatherManager.m_targetRain = 0;
                }

                Debug.Log("[RF].Hydrology  Starting Storm Drain Mod!");
                initialized = true;
            } else if (!initialized)
            {
                return;
            }
            if (isRaining && improvedSimulation && stormTime < (decimal)stormDuration && Math.Abs(_weatherManager.m_currentRain - beforeTickCurrentIntensity) <= 0.5)
            {
                _weatherManager.m_currentRain = beforeTickCurrentIntensity;
            } else
            {
                improvedSimulation = false;
            }
            if (_weatherManager.m_currentRain == 0 && isRaining == false)
            {
                foreach (ushort id in _newBuildingIDs)
                {
                    _buildingIDs.Add(id);
                   // Debug.Log("[RF].Hydrology  Moved _newBuildingID " + id.ToString() + " to _buildingID");
                }
              
                foreach (ushort id in _buildingIDs)
                {
                    if (!reviewBuilding(id))
                    {
                        _removeBuildingIDs.Add(id);
                        //Debug.Log("[RF].Hydrology  Removed _BuildingID " + id.ToString() + " because !reviewed");
                    }
                }
                foreach (ushort id in _removeBuildingIDs)
                {
                    _buildingIDs.Remove(id);
                    //Debug.Log("[RF].Hydrology  ReMoved _BuildingID " + id.ToString());
                }
                _newBuildingIDs.Clear();
                _removeBuildingIDs.Clear();
                if (cleanUpCycle == true)
                {
                    _weatherManager.m_targetRain = 1;
                }
                //Debug.Log("[RF].Hydrology  not raining ");
            }
            else if (_weatherManager.m_currentRain > 0 && isRaining == false && simulationTimeDelta > 0) {
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
                if (!Singleton<UnlockManager>.instance.Unlocked(unlockMilestone) && ModSettings.PreventRainBeforeMilestone == true)
                {
                    _weatherManager.m_currentRain = 0;
                    _weatherManager.m_targetRain = 0;
                }
                else {
                    currentChunk = 0;
                    finishedChunks = 0;
                    //Debug.Log("[RF].Hydrology MaxRefreshRate = " + ModSettings._maxRefreshRate.ToString());
                    //Debug.Log("[RF].Hydrlogy _BuildingIDChunks.length = " + _buildingIDChunks.Length.ToString());

                    for (int chunks = 0; chunks < ModSettings._maxRefreshRate; chunks++)
                    {
                        if (_buildingIDChunks[chunks].Count > 0)
                        {
                            _buildingIDChunks[chunks].Clear();
                            //Debug.Log("[RF].Hydrology Clearing chunk " + chunks);
                        }
                        else
                        {
                            //Debug.Log("[RF].Hydrology Chunk " + chunks.ToString() + " was already empty.");
                        }

                    }
                    _preRainfallLandvalues = new int[_capacity];
                    foreach (ushort id in _buildingIDs)
                    {
                        if (id <= _capacity)
                        {
                            if (calculateFlowRate(id, _weatherManager.m_currentRain) > 0u/*&& ModSettings.EasyMode == false*/)
                            {
                                _waterSources.Add(newWaterSourceAtBuidling(id));
                                //Debug.Log("[RF].Hydrology  Added  Water for building " + id.ToString() + " in _buildingID");
                            }
                            /*else if (ModSettings.EasyMode == true)
                            {
                                Debug.Log("[RF].Hydrology New Storm Easy Mode Adding SWA");
                                Hydraulics.addStormwaterAccumulation(id, calculateFlowRate(id, _weatherManager.m_currentRain));
                            }*/
                            _buildingIDChunks[currentChunk].Add(id);
                            //Debug.Log("[RF].Hydrology Added Building " + id.ToString() + " to chunk " + currentChunk.ToString());
                            if (currentChunk + 1 < ModSettings.RefreshRate)
                            {
                                currentChunk++;
                            }
                            else
                            {
                                currentChunk = 0;
                            }
                            Building currentBuilding = _buildingManager.m_buildings.m_buffer[id];
                            Singleton<ImmaterialResourceManager>.instance.CheckLocalResource(ImmaterialResourceManager.Resource.LandValue, currentBuilding.m_position, out _preRainfallLandvalues[id]);
                        }

                    }
                    isRaining = true;

                    //Debug.Log("[RF]Hydrology.onUpdate1 Current Rainfall = " + _weatherManager.m_currentRain.ToString());


                    if (ModSettings.CityName != ModSettings.UnmoddedCityName && ModSettings.StormDistributionName != ModSettings.UnmoddedStormDistributionName && _weatherManager.m_targetRain > 0)
                    {
                        bool flag = true;
                        stormDuration = random.Next(ModSettings.MinimumStormDuration, ModSettings.MaximumStormDuration);
                        stormDuration -= stormDuration % ModSettings._stormTimeStep;
                        Debug.Log("[RF]Hydrology.OnUpdate1 stormDuration = " + stormDuration.ToString());
                        cityName = ModSettings.CityName;
                        //Debug.Log("[RF]Hydrology.OnUpdate1 cityName = " + cityName.ToString());
                        intensityCurveName = ModSettings.StormDistributionName;
                        //Debug.Log("[RF]Hydrology.OnUpdate1 intensity curve name = " + intensityCurveName.ToString());
                        intensityTargetLock = _weatherManager.m_targetRain;
                        //Debug.Log("[RF]Hydrology.OnUpdate1 intensityTargetLock = " + intensityTargetLock.ToString());
                        stormDepth = DepthDurationFrequencyIO.GetDepth(cityName, stormDuration, intensityTargetLock);
                        Debug.Log("[RF]Hydrology.OnUpdate1 storm depth = " + stormDepth.ToString());
                        SortedList<float, float> initialStormDepthCurve = new SortedList<float, float>();
                        bool flag1 = StormDistributionIO.GetDepthCurve(intensityCurveName, ref initialStormDepthCurve);
                        bool flag2 = StormDistributionIO.reviewDepthCurve(initialStormDepthCurve, ModSettings._maxStormDuration);
                        if (!flag1 || !flag2)
                        {
                            flag = false;
                            if (!flag1)
                                Debug.Log("[RF]Hydrology.OnUpdate Could not find Intensity Curve " + intensityCurveName);
                            if (!flag2)
                                Debug.Log("[RF]Hydrology.OnUpdate DepthCurve failed review " + intensityCurveName);
                        }
                        Debug.Log("[RF]Hydrology.OnUpdate1 flag1 = " + flag1.ToString());
                        if (flag)
                        {
                            StormDistributionIO.logCurve(initialStormDepthCurve, "Initial Storm Depth Curve");
                            bool flag3;
                            SortedList<float, float> reducedDepthCurve;
                            if (stormDuration < ModSettings._maxStormDuration)
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
                                stormDepthCurve = new SortedList<float, float>();
                                stormDepthCurve = StormDistributionIO.ScaleDepthCurve(reducedDepthCurve, stormDepth);
                                stormIntensityCurve = StormDistributionIO.GetIntensityCurve(stormDepthCurve);
                                StormDistributionIO.logCurve(stormDepthCurve, "Storm Depth Curve");
                                StormDistributionIO.logCurve(stormIntensityCurve, "Storm Intensity Curve");
                                if (StormDistributionIO.GetMaxValue(stormDepthCurve) <= 0)
                                {
                                    flag = false;
                                }
                                Debug.Log("[RF]Hydrology.OnUpdate1 Starting storm with Depth = " + StormDistributionIO.GetMaxValue(stormDepthCurve).ToString() + " And Max intensity " + StormDistributionIO.GetMaxValue(stormIntensityCurve).ToString());
                                beforeTickCurrentIntensity = rainStep;
                                //Debug.Log("[RF]Hydrology.OnUpdate1 beforeTickCurrentIntensity = " + beforeTickCurrentIntensity.ToString());

                                if (!previousStorm)
                                {
                                    stormTime = (decimal)(simulationTimeDelta * ModSettings.TimeScale);
                                    stormIntensityCurveKeyIndex = stormTime / (decimal)stormIntensityCurve.Keys[1];
                                    //Debug.Log("[RF]Hydrology.OnUpdate1 stormIntensityCurveKeyIndex = " + stormIntensityCurveKeyIndex.ToString());
                                }
                                else if (ModSettings.PreviousStormOption == 1)
                                {
                                    stormTime = (decimal)(simulationTimeDelta * ModSettings.TimeScale);
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
                                else if (ModSettings.PreviousStormOption == 2)
                                {
                                    stormTime = (decimal)(stormDuration);
                                    float temporaryIntensity = 0f;
                                    decimal stormIntensityCurveKeyIndexDelta = (decimal)(simulationTimeDelta * ModSettings.TimeScale) / (decimal)stormIntensityCurve.Keys[1];
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
                            if (flag)
                            {
                                improvedSimulation = true;
                            }
                            else
                            {
                                improvedSimulation = false;
                            }
                            //Debug.Log("[RF]Hydrology.OnUpdate1 simulationTimeDelta = " + simulationTimeDelta.ToString());
                            // Debug.Log("[RF]Hydrology.OnUpdate1 timeScale = " + ModSettings.TimeScale.ToString());
                            //Debug.Log("[RF]Hydrology.OnUpdate1 (simulationTimeDelta * ModSettings.TimeScale) = " + (simulationTimeDelta * ModSettings.TimeScale).ToString());

                        }

                        else
                        {
                            Debug.Log("[RF]Hydrology.OnUpdate1 finished before it started");
                        }

                    }
                    if (ModSettings.ChirpForecasts == true && improvedSimulation == true)
                    {
                        ChirpForecast.SendMessage(forecasterName, generateRainFallForecast());
                    }
                }
                // Debug.Log("[RF].Hydrology  Started to Rain with int = " + _weatherManager.m_currentRain.ToString() + " but will rain " + _weatherManager.m_targetRain.ToString());
            }
            else if (_weatherManager.m_currentRain > 0 && isRaining == true && simulationTimeDelta > 0)
            {
                if (ModSettings.ChirpRainTweets == true && random.Next(0,10000) < ChirpRainTweetChance )
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
                foreach (ushort id in _newBuildingIDs)
                {
                    _buildingIDs.Add(id);
                    if (calculateFlowRate(id, _weatherManager.m_currentRain) > 0u)
                    {
                        _waterSources.Add(newWaterSourceAtBuidling(id));
                        //Debug.Log("[RF].Hydrology  Added Water for building " + id.ToString() + " in _newBuildingID");
                    }
                }
                _newBuildingIDs.Clear();
                foreach (ushort id in _removeBuildingIDs)
                {
                    removeWaterSourceAtBuilding(id);
                    _buildingIDs.Remove(id);
                    //Debug.Log("[RF].Hydrology  Removed Water for building " + id.ToString() + " from _removeBuildingID");
                }
                _removeBuildingIDs.Clear();

                //Debug.Log("[RF].Hydrology Finished Chunks = " + finishedChunks.ToString() + " _realTimeCount = " + _realTimeCount.ToString());
                while (finishedChunks+1 < _realTimeCount)
                {
                    //Debug.Log("[RF].Hydrology _buildingIDChunks[finishedChunks].length = " + _buildingIDChunks[finishedChunks].Count);
                    if (_buildingIDChunks[finishedChunks].Count > 0)
                    {
                        foreach (ushort id in _buildingIDChunks[finishedChunks])
                        {
                            uint flowRate = (uint)calculateFlowRate(id, _weatherManager.m_currentRain);
                            //Debug.Log("[RF].Hydrology  " + "Building " + id.ToString() + " is flowing at " + flowRate);
                            //Debug.Log("[RF].Hydrology  wsID.len = " + _waterSourceIDs.Length + " id is " + id);
                            if (id <= _capacity && simulationTimeDelta > 0 && _buildingIDs.Contains(id))
                            {
                                if (_waterSourceIDs[id] != 0 /*&& ModSettings.EasyMode == false*/)
                                {
                                    WaterSource currentSource = _waterSimulation.LockWaterSource(_waterSourceIDs[id]);
                                    Building currentBuilding = _buildingManager.m_buildings.m_buffer[id];
                                    BuildingAI currentBuildingAI = currentBuilding.Info.m_buildingAI;
                                    //Debug.Log("[RF].Hydrology  " + id.ToString() + " building has flow " + currentSource.m_flow);
                                    if (flowRate > 0u)
                                    {
                                        if (cleanUpCycle == false)
                                        {
                                            currentSource.m_outputRate = flowRate;
                                        } else
                                        {
                                            currentSource.m_inputRate = 100u;
                                            currentSource.m_outputRate = 0;
                                        }
                                        currentSource.m_flow = flowRate;
                                        currentSource.m_water = waterSourceMaxQuantitiy;
                                        int buildingPollutionAccumulation;
                                        int buildingNoiseAccumulation;
                                        currentBuildingAI.GetPollutionAccumulation(out buildingPollutionAccumulation, out buildingNoiseAccumulation);
                                        if (buildingPollutionAccumulation > 0 && ModSettings.SimulatePollution)
                                        {
                                            //Debug.Log("[RF]Hydrology.Update building " + id + " pollution accumulation = " + buildingPollutionAccumulation.ToString());
                                            currentSource.m_pollution = (uint)Mathf.Max((float)currentSource.m_water, (float)currentSource.m_water * ((float)buildingPollutionAccumulation / 100f));

                                        }
                                        //currentSource.m_pollution += (uint)calculatePollution(id, _weatherManager.m_currentRain);
                                        //Debug.Log("[RF].Hydrology  water source at building " + id.ToString() + " water is " + currentSource.m_water.ToString());
                                    }
                                    /*
                                     if (simulationTimeDelta > 0)
                                    {
                                        Debug.Log("[RF].Hydrology  " + id.ToString() + " building has flow " + currentSource.m_flow);
                                        Debug.Log("[RF].Hydrology  " + id.ToString() + " building has water " + currentSource.m_water);
                                        Debug.Log("[RF].Hydrology  " + id.ToString() + " building has pollution " + currentSource.m_pollution);
                                    }
                                    */
                                    _waterSimulation.UnlockWaterSource(_waterSourceIDs[id], currentSource);
                                    if (flowRate <= 0u)
                                    {
                                        removeWaterSourceAtBuilding(id);
                                    }
                                }
                                else if (flowRate > 0u /*&& ModSettings.EasyMode == false*/)
                                {
                                    _waterSources.Add(newWaterSourceAtBuidling(id));
                                    //Debug.Log("[RF].Hydrology  Added Water for building " + id.ToString());
                                } else if (flowRate > 0u)
                                {
                                    Debug.Log("[RF].Hydrology Raining Easy Mode Adding SWA");
                                    Hydraulics.addStormwaterAccumulation(id, calculateFlowRate(id, flowRate));
                                }
                            }
                            else if (simulationTimeDelta > 0)
                            {
                                Debug.Log("[RF].Hydrology  ID is larger than Capactity. Cannot add source at building " + id.ToString() + " Capacity is " + _capacity.ToString());
                            }
                        }
                    }
                    finishedChunks++;
                }

                if (_realTimeCount > ModSettings.RefreshRate)
                {
                    _realTimeCount = 0;
                    currentChunk = 0;
                    finishedChunks = 0;
                    for (int chunks = 0; chunks < ModSettings._maxRefreshRate; chunks++)
                    {
                        _buildingIDChunks[chunks].Clear();
                    }
                    foreach (ushort id in _buildingIDs)
                    {
                        if (id <= _capacity)
                        {
                            _buildingIDChunks[currentChunk].Add(id);
                            if (currentChunk+1 < ModSettings.RefreshRate)
                            {
                                currentChunk++;
                            }
                            else
                            {
                                currentChunk = 0;
                            }
                        }
                    }
                }
                else if (simulationTimeDelta > 0) {
                    _realTimeCount += realTimeDelta;
                    if (improvedSimulation)
                    {
                        //Debug.Log("[RF]Hydrology.onUpdate2 Current Rainfall = " + _weatherManager.m_currentRain.ToString());
                        stormTime += (decimal)(simulationTimeDelta * ModSettings.TimeScale);
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
                            } else if (stormTime < (decimal)timeRangeMinimum)
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
                            improvedSimulation = false;
                            Debug.Log("[RF]Hydrology.OnUpdate2 stormTime = " + stormTime.ToString());
                            Debug.Log("[RF]Hydrology.OnUpdate2 stormIntensityCurveKeyIndexFloor = " + stormIntensityCurveKeyIndexFloor.ToString());
                            Debug.Log("[RF]Hydrology.OnUpdate2 stormIntensityCurveKeyIndexCeil = " + stormIntensityCurveKeyIndexCeil.ToString());
                            Debug.Log("[RF]Hydrology.OnUpdate2 timeRangeMinimum = " + timeRangeMinimum.ToString());
                            Debug.Log("[RF]Hydrology.OnUpdate2 timeRangeMaximum = " + timeRangeMaximum.ToString());
                            Debug.Log("[RF]Hydrology.OnUpdate2 attempts = " + attempts.ToString());
                            Debug.Log("[RF]Hydrology.OnUpdate Could not find time range for stormTime = " + stormTime.ToString());
                        } else
                        {
                            timeRange = timeRangeMaximum - timeRangeMinimum;
                            decimal timePercentage = (stormTime - (decimal)timeRangeMinimum) / (decimal)timeRange;
                            stormIntensityCurveKeyIndex = (decimal)stormIntensityCurveKeyIndexFloor + timePercentage;
                            //Debug.Log("[RF]Hydrology Update stormTime = " + stormTime.ToString() + " of " + stormDuration.ToString() + " rainfall = " + _weatherManager.m_currentRain.ToString() + " max = " + StormDistributionIO.GetMaxValue(stormIntensityCurve) + " total depth = " + StormDistributionIO.GetMaxDepth(stormIntensityCurve));
                        }
                    }
                }
                //Debug.Log("[RF].Hydrology  Raining with int = " + _weatherManager.m_currentRain.ToString() + " but will rain " + _weatherManager.m_targetRain.ToString());
            }
            else if (_weatherManager.m_currentRain == 0 && isRaining == true)
            {
                //Debug.Log("[RF].Hydrology  _buildings is " + _buildingIDs.Count.ToString());
                foreach (ushort id in _buildingIDs)
                {
                    if (id <= _capacity)
                    {
                        //Debug.Log("[RF].Hydrology  id = " + id.ToString());
                        //Debug.Log("[RF].Hydrology  _waterSources.count = " + _waterSources.Count.ToString());
                        //Debug.Log("[RF].Hydrology  _waterSourceIDs[id] = " + _waterSourceIDs[id].ToString());
                        //Debug.Log("[RF].Hydrology  _waterSource has Output Rate = " + _waterSimulation.m_waterSources.m_buffer[_waterSourceIDs[id]].m_outputRate.ToString());
                        if (_waterSourceIDs[id] != 0)
                        {
                            removeWaterSourceAtBuilding(id);
                            //Debug.Log("[RF].Hydrology  Removed Water for building " + id.ToString() + " from _BuildingID");
                        }
                        else { 
                            //Debug.Log("[RF].Hydrology  There was no water source to remove from building " + id.ToString());
                        }
                
                    } else
                    {
                        Debug.Log("[RF].Hydrology  ID is larger than Capactity. Cannot remove source at building " + id.ToString() + " Capacity is " + _capacity.ToString());
                    }
                }
                isRaining = false;
                cleanUpCycle = false;
                _preRainfallLandvalues = new int[_capacity];
               //Debug.Log("[RF].Hydrology  No longer Raining. int = " + _weatherManager.m_currentRain.ToString() + " but will rain " + _weatherManager.m_targetRain.ToString());
            }

            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }
        public override void OnAfterSimulationTick()
        {
            if (isRaining)
            {
                //Debug.Log("[RF]Hydrology.onAfterSimulationTick Current Rainfall = " + _weatherManager.m_currentRain.ToString());
                if (isRaining) {
                  
                    if (improvedSimulation)
                    {
                        if (_weatherManager.m_targetRain != intensityTargetLock && intensityTargetLock > 0)
                        {
                            _weatherManager.m_targetRain = intensityTargetLock;
                            _weatherManager.m_currentRain = beforeTickCurrentIntensity;
                        }
                        if (stormTime < (decimal)stormDuration && Mathf.Abs(_weatherManager.m_currentRain - beforeTickCurrentIntensity) <= 0.5)
                        {
                            _weatherManager.m_currentRain = beforeTickCurrentIntensity;
                        } else {
                            Debug.Log("[RF]Hydrology.OnAfterTick stormTime < StormDuration " + (stormTime < (decimal)stormDuration).ToString());
                            Debug.Log("[RF]Hydrology.OnAfterTick CurrentRain-BTCI = " + (Mathf.Abs(_weatherManager.m_currentRain - beforeTickCurrentIntensity)).ToString() + " CR-CTCI<0.01 = " + (Mathf.Abs(_weatherManager.m_currentRain - beforeTickCurrentIntensity) <= 0.01).ToString());
                            improvedSimulation = false;
                            Debug.Log("[RF]Hydrology.OnAfterTick Ended improved simulation stormTime = " + stormTime.ToString() + " stormDuration = " + stormDuration.ToString() + " currentRain = " + _weatherManager.m_currentRain + " bTCI = " + beforeTickCurrentIntensity);
                        }
                    }
                    
                }
            }
            base.OnAfterSimulationTick();
        }


        public WaterSource newWaterSourceAtBuidling(ushort id)
        {

            WaterSource surfaceflow = default(WaterSource);
            /*if (ModSettings.EasyMode == true)
            {
                Debug.Log("[RF] newWaterSourceAtBuilding EasyMode returning SurfaceFlow ");
                return surfaceflow;
            }*/

            uint flowRate = (uint)calculateFlowRate(id, _weatherManager.m_currentRain);

            surfaceflow.m_flow = flowRate;
            if (cleanUpCycle == false)
            {
                surfaceflow.m_inputRate = 0u;
                surfaceflow.m_outputRate = flowRate;
            } else
            {
                surfaceflow.m_inputRate = 100u;
                surfaceflow.m_outputRate = 0;
            }

            surfaceflow.m_outputPosition = _buildingManager.m_buildings.m_buffer[id].CalculateSidewalkPosition();
            Building currentBuilding = this._buildingManager.m_buildings.m_buffer[id];
            BuildingAI currentBuildingAI = currentBuilding.Info.m_buildingAI;
            if (currentBuildingAI is NaturalDrainageAI)
            {
                Vector3 drainageAreaPosition = currentBuilding.m_position;
                int miny;
                int avgy;
                int maxy;
                _terrainManager.CalculateAreaHeight(drainageAreaPosition.x - currentBuilding.Width / 2, drainageAreaPosition.z + currentBuilding.Length / 2, drainageAreaPosition.x + currentBuilding.Width / 2, drainageAreaPosition.z + currentBuilding.Length / 2, out miny, out avgy, out maxy);
                surfaceflow.m_inputPosition = new Vector3(drainageAreaPosition.x, maxy, drainageAreaPosition.z);
                surfaceflow.m_outputPosition = surfaceflow.m_inputPosition;
            } else {
                Vector3 buildingPosition = currentBuilding.m_position;
                Vector3 sidewalkPosition = currentBuilding.CalculateSidewalkPosition();
                Vector3 SidewalkSetback = buildingPosition - sidewalkPosition;
                float distanceFromSidewalk = 8f;
                float scaleValue = (SidewalkSetback.magnitude + distanceFromSidewalk) / SidewalkSetback.magnitude;
                Vector3 Scaler = new Vector3(scaleValue, scaleValue, scaleValue);
                Vector3 StreetSetback = Vector3.Scale(SidewalkSetback, Scaler);
                Vector3 StreetPosition = buildingPosition - StreetSetback;
                //Debug.Log("old height " + StreetPosition.y.ToString());
                //StreetPosition.y = _terrainManager.Calc(StreetPosition.x, StreetPosition.y);
                int miny;
                int avgy;
                int maxy;
                _terrainManager.CalculateAreaHeight(StreetPosition.x, StreetPosition.z, StreetPosition.x, StreetPosition.z, out miny, out avgy, out maxy);
                float avgyf = (float)avgy / 64f;
                //Debug.Log("Miny " + minyf.ToString() + " avgy " + avgyf.ToString() + " maxy " + maxyf.ToString());
                StreetPosition.y = avgyf;
                surfaceflow.m_inputPosition = StreetPosition;
                surfaceflow.m_outputPosition = StreetPosition;
            }
            ushort target = (ushort)(surfaceflow.m_outputPosition.y + 25);
            target = (ushort)Mathf.Clamp(target, 0, 65535);
            
            
            //Debug.Log("[STORM DRAINS] Building" + id.ToString() + " pos is " + buildingPosition.ToString());
           // Debug.Log("[STORM DRAINS] Building " + id.ToString() + " sidewalk pos is " + sidewalkPosition.ToString());
            //Debug.Log("[STORM DRAINS] Building " + id.ToString() + " street pos is " + StreetPosition.ToString());
            surfaceflow.m_type = 2;
            surfaceflow.m_target = target;
            surfaceflow.m_water = waterSourceMaxQuantitiy;
            int buildingPollutionAccumulation;
            int buildingNoiseAccumulation;
            currentBuildingAI.GetPollutionAccumulation(out buildingPollutionAccumulation, out buildingNoiseAccumulation);
            if (buildingPollutionAccumulation > 0 && ModSettings.SimulatePollution)
            {
                //Debug.Log("[RF]Hydrology.Update building " + id + " pollution accumulation = " + buildingPollutionAccumulation.ToString());
                surfaceflow.m_pollution = (uint)Mathf.Max((float)surfaceflow.m_water, (float)surfaceflow.m_water * ((float)buildingPollutionAccumulation / 100f));

            }
            ushort num;
            _waterSimulation.CreateWaterSource(out num, surfaceflow);
            _waterSourceIDs[id] = num;
            return surfaceflow;
        }
        public void removeWaterSourceAtBuilding(ushort id)
        {
            if (id <= _capacity)
            {
                _waterSimulation.ReleaseWaterSource(_waterSourceIDs[id]);
                _waterSources.Remove(_waterSimulation.m_waterSources.m_buffer[_waterSourceIDs[id]]);
                _waterSourceIDs[id] = 0;
            } else
            {
                Debug.Log("[RF].Hydrology  ID is larger than Capacity. Cannot remove source at building " + id.ToString() + " Capacity is " + _capacity.ToString());
            }
        }
        public int calculateFlowRate(ushort id, float rainIntensity)
        {
            Building _currentBuilding = _buildingManager.m_buildings.m_buffer[id];
            int Area = _buildingManager.m_buildings.m_buffer[id].Length * _buildingManager.m_buildings.m_buffer[id].Width;

            BuildingAI ai = _buildingManager.m_buildings.m_buffer[id].Info.m_buildingAI;
            
            float runoffCoefficient = 0f;
            if (ai is PlayerBuildingAI) {
                if (ai is CemeteryAI)
                    runoffCoefficient = ModSettings.PublicBuildingsRunoffCoefficients["CemetaryAI"].Coefficient;
                else if (ai is ParkAI)
                    runoffCoefficient = ModSettings.PublicBuildingsRunoffCoefficients["ParkAI"].Coefficient;
                else if (ai is LandfillSiteAI)
                    runoffCoefficient = ModSettings.PublicBuildingsRunoffCoefficients["LandfillSiteAI"].Coefficient;
                else if (ai is FireStationAI)
                    runoffCoefficient = ModSettings.PublicBuildingsRunoffCoefficients["FirestationAI"].Coefficient;
                else if (ai is PoliceStationAI)
                    runoffCoefficient = ModSettings.PublicBuildingsRunoffCoefficients["PoliceStationAI"].Coefficient;
                else if (ai is HospitalAI)
                    runoffCoefficient = ModSettings.PublicBuildingsRunoffCoefficients["HospitalAI"].Coefficient;
                else if (ai is PowerPlantAI)
                    runoffCoefficient = ModSettings.PublicBuildingsRunoffCoefficients["PowerPlantAI"].Coefficient;
                else if (ai is WindTurbineAI)
                    runoffCoefficient = 0.0f;
                else if (ai is StormDrainAI)
                    runoffCoefficient = 0.0f;
                else if (ai is NaturalDrainageAI)
                {
                    NaturalDrainageAI naturalDrainage = ai as NaturalDrainageAI;
                    runoffCoefficient = ModSettings.PublicBuildingsRunoffCoefficients["NaturalDrainageAI"].Coefficient * naturalDrainage.m_naturalDrainageMultiplier;
                }

                else if (ai is WaterFacilityAI)
                    runoffCoefficient = 0.0f;
                else if (ai is DepotAI)
                    runoffCoefficient = ModSettings.PublicBuildingsRunoffCoefficients["DepotAI"].Coefficient;
                else if (ai is AnimalMonumentAI)
                    runoffCoefficient = 0.0f;
                else if (ai is DecorationBuildingAI)
                    runoffCoefficient = 0.0f;
                else if (ai is CargoStationAI)
                    runoffCoefficient = ModSettings.PublicBuildingsRunoffCoefficients["CargoStationAI"].Coefficient;
                else if (ai is PowerPoleAI)
                    runoffCoefficient = 0.0f;
                else if (ai is SaunaAI)
                    runoffCoefficient = ModSettings.PublicBuildingsRunoffCoefficients["SaunaAI"].Coefficient;
                else if (ai is SnowDumpAI)
                    runoffCoefficient = 0.0f;
                else if (ai is SchoolAI)
                    runoffCoefficient = ModSettings.PublicBuildingsRunoffCoefficients["SchoolAI"].Coefficient;
                else
                    runoffCoefficient = ModSettings.PublicBuildingsRunoffCoefficients["OtherPlayerBuildingAI"].Coefficient;
            }
            else if (ai is PrivateBuildingAI)
            {
                if (ai is ResidentialBuildingAI && (_currentBuilding.m_flags & Building.Flags.HighDensity) == Building.Flags.None)
                    runoffCoefficient = ModSettings.PrivateBuildingsRunoffCoefficients["LowDensityResidentialBuildingAI"].Coefficient;
                else if (ai is CommercialBuildingAI && (_currentBuilding.m_flags & Building.Flags.HighDensity) == Building.Flags.None || ai is OfficeBuildingAI)
                    runoffCoefficient = ModSettings.PrivateBuildingsRunoffCoefficients["LowDensityCommercialBuildingAI"].Coefficient;
                else if (ai is ResidentialBuildingAI && (_currentBuilding.m_flags & Building.Flags.HighDensity) != Building.Flags.None)
                    runoffCoefficient = ModSettings.PrivateBuildingsRunoffCoefficients["HighDensityResidentialBuildingAI"].Coefficient;
                else if (ai is CommercialBuildingAI && (_currentBuilding.m_flags & Building.Flags.HighDensity) != Building.Flags.None)
                    runoffCoefficient = ModSettings.PrivateBuildingsRunoffCoefficients["HighDensityCommercialBuildingAI"].Coefficient;
                else if (ai is IndustrialBuildingAI)
                {
                    if (_buildingManager.m_buildings.m_buffer[id].Info.GetSubService() == ItemClass.SubService.IndustrialFarming)
                        runoffCoefficient = ModSettings.PrivateBuildingsRunoffCoefficients["FarmingIndustrialBuildingAI"].Coefficient;
                    else if (_buildingManager.m_buildings.m_buffer[id].Info.GetSubService() == ItemClass.SubService.IndustrialForestry)
                        runoffCoefficient = ModSettings.PrivateBuildingsRunoffCoefficients["ForestryIndustrialBuildingAI"].Coefficient;
                    else if (_buildingManager.m_buildings.m_buffer[id].Info.GetSubService() == ItemClass.SubService.IndustrialOre)
                        runoffCoefficient = ModSettings.PrivateBuildingsRunoffCoefficients["OreIndustrialBuildingAI"].Coefficient;
                    else if (_buildingManager.m_buildings.m_buffer[id].Info.GetSubService() == ItemClass.SubService.IndustrialOil)
                        runoffCoefficient = ModSettings.PrivateBuildingsRunoffCoefficients["OilIndustryBuildingAI"].Coefficient;
                    else
                        runoffCoefficient = ModSettings.PrivateBuildingsRunoffCoefficients["GenericIndustryBuildingAI"].Coefficient;
                }
                else
                    runoffCoefficient = ModSettings.PrivateBuildingsRunoffCoefficients["OtherPrivateBuildingAI"].Coefficient;
            } else
            {
                runoffCoefficient = 0.5f;
            }
                
            return Mathf.RoundToInt(runoffCoefficient*rainIntensity*Area*(ModSettings.Difficulty*0.01f));
        }
        public bool reviewBuilding(int id)
        {
            if ((_buildingManager.m_buildings.m_buffer[id].m_flags & Building.Flags.Created) == Building.Flags.None || (_buildingManager.m_buildings.m_buffer[id].m_flags & Building.Flags.Untouchable) != Building.Flags.None || (_buildingManager.m_buildings.m_buffer[id].m_flags & Building.Flags.BurnedDown) != Building.Flags.None || (_buildingManager.m_buildings.m_buffer[id].m_flags & Building.Flags.Demolishing) != Building.Flags.None || (_buildingManager.m_buildings.m_buffer[id].m_flags & Building.Flags.Deleted) != Building.Flags.None)
            { 
               // Debug.Log("[RF].Hydrology  Failed Flag Test: " + _buildingManager.m_buildings.m_buffer[id].m_flags.ToString());
                return false;
            }
            BuildingAI ai = _buildingManager.m_buildings.m_buffer[id].Info.m_buildingAI;
            if (ai is WaterFacilityAI || ai is WindTurbineAI || ai is WildlifeSpawnPointAI || ai is AnimalMonumentAI || ai is PowerPoleAI || ai is DecorationBuildingAI) {
               // Debug.Log("[RF].Hydrology  Failed AI Test: " + ai.ToString());
                return false;
            }
            if (ai is StormDrainAI)
            {
               
                    return false;
            }
            return true;
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
            beforeDepthAdjectiveStatements = new List<string>();
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

            beforeDepthAdjectiveStatements.Add("We are anticipating a");
            beforeDepthAdjectiveStatements.Add("We are expecting a");
            beforeDepthAdjectiveStatements.Add("This storm will drop a");
            beforeDepthAdjectiveStatements.Add("The depth of the storm is forecasted to be a");

            depthAdjectives.Add(0.25f, " measly");
            depthAdjectives.Add(0.5f, " mild");
            depthAdjectives.Add(0.75f, " small");
            depthAdjectives.Add(1.0f, " normal");
            depthAdjectives.Add(1.25f, "n average");
            depthAdjectives.Add(1.5f, "n above average");
            depthAdjectives.Add(1.75f, " significant");
            depthAdjectives.Add(2.0f, " large");
            depthAdjectives.Add(2.5f, " heavy");
            depthAdjectives.Add(3.0f, " whopping");
            depthAdjectives.Add(4.0f, " formidable");
            depthAdjectives.Add(5.0f, " extreme");
            depthAdjectives.Add(7.0f, " insane");
            depthAdjectives.Add(9.0f, " godly");
            depthAdjectives.Add(10.0f, " astronomical");
            depthAdjectives.Add(1000f, " ark-worthy");

            beforeIntensityAdjectiveStatements.Add(" units of rain with a max intensity of ");
            beforeIntensityAdjectiveStatements.Add(" units of rainfall that will hit a peak intensity of ");
            beforeIntensityAdjectiveStatements.Add(" units after hitting a climax of ");

            intensityAdjectives.Add(0.5f, " measly");
            intensityAdjectives.Add(0.75f, " mild");
            intensityAdjectives.Add(1.0f, " small");
            intensityAdjectives.Add(1.25f, " normal");
            intensityAdjectives.Add(1.5f, "n average");
            intensityAdjectives.Add(1.75f, "n above average");
            intensityAdjectives.Add(2.5f, " significant");
            intensityAdjectives.Add(3.0f, " large");
            intensityAdjectives.Add(4.0f, " heavy");
            intensityAdjectives.Add(5.0f, " whopping");
            intensityAdjectives.Add(7.5f, " formidable");
            intensityAdjectives.Add(9.0f, " extreme");
            intensityAdjectives.Add(12.0f, " insane");
            intensityAdjectives.Add(15.0f, " godly");
            intensityAdjectives.Add(20.0f, " astronomical");
            intensityAdjectives.Add(1000f, " ark-worthy");

            beforeReturnRateStatements.Add("Analysts are predicting this type of event occurs every ");
            beforeReturnRateStatements.Add("We haven't seen a storm like this in the past ");
            beforeReturnRateStatements.Add("We shouldn't see another storm like this for the next ");
            beforeReturnRateStatements.Add("Records show that a storm like this occured only once in the past ");

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
            fullForecast.Append(convertMinToHourMin(stormDuration-Mathf.Round((float)stormTime)) + " ");
            fullForecast.Append(randomString(beforeDepthAdjectiveStatements));


            foreach(KeyValuePair<float, string> pair in depthAdjectives)
            {
                if (stormDepth <= pair.Key)
                {

                    fullForecast.Append(pair.Value + " " + (Mathf.Floor(stormDepth*10f)/10f).ToString());
                    break;
                }
            }

            fullForecast.Append(randomString(beforeIntensityAdjectiveStatements));
            float maxIntensity = StormDistributionIO.GetMaxValue(stormIntensityCurve);
            /*foreach (KeyValuePair<float, string> pair in intensityAdjectives)
            {
                if (maxIntensity <= pair.Key)
                {
                    fullForecast.Append(pair.Value + " " + (Mathf.Floor(maxIntensity * 10f) / 10f).ToString() +  " units/hr. ");
                    break;
                }
            }*/
            fullForecast.Append((Mathf.Floor(maxIntensity * 10f) / 10f).ToString() + " units/hr");
            /*fullForecast.Append(randomString(beforeReturnRateStatements));
            fullForecast.Append(Mathf.FloorToInt((DepthDurationFrequencyIO.GetReturnPeriod(intensityTargetLock))).ToString());
            fullForecast.Append(" year");
            if (Mathf.FloorToInt((DepthDurationFrequencyIO.GetReturnPeriod(intensityTargetLock))) >= 1)
                fullForecast.Append("s");
            */
            fullForecast.Append(". "+randomString(closingStatements));
            fullForecast.Append(" #RainForecast");
            return fullForecast.ToString();

        }
        private string convertMinToHourMin(float time)
        {
            string min = ((int)time % 60).ToString();
            string hour = ((int)time / 60).ToString();
            if (((int)time / 60) > 0)
                return (hour + " hr. and " + min + " min.");
            return (min + " min.");
        }
        public static HashSet<ushort> getBuildingList()
        {
            return Hydrology.instance._buildingIDs;
        }
    }
}