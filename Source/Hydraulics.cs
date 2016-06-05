
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Math;
using ICities;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

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
        
        private HashSet<ushort> _SDinlets;
        private HashSet<ushort> _SDoutlets;
        private HashSet<ushort> _SDdetentionBasins;
        private HashSet<ushort> _removeSDinlets;
        private HashSet<ushort> _removeSDoutlets;
        private HashSet<ushort> _removeSDdetentionBasins;
        public  HashSet<ushort> _previousFacilityWaterSources;
       

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
        }

        public override void OnCreated(IThreading threading)
        {
            InitializeManagers();
            instance = this;
            initialized = false;
            loaded = false;
            _capacity = _buildingManager.m_buildings.m_buffer.Length;
            _stormwaterAccumulation = new int[_capacity];
            _detainedStormwater = new int[_capacity];
            _districts = new byte[_capacity];
            _SDinlets = new HashSet<ushort>();
            _SDoutlets = new HashSet<ushort>();
            _SDdetentionBasins = new HashSet<ushort>();
            _removeSDinlets = new HashSet<ushort>();
            _removeSDoutlets = new HashSet<ushort>();
            _previousFacilityWaterSources = new HashSet<ushort>();
            _removeSDdetentionBasins = new HashSet<ushort>();
            _simulationTimeCount = 0f;
            _relocateDelay = 0f;
            _infiltrationPeriod = 1f;
            created = true;
            base.OnCreated(threading);
        }

        public override void OnBeforeSimulationTick()
        {
            if (terminated) return;

            if (!initialized) return;
           
            if (!loaded) return;

            if (!_buildingManager.m_buildingsUpdated) return;
          

            for (int i = 0; i < _buildingManager.m_updatedBuildings.Length; i++)
            {
                ulong ub = _buildingManager.m_updatedBuildings[i];
               
                if (ub != 0)
                {
                    for (int j = 0; j < 64; j++)
                    {
                        if ((ub & (ulong)1 << j) != 0)
                        {
                            ushort id = (ushort)(i << 6 | j);
                            Building currentBuilding = _buildingManager.m_buildings.m_buffer[id];
                            bool flag = (currentBuilding.m_flags & Building.Flags.Completed) != Building.Flags.None;
                            StormDrainAI currentStormDrainAi = currentBuilding.Info.m_buildingAI as StormDrainAI;
                           
                            if (currentStormDrainAi != null && flag == true)
                            {
                                if (currentStormDrainAi.m_stormWaterOutlet != 0)
                                {
                                    if (!_SDoutlets.Contains(id))
                                    {
                                        _SDoutlets.Add(id);
                                        _districts[id] = _districtManager.GetDistrict(currentBuilding.m_position);
                                        //Debug.Log("[RF].Hydraulics Added Outlet " + id.ToString() + " to district " + _districts[id]);
                                    } else
                                    {
                                        _SDoutlets.Remove(id);
                                        //Debug.Log("[RF].Hydraulics removed Outlet " + id.ToString() + " from district " + _districts[id]);
                                        _districts[id] = (byte)0;
                                    }
                                }
                                else if (currentStormDrainAi.m_stormWaterIntake != 0)
                                {
                                    if (!_SDinlets.Contains(id))
                                    {
                                        _SDinlets.Add(id);
                                        _districts[id] = _districtManager.GetDistrict(currentBuilding.m_position);
                                        //Debug.Log("[RF].Hydraulics Added Inlet " + id.ToString() + " to district " + _districts[id]);
                                    } else
                                    {
                                        _SDinlets.Remove(id);
                                        //Debug.Log("[RF].Hydraulics removed Inlet " + id.ToString() + " from district " + _districts[id]);
                                        _districts[id] = (byte)0;
                                    }
                                } else if (currentStormDrainAi.m_stormWaterDetention != 0)
                                {
                                    if (!_SDdetentionBasins.Contains(id))
                                    {
                                        _SDdetentionBasins.Add(id);
                                        _districts[id] = _districtManager.GetDistrict(currentBuilding.m_position);
                                    } else
                                    {
                                        _SDdetentionBasins.Remove(id);
                                        _districts[id] = (byte)0;
                                    }
                                }
                            }

                        }
                    }
                }
            }
            base.OnBeforeSimulationTick();
        }

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (terminated)
                return;

            if (!loaded)
                return;

            if (!created)
                return;

            if (!initialized)
            {
                //Debug.Log("Initializing!");
                InitializeManagers();
                //Debug.Log("Initialized managers");
                _SDinlets.Clear();
                _SDoutlets.Clear();
                _SDdetentionBasins.Clear();
                _removeSDinlets.Clear();
                _removeSDoutlets.Clear();
                _removeSDdetentionBasins.Clear();
                buildingWindowGameObject = new GameObject("buildingWindowObject");
                serviceBuildingInfo = UIView.Find<UIPanel>("(Library) CityServiceWorldInfoPanel");
                _cityServiceWorldInfoPanel = serviceBuildingInfo.gameObject.transform.GetComponentInChildren<CityServiceWorldInfoPanel>();
                //Debug.Log("Cleared hashsets");
                _capacity = _buildingManager.m_buildings.m_buffer.Length;
                _stormwaterAccumulation = new int[_capacity];
                _districts = new byte[_capacity];
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
                    
                        StormDrainAI currentStormDrainAi;
                        WaterFacilityAI currentWaterFacilityAI;
                        currentWaterFacilityAI = currentBuilding.Info.m_buildingAI as WaterFacilityAI;
                 
                        currentStormDrainAi = currentBuilding.Info.m_buildingAI as StormDrainAI;

                        if (currentStormDrainAi != null && flag == true)
                        {
                            if (currentStormDrainAi.m_stormWaterOutlet != 0)
                            {
                                /*
                                if (currentBuilding.Info.m_placementMode == BuildingInfo.PlacementMode.Shoreline)
                                    currentBuilding.Info.m_placementMode = BuildingInfo.PlacementMode.OnTerrain;
                                */
                                _SDoutlets.Add(id);
                                _districts[id] = _districtManager.GetDistrict(currentBuilding.m_position);
                                if (currentBuilding.m_waterSource != 0)
                                    _previousFacilityWaterSources.Add(currentBuilding.m_waterSource);
                                //Debug.Log("[RF].Hydraulics Added Outlet " + id.ToString() + " to district " + _districts[id]);
                            }
                            else if (currentStormDrainAi.m_stormWaterIntake != 0)
                            {

                                _SDinlets.Add(id);
                                _districts[id] = _districtManager.GetDistrict(currentBuilding.m_position);
                                if (currentBuilding.m_waterSource != 0)
                                    _previousFacilityWaterSources.Add(currentBuilding.m_waterSource);
                                //Debug.Log("[RF].Hydraulics Added Inlet " + id.ToString() + " to district " + _districts[id]);

                            }
                            else if (currentStormDrainAi.m_stormWaterDetention != 0)
                            {

                                _SDdetentionBasins.Add(id);
                                _districts[id] = _districtManager.GetDistrict(currentBuilding.m_position);
                                //Debug.Log("[RF].Hydraulics Added detention basin " + id.ToString() + " to district " + _districts[id]);
                            }
                        }
                        else if (currentWaterFacilityAI != null && flag == true)
                        {
                            if (currentBuilding.m_waterSource != 0)
                                _previousFacilityWaterSources.Add(currentBuilding.m_waterSource);
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
                
                foreach (ushort id in _SDinlets)
                {
                    if (reviewID(id))
                    {
                        Building currentBuilding = _buildingManager.m_buildings.m_buffer[id];
                        StormDrainAI currentStormDrainAi = currentBuilding.Info.m_buildingAI as StormDrainAI;
                        
                        if (currentStormDrainAi.m_stormWaterIntake != 0)
                        {
                        
                            _districts[id] = _districtManager.GetDistrict(currentBuilding.m_position);
                         
                        }
                        else
                            _removeSDinlets.Add(id);
                    } else
                    {
                        _removeSDinlets.Add(id);
                    }
                }
                foreach (ushort id in _SDoutlets)
                {
                    if (reviewID(id))
                    {
                        Building currentBuilding = _buildingManager.m_buildings.m_buffer[id];
                        
                        StormDrainAI currentStormDrainAi = currentBuilding.Info.m_buildingAI as StormDrainAI;
                       
                        if (currentStormDrainAi.m_stormWaterOutlet != 0)
                        {
                            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                            {
                                if (currentBuilding.Info.m_placementMode == BuildingInfo.PlacementMode.Shoreline)
                                    currentBuilding.Info.m_placementMode = BuildingInfo.PlacementMode.OnTerrain;
                            } else if (currentBuilding.Info.m_placementMode == BuildingInfo.PlacementMode.OnTerrain)
                            {
                                currentBuilding.Info.m_placementMode = BuildingInfo.PlacementMode.Shoreline;
                            }
                            _districts[id] = _districtManager.GetDistrict(currentBuilding.m_position);
                          
                        }
                        else
                            _removeSDoutlets.Add(id);
                    }
                    else
                    {
                        _removeSDoutlets.Add(id);
                    }
                }
                _simulationTimeCount += simulationTimeDelta * ModSettings.TimeScale;
                foreach (ushort id in _SDdetentionBasins)
                {
                    if (reviewID(id))
                    {
                        Building currentBuilding = _buildingManager.m_buildings.m_buffer[id];

                        StormDrainAI currentStormDrainAi = currentBuilding.Info.m_buildingAI as StormDrainAI;
                        if (currentStormDrainAi.m_stormWaterDetention != 0)
                        {
                            _districts[id] = _districtManager.GetDistrict(currentBuilding.m_position);
                         
                            if (_simulationTimeCount > _infiltrationPeriod)
                            {
                                Hydraulics.removeDetainedStormwater(id, currentStormDrainAi.m_stormWaterInfiltration);
                            } 
                        }
                        else
                            _removeSDdetentionBasins.Add(id);
                    }
                    else
                    {
                        _removeSDdetentionBasins.Add(id);
                    }
                }
                if (_simulationTimeCount > _infiltrationPeriod)
                {
                    _simulationTimeCount = 0f;
                }
                foreach(ushort id in _removeSDinlets)
                {
                    _SDinlets.Remove(id);
                    //Debug.Log("[RF].Hydraulics Removed inlet " + id.ToString());
                    _stormwaterAccumulation[id] = 0;
                    _districts[id] = (byte) 0;
                }
                foreach (ushort id in _removeSDoutlets)
                {
                    _SDoutlets.Remove(id);
                    //Debug.Log("[RF].Hydraulics Removed outlet " + id.ToString());
                    _districts[id] = (byte)0;
                }
                foreach (ushort id in _removeSDdetentionBasins)
                {
                    _SDdetentionBasins.Remove(id);
                    _districts[id] = (byte)0;
                }
                _removeSDinlets.Clear();
                _removeSDoutlets.Clear();
                _removeSDdetentionBasins.Clear();


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
                                if (currentBuildingAI.m_stormWaterOutlet != 0 && currentBuildingAI.m_electricityConsumption == 0)
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
                        }
                        else if (Input.GetKey(KeyCode.PageUp))
                        {

                            var buildingID = GetParentInstanceId().Building;

                            Building currentBuilding = _buildingManager.m_buildings.m_buffer[buildingID];
                            StormDrainAI currentBuildingAI = currentBuilding.Info.m_buildingAI as StormDrainAI;
                            if (currentBuildingAI != null)
                            {
                                if (currentBuildingAI.m_stormWaterOutlet != 0 && currentBuildingAI.m_electricityConsumption == 0)
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
            return Hydraulics.instance._stormwaterAccumulation[id];
        }
        public static int addStormwaterAccumulation(int id, int amount)
        {
            Hydraulics.instance._stormwaterAccumulation[id] += amount;
            //Debug.Log("[RF].Hydraulics Stormwater Accumulation at " + id.ToString() + " is raised to " + Hydraulics.instance._stormwaterAccumulation[id]);
            return Hydraulics.instance._stormwaterAccumulation[id];
        }
        public static int removeStormwaterAccumulation(int id, int amount)
        {
            if (amount < 0)
                return 0;
            if (Hydraulics.instance._stormwaterAccumulation[id] > amount)
            {
                Hydraulics.instance._stormwaterAccumulation[id] -= amount;
                //Debug.Log("[RF].Hydraulics Stormwater Accumulation at " + id.ToString() + " has dropped to " + Hydraulics.instance._stormwaterAccumulation[id]);
                return amount;
            } else
            {
                int removed = Hydraulics.instance._stormwaterAccumulation[id];
                Hydraulics.instance._stormwaterAccumulation[id] = 0;
                //Debug.Log("[RF].Hydraulics Stormwater Accumulation at " + id.ToString() + " has dropped to " + Hydraulics.instance._stormwaterAccumulation[id]);
                return removed;
            }
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

        public static int getDistrictStormwaterAccumulation(byte district)
        {
            int districtStormwaterAccumulation = 0;
            foreach (ushort id in Hydraulics.instance._SDinlets)
            {
                if (Hydraulics.instance._districts[id] == district)
                {
                    districtStormwaterAccumulation += Hydraulics.instance._stormwaterAccumulation[id];
                }

            }
            //Debug.Log("[RF].Hydraulics Stormwater Accumulation at District " + district.ToString() + " is " + districtStormwaterAccumulation);
            return districtStormwaterAccumulation;
        }
        public static int getDistrictStormwaterAccumulation(int BuildingId)
        {
            Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[BuildingId];
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentBuilding.m_position);
            //Debug.Log("[RF].Hydraulics Building " + BuildingId.ToString() + " is in district " + district.ToString());
            return getDistrictStormwaterAccumulation(district);
        }
        public static int getDistrictOutletCapacity(byte district)
        {
            int districtOutletCapacity = 0;
            foreach (ushort id in Hydraulics.instance._SDoutlets)
            {
                if (Hydraulics.instance._districts[id] == district)
                {
                    Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    StormDrainAI currentBuildingAI = currentBuilding.Info.m_buildingAI as StormDrainAI;
                    if (currentBuildingAI != null)
                    {
                        if (currentBuildingAI.m_stormWaterOutlet != 0 && (currentBuilding.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None)
                        {
                            districtOutletCapacity += currentBuildingAI.m_stormWaterOutlet;
                        }
                    }
                }

            }
            //Debug.Log("[RF].Hydraulics Stormdrain Capacity at District " + district.ToString() + " is " + districtOutletCapacity);
            return districtOutletCapacity;
        }
        public static int getDistrictOutletCapacity(int BuildingId)
        {
            Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[BuildingId];
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentBuilding.m_position);
            //Debug.Log("[RF].Hydraulics Building " + BuildingId.ToString() + " is in district " + district.ToString());
            return getDistrictOutletCapacity(district);
        }
        public static int getDistrictInletCapacity(byte district)
        {
            int districtInletCapacity = 0;
            foreach (ushort id in Hydraulics.instance._SDinlets)
            {
                if (Hydraulics.instance._districts[id] == district)
                {
                    Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    StormDrainAI currentBuildingAI = currentBuilding.Info.m_buildingAI as StormDrainAI;
                    if (currentBuildingAI != null)
                    {
                        if (currentBuildingAI.m_stormWaterIntake != 0 && (currentBuilding.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None)
                        {
                            districtInletCapacity += currentBuildingAI.m_stormWaterIntake;
                        }
                    }
                }

            }
            //Debug.Log("[RF].Hydraulics Stormdrain Capacity at District " + district.ToString() + " is " + districtOutletCapacity);
            return districtInletCapacity;
        }
        public static int getDistrictInletCapacity(int BuildingId)
        {
            Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[BuildingId];
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentBuilding.m_position);
            //Debug.Log("[RF].Hydraulics Building " + BuildingId.ToString() + " is in district " + district.ToString());
            return getDistrictInletCapacity(district);
        }

        public static int getDistrictDetentionCapacity(byte district)
        {
            int districtDetentionCapacity = 0;
            foreach (ushort id in Hydraulics.instance._SDdetentionBasins)
            {
                if (Hydraulics.instance._districts[id] == district)
                {
                    Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    StormDrainAI currentBuildingAI = currentBuilding.Info.m_buildingAI as StormDrainAI;
                    if (currentBuildingAI != null)
                    {
                        if (currentBuildingAI.m_stormWaterDetention != 0 && (currentBuilding.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None)
                        {
                            districtDetentionCapacity += currentBuildingAI.m_stormWaterDetention;
                        }
                    }
                }

            }
            //Debug.Log("[RF].Hydraulics Stormdrain detention capacity at District " + district.ToString() + " is " + districtDetentionCapacity);
            return districtDetentionCapacity;
        }
        public static int getDistrictDetentionCapacity(int BuildingId)
        {
            Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[BuildingId];
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentBuilding.m_position);
            //Debug.Log("[RF].Hydraulics Building " + BuildingId.ToString() + " is in district " + district.ToString());
            return getDistrictDetentionCapacity(district);
        }
        public static int getDistrictDetainedStormwater(byte district)
        {
            int districtDetainedStormwater = 0;
            foreach (ushort id in Hydraulics.instance._SDdetentionBasins)
            {
                if (Hydraulics.instance._districts[id] == district)
                {
                    Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    StormDrainAI currentBuildingAI = currentBuilding.Info.m_buildingAI as StormDrainAI;
                    if (currentBuildingAI != null)
                    {
                        bool flag1 = currentBuildingAI.m_stormWaterDetention != 0;
                        bool flag2 = (currentBuilding.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None;
                        if (flag1 && flag2)
                        {
                            districtDetainedStormwater += Hydraulics.getDetainedStormwater(id);
                        }
                    }
                }

            }
            //Debug.Log("[RF].Hydraulics Stormdrain Capacity at District " + district.ToString() + " is " + districtOutletCapacity);
            return districtDetainedStormwater;
        }
        public static int getDistrictDetainedStormwater(int BuildingId)
        {
            Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[BuildingId];
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentBuilding.m_position);
            //Debug.Log("[RF].Hydraulics Building " + BuildingId.ToString() + " is in district " + district.ToString());
            return getDistrictDetainedStormwater(district);
        }
        public static bool checkGravityFlowForInlet(int inletId)
        {
            Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[inletId];
        
       
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentInlet.m_position);
            if (Hydraulics.instance._SDoutlets.Count > 0)
            {
                foreach (ushort id in Hydraulics.instance._SDoutlets)
                {
                    if (Hydraulics.instance._districts[id] == district)
                    {
                        Building currentOutlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                        StormDrainAI currentInletAI = currentInlet.Info.m_buildingAI as StormDrainAI;
                        StormDrainAI currentOutletAI = currentOutlet.Info.m_buildingAI as StormDrainAI;
                        if (currentInlet.m_position.y > currentOutlet.m_position.y || currentOutletAI.m_electricityConsumption > 0 || currentInletAI.m_electricityConsumption > 0)
                            return true;
                    }
                }
                foreach (ushort id in Hydraulics.instance._SDdetentionBasins)
                {
                    Building currentDetentionBasin = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    StormDrainAI currentDetentionBasinAI = currentDetentionBasin.Info.m_buildingAI as StormDrainAI;
                    if (currentDetentionBasinAI != null)
                    {
                        bool flag1 = currentDetentionBasinAI.m_stormWaterDetention > 0;
                        bool flag2 = Hydraulics.instance._districts[id] == district;
                        bool flag3 = (currentDetentionBasin.m_problems & Notification.Problem.LandfillFull) == Notification.Problem.None;
                        if (flag1 && flag2 && flag3) { 
                            return true;
                        }
                    }
                        
                }
            }
            return false;
        }
        public static bool checkGravityFlowForInlet(int inletId, int outletId)
        {
            Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[inletId];
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentInlet.m_position);

            if (Hydraulics.instance._districts[outletId] == district)
            {
                Building currentOutlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[outletId];
                StormDrainAI currentInletAI = currentInlet.Info.m_buildingAI as StormDrainAI;
                StormDrainAI currentOutletAI = currentOutlet.Info.m_buildingAI as StormDrainAI;
                if (currentInlet.m_position.y > currentOutlet.m_position.y || currentOutletAI.m_electricityConsumption > 0 || currentInletAI.m_electricityConsumption > 0)
                    return true;
            }
            
            return false;
        }
        public static int getOutletCapacityForInlet(int inletID)
        {
            int outletCapacity = 0;
            Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[inletID];
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentInlet.m_position);

            foreach (ushort id in Hydraulics.instance._SDoutlets)
            {
                if (Hydraulics.instance._districts[id] == district)
                {
                    Building currentOutlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    StormDrainAI currentOutletAI = currentOutlet.Info.m_buildingAI as StormDrainAI;
                    if (currentOutletAI != null)
                    {
                        bool flag1 = currentOutletAI.m_stormWaterOutlet > 0;
                        bool flag2 = checkGravityFlowForInlet(inletID, id);
                        bool flag3 = (currentOutlet.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None;
                        if (flag1 && flag2 && flag3)
                        {
                            outletCapacity += currentOutletAI.m_stormWaterOutlet;
                        }
                    }
                }
            }
            return outletCapacity;
        }
        public static int getStormWaterAccumulationForOutlet(int outletID)
        {
            int stormwaterAccumulation = 0;
            Building currentOutlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[outletID];
            StormDrainAI currentOutletAI = currentOutlet.Info.m_buildingAI as StormDrainAI;
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentOutlet.m_position);
            foreach (ushort inletId in Hydraulics.instance._SDinlets)
            {
                if (Hydraulics.instance._districts[inletId] == district)
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
                }

            }
            return stormwaterAccumulation;
        }
        public static int removeDistrictStormwaterAccumulation(byte district, int amount, int outletID, bool checkGravityFlow)
        {
            int districtStormwaterAccumulation = Hydraulics.getDistrictStormwaterAccumulation(district);
            int minimumStormwaterAccumulation = amount;
            Building outletBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[outletID];
            StormDrainAI outletBuildingAI = outletBuilding.Info.m_buildingAI as StormDrainAI;
            bool simulatePollution = false;
            WaterSource outletWaterSource = new WaterSource();
            if (outletBuildingAI != null)
            {
                if (outletBuilding.m_waterSource != 0 && outletBuildingAI.m_stormWaterOutlet > 0)
                {
                    simulatePollution = true;
                    outletWaterSource = Hydraulics.instance._waterSimulation.LockWaterSource(outletBuilding.m_waterSource);
                    outletWaterSource.m_pollution = 0;
                } 
            } 
            
            if (districtStormwaterAccumulation <= amount)
            {
                foreach (ushort id in Hydraulics.instance._SDinlets)
                {
                    Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    bool flag1 = Hydraulics.instance._districts[id] == district;
                    bool flag2 = Hydraulics.instance._stormwaterAccumulation[id] > 0;
                    bool flag3 = (currentInlet.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None;
                    bool flag4;
                    if (checkGravityFlow == true)
                        flag4 = Hydraulics.checkGravityFlowForInlet(id, outletID);
                    else
                        flag4 = true;

                    if (flag1 && flag2 && flag3 && flag4)
                    {
                        if (simulatePollution)
                        {

                            outletWaterSource.m_pollution += (uint)(Mathf.Round((float)Hydraulics.instance._stormwaterAccumulation[id] * 0.5f * ((float)currentInlet.m_waterPollution / 255f)));
                            //Debug.Log("currentInlet.m_waterPolution = " + currentInlet.m_waterPollution.ToString());
                        }
                        Hydraulics.instance._stormwaterAccumulation[id] = 0;
                       
                    }
                }
                if (simulatePollution)
                {
                    outletWaterSource.m_pollution = (uint)Mathf.Min(outletWaterSource.m_pollution, outletWaterSource.m_water * 100 / Mathf.Max(100, Hydraulics.instance._waterSimulation.GetPollutionDisposeRate() * 100)); 
                    //Debug.Log("[RF].Hydraulics Set pollution at building " + outletID.ToString() + " to " + outletWaterSource.m_pollution.ToString() + " out of " + outletWaterSource.m_water.ToString());
                    Hydraulics.instance._waterSimulation.UnlockWaterSource(outletBuilding.m_waterSource, outletWaterSource);
                }
                //Debug.Log("[RF].Hydraulics Removed districtStormwaterAccumulation = " + districtStormwaterAccumulation.ToString() + " From District " + district.ToString());
                return districtStormwaterAccumulation;
            } else
            {
                int removed = 0;
                HashSet<ushort> districtInlets = new HashSet<ushort>();
                HashSet<ushort> emptyInlets = new HashSet<ushort>();
                foreach (ushort id in Hydraulics.instance._SDinlets)
                {
                    Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    bool flag1 = Hydraulics.instance._districts[id] == district;
                    bool flag2 = Hydraulics.instance._stormwaterAccumulation[id] > 0;
                    bool flag3 = (currentInlet.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None;
                    bool flag4;
                    if (checkGravityFlow == true)
                        flag4 = Hydraulics.checkGravityFlowForInlet(id, outletID);
                    else
                        flag4 = true;
                    
                    if (flag1 && flag2 && flag3 && flag4)
                    {
                        districtInlets.Add(id);
                        if (minimumStormwaterAccumulation > Hydraulics.instance._stormwaterAccumulation[id])
                            minimumStormwaterAccumulation = Hydraulics.instance._stormwaterAccumulation[id];
                    }
                }
                while (removed < amount)
                {
                    int removalAmount = minimumStormwaterAccumulation;
                    //Debug.Log("[RF].Hydrulics Removal Amount = " + removalAmount.ToString());
                    minimumStormwaterAccumulation = amount - removed;
                    //Debug.Log("[RF].Hydrulics amount - removed = " + minimumStormwaterAccumulation.ToString());
                    
                    foreach (ushort id in districtInlets)
                    {
                        /*Debug.Log("[RF].Hydrulics removed = " + removed.ToString());
                        Debug.Log("[RF].Hydrulics minimumStormwaterAccumulation = " + minimumStormwaterAccumulation.ToString());
                        Debug.Log("[RF].Hydrulics SDA = " + Hydraulics.getDistrictStormwaterAccumulation(id).ToString() + " at district " + district.ToString());
                        Debug.Log("[RF].Hydrulics SDA = " + Hydraulics.getStormwaterAccumulation(id).ToString() + " at building " + id.ToString());
                        */
                        Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                        if (removed + removalAmount <= amount)
                        {
                            int justRemoved = Hydraulics.removeStormwaterAccumulation(id, removalAmount);
                            removed += justRemoved;
                            if (simulatePollution)
                            {
                                outletWaterSource.m_pollution += (uint)(Mathf.Round((float)justRemoved * 0.5f * ((float)currentInlet.m_waterPollution / 255f)));
                            }
                        }
                        else {
                            int justRemoved = Hydraulics.removeStormwaterAccumulation(id, amount - removed);
                            removed += justRemoved;
                            if (simulatePollution)
                            {
                               
                                outletWaterSource.m_pollution += (uint)(Mathf.Round((float)justRemoved * 0.5f* ((float)currentInlet.m_waterPollution / 255f)));
                            }
                        }
                        //Debug.Log("[RF].Hydrulics removed = " + removed.ToString());
                        //Debug.Log("[RF].Hydrulics SDA = " + Hydraulics.getDistrictStormwaterAccumulation(id).ToString() + " at district " + district.ToString());
                        //Debug.Log("[RF].Hydrulics SWA = " + Hydraulics.getStormwaterAccumulation(id).ToString() + " at building " + id.ToString());
                        if (minimumStormwaterAccumulation > Hydraulics.getStormwaterAccumulation(id) && Hydraulics.getStormwaterAccumulation(id) > 0)
                            minimumStormwaterAccumulation = Hydraulics.getStormwaterAccumulation(id);
                        
                        //Debug.Log("[RF].Hydrulics minimumStormwaterAccumulation = " + minimumStormwaterAccumulation.ToString());
                        if (Hydraulics.getStormwaterAccumulation(id) == 0)
                            emptyInlets.Add(id);
                        else if (Hydraulics.getStormwaterAccumulation(id) < 0)
                        {
                            Hydraulics.instance._stormwaterAccumulation[id] = 0;
                            Debug.Log("[RF].Hydraulics Error SWS < 0");
                            emptyInlets.Add(id);
                        }
                        if (removed >= amount)
                            break;
                    }

                    foreach (ushort id in emptyInlets)
                    {
                        //Debug.Log("[RF].Hydraulics Removing empty inlet" + id.ToString());
                        districtInlets.Remove(id);
                    }
                    emptyInlets.Clear();
                    if (districtInlets.Count == 0)
                    {
                        if (simulatePollution)
                        {
                            outletWaterSource.m_pollution = (uint)Mathf.Min(outletWaterSource.m_pollution, outletWaterSource.m_water * 100 / Mathf.Max(100, Hydraulics.instance._waterSimulation.GetPollutionDisposeRate() * 100));
                            //Debug.Log("[RF].Hydraulics Set pollution at building " + outletID.ToString() + " to " + outletWaterSource.m_pollution.ToString() + " out of " + outletWaterSource.m_water.ToString());
                            Hydraulics.instance._waterSimulation.UnlockWaterSource(outletBuilding.m_waterSource, outletWaterSource);
                        }
                        //Debug.Log("[RF].Hydraulics Removed = " + removed.ToString() + " From District " + district.ToString());
                        return removed;
                    }
                    
                }
            }
            if (simulatePollution)
            {
                outletWaterSource.m_pollution = (uint)Mathf.Min(outletWaterSource.m_pollution, outletWaterSource.m_water * 100u / (uint)Mathf.Max(100, Hydraulics.instance._waterSimulation.GetPollutionDisposeRate() * 100));
                //Debug.Log("[RF].Hydraulics Set pollution at building " + outletID.ToString() + " to " + outletWaterSource.m_pollution.ToString() + " out of " + outletWaterSource.m_water.ToString());
                Hydraulics.instance._waterSimulation.UnlockWaterSource(outletBuilding.m_waterSource, outletWaterSource);
            }
            //Debug.Log("[RF].Hydraulics Removed amount =  " + amount.ToString() + " From District " + district.ToString());
            return amount;
        }
        
        public static int removeDistrictStormwaterAccumulation(int BuildingId, int amount, int outletID, bool checkGravityFlow)
        {
            Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[BuildingId];
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentBuilding.m_position);
            //Debug.Log("[RF].Hydraulics Building " + BuildingId.ToString() + " is in district " + district.ToString());
            return removeDistrictStormwaterAccumulation(district, amount, outletID, checkGravityFlow);
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
    }
}