using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Math;
using ICities;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Reflection;
using System.Linq;

namespace Rainfall
{
    public class Hydraulics : ThreadingExtensionBase
    {
        private BuildingManager _buildingManager;
        private DistrictManager _districtManager;
        private TerrainManager _terrainManager;
        private WaterSimulation _waterSimulation; 
        private UIPanel serviceBuildingInfo;
        private CityServiceWorldInfoPanel _cityServiceWorldInfoPanel;
        private GameObject buildingWindowGameObject;
        private FieldInfo baseSub;
        public static Hydraulics instance = null;
        private int _capacity;
        public bool initialized;
        public bool loaded;
        public bool terminated;
        private int[] _stormwaterAccumulation;
        private int[] _detainedStormwater;
        private byte[] _districts;
        private float _simulationTimeCount;
        private float _infiltrationPeriod;
        private bool created = false;
        private float _relocateDelay;
        private int[] _hydraulicRate;
        
        private int[] _pollutants;
        private int[] _variableCapacity;
        private float eightyOneTilesDelay = 30f;
        
        private HashSet<ushort> _SDinlets;
        private HashSet<ushort> _SDoutlets;
        private HashSet<ushort> _SDdetentionBasins;
        
        
        public  HashSet<ushort> _previousFacilityWaterSources;
        private HashSet<ushort> _naturalDrainageAssets;
        //private HashSet<ushort> _snowpackAssets;
        public HashSet<ushort> _snowpackAssets10;
        public HashSet<ushort> _snowpackAssets20;
        public HashSet<ushort> _snowpackAssets30;


        private string[] _drainageGroups;
        private HashSet<string> _drainageGroupsNames;
        public Queue  _unpairedUpstreamCulverts;
        public Queue  _unpairedDownstreamCulverts;

        public HashSet<ushort> netsegments;

        public bool[] snowpackAssetsInitialized = { false, false, false };

        //private int _lastUsedCulvert = 0;
        public Hydraulics()
        {
        }

        public static void deinitialize()
        {
            Hydraulics.instance.initialized = false;
            Hydraulics.instance.loaded = false;
            Hydraulics.instance.terminated = true;
        }
        private void InitializeManagers()
        {
            _buildingManager = Singleton<BuildingManager>.instance;
            _districtManager = Singleton<DistrictManager>.instance;
            _terrainManager = Singleton<TerrainManager>.instance;
            _waterSimulation = _terrainManager.WaterSimulation;
            //Debug.Log("Initializing Managers()");
            /*Debug.Log("milestone names are:");
            foreach (KeyValuePair<string, MilestoneInfo> pair in Singleton<UnlockManager>.instance.m_allMilestones)
            {
                Debug.Log(pair.Key);
            }*/

        }

        public override void OnCreated(IThreading threading)
        {
            
            instance = this;
            initialized = false;
            loaded = false;
            InitializeManagers();
            //Debug.Log("[RF].Hydraulics.initializing Initialized managers");
            _capacity = _buildingManager.m_buildings.m_buffer.Length;
            _stormwaterAccumulation = new int[_capacity];
            _detainedStormwater = new int[_capacity];
            _districts = new byte[_capacity];
            _hydraulicRate = new int[_capacity];
            _variableCapacity = new int[_capacity];
            
            _drainageGroups = new string[_capacity];
            
            _SDinlets = new HashSet<ushort>();
            _SDoutlets = new HashSet<ushort>();
            _SDdetentionBasins = new HashSet<ushort>();
            //_snowpackAssets = new HashSet<ushort>();
            _naturalDrainageAssets = new HashSet<ushort>();
            netsegments = new HashSet<ushort>();
           
            _previousFacilityWaterSources = new HashSet<ushort>();
            _unpairedUpstreamCulverts = new Queue();
            _unpairedDownstreamCulverts = new Queue();
            _simulationTimeCount = 0f;
            _relocateDelay = 0f;
            _infiltrationPeriod = 1f;
            
            _drainageGroupsNames = new HashSet<string>();
            created = true;
            //_snowpackAssets10 = new HashSet<ushort>();
            //_snowpackAssets20 = new HashSet<ushort>();
            //_snowpackAssets30 = new HashSet<ushort>();
            //Debug.Log("[RF].Hydraulics Created!");
            base.OnCreated(threading);
        }

        
        public override void OnBeforeSimulationTick()
        {
            
            base.OnBeforeSimulationTick();
        }

        private static IEnumerable<UIPanel> GetUIPanelInstances() => UIView.library.m_DynamicPanels.Select(p => p.instance).OfType<UIPanel>();
        private static string[] GetUIPanelNames() => GetUIPanelInstances().Select(p => p.name).ToArray();
        private UIPanel GetPanel(string name)
        {
            return GetUIPanelInstances().FirstOrDefault(p => p.name == name);
        }

    public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
          
            if (terminated)
                return;

            if (!loaded)
                return;

            if (!created)
                return;

            /*
            if (eightyOneTilesDelay > 0f)
            {
                eightyOneTilesDelay -= realTimeDelta;
                return;
            }*/

            if (!initialized)
            {
                //Debug.Log("[RF].Hydraulics.initializing Initializing!");
                InitializeManagers();
                //Debug.Log("[RF].Hydraulics.initializing Initialized managers");
                _SDinlets.Clear();
                _SDoutlets.Clear();
                _SDdetentionBasins.Clear();
                //Debug.Log("[RF].Hydraulics.initializing Cleared hashsets");

                buildingWindowGameObject = new GameObject("buildingWindowObject");
                //serviceBuildingInfo = UIView.Find<UIPanel>("(Library) CityServiceWorldInfoPanel");
                serviceBuildingInfo = GetPanel("(Library) CityServiceWorldInfoPanel");
                _cityServiceWorldInfoPanel = serviceBuildingInfo.gameObject.transform.GetComponentInChildren<CityServiceWorldInfoPanel>();
                //Debug.Log("[RF].Hydraulics.initializing WorldServiceInfoPanelStuff");
                _capacity = _buildingManager.m_buildings.m_buffer.Length;
                _stormwaterAccumulation = new int[_capacity];
                _districts = new byte[_capacity];
                _pollutants = new int[_capacity];
                //Debug.Log("Initialized arrays");
                initialized = true;
                terminated = false;
                //Debug.Log("Initialized");
                for (ushort id = 0; id < _capacity; id++)
                {
                    //Fix it here
                    try
                    {
                        Building currentBuilding = _buildingManager.m_buildings.m_buffer[id];
                   
                        bool flag = (currentBuilding.m_flags & Building.Flags.Completed) != Building.Flags.None;
                        if (flag == true)
                        {
                            StormDrainAI currentStormDrainAi;
                            WaterFacilityAI currentWaterFacilityAI;
                            currentWaterFacilityAI = currentBuilding.Info.m_buildingAI as WaterFacilityAI;

                            currentStormDrainAi = currentBuilding.Info.m_buildingAI as StormDrainAI;
                            NaturalDrainageAI currentNaturalDrinageAI = currentBuilding.Info.m_buildingAI as NaturalDrainageAI;
                            //SnowpackAI currentSnowpackAI = currentBuilding.Info.m_buildingAI as SnowpackAI;

                            if (currentStormDrainAi != null)
                            {
                                if (currentStormDrainAi.m_stormWaterOutlet != 0)
                                {

                                    addOutlet(id);
                                    if (currentBuilding.m_waterSource != 0)
                                        _previousFacilityWaterSources.Add(currentBuilding.m_waterSource);
                                    //Debug.Log("[RF].Hydraulics Added Outlet " + id.ToString() + " to district " + _districts[id]);
                                }
                                else if (currentStormDrainAi.m_stormWaterIntake != 0)
                                {

                                    addInlet(id);
                                    if (currentBuilding.m_waterSource != 0)
                                        _previousFacilityWaterSources.Add(currentBuilding.m_waterSource);
                                    //Debug.Log("[RF].Hydraulics Added Inlet " + id.ToString() + " to district " + _districts[id]);

                                }
                                else if (currentStormDrainAi.m_stormWaterDetention != 0)
                                {

                                    addDetentionBasin(id);
                                    //Debug.Log("[RF].Hydraulics Added detention basin " + id.ToString() + " to district " + _districts[id]);
                                }
                            }
                            else if (currentWaterFacilityAI != null)
                            {
                                if (currentBuilding.m_waterSource != 0)
                                    _previousFacilityWaterSources.Add(currentBuilding.m_waterSource);
                            }
                            else if (currentNaturalDrinageAI != null)
                            {
                                addNaturalDrainageAsset(id);
                            }/*
                            else if (currentSnowpackAI != null)
                            {
                                addSnowpackAsset(id);
                                if (currentSnowpackAI.m_temperatureDifference == -10)
                                {
                                    Hydraulics.instance._snowpackAssets10.Add(id);
                                }
                                else if (currentSnowpackAI.m_temperatureDifference == -20)
                                {
                                    Hydraulics.instance._snowpackAssets20.Add(id);
                                }
                                else if (currentSnowpackAI.m_temperatureDifference == -30)
                                {
                                    Hydraulics.instance._snowpackAssets30.Add(id);
                                }
                            }*/
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log("[RF].Hydraulics.Inialize Encountered Exception " + e);
                    }

                }
                Debug.Log("[RF].Hydraulics Initialized!");  
            }
            else
            {
                /*
                HashSet<ushort> _SDInletIterator = _SDinlets; 
                foreach (ushort id in _SDInletIterator)
                {
                    if (reviewID(id))
                    {
                        Building currentBuilding = _buildingManager.m_buildings.m_buffer[id];
                        StormDrainAI currentStormDrainAi = currentBuilding.Info.m_buildingAI as StormDrainAI;
                        
                        if (currentStormDrainAi.m_stormWaterIntake != 0)
                        {

                            updateDistrictAndDrainageGroup(id);

                        }
                        else
                            removeInlet(id);
                    } else
                    {
                        removeInlet(id);
                    }
                }
                HashSet<ushort> _SDOutletIterator = _SDoutlets;
                foreach (ushort id in _SDOutletIterator)
                {
                    if (reviewID(id))
                    {
                        Building currentBuilding = _buildingManager.m_buildings.m_buffer[id];
                        
                        StormDrainAI currentStormDrainAi = currentBuilding.Info.m_buildingAI as StormDrainAI;
                       
                        if (currentStormDrainAi.m_stormWaterOutlet != 0)
                        {
                            updateDistrictAndDrainageGroup(id);

                        }
                        else
                            removeOutlet(id);
                    }
                    else
                    {
                        removeOutlet(id);
                    }
                }
                */
                _simulationTimeCount += simulationTimeDelta;

                HashSet<ushort> _SDDetentionBasinsIterator = new HashSet<ushort>(_SDdetentionBasins);
                foreach (ushort id in _SDDetentionBasinsIterator)
                {
                    if (reviewID(id))
                    {
                        Building currentBuilding = _buildingManager.m_buildings.m_buffer[id];

                        StormDrainAI currentStormDrainAi = currentBuilding.Info.m_buildingAI as StormDrainAI;
                        if (currentStormDrainAi.m_stormWaterDetention != 0)
                        {
                            updateDistrictAndDrainageGroup(id);
                            
                            if (_simulationTimeCount > _infiltrationPeriod)
                            {
                                Hydraulics.removeDetainedStormwater(id, currentStormDrainAi.m_stormWaterInfiltration);
                            } 
                        }
                        else
                            removeDetentionBasin(id);
                    }
                    else
                    {
                        removeDetentionBasin(id);
                    }
                }
                if (_simulationTimeCount > _infiltrationPeriod)
                {
                    _simulationTimeCount = 0f;
                }
             


                if (serviceBuildingInfo.isVisible)
                {
                    if (_relocateDelay == 0)
                    {
                        if (Input.GetKey(KeyCode.PageDown))
                        {

                            var buildingID = GetParentInstanceId().Building;
                            Building currentBuilding = _buildingManager.m_buildings.m_buffer[buildingID];
                            StormDrainAI currentBuildingAI = currentBuilding.Info.m_buildingAI as StormDrainAI;
                            if (currentBuildingAI != null)
                            {
                                if (currentBuildingAI.m_stormWaterOutlet != 0 /*&& currentBuildingAI.m_electricityConsumption == 0*/)
                                {
                                    _relocateDelay = 0.1f;
                                    Vector3 newPosition = currentBuilding.m_position;
                                    newPosition.y -= 1;
                                    float angle = currentBuilding.m_angle;


                                    try
                                    {
                                        if (currentBuilding.m_waterSource != 0)
                                        {
                                            WaterSource source = _waterSimulation.m_waterSources.m_buffer[currentBuilding.m_waterSource];
                                            Vector3 vector = source.m_inputPosition;
                                            if (!_terrainManager.HasWater(VectorUtils.XZ(vector)))
                                            {
                                                vector = currentBuilding.CalculatePosition(currentBuildingAI.m_waterLocationOffset);
                                                //Debug.Log("[RF] vector has water");
                                                if (!_terrainManager.GetClosestWaterPos(ref vector, currentBuildingAI.m_waterEffectDistance))
                                                {
                                                    _waterSimulation.ReleaseWaterSource(currentBuilding.m_waterSource);
                                                }
                                            }
                                            currentBuilding.m_waterSource = 0;

                                        }
                                        _buildingManager.RelocateBuilding(buildingID, newPosition, angle);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.Log("[RF]Hydraulics Could not raise " + buildingID + " encountered exception " + e.ToString());

                                    }


                                }
                            }
                            NaturalDrainageAI currentBuildingNDAI = currentBuilding.Info.m_buildingAI as NaturalDrainageAI;
                            if (currentBuildingNDAI != null)
                            {
                                if (currentBuildingNDAI.m_standingWaterDepth > 0)
                                {
                                    try
                                    {
                                        TerrainManager instance = Singleton<TerrainManager>.instance;
                                        WaterSimulation waterSimulation = instance.WaterSimulation;
                                        if (currentBuilding.m_waterSource != 0)
                                        {
                                            WaterSource sourceData = waterSimulation.LockWaterSource(currentBuilding.m_waterSource);
                                          
                                            if (sourceData.m_target > currentBuilding.m_position.y + 1f)
                                            {
                                                _relocateDelay = 0.1f;
                                                sourceData.m_target = (ushort)(sourceData.m_target - 1u);
                                            }
                                            waterSimulation.UnlockWaterSource(currentBuilding.m_waterSource, sourceData);
                                        }
                                    } catch (Exception e)
                                    {
                                        Debug.Log("Tried to elevate natural drainage asset. encountered exception " + e.ToString());
;                                    }
                                }
                            }
                          
                        }

                        else if (Input.GetKey(KeyCode.PageUp))
                        {

                            var buildingID = GetParentInstanceId().Building;

                            Building currentBuilding = _buildingManager.m_buildings.m_buffer[buildingID];
                            StormDrainAI currentBuildingAI = currentBuilding.Info.m_buildingAI as StormDrainAI;
                            if (currentBuildingAI != null)
                            {
                                if (currentBuildingAI.m_stormWaterOutlet != 0/* && currentBuildingAI.m_electricityConsumption == 0*/)
                                {
                                    _relocateDelay = 0.1f;
                                    Vector3 newPosition = currentBuilding.m_position;
                                    newPosition.y += 1;
                                    float angle = currentBuilding.m_angle;


                                    try
                                    {
                                        if (currentBuilding.m_waterSource != 0)
                                        {
                                            WaterSource source = _waterSimulation.m_waterSources.m_buffer[currentBuilding.m_waterSource];
                                            Vector3 vector = source.m_inputPosition;
                                            if (!_terrainManager.HasWater(VectorUtils.XZ(vector)))
                                            {
                                                vector = currentBuilding.CalculatePosition(currentBuildingAI.m_waterLocationOffset);
                                                //Debug.Log("[RF] vector has water");
                                                if (!_terrainManager.GetClosestWaterPos(ref vector, currentBuildingAI.m_waterEffectDistance))
                                                {
                                                    _waterSimulation.ReleaseWaterSource(currentBuilding.m_waterSource);
                                                }
                                            }
                                            currentBuilding.m_waterSource = 0;

                                        }
                                        _buildingManager.RelocateBuilding(buildingID, newPosition, angle);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.Log("[RF]Hydraulics Could not raise " + buildingID + " encountered exception " + e.ToString());

                                    }

                                }
                            }

                            NaturalDrainageAI currentBuildingNDAI = currentBuilding.Info.m_buildingAI as NaturalDrainageAI;
                            if (currentBuildingNDAI != null)
                            {
                                if (currentBuildingNDAI.m_standingWaterDepth > 0)
                                {
                                    try
                                    {
                                        TerrainManager instance = Singleton<TerrainManager>.instance;
                                        WaterSimulation waterSimulation = instance.WaterSimulation;
                                        if (currentBuilding.m_waterSource != 0)
                                        {
                                            WaterSource sourceData = waterSimulation.LockWaterSource(currentBuilding.m_waterSource);
                                            
                                                _relocateDelay = 0.1f;
                                                sourceData.m_target = (ushort)(sourceData.m_target + 1u);
                                            waterSimulation.UnlockWaterSource(currentBuilding.m_waterSource, sourceData);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.Log("Tried to elevate natural drainage asset. encountered exception " + e.ToString());
                                        ;
                                    }
                                }
                            }

                        }
                    } else
                    {
                        _relocateDelay -= realTimeDelta;
                        if (_relocateDelay < 0)
                            _relocateDelay = 0;
                    }
                }
            }
           
            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }

        public static int getStormwaterAccumulation(int id)
        {
            //Debug.Log("[RF].Hydraulics Stormwater Accumulation at " + id.ToString() + " is " + Hydraulics.instance._stormwaterAccumulation[id]);
            //if (ModSettings.EasyMode == false || Hydraulics.instance._SDinlets.Contains((ushort)id))
                return Hydraulics.instance._stormwaterAccumulation[id];
            //Debug.Log("[RF].Hydraulics.getStormwaterAccumulation() Easmy mode");
            //return Hydraulics.instance._easyModeSWA[id];
        }
        public static int addStormwaterAccumulation(int id, int amount)
        {
            //if (ModSettings.EasyMode == false || Hydraulics.instance._SDinlets.Contains((ushort)id))
            //{
                Hydraulics.instance._stormwaterAccumulation[id] += amount;
                //Debug.Log("[RF].Hydraulics Stormwater Accumulation at " + id.ToString() + " is raised to " + Hydraulics.instance._stormwaterAccumulation[id]);
                return Hydraulics.instance._stormwaterAccumulation[id];
            //}
            //Debug.Log("[RF].Hydraulics.addStormwaterAccumulation EAsy mode");

           //Hydraulics.instance._easyModeSWA[id] += amount;
            //return Hydraulics.instance._easyModeSWA[id];
        }
        public static int removeStormwaterAccumulation(int id, int amount)
        {
            if (amount < 0)
                return 0;
            //if (ModSettings.EasyMode == false || Hydraulics.instance._SDinlets.Contains((ushort)id))
            //{
                if (Hydraulics.instance._stormwaterAccumulation[id] > amount)
                {
                    Hydraulics.instance._stormwaterAccumulation[id] -= amount;
                    //Debug.Log("[RF].Hydraulics Stormwater Accumulation at " + id.ToString() + " has dropped to " + Hydraulics.instance._stormwaterAccumulation[id]);
                    return amount;
                }
                else
                {
                    int removed = Hydraulics.instance._stormwaterAccumulation[id];
                    Hydraulics.instance._stormwaterAccumulation[id] = 0;
                    //Debug.Log("[RF].Hydraulics Stormwater Accumulation at " + id.ToString() + " has dropped to " + Hydraulics.instance._stormwaterAccumulation[id]);
                    return removed;
                }
            //}
            /*Debug.Log("[RF].Hydraulics.removeSWA EAsy mode");
            if (Hydraulics.instance._easyModeSWA[id] > amount)
            {
                Hydraulics.instance._easyModeSWA[id] -= amount;
                return amount;
            }
            else
            {
                int removed = Hydraulics.instance._easyModeSWA[id];
                Hydraulics.instance._easyModeSWA[id] = 0;
                return removed;
            }
            */
        }
        public static int getDetainedStormwater(int id)
        {
            //Debug.Log("[RF].Hydraulics Stormwater Accumulation at " + id.ToString() + " is " + Hydraulics.instance._stormwaterAccumulation[id]);
            return Hydraulics.instance._detainedStormwater[id];
        }
        public static int addDetainedStormwater(int id, int amount)
        {
            Hydraulics.instance._detainedStormwater[id] += amount;
            //Debug.Log("[RF].Hydraulics Detatined Stormwater at " + id.ToString() + " is raised to " + Hydraulics.instance._detainedStormwater[id]);
            return Hydraulics.instance._detainedStormwater[id];
        }
        public static int removeDetainedStormwater(int id, int amount)
        {
            if (amount < 0)
                return 0;
            if (Hydraulics.instance._detainedStormwater[id] > amount)
            {
                Hydraulics.instance._detainedStormwater[id] -= amount;
                //Debug.Log("[RF].Hydraulics Detained Stormwater at " + id.ToString() + " has dropped to " + Hydraulics.instance._detainedStormwater[id]);
                return amount;
            }
            else
            {
                int removed = Hydraulics.instance._detainedStormwater[id];
                Hydraulics.instance._detainedStormwater[id] = 0;
                //Debug.Log("[RF].Hydraulics Detained Stormwater at " + id.ToString() + " has dropped to " + Hydraulics.instance._detainedStormwater[id]);
                return removed;
            }
        }

       
        public static int getAreaStormwaterAccumulation(int buildingID)
        {
            Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[buildingID];
            
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentBuilding.m_position);
            string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)buildingID, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)buildingID, InstanceID.Empty)) {
                overrideDistrictControl = true;
            }
            
            //Debug.Log("[RF].Hydraulics Building " + BuildingId.ToString() + " is in district " + district.ToString());
            int areaStormwaterAccumulation = 0;
            HashSet<ushort> _SDInletIterator = new HashSet<ushort>(Hydraulics.instance._SDinlets);
            foreach (ushort id in _SDInletIterator)
            {
                int stormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");
                if (stormDrainAssetControlOption == OptionHandler._NoControlOption 
                    || stormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[id] == district 
                    || stormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                    || stormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                    || stormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                {
                    areaStormwaterAccumulation += Hydraulics.instance._stormwaterAccumulation[id];
                }
                        


            }
            return areaStormwaterAccumulation;
        }
        public static int getAreaOutletCapacity(int BuildingId)
        {
            Building startingBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[BuildingId];
            byte district = Hydraulics.instance._districtManager.GetDistrict(startingBuilding.m_position);
            string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)BuildingId, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)BuildingId, InstanceID.Empty))
            {
                overrideDistrictControl = true;
            }
            int areaOutletCapacity = 0;
            HashSet<ushort> _SDOutletIterator = new HashSet<ushort>(Hydraulics.instance._SDoutlets);
            foreach (ushort id in _SDOutletIterator)
            {
                int StormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");
                if (StormDrainAssetControlOption == OptionHandler._NoControlOption
                || StormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[id] == district
                || StormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                {
                    Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                        StormDrainAI currentBuildingAI = currentBuilding.Info.m_buildingAI as StormDrainAI;
                        if (currentBuildingAI != null)
                        {
                            if (currentBuildingAI.m_stormWaterOutlet != 0 && (currentBuilding.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None && (currentBuilding.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None)
                            {
                            areaOutletCapacity += Mathf.RoundToInt(currentBuildingAI.m_stormWaterOutlet * OptionHandler.getSliderSetting("OutletRateMultiplier"));
                            }
                        } else
                    {
                        removeOutlet(id);
                    }
                    }
                

            }
            //Debug.Log("[RF].Hydraulics Stormdrain Capacity at District " + district.ToString() + " is " + districtOutletCapacity);
            return areaOutletCapacity;
        }
       
        public static int getAreaInletCapacity(int BuildingId)
        {
            Building startingBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[BuildingId];
            byte district = Hydraulics.instance._districtManager.GetDistrict(startingBuilding.m_position);
            string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)BuildingId, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)BuildingId, InstanceID.Empty))
            {
                overrideDistrictControl = true;
            }
            int areaInletCapacity = 0;
            HashSet<ushort> iterator = new HashSet<ushort>(Hydraulics.instance._SDinlets);
            foreach (ushort id in iterator)
            {
                int StormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");
                if (StormDrainAssetControlOption == OptionHandler._NoControlOption
                || StormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[id] == district
                || StormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                {
                    Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    StormDrainAI currentBuildingAI = currentBuilding.Info.m_buildingAI as StormDrainAI;
                    if (currentBuildingAI != null)
                    {
                        if (currentBuildingAI.m_stormWaterIntake != 0 && (currentBuilding.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None && (currentBuilding.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None)
                        {
                            areaInletCapacity += Mathf.RoundToInt((float)currentBuildingAI.m_stormWaterIntake * OptionHandler.getSliderSetting("InletRateMultiplier"));
                        }
                    } else
                    {
                        removeInlet(id);
                    }
                }

            }
            //Debug.Log("[RF].Hydraulics Stormdrain Capacity at District " + district.ToString() + " is " + districtOutletCapacity);
            return areaInletCapacity;
        }
        

        public static int getAreaDetentionCapacity(int BuildingId)
        {
            Building startingBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[BuildingId];
            byte district = Hydraulics.instance._districtManager.GetDistrict(startingBuilding.m_position);
            string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)BuildingId, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)BuildingId, InstanceID.Empty))
            {
                overrideDistrictControl = true;
            }
            int areaDetentionCapacity = 0;
            HashSet<ushort> iterator = new HashSet<ushort>(Hydraulics.instance._SDdetentionBasins);
            foreach (ushort id in iterator)
            {
                int StormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");

                if (StormDrainAssetControlOption == OptionHandler._NoControlOption
               || StormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[id] == district
               || StormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
               || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
               || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                {
                    Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    StormDrainAI currentBuildingAI = currentBuilding.Info.m_buildingAI as StormDrainAI;
                    if (currentBuildingAI != null)
                    {
                        if (currentBuildingAI.m_stormWaterDetention != 0 && (currentBuilding.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None && (currentBuilding.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None)
                        {
                            areaDetentionCapacity += currentBuildingAI.m_stormWaterDetention;
                        }
                    }
                    else
                        removeDetentionBasin(id);
                }

            }
            //Debug.Log("[RF].Hydraulics Stormdrain detention capacity at District " + district.ToString() + " is " + districtDetentionCapacity);
            return areaDetentionCapacity;
        }
       
        public static int getAreaDetainedStormwater(int BuildingId)
        {
            Building startingBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[BuildingId];
            byte district = Hydraulics.instance._districtManager.GetDistrict(startingBuilding.m_position);
            string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)BuildingId, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)BuildingId, InstanceID.Empty))
            {
                overrideDistrictControl = true;
            }
            int areaDetainedStormwater = 0;
            HashSet<ushort> iterator = new HashSet<ushort>(Hydraulics.instance._SDdetentionBasins);
            foreach (ushort id in iterator)
            {
                int StormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");

                if (StormDrainAssetControlOption == OptionHandler._NoControlOption
               || StormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[id] == district
               || StormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
               || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
               || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                {
                    Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    StormDrainAI currentBuildingAI = currentBuilding.Info.m_buildingAI as StormDrainAI;
                    if (currentBuildingAI != null)
                    {
                        bool flag1 = currentBuildingAI.m_stormWaterDetention != 0;
                        bool flag2 = (currentBuilding.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None;
                        bool flag3 = (currentBuilding.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None;
                        if (flag1 && flag2)
                        {
                            areaDetainedStormwater += Hydraulics.getDetainedStormwater(id);
                        }
                    }
                    else
                        removeDetentionBasin(id);
                }

            }
            //Debug.Log("[RF].Hydraulics Stormdrain Capacity at District " + district.ToString() + " is " + districtOutletCapacity);
            return areaDetainedStormwater;
        }
      
        public static bool checkGravityFlowForInlet(int inletId)
        {
            Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[inletId];
            string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)inletId, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)inletId, InstanceID.Empty))
            {
                overrideDistrictControl = true;
            }

            byte district = Hydraulics.instance._districtManager.GetDistrict(currentInlet.m_position);
            if (Hydraulics.instance._SDoutlets.Count > 0 || Hydraulics.instance._SDdetentionBasins.Count > 0)
            {
                HashSet<ushort> outletIterator = new HashSet<ushort>(Hydraulics.instance._SDoutlets);
                foreach (ushort id in outletIterator)
                {
                    int StormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");

                    if (StormDrainAssetControlOption == OptionHandler._NoControlOption
                        || StormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[id] == district
                        || StormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                    {

                        Building currentOutlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                        StormDrainAI currentInletAI = currentInlet.Info.m_buildingAI as StormDrainAI;
                        StormDrainAI currentOutletAI = currentOutlet.Info.m_buildingAI as StormDrainAI;
                        if (currentInletAI != null)
                        {
                            if (currentOutletAI != null)
                            {
                                if ((currentOutlet.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None)
                                {
                                    int GravityDrainageOption = OptionHandler.getDropdownSetting("GravityDrainageOption");
                                    if (GravityDrainageOption == OptionHandler._ImprovedGravityDrainageOption)
                                    {
                                        float inletWSE = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(currentInlet.m_position + currentInletAI.m_waterLocationOffset));
                                        float outletWSE = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(currentOutlet.m_position + currentOutletAI.m_waterLocationOffset));

                                        if (Mathf.Max(currentInlet.m_position.y, inletWSE) > Mathf.Max(currentOutlet.m_position.y + currentOutletAI.m_invert, outletWSE) || currentOutletAI.m_electricityConsumption > 0 || currentInletAI.m_electricityConsumption > 0)
                                            return true;
                                    }
                                    else if (GravityDrainageOption == OptionHandler._SimpleGravityDrainageOption)
                                    {

                                        if (currentInlet.m_position.y > currentOutlet.m_position.y || currentOutletAI.m_electricityConsumption > 0 || currentInletAI.m_electricityConsumption > 0)
                                            return true;
                                    }
                                    else if (GravityDrainageOption == OptionHandler._IgnoreGravityDrainageOption)
                                    {
                                        return true;
                                    }
                                }
                            }
                            else
                                removeOutlet(id);
                        }
                        else
                            removeInlet((ushort)inletId);
                    }
                }
                HashSet<ushort> basinIterator = new HashSet<ushort>(Hydraulics.instance._SDdetentionBasins);
                foreach (ushort id in basinIterator)
                {
                    int StormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");

                    if (StormDrainAssetControlOption == OptionHandler._NoControlOption
                        || StormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[id] == district
                        || StormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                    {
                        Building currentDetentionBasin = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                        StormDrainAI currentDetentionBasinAI = currentDetentionBasin.Info.m_buildingAI as StormDrainAI;
                        StormDrainAI currentInletAI = currentInlet.Info.m_buildingAI as StormDrainAI;
                        if (currentInletAI != null)
                        {
                            if (currentDetentionBasinAI != null)
                            {
                                bool flag1 = currentDetentionBasinAI.m_stormWaterDetention > 0;
                                bool flag2 = Hydraulics.instance._districts[id] == district;
                                bool flag3 = (currentDetentionBasin.m_problems & Notification.Problem.LandfillFull) == Notification.Problem.None;
                                bool flag4 = false;
                                bool flag5 = (currentDetentionBasin.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None;
                                int GravityDrainageOption = OptionHandler.getDropdownSetting("GravityDrainageOption");
                                if (GravityDrainageOption == OptionHandler._ImprovedGravityDrainageOption)
                                {
                                    float inletWSE = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(currentInlet.m_position + currentInletAI.m_waterLocationOffset));
                                    float basinWSE = Hydraulics.getDetentionBasinWSE(id);

                                    if (Mathf.Max(currentInlet.m_position.y, inletWSE) > basinWSE || currentInletAI.m_electricityConsumption > 0)
                                        flag4 = true;
                                }
                                else if (GravityDrainageOption == OptionHandler._SimpleGravityDrainageOption)
                                {

                                    flag4 = true;
                                }
                                else if (GravityDrainageOption == OptionHandler._IgnoreGravityDrainageOption)
                                {
                                    flag4 = true;
                                }
                                if (flag1 && flag2 && flag3 && flag4 && flag5)
                                {
                                    return true;
                                }
                            }
                            else
                                removeDetentionBasin(id);
                        }
                        else
                            removeInlet((ushort)inletId);
                    } 
                }
            }
            return false;
        }
        public static bool checkGravityFlowForInlet(int inletId, int outletId)
        {
            Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[inletId];
            string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)inletId, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)inletId, InstanceID.Empty))
            {
                overrideDistrictControl = true;
            }

            byte district = Hydraulics.instance._districtManager.GetDistrict(currentInlet.m_position);
            int StormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");

            if (StormDrainAssetControlOption == OptionHandler._NoControlOption
                        || StormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[outletId] == district
                        || StormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[outletId] == drainageGroup
                        || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[outletId] == drainageGroup
                        || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[outletId] == district)
            {
                Building currentOutlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[outletId];
                StormDrainAI currentInletAI = currentInlet.Info.m_buildingAI as StormDrainAI;
                if (currentInletAI == null)
                {
                    removeInlet((ushort)inletId);
                    return false;
                }
                StormDrainAI currentOutletAI = currentOutlet.Info.m_buildingAI as StormDrainAI;
                if (currentOutletAI == null)
                {
                    removeOutlet((ushort)outletId);
                    return false;
                }
                if ((currentOutlet.m_problems & Notification.Problem.TurnedOff) != Notification.Problem.None) {
                    return false;
                }
                int GravityDrainageOption = OptionHandler.getDropdownSetting("GravityDrainageOption");

                if (GravityDrainageOption == OptionHandler._ImprovedGravityDrainageOption)
                {
                    float inletWSE = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(currentInlet.m_position + currentInletAI.m_waterLocationOffset));
                    if (currentOutletAI.m_stormWaterOutlet > 0)
                    {
                        float outletWSE = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(currentOutlet.m_position + currentOutletAI.m_waterLocationOffset));

                        if (Mathf.Max(currentInlet.m_position.y, inletWSE) > Mathf.Max(currentOutlet.m_position.y + currentOutletAI.m_invert, outletWSE) || currentOutletAI.m_electricityConsumption > 0 && (currentOutlet.m_problems & Notification.Problem.Electricity) == Notification.Problem.None || currentInletAI.m_electricityConsumption > 0 && (currentInlet.m_problems & Notification.Problem.Electricity) == Notification.Problem.None)
                            return true;
                        if (currentInletAI.m_culvert == true)
                        {
                            if (Mathf.Max(currentInlet.m_position.y + currentInletAI.m_soffit, inletWSE) > Mathf.Max(currentOutlet.m_position.y + currentOutletAI.m_invert, outletWSE))
                                return true;
                        }
                    } else if (currentOutletAI.m_stormWaterDetention > 0)
                    {
                        float basinWSE = Hydraulics.getDetentionBasinWSE(outletId);
                        bool flag3 = (currentOutlet.m_problems & Notification.Problem.LandfillFull) == Notification.Problem.None;
                        bool flag4 = false;
                        if (Mathf.Max(currentInlet.m_position.y, inletWSE) > basinWSE || currentInletAI.m_electricityConsumption > 0 && (currentInlet.m_problems & Notification.Problem.Electricity) == Notification.Problem.None)
                            flag4 = true;
                        if (flag3 && flag4)
                            return true;
                    }
                
                } else if (GravityDrainageOption == OptionHandler._SimpleGravityDrainageOption)
                {
                    if (currentInlet.m_position.y > currentOutlet.m_position.y || currentOutletAI.m_electricityConsumption > 0 && (currentOutlet.m_problems & Notification.Problem.Electricity) == Notification.Problem.None || currentInletAI.m_electricityConsumption > 0 && (currentInlet.m_problems & Notification.Problem.Electricity) == Notification.Problem.None || currentOutletAI.m_stormWaterDetention > 0)
                        return true;
                } else if (GravityDrainageOption == OptionHandler._IgnoreGravityDrainageOption)
                {
                    return true;
                }
            }
            
            return false;
        }
        public static int getOutletCapacityForInlet(int inletID)
        {
            int outletCapacity = 0;
            Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[inletID];
            StormDrainAI currentInletAI = currentInlet.Info.m_buildingAI as StormDrainAI;
            if (currentInletAI == null)
            {
                removeInlet((ushort)inletID);
                return 0;
            }
            string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)inletID, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)inletID, InstanceID.Empty))
            {
                overrideDistrictControl = true;
            }
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentInlet.m_position);
            HashSet<ushort> iterator = new HashSet<ushort>(Hydraulics.instance._SDoutlets);
            foreach (ushort id in iterator)
            {
                int StormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");

                if (StormDrainAssetControlOption == OptionHandler._NoControlOption
                       || StormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[id] == district
                       || StormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                       || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                       || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                {
                    Building currentOutlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    StormDrainAI currentOutletAI = currentOutlet.Info.m_buildingAI as StormDrainAI;
                    if (currentOutletAI != null)
                    {
                        bool flag1 = currentOutletAI.m_stormWaterOutlet > 0;
                        bool flag2 = checkGravityFlowForInlet(inletID, id);
                        bool flag3 = (currentOutlet.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None;
                        bool flag4 = (currentOutlet.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None;
                        if (flag1 && flag2 && flag3 && flag4)
                        {
                            outletCapacity += Mathf.RoundToInt(currentOutletAI.m_stormWaterOutlet *OptionHandler.getSliderSetting("OutletRateMultiplier"));
                        }
                    }
                    else
                        removeOutlet(id);
                }
            }
            return outletCapacity;
        }
        public static ushort getLowestOutletForInlet(int inletID)
        {
            float outletElev = 999999f;
            ushort outletID = 0;
            Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[inletID];
            StormDrainAI currentInletAI = currentInlet.Info.m_buildingAI as StormDrainAI;
            if (currentInletAI == null)
            {
                removeInlet((ushort)inletID);
                return 0;
            }
            string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)inletID, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)inletID, InstanceID.Empty))
            {
                overrideDistrictControl = true;
            }
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentInlet.m_position);
            HashSet<ushort> outletIterator = new HashSet<ushort>(Hydraulics.instance._SDoutlets);
            foreach (ushort id in outletIterator)
            {
                int StormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");

                if (StormDrainAssetControlOption == OptionHandler._NoControlOption
                        || StormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[id] == district
                        || StormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                {
                    Building currentOutlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    StormDrainAI currentOutletAI = currentOutlet.Info.m_buildingAI as StormDrainAI;
                    if (currentOutletAI != null)
                    {

                        bool flag1 = currentOutletAI.m_stormWaterOutlet > 0;
                        bool flag3 = (currentOutlet.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None;
                        float currentOutletWSE = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(currentOutlet.m_position + currentOutletAI.m_waterLocationOffset));
                        float currentOutletElev = Mathf.Max(currentOutletWSE, currentOutlet.m_position.y + currentOutletAI.m_invert);
                        bool flag2 = (currentOutletElev < outletElev);
                        bool flag4 = (currentOutlet.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None;
                        if (flag1 && flag2 && flag3 && flag4)
                        {
                            outletID = id;
                            outletElev = currentOutletElev;
                        }

                    }
                    else
                        removeOutlet(id);
                }
            }
            HashSet<ushort> basinIterator = new HashSet<ushort>(Hydraulics.instance._SDdetentionBasins);
            foreach (ushort id in basinIterator)
            {
                int StormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");

                if (StormDrainAssetControlOption == OptionHandler._NoControlOption
                       || StormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[id] == district
                       || StormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                       || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                       || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                {
                    Building currentBasin = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    StormDrainAI currentBasinAI = currentBasin.Info.m_buildingAI as StormDrainAI;
                    if (currentBasinAI != null)
                    {
                        if (currentBasinAI.m_stormWaterDetention > 0)
                        {
                            float basinWSE = Hydraulics.getDetentionBasinWSE(id);
                            bool flag2 = (currentBasin.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None;
                            bool flag3 = (currentBasin.m_problems & Notification.Problem.LandfillFull) == Notification.Problem.None;
                            bool flag4 = false;
                            if (basinWSE < outletElev)
                                flag4 = true;
                            if (flag2 && flag3 && flag4)
                            {
                                outletID = id;
                                outletElev = basinWSE;
                            }
                        }
                    }
                    else
                        removeDetentionBasin(id);
                }
            }
            return outletID;
        }
        public static int getStormWaterAccumulationForOutlet(int outletID)
        {
            int stormwaterAccumulation = 0;
            Building currentOutlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[outletID];
            StormDrainAI currentOutletAI = currentOutlet.Info.m_buildingAI as StormDrainAI;
            if (currentOutletAI == null)
            {
                removeOutlet((ushort)outletID);
                return 0;
            }
            string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)outletID, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)outletID, InstanceID.Empty))
            {
                overrideDistrictControl = true;
            }
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentOutlet.m_position);
            HashSet<ushort> iterator = new HashSet<ushort>(Hydraulics.instance._SDinlets);
            foreach (ushort inletId in iterator)
            {
                int StormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");

                if (StormDrainAssetControlOption == OptionHandler._NoControlOption
                       || StormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[inletId] == district
                       || StormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[inletId] == drainageGroup
                       || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[inletId] == drainageGroup
                       || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[inletId] == district)
                {
                    Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[inletId];
                    StormDrainAI currentInletAI = currentInlet.Info.m_buildingAI as StormDrainAI;
                    if (currentInletAI != null)
                    {
                        bool flag1 = currentOutletAI.m_electricityConsumption == 0;
                        bool flag2 = true;
                        if (flag1)
                            flag2 = checkGravityFlowForInlet(inletId, outletID);
                        bool flag3 = (currentInlet.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None;
                        if (flag2 && flag3)
                            stormwaterAccumulation += Hydraulics.instance._stormwaterAccumulation[inletId];
                    }
                    else
                        removeInlet(inletId);
                }

            }
            /*if (ModSettings.EasyMode == true)
            {
                Debug.Log("[RF].Hydraulics.getSWA for Outlet EAsy mode");
                HashSet<ushort> buildingList = Hydrology.getBuildingList();
                foreach (ushort buildingID in buildingList)
                {
                    Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[buildingID];
                    byte buildingDistrict = Hydraulics.instance._districtManager.GetDistrict(currentBuilding.m_position);
                    if (ModSettings.DistrictControl == true && buildingDistrict == district || ModSettings.DistrictControl == false)
                    {
                        stormwaterAccumulation += Hydraulics.instance._easyModeSWA[buildingID];
                    }
                }
            }*/
            return stormwaterAccumulation;
        }
        public static int removeAreaStormwaterAccumulation(int amount, int outletID, bool checkGravityFlow)
        {
            //Debug.Log("[RF] Remove Area Stormwater Acculation");
            int districtStormwaterAccumulation = Hydraulics.getAreaStormwaterAccumulation(outletID);
            string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)outletID, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)outletID, InstanceID.Empty))
            {
                overrideDistrictControl = true;
            }
            
            int minimumStormwaterAccumulation = amount;
            Building outletBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[outletID];
            byte district = Hydraulics.instance._districtManager.GetDistrict(outletBuilding.m_position);
            if ((outletBuilding.m_problems & Notification.Problem.TurnedOff) != Notification.Problem.None)
                return 0;

            StormDrainAI outletBuildingAI = outletBuilding.Info.m_buildingAI as StormDrainAI;
            bool simulatePollution = false;
            bool simulateFiltration = false;
            int pollutantsFromInlet = 0;
            WaterSource outletWaterSource = new WaterSource();
            if (outletBuildingAI != null)
            {
                if (outletBuilding.m_waterSource != 0 && outletBuildingAI.m_stormWaterOutlet > 0)
                {
                    //simulatePollution = true;
                    outletWaterSource = Hydraulics.instance._waterSimulation.LockWaterSource(outletBuilding.m_waterSource);
                    outletWaterSource.m_pollution = 0;
                } 
            } else
            {
                removeOutlet((ushort)outletID);
                return 0;
            }
            //if (/*ModSettings.EasyMode == true || */OptionHandler.getCheckboxSetting("SimulatePollution") == false || outletBuildingAI.m_filter == true)
            //{
                //Debug.Log("[RF].Hydraulics.removeDistrictSWA EAsy mode means no pollution");
                //No pollution mechanics in easy mode or if you turn of pollution simulation
                //simulatePollution = false;
            //}
            if (outletBuildingAI.m_filter == true)
            {
                simulateFiltration = true;
            }
            
            if (districtStormwaterAccumulation <= amount)
            {
                HashSet<ushort> inletIterator = new HashSet<ushort>(Hydraulics.instance._SDinlets);
                foreach (ushort id in inletIterator)
                {
                    Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    StormDrainAI currentInletAI = currentInlet.Info.m_buildingAI as StormDrainAI;
                    if (currentInletAI != null)
                    {
                        int StormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");

                        bool flag1 = (StormDrainAssetControlOption == OptionHandler._NoControlOption
                       || StormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[id] == district
                       || StormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                       || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                       || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district);
                        bool flag2 = Hydraulics.instance._stormwaterAccumulation[id] > 0;
                        bool flag3 = (currentInlet.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None;
                        bool flag4;
                        bool flag5 = (currentInlet.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None;
                        if (checkGravityFlow == true)
                            flag4 = Hydraulics.checkGravityFlowForInlet(id, outletID);
                        else
                            flag4 = true;

                        if (flag1 && flag2 && flag3 && flag4 && flag5)
                        {
                            if (simulatePollution)
                            {

                                outletWaterSource.m_pollution += (uint)(Mathf.Round((float)Hydraulics.instance._stormwaterAccumulation[id] * 0.5f * ((float)currentInlet.m_waterPollution / 255f)));
                                //Debug.Log("currentInlet.m_waterPollution = " + currentInlet.m_waterPollution.ToString());
                            }
                            else if (simulateFiltration)
                            {
                                pollutantsFromInlet += (int)(Mathf.Round((float)Hydraulics.instance._stormwaterAccumulation[id] * 0.5f * ((float)currentInlet.m_waterPollution / 255f)));
                            }
                            Hydraulics.instance._stormwaterAccumulation[id] = 0;

                        }
                    }
                    else
                        removeInlet(id);
                }
                /*
                if (ModSettings.EasyMode == true)
                {
                    Debug.Log("[RF].Hydraulics.removeDistrictSWA EAsy mode less than total");
                    HashSet<ushort> buildingList = Hydrology.getBuildingList();
                    Debug.Log(buildingList.Count);
                    foreach (ushort buildingID in buildingList)
                    {
                        Debug.Log(buildingID);
                        try {
                            Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[buildingID];
                            byte buildingDistrict = Hydraulics.instance._districtManager.GetDistrict(currentBuilding.m_position);
                            bool flag1 = buildingDistrict == district;
                            if (ModSettings.DistrictControl == false)
                                flag1 = true;
                            bool flag2 = Hydraulics.instance._easyModeSWA[buildingID] > 0;
                            bool flag3 = (currentBuilding.m_problems & Notification.Problem.Water) == Notification.Problem.None;
                            //Ignoring Gravity Effects for Easy mode because Easy mode
                            if (flag1 && flag2 && flag3)
                            {

                                Hydraulics.instance._easyModeSWA[buildingID] = 0;

                            }
                        } catch (Exception e)
                        {
                            Debug.Log("Couldn't work for building id " + buildingID.ToString() + " because exception " + e.ToString());
                        }


                    }
                }
                */
                if (simulatePollution)
                {
                    outletWaterSource.m_pollution = (uint)Mathf.Min(outletWaterSource.m_pollution, outletWaterSource.m_water * 100 / Mathf.Max(100, Hydraulics.instance._waterSimulation.GetPollutionDisposeRate() * 100)); 
                    //Debug.Log("[RF].Hydraulics Set pollution at building " + outletID.ToString() + " to " + outletWaterSource.m_pollution.ToString() + " out of " + outletWaterSource.m_water.ToString());
                    
                } else if (simulateFiltration)
                {
                    Hydraulics.addPollutants(outletID, (int)(Mathf.Min(pollutantsFromInlet, outletWaterSource.m_water * 100 / Mathf.Max(100, Hydraulics.instance._waterSimulation.GetPollutionDisposeRate() * 100))/10f));

                }
                if (outletBuildingAI != null)
                {
                    if (outletBuilding.m_waterSource != 0 && outletBuildingAI.m_stormWaterOutlet > 0)
                    {
                        Hydraulics.instance._waterSimulation.UnlockWaterSource(outletBuilding.m_waterSource, outletWaterSource);
                    }
                } else
                {
                    removeOutlet((ushort)outletID);
                    return 0;
                }
                //Debug.Log("[RF].Hydraulics.removeDistrictSWA EAsy mode less than total made it to return " + districtStormwaterAccumulation);
                //Debug.Log("[RF].Hydraulics Removed districtStormwaterAccumulation = " + districtStormwaterAccumulation.ToString() + " From District " + district.ToString());
                return districtStormwaterAccumulation;
            } else
            {
                //Debug.Log("[RF]Hydraulics.RemoveStormwaterAccumulation Full Outlet");
                
                int removed = 0;
                HashSet<ushort> districtSources = new HashSet<ushort>();
                HashSet<ushort> emptySources = new HashSet<ushort>();
                HashSet<ushort> inletIterator = new HashSet<ushort>(Hydraulics.instance._SDinlets);
                foreach (ushort id in inletIterator)
                {

                    Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    StormDrainAI currentInletAI = currentInlet.Info.m_buildingAI as StormDrainAI;
                    if (currentInletAI != null)
                    {
                        int StormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");

                        bool flag1 = (StormDrainAssetControlOption == OptionHandler._NoControlOption
                         || StormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[id] == district
                         || StormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                         || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                         || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district);
                        bool flag2 = Hydraulics.instance._stormwaterAccumulation[id] > 0;
                        bool flag3 = (currentInlet.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None;
                        bool flag4;
                        bool flag5 = (currentInlet.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None;
                        if (checkGravityFlow == true)
                            flag4 = Hydraulics.checkGravityFlowForInlet(id, outletID);
                        else
                            flag4 = true;

                        if (flag1 && flag2 && flag3 && flag4 && flag5)
                        {
                            districtSources.Add(id);
                            //Debug.Log("[RF]Hydraulics.RemoveStormwaterAccumulation Added district source " + id.ToString());
                            if (minimumStormwaterAccumulation > Hydraulics.instance._stormwaterAccumulation[id])
                                minimumStormwaterAccumulation = Hydraulics.instance._stormwaterAccumulation[id];
                        } else
                        {
                            //Debug.Log("[RF]Hydraulics.RemoveStormwaterAccumulation didn't add district source " + id.ToString());
                        }
                    }
                    else
                        removeInlet(id);
                }
                /*if (ModSettings.EasyMode == true)
                {
                    Debug.Log("[RF].Hydraulics.removeDistrictSWA EAsy mode greater than total");
                    HashSet<ushort> buildingList = Hydrology.getBuildingList();
                    foreach (ushort buildingID in buildingList)
                    {
                        Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[buildingID];
                        byte buildingDistrict = Hydraulics.instance._districtManager.GetDistrict(currentBuilding.m_position);
                        bool flag1 = buildingDistrict == district;
                        if (ModSettings.DistrictControl == false)
                            flag1 = true;
                        bool flag2 = Hydraulics.instance._easyModeSWA[buildingID] > 0;
                        bool flag3 = (currentBuilding.m_problems & Notification.Problem.Water) == Notification.Problem.None;
                        //Ignoring Gravity Effects for Easy mode because Easy mode
                        if (flag1 && flag2 && flag3)
                        {
                            districtSources.Add(buildingID);
                            if (minimumStormwaterAccumulation > Hydraulics.instance._easyModeSWA[buildingID])
                                minimumStormwaterAccumulation = Hydraulics.instance._easyModeSWA[buildingID];

                        }


                    }
                }
                */
                int attempts = 0;
                //Debug.Log("Here");
                while (removed < amount)
                {
                    int removalAmount = minimumStormwaterAccumulation;
                    //Debug.Log("[RF].Hydrulics Removal Amount = " + removalAmount.ToString());
                    minimumStormwaterAccumulation = amount - removed;
                    //Debug.Log("[RF].Hydrulics amount - removed = " + minimumStormwaterAccumulation.ToString());
                    
                    foreach (ushort id in districtSources)
                    {
                        //Debug.Log("[RF].Hydrulics removed = " + removed.ToString());
                        //Debug.Log("[RF].Hydrulics minimumStormwaterAccumulation = " + minimumStormwaterAccumulation.ToString());
                        //Debug.Log("[RF].Hydrulics SDA = " + Hydraulics.getAreaStormwaterAccumulation(id).ToString() + " at district " + district.ToString());
                        //Debug.Log("[RF].Hydrulics SDA = " + Hydraulics.getStormwaterAccumulation(id).ToString() + " at building " + id.ToString());
                        
                        Building currentSource = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                        if (removed + removalAmount <= amount)
                        {
                            int justRemoved = Hydraulics.removeStormwaterAccumulation(id, removalAmount);
                            removed += justRemoved;
                            if (simulatePollution)
                            {
                                outletWaterSource.m_pollution += (uint)(Mathf.Round((float)justRemoved * 0.5f * ((float)currentSource.m_waterPollution / 255f)));
                            }
                        }
                        else {
                            int justRemoved = Hydraulics.removeStormwaterAccumulation(id, amount - removed);
                            removed += justRemoved;
                            if (simulatePollution)
                            {
                               
                                outletWaterSource.m_pollution += (uint)(Mathf.Round((float)justRemoved * 0.5f* ((float)currentSource.m_waterPollution / 255f)));
                            }
                            else if (simulateFiltration)
                            {
                                pollutantsFromInlet += (int)(Mathf.Round((float)Hydraulics.instance._stormwaterAccumulation[id] * 0.5f * ((float)currentSource.m_waterPollution / 255f)));
                            }
                        }
                        //Debug.Log("[RF].Hydrulics removed = " + removed.ToString());
                        //Debug.Log("[RF].Hydrulics SDA = " + Hydraulics.getAreaStormwaterAccumulation(id).ToString() + " at district " + district.ToString());
                        //Debug.Log("[RF].Hydrulics SWA = " + Hydraulics.getStormwaterAccumulation(id).ToString() + " at building " + id.ToString());
                        if (minimumStormwaterAccumulation > Hydraulics.getStormwaterAccumulation(id) && Hydraulics.getStormwaterAccumulation(id) > 0)
                            minimumStormwaterAccumulation = Hydraulics.getStormwaterAccumulation(id);
                        
                        //Debug.Log("[RF].Hydrulics minimumStormwaterAccumulation = " + minimumStormwaterAccumulation.ToString());
                        if (Hydraulics.getStormwaterAccumulation(id) == 0)
                            emptySources.Add(id);
                        else if (Hydraulics.getStormwaterAccumulation(id) < 0)
                        {
                            Hydraulics.instance._stormwaterAccumulation[id] = 0;
                            //Debug.Log("[RF].Hydraulics Error SWS < 0");
                            emptySources.Add(id);
                        }
                     
                        if (removed >= amount )
                        {
                          
                            break;
                        }
                    }

                    foreach (ushort id in emptySources)
                    {
                      // Debug.Log("[RF].Hydraulics Removing empty inlet" + id.ToString());
                        districtSources.Remove(id);
                    }
                    emptySources.Clear();
                    if (districtSources.Count == 0)
                    {
                        if (simulatePollution)
                        {
                            outletWaterSource.m_pollution = (uint)Mathf.Min(outletWaterSource.m_pollution, outletWaterSource.m_water * 100 / Mathf.Max(100, Hydraulics.instance._waterSimulation.GetPollutionDisposeRate() * 100));
                            //Debug.Log("[RF].Hydraulics Set pollution at building " + outletID.ToString() + " to " + outletWaterSource.m_pollution.ToString() + " out of " + outletWaterSource.m_water.ToString());
                        } else if (simulateFiltration)
                        {
                            Hydraulics.addPollutants(outletID, (int)(Mathf.Min(pollutantsFromInlet, outletWaterSource.m_water * 100 / Mathf.Max(100, Hydraulics.instance._waterSimulation.GetPollutionDisposeRate() * 100))/10f));
                        }
                        if (outletBuildingAI != null)
                        {
                            if (outletBuilding.m_waterSource != 0 && outletBuildingAI.m_stormWaterOutlet > 0)
                            {
                                Hydraulics.instance._waterSimulation.UnlockWaterSource(outletBuilding.m_waterSource, outletWaterSource);
                            }
                        } else
                        {
                            removeOutlet((ushort)outletID);
                            return 0;
                        }
                        //Debug.Log("[RF].Hydraulics Removed = " + removed.ToString() + " From District " + district.ToString());
                        return removed;
                    }
                    if (districtSources.Count == 0 || removalAmount == 0 || attempts > 1000)
                    {
                        //Debug.Log("[RF] removed = " + removed.ToString() + " amount = " + amount.ToString());
                        break;
                    }
                    attempts += 1;
                }
            }
            if (simulatePollution)
            {
                outletWaterSource.m_pollution = (uint)Mathf.Min(outletWaterSource.m_pollution, outletWaterSource.m_water * 100u / (uint)Mathf.Max(100, Hydraulics.instance._waterSimulation.GetPollutionDisposeRate() * 100));
                //Debug.Log("[RF].Hydraulics Set pollution at building " + outletID.ToString() + " to " + outletWaterSource.m_pollution.ToString() + " out of " + outletWaterSource.m_water.ToString());
               
            }
            if (outletBuildingAI != null)
            {
                if (outletBuilding.m_waterSource != 0 && outletBuildingAI.m_stormWaterOutlet > 0)
                {
                    Hydraulics.instance._waterSimulation.UnlockWaterSource(outletBuilding.m_waterSource, outletWaterSource);
                }
            }
            else
            {
                removeOutlet((ushort)outletID);

            }
            //Debug.Log("[RF].Hydraulics Removed amount =  " + amount.ToString() + " From District " + district.ToString());
            return amount;
        }
        
       
        
        public bool reviewID(int id)
        {
            Building currentBuilding = _buildingManager.m_buildings.m_buffer[id];
            bool flag = (currentBuilding.m_flags & Building.Flags.Completed) != Building.Flags.None;
            StormDrainAI currentStormDrainAi = currentBuilding.Info.m_buildingAI as StormDrainAI;
            if (currentStormDrainAi != null && flag == true) {
                return true;
            }
            return false;
        }
        private InstanceID GetParentInstanceId()
        {

            try
            {
                baseSub = _cityServiceWorldInfoPanel.GetType().GetField("m_InstanceID", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            catch (Exception e)
            {
                Debug.Log("Could not find building id from base sub encountered exception " + e);
            }
            return (InstanceID)baseSub.GetValue(_cityServiceWorldInfoPanel);
        }


        public static int getHydraulicRate(int id)
        {
            //Debug.Log("[RF].Hydraulics HydraulicRate  at " + id.ToString() + " is " + Hydraulics.instance._hydraulicRate[id]);
            return Hydraulics.instance._hydraulicRate[id];
        }
        public static int setHydraulicRate(int id, int amount)
        {
            Hydraulics.instance._hydraulicRate[id] = amount;
            //Debug.Log("[RF].Hydraulics HydraulicRate  at " + id.ToString() + " is raised to " + Hydraulics.instance._hydraulicRate[id]);
            return Hydraulics.instance._hydraulicRate[id];
        }
        
        public static int getDistrictHydraulicRate(int BuildingId, HashSet<ushort> structures)
        {
            int areaHydraulicRate = 0;
           string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)BuildingId, InstanceID.Empty);
            bool overrideDistrictControl = false;
                if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)BuildingId, InstanceID.Empty))
                {
                     overrideDistrictControl = true;
                }
            Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[BuildingId];
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentBuilding.m_position);
            foreach (ushort id in structures)
            {
                int StormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");

                if (StormDrainAssetControlOption == OptionHandler._NoControlOption
                         || StormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[id] == district
                         || StormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                         || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                         || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                {
                    areaHydraulicRate += Hydraulics.instance._hydraulicRate[id];
                }

            }
            //Debug.Log("[RF].Hydraulics HydraulicRate  at District " + district.ToString() + " is " + districtHydraulicRate);
            return areaHydraulicRate;
        }
      
        public static HashSet<ushort> getInletList()
        {
            return Hydraulics.instance._SDinlets;
        }
        public static HashSet<ushort> getOutletList()
        {
            return Hydraulics.instance._SDoutlets;
        }
        public static HashSet<ushort> getDetentionBasinList()
        {
            return Hydraulics.instance._SDdetentionBasins;
        }

        public static int getVariableCapacity(int id)
        {
            //Debug.Log("[RF].Hydraulics VariableCapacity  at " + id.ToString() + " is " + Hydraulics.instance._variableCapacity[id]);
            return Hydraulics.instance._variableCapacity[id];
        }
        public static int setVariableCapacity(int id, int amount)
        {
            Hydraulics.instance._variableCapacity[id] = amount;
            //Debug.Log("[RF].Hydraulics VariableCapacity  at " + id.ToString() + " is raised to " + Hydraulics.instance._variableCapacity[id]);
            return Hydraulics.instance._variableCapacity[id];
        }

        public static int getAreaVariableCapacity(int BuildingId, HashSet<ushort> structures)
        {
            Building startingBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[BuildingId];
            byte district = Hydraulics.instance._districtManager.GetDistrict(startingBuilding.m_position);
            int areaVariableCapacity = 0;
            string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)BuildingId, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)BuildingId, InstanceID.Empty))
            {
                overrideDistrictControl = true;
            }
            foreach (ushort id in structures)
            {
                int StormDrainAssetControlOption = OptionHandler.getDropdownSetting("StormDrainAssetControlOption");

                if (StormDrainAssetControlOption == OptionHandler._NoControlOption
                        || StormDrainAssetControlOption == OptionHandler._DistrictControlOption && Hydraulics.instance._districts[id] == district
                        || StormDrainAssetControlOption == OptionHandler._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || StormDrainAssetControlOption == OptionHandler._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                {
                    Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    if ((currentBuilding.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None && (currentBuilding.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None)
                        areaVariableCapacity += Hydraulics.instance._variableCapacity[id];
                }

            }
            //Debug.Log("[RF].Hydraulics VariableCapacity  at District " + district.ToString() + " is " + districtVariableCapacity);
            return areaVariableCapacity;
        }
        
        public static float getDetentionBasinWSE(int buildingID)
        {
            Building currentDetentionBasin = Hydraulics.instance._buildingManager.m_buildings.m_buffer[buildingID];
            StormDrainAI currentDetentionBasinAI = currentDetentionBasin.Info.m_buildingAI as StormDrainAI;
            if (currentDetentionBasinAI != null && currentDetentionBasinAI.m_stormWaterDetention > 0)
            {
                int DetainedStormwater = (int)Hydraulics.getDetainedStormwater(buildingID);
                float basinElevation = currentDetentionBasin.m_position.y;
                float basinInvert = basinElevation + currentDetentionBasinAI.m_invert;
                float basinSoffit = basinElevation + currentDetentionBasinAI.m_soffit;
                float basinDepth = basinSoffit - basinInvert;
                float basinWSE = basinInvert + basinDepth * (float)DetainedStormwater / (float)currentDetentionBasinAI.m_stormWaterDetention;
                return basinWSE;
            } else
            {
                removeDetentionBasin((ushort)buildingID);
            }
            return 0f;
        }
        public static int addPollutants(int buildingId, int amount)
        {
            Hydraulics.instance._pollutants[buildingId] += amount;
            return Hydraulics.instance._pollutants[buildingId];
        }
        public static int getPollutants(int buildingID)
        {
            return Hydraulics.instance._pollutants[buildingID];
        }
        public static int removePollutants(int buildingID, int amount)
        {
            int removed;
            if (amount >= Hydraulics.getPollutants(buildingID))
            {
                removed = Hydraulics.instance._pollutants[buildingID];
                Hydraulics.instance._pollutants[buildingID] = 0;
                
            }
            else {
                removed = amount;
                Hydraulics.instance._pollutants[buildingID] -= amount;
            }
            return removed;
        }
        
        public static void deleteAllAssets()
        {
            /*
            HashSet<ushort> rainfallAssets = new HashSet<ushort>(); ;
            rainfallAssets.UnionWith(Hydraulics.instance._SDinlets);
            rainfallAssets.UnionWith(Hydraulics.instance._SDoutlets);
            rainfallAssets.UnionWith(Hydraulics.instance._SDdetentionBasins);
            rainfallAssets.UnionWith(Hydraulics.instance._naturalDrainageAssets);
            rainfallAssets.UnionWith(Hydraulics.instance._snowpackAssets);
            foreach(ushort id in rainfallAssets)
            {
                Hydraulics.instance._buildingManager.ReleaseBuilding(id);
            }*/
            if (Hydraulics.instance.loaded == true)
            {
                try
                {
                    BuildingManager _buildingManager = Singleton<BuildingManager>.instance;
                    int _capacity = _buildingManager.m_buildings.m_buffer.Length;
                    int id;
                    for (id = 0; id < _capacity; id++)
                    {
                        if ((_buildingManager.m_buildings.m_buffer[id].m_flags & Building.Flags.Created) == Building.Flags.None || (_buildingManager.m_buildings.m_buffer[id].m_flags & Building.Flags.Untouchable) != Building.Flags.None || (_buildingManager.m_buildings.m_buffer[id].m_flags & Building.Flags.BurnedDown) != Building.Flags.None || (_buildingManager.m_buildings.m_buffer[id].m_flags & Building.Flags.Demolishing) != Building.Flags.None || (_buildingManager.m_buildings.m_buffer[id].m_flags & Building.Flags.Deleted) != Building.Flags.None)
                        {
                            // Debug.Log("[RF].Hydrology  Failed Flag Test: " + _buildingManager.m_buildings.m_buffer[id].m_flags.ToString());

                        }
                        else
                        {
                            BuildingAI ai = _buildingManager.m_buildings.m_buffer[id].Info.m_buildingAI;
                            if (ai is NaturalDrainageAI || ai is StormDrainAI)
                            {
                                // Debug.Log("[RF].Hydrology  Failed AI Test: " + ai.ToString());
                                _buildingManager.ReleaseBuilding((ushort)id);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("[PLS].deleteAllAssets Encountered Exception " + e);
                }
            }

        }
        public static void addInlet(ushort id)
        {
            Hydraulics.instance._SDinlets.Add(id);
            Hydraulics.updateDistrictAndDrainageGroup(id);
        }
        public static void removeInlet(ushort id)
        {
            if (Hydraulics.instance._SDinlets.Contains(id))
            {
                Hydraulics.instance._SDinlets.Remove(id);
                Hydraulics.instance._stormwaterAccumulation[id] = 0;
                Hydraulics.instance._districts[id] = (byte)0;
                Hydraulics.instance._drainageGroups[id] = String.Empty;
            }

        }
        public static void addOutlet(ushort id)
        {
            Hydraulics.instance._SDoutlets.Add(id);
            Hydraulics.updateDistrictAndDrainageGroup(id);

        }
        public static void removeOutlet(ushort id)
        {
            if (Hydraulics.instance._SDoutlets.Contains(id))
            {
                Hydraulics.instance._SDoutlets.Remove(id);
                Hydraulics.instance._stormwaterAccumulation[id] = 0;
                Hydraulics.instance._districts[id] = (byte)0;
                Hydraulics.instance._drainageGroups[id] = String.Empty;
            }
        }
        public static void addDetentionBasin(ushort id)
        {
            Hydraulics.instance._SDdetentionBasins.Add(id);
            Hydraulics.updateDistrictAndDrainageGroup(id);
        }
        public static void removeDetentionBasin(ushort id)
        {
            if (Hydraulics.instance._SDdetentionBasins.Contains(id)) { 
                Hydraulics.instance._SDdetentionBasins.Remove(id);
                Hydraulics.instance._stormwaterAccumulation[id] = 0;
                Hydraulics.instance._districts[id] = (byte)0;
                Hydraulics.instance._drainageGroups[id] = String.Empty;
            }
        }
        public static void addNaturalDrainageAsset(ushort id)
        {
            Hydraulics.instance._naturalDrainageAssets.Add(id);
        }
        
        public static void removeNaturalDrainageAsset(ushort id)
        {
            if (Hydraulics.instance._naturalDrainageAssets.Contains(id))
                Hydraulics.instance._naturalDrainageAssets.Remove(id);
        }
        /*
        public static void addSnowpackAsset(ushort id)
        {
            Hydraulics.instance._snowpackAssets.Add(id);
        }
        
        public static void removeSnowPackAsset(ushort id)
        {
            if (Hydraulics.instance._snowpackAssets.Contains(id))
                Hydraulics.instance._snowpackAssets.Remove(id);
        }*/
        public static void updateDistrictAndDrainageGroup(ushort id)
        {
            Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
            if (Hydraulics.instance._districts == null || Hydraulics.instance._drainageGroups == null || Hydraulics.instance._drainageGroupsNames == null)
            {
                return;
            }
            Hydraulics.instance._districts[id] = Hydraulics.instance._districtManager.GetDistrict(currentBuilding.m_position);
            Hydraulics.instance._drainageGroups[id] = Hydraulics.instance._buildingManager.GetBuildingName(id, InstanceID.Empty);
            if (Hydraulics.instance._drainageGroupsNames.Contains(Hydraulics.instance._drainageGroups[id]) == false && Hydraulics.instance._drainageGroups[id] != Hydraulics.instance._buildingManager.GetDefaultBuildingName(id, InstanceID.Empty))
                Hydraulics.instance._drainageGroupsNames.Add(Hydraulics.instance._drainageGroups[id]);
            
        }
        /*
        public static string generateCulvertName(ushort id, bool upstream)
        {
            string nameBase = "Culvert ";
            if (upstream)
            {
                if (Hydraulics.instance._unpairedDownstreamCulverts.Count > 0)
                {
                    return (string)Hydraulics.instance._unpairedDownstreamCulverts.Dequeue();
                } else
                {
                    string newName;
                    do {
                        Hydraulics.instance._lastUsedCulvert += 1;
                        newName = nameBase + Hydraulics.instance._lastUsedCulvert.ToString();
                    } while (Hydraulics.instance._drainageGroupsNames.Contains(newName));
                    Hydraulics.instance._unpairedUpstreamCulverts.Enqueue(newName);
                    return newName;
                }
            } else
            {
                if (Hydraulics.instance._unpairedUpstreamCulverts.Count > 0)
                {
                    return (string)Hydraulics.instance._unpairedUpstreamCulverts.Dequeue();
                } else
                {
                    string newName;
                    do
                    {
                        Hydraulics.instance._lastUsedCulvert += 1;
                        newName = nameBase + Hydraulics.instance._lastUsedCulvert.ToString();
                    } while (Hydraulics.instance._drainageGroupsNames.Contains(newName));
                    Hydraulics.instance._unpairedDownstreamCulverts.Enqueue(newName);
                    return newName;
                }
            }
            
        }
        */


    }
}