
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
        private int[] _hydraulicRate;
        private int[] _easyModeSWA;
        private int[] _pollutants;
        private int[] _variableCapacity;
        
        private HashSet<ushort> _SDinlets;
        private HashSet<ushort> _SDoutlets;
        private HashSet<ushort> _SDdetentionBasins;
        private HashSet<ushort> _removeSDinlets;
        private HashSet<ushort> _removeSDoutlets;
        private HashSet<ushort> _removeSDdetentionBasins;
        public  HashSet<ushort> _previousFacilityWaterSources;

        private string[] _drainageGroups;
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
            /*Debug.Log("milestone names are:");
            foreach (KeyValuePair<string, MilestoneInfo> pair in Singleton<UnlockManager>.instance.m_allMilestones)
            {
                Debug.Log(pair.Key);
            }*/

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
            _hydraulicRate = new int[_capacity];
            _variableCapacity = new int[_capacity];
            _easyModeSWA = new int[_capacity];
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
            _drainageGroups = new string[_capacity];
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
                                _drainageGroups[id] = _buildingManager.GetBuildingName(id, InstanceID.Empty);
                                if (currentBuilding.m_waterSource != 0)
                                    _previousFacilityWaterSources.Add(currentBuilding.m_waterSource);
                                //Debug.Log("[RF].Hydraulics Added Outlet " + id.ToString() + " to district " + _districts[id]);
                            }
                            else if (currentStormDrainAi.m_stormWaterIntake != 0)
                            {

                                _SDinlets.Add(id);
                                _districts[id] = _districtManager.GetDistrict(currentBuilding.m_position);
                                _drainageGroups[id] = _buildingManager.GetBuildingName(id, InstanceID.Empty);
                                if (currentBuilding.m_waterSource != 0)
                                    _previousFacilityWaterSources.Add(currentBuilding.m_waterSource);
                                //Debug.Log("[RF].Hydraulics Added Inlet " + id.ToString() + " to district " + _districts[id]);

                            }
                            else if (currentStormDrainAi.m_stormWaterDetention != 0)
                            {

                                _SDdetentionBasins.Add(id);
                                _districts[id] = _districtManager.GetDistrict(currentBuilding.m_position);
                                _drainageGroups[id] = _buildingManager.GetBuildingName(id, InstanceID.Empty);
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
                            /*
                            if (currentBuilding.Info.m_placementMode != BuildingInfo.PlacementMode.OnTerrain || currentBuilding.Info.m_placementMode != BuildingInfo.PlacementMode.Shoreline)
                            {
                                if (currentStormDrainAi.m_electricityConsumption == 0)
                                    currentBuilding.Info.m_placementMode = BuildingInfo.PlacementMode.OnTerrain;
                                else
                                    currentBuilding.Info.m_placementMode = BuildingInfo.PlacementMode.Shoreline;
                            }*/
                            _districts[id] = _districtManager.GetDistrict(currentBuilding.m_position);
                            _drainageGroups[id] = _buildingManager.GetBuildingName(id, InstanceID.Empty);

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
                            /*
                            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                            {
                                if (currentBuilding.Info.m_placementMode == BuildingInfo.PlacementMode.Shoreline)
                                    currentBuilding.Info.m_placementMode = BuildingInfo.PlacementMode.OnTerrain;
                            } else if (currentBuilding.Info.m_placementMode == BuildingInfo.PlacementMode.OnTerrain)
                            {
                                currentBuilding.Info.m_placementMode = BuildingInfo.PlacementMode.Shoreline;
                            }
                            */
                            _districts[id] = _districtManager.GetDistrict(currentBuilding.m_position);
                            _drainageGroups[id] = _buildingManager.GetBuildingName(id, InstanceID.Empty);

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
                            _drainageGroups[id] = _buildingManager.GetBuildingName(id, InstanceID.Empty);
                            /*if (currentBuilding.Info.m_placementMode != BuildingInfo.PlacementMode.OnGround)
                            {
                                currentBuilding.Info.m_placementMode = BuildingInfo.PlacementMode.OnGround;
                            }*/
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
                    _drainageGroups[id] = String.Empty;
                }
                foreach (ushort id in _removeSDoutlets)
                {
                    _SDoutlets.Remove(id);
                    //Debug.Log("[RF].Hydraulics Removed outlet " + id.ToString());
                    _districts[id] = (byte)0;
                    _drainageGroups[id] = String.Empty;
                }
                foreach (ushort id in _removeSDdetentionBasins)
                {
                    _SDdetentionBasins.Remove(id);
                    _districts[id] = (byte)0;
                    _drainageGroups[id] = String.Empty;
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

            foreach (ushort id in Hydraulics.instance._SDinlets)
            {
                if (   ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption 
                    || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[id] == district 
                    || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                    || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                    || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
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
            foreach (ushort id in Hydraulics.instance._SDoutlets)
            {

                if (ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption
                || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[id] == district
                || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                {
                    Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                        StormDrainAI currentBuildingAI = currentBuilding.Info.m_buildingAI as StormDrainAI;
                        if (currentBuildingAI != null)
                        {
                            if (currentBuildingAI.m_stormWaterOutlet != 0 && (currentBuilding.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None && (currentBuilding.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None)
                            {
                                areaOutletCapacity += currentBuildingAI.m_stormWaterOutlet;
                            }
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
            foreach (ushort id in Hydraulics.instance._SDinlets)
            {
                if (ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption
                || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[id] == district
                || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                {
                    Building currentBuilding = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    StormDrainAI currentBuildingAI = currentBuilding.Info.m_buildingAI as StormDrainAI;
                    if (currentBuildingAI != null)
                    {
                        if (currentBuildingAI.m_stormWaterIntake != 0 && (currentBuilding.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None && (currentBuilding.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None)
                        {
                            areaInletCapacity += currentBuildingAI.m_stormWaterIntake;
                        }
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
            foreach (ushort id in Hydraulics.instance._SDdetentionBasins)
            {
                if (ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption
               || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[id] == district
               || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
               || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
               || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
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
            foreach (ushort id in Hydraulics.instance._SDdetentionBasins)
            {
                if (ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption
               || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[id] == district
               || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
               || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
               || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
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
                foreach (ushort id in Hydraulics.instance._SDoutlets)
                {
                    if (ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption
                        || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[id] == district
                        || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                    {

                        Building currentOutlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                        StormDrainAI currentInletAI = currentInlet.Info.m_buildingAI as StormDrainAI;
                        StormDrainAI currentOutletAI = currentOutlet.Info.m_buildingAI as StormDrainAI;
                        if ((currentOutlet.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None)
                        {
                            if (ModSettings.GravityDrainageOption == ModSettings._ImprovedGravityDrainageOption)
                            {
                                float inletWSE = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(currentInlet.m_position + currentInletAI.m_waterLocationOffset));
                                float outletWSE = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(currentOutlet.m_position + currentOutletAI.m_waterLocationOffset));

                                if (Mathf.Max(currentInlet.m_position.y, inletWSE) > Mathf.Max(currentOutlet.m_position.y + currentOutletAI.m_invert, outletWSE) || currentOutletAI.m_electricityConsumption > 0 || currentInletAI.m_electricityConsumption > 0)
                                    return true;
                            }
                            else if (ModSettings.GravityDrainageOption == ModSettings._SimpleGravityDrainageOption)
                            {

                                if (currentInlet.m_position.y > currentOutlet.m_position.y || currentOutletAI.m_electricityConsumption > 0 || currentInletAI.m_electricityConsumption > 0)
                                    return true;
                            }
                            else if (ModSettings.GravityDrainageOption == ModSettings._IgnoreGravityDrainageOption)
                            {
                                return true;
                            }
                        }
                    }
                }
                foreach (ushort id in Hydraulics.instance._SDdetentionBasins)
                {
                    if (ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption
                        || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[id] == district
                        || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
                    {
                        Building currentDetentionBasin = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                        StormDrainAI currentDetentionBasinAI = currentDetentionBasin.Info.m_buildingAI as StormDrainAI;
                        StormDrainAI currentInletAI = currentInlet.Info.m_buildingAI as StormDrainAI;
                        if (currentDetentionBasinAI != null)
                        {
                            bool flag1 = currentDetentionBasinAI.m_stormWaterDetention > 0;
                            bool flag2 = Hydraulics.instance._districts[id] == district;
                            bool flag3 = (currentDetentionBasin.m_problems & Notification.Problem.LandfillFull) == Notification.Problem.None;
                            bool flag4 = false;
                            bool flag5 = (currentDetentionBasin.m_problems & Notification.Problem.TurnedOff) == Notification.Problem.None;
                            if (ModSettings.GravityDrainageOption == ModSettings._ImprovedGravityDrainageOption)
                            {
                                float inletWSE = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(currentInlet.m_position + currentInletAI.m_waterLocationOffset));
                                float basinWSE = Hydraulics.getDetentionBasinWSE(id);

                                if (Mathf.Max(currentInlet.m_position.y, inletWSE) >  basinWSE || currentInletAI.m_electricityConsumption > 0)
                                    flag4 = true;
                            }
                            else if (ModSettings.GravityDrainageOption == ModSettings._SimpleGravityDrainageOption)
                            {

                                flag4 = true;
                            }
                            else if (ModSettings.GravityDrainageOption == ModSettings._IgnoreGravityDrainageOption)
                            {
                                flag4 = true;
                            }
                            if (flag1 && flag2 && flag3 && flag4 && flag5)
                            {
                                return true;
                            }
                        }
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
            if (ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption
                        || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[outletId] == district
                        || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[outletId] == drainageGroup
                        || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[outletId] == drainageGroup
                        || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[outletId] == district)
            {
                Building currentOutlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[outletId];
                StormDrainAI currentInletAI = currentInlet.Info.m_buildingAI as StormDrainAI;
                StormDrainAI currentOutletAI = currentOutlet.Info.m_buildingAI as StormDrainAI;
                if ((currentOutlet.m_problems & Notification.Problem.TurnedOff) != Notification.Problem.None) {
                    return false;
                }
                if (ModSettings.GravityDrainageOption == ModSettings._ImprovedGravityDrainageOption)
                {
                    float inletWSE = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(currentInlet.m_position + currentInletAI.m_waterLocationOffset));
                    if (currentOutletAI.m_stormWaterOutlet > 0)
                    {
                        float outletWSE = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(currentOutlet.m_position + currentOutletAI.m_waterLocationOffset));

                        if (Mathf.Max(currentInlet.m_position.y, inletWSE) > Mathf.Max(currentOutlet.m_position.y + currentOutletAI.m_invert, outletWSE) || currentOutletAI.m_electricityConsumption > 0 || currentInletAI.m_electricityConsumption > 0 )
                            return true;
                    } else if (currentOutletAI.m_stormWaterDetention > 0)
                    {
                        float basinWSE = Hydraulics.getDetentionBasinWSE(outletId);
                        bool flag3 = (currentOutlet.m_problems & Notification.Problem.LandfillFull) == Notification.Problem.None;
                        bool flag4 = false;
                        if (Mathf.Max(currentInlet.m_position.y, inletWSE) > basinWSE || currentInletAI.m_electricityConsumption > 0)
                            flag4 = true;
                        if (flag3 && flag4)
                            return true;
                    }
                
                } else if (ModSettings.GravityDrainageOption == ModSettings._SimpleGravityDrainageOption)
                {
                    if (currentInlet.m_position.y > currentOutlet.m_position.y || currentOutletAI.m_electricityConsumption > 0 || currentInletAI.m_electricityConsumption > 0 || currentOutletAI.m_stormWaterDetention > 0)
                        return true;
                } else if (ModSettings.GravityDrainageOption == ModSettings._IgnoreGravityDrainageOption)
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
            string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)inletID, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)inletID, InstanceID.Empty))
            {
                overrideDistrictControl = true;
            }
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentInlet.m_position);

            foreach (ushort id in Hydraulics.instance._SDoutlets)
            {
                if (ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption
                       || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[id] == district
                       || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                       || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                       || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
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
                            outletCapacity += currentOutletAI.m_stormWaterOutlet;
                        }
                    }
                }
            }
            return outletCapacity;
        }
        public static ushort getLowestOutletForInlet(int inletID)
        {
            float outletElev = 999999f;
            ushort outletID = 0;
            Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[inletID];
            string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)inletID, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)inletID, InstanceID.Empty))
            {
                overrideDistrictControl = true;
            }
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentInlet.m_position);

            foreach (ushort id in Hydraulics.instance._SDoutlets)
            {
                if (ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption
                        || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[id] == district
                        || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
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
                }
            }
            foreach (ushort id in Hydraulics.instance._SDdetentionBasins)
            {
                if (ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption
                       || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[id] == district
                       || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                       || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                       || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
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
                }
            }
            return outletID;
        }
        public static int getStormWaterAccumulationForOutlet(int outletID)
        {
            int stormwaterAccumulation = 0;
            Building currentOutlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[outletID];
            StormDrainAI currentOutletAI = currentOutlet.Info.m_buildingAI as StormDrainAI;
            string drainageGroup = Hydraulics.instance._buildingManager.GetBuildingName((ushort)outletID, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Hydraulics.instance._buildingManager.GetDefaultBuildingName((ushort)outletID, InstanceID.Empty))
            {
                overrideDistrictControl = true;
            }
            byte district = Hydraulics.instance._districtManager.GetDistrict(currentOutlet.m_position);
            foreach (ushort inletId in Hydraulics.instance._SDinlets)
            {
                if (ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption
                       || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[inletId] == district
                       || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[inletId] == drainageGroup
                       || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[inletId] == drainageGroup
                       || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[inletId] == district)
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
                    simulatePollution = true;
                    outletWaterSource = Hydraulics.instance._waterSimulation.LockWaterSource(outletBuilding.m_waterSource);
                    outletWaterSource.m_pollution = 0;
                } 
            } 
            if (/*ModSettings.EasyMode == true || */ModSettings.SimulatePollution == false || outletBuildingAI.m_filter == true)
            {
                //Debug.Log("[RF].Hydraulics.removeDistrictSWA EAsy mode means no pollution");
                //No pollution mechanics in easy mode or if you turn of pollution simulation
                simulatePollution = false;
            }
            if (outletBuildingAI.m_filter == true)
            {
                simulateFiltration = true;
            }
            
            if (districtStormwaterAccumulation <= amount)
            {
                foreach (ushort id in Hydraulics.instance._SDinlets)
                {
                    Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    bool flag1 = (ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption
                       || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[id] == district
                       || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                       || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                       || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district);
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
                        } else if (simulateFiltration)
                        {
                            pollutantsFromInlet += (int)(Mathf.Round((float)Hydraulics.instance._stormwaterAccumulation[id] * 0.5f * ((float)currentInlet.m_waterPollution / 255f)));
                        }
                        Hydraulics.instance._stormwaterAccumulation[id] = 0;
                       
                    }
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
                }
                //Debug.Log("[RF].Hydraulics.removeDistrictSWA EAsy mode less than total made it to return " + districtStormwaterAccumulation);
                //Debug.Log("[RF].Hydraulics Removed districtStormwaterAccumulation = " + districtStormwaterAccumulation.ToString() + " From District " + district.ToString());
                return districtStormwaterAccumulation;
            } else
            {
                int removed = 0;
                HashSet<ushort> districtSources = new HashSet<ushort>();
                HashSet<ushort> emptySources = new HashSet<ushort>();
                foreach (ushort id in Hydraulics.instance._SDinlets)
                {
                    Building currentInlet = Hydraulics.instance._buildingManager.m_buildings.m_buffer[id];
                    bool flag1 = (ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption
                         || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[id] == district
                         || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                         || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                         || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district);
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
                        if (minimumStormwaterAccumulation > Hydraulics.instance._stormwaterAccumulation[id])
                            minimumStormwaterAccumulation = Hydraulics.instance._stormwaterAccumulation[id];
                    }
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
                while (removed < amount)
                {
                    int removalAmount = minimumStormwaterAccumulation;
                    //Debug.Log("[RF].Hydrulics Removal Amount = " + removalAmount.ToString());
                    minimumStormwaterAccumulation = amount - removed;
                    //Debug.Log("[RF].Hydrulics amount - removed = " + minimumStormwaterAccumulation.ToString());
                    
                    foreach (ushort id in districtSources)
                    {
                        /*Debug.Log("[RF].Hydrulics removed = " + removed.ToString());
                        Debug.Log("[RF].Hydrulics minimumStormwaterAccumulation = " + minimumStormwaterAccumulation.ToString());
                        Debug.Log("[RF].Hydrulics SDA = " + Hydraulics.getDistrictStormwaterAccumulation(id).ToString() + " at district " + district.ToString());
                        Debug.Log("[RF].Hydrulics SDA = " + Hydraulics.getStormwaterAccumulation(id).ToString() + " at building " + id.ToString());
                        */
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
                        //Debug.Log("[RF].Hydrulics SDA = " + Hydraulics.getDistrictStormwaterAccumulation(id).ToString() + " at district " + district.ToString());
                        //Debug.Log("[RF].Hydrulics SWA = " + Hydraulics.getStormwaterAccumulation(id).ToString() + " at building " + id.ToString());
                        if (minimumStormwaterAccumulation > Hydraulics.getStormwaterAccumulation(id) && Hydraulics.getStormwaterAccumulation(id) > 0)
                            minimumStormwaterAccumulation = Hydraulics.getStormwaterAccumulation(id);
                        
                        //Debug.Log("[RF].Hydrulics minimumStormwaterAccumulation = " + minimumStormwaterAccumulation.ToString());
                        if (Hydraulics.getStormwaterAccumulation(id) == 0)
                            emptySources.Add(id);
                        else if (Hydraulics.getStormwaterAccumulation(id) < 0)
                        {
                            Hydraulics.instance._stormwaterAccumulation[id] = 0;
                            Debug.Log("[RF].Hydraulics Error SWS < 0");
                            emptySources.Add(id);
                        }
                        if (removed >= amount)
                            break;
                    }

                    foreach (ushort id in emptySources)
                    {
                        //Debug.Log("[RF].Hydraulics Removing empty inlet" + id.ToString());
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
                        }
                        //Debug.Log("[RF].Hydraulics Removed = " + removed.ToString() + " From District " + district.ToString());
                        return removed;
                    }
                    if (districtSources.Count == 0 || removalAmount == 0)
                    {
                        break;
                    }
                }
            }
            if (simulatePollution)
            {
                outletWaterSource.m_pollution = (uint)Mathf.Min(outletWaterSource.m_pollution, outletWaterSource.m_water * 100u / (uint)Mathf.Max(100, Hydraulics.instance._waterSimulation.GetPollutionDisposeRate() * 100));
                //Debug.Log("[RF].Hydraulics Set pollution at building " + outletID.ToString() + " to " + outletWaterSource.m_pollution.ToString() + " out of " + outletWaterSource.m_water.ToString());
                if (outletBuildingAI != null)
                {
                    if (outletBuilding.m_waterSource != 0 && outletBuildingAI.m_stormWaterOutlet > 0)
                    {
                        Hydraulics.instance._waterSimulation.UnlockWaterSource(outletBuilding.m_waterSource, outletWaterSource);
                    }
                }
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
                
                if (ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption
                         || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[id] == district
                         || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                         || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                         || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
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
                if (ModSettings.StormDrainAssetControlOption == ModSettings._NoControlOption
                        || ModSettings.StormDrainAssetControlOption == ModSettings._DistrictControlOption && Hydraulics.instance._districts[id] == district
                        || ModSettings.StormDrainAssetControlOption == ModSettings._IDControlOption && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == true && Hydraulics.instance._drainageGroups[id] == drainageGroup
                        || ModSettings.StormDrainAssetControlOption == ModSettings._IDOverrideOption && overrideDistrictControl == false && Hydraulics.instance._districts[id] == district)
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
            foreach (ushort id in Hydraulics.instance._SDinlets)
            {
                Hydraulics.instance._buildingManager.ReleaseBuilding(id);
            }
            Hydraulics.instance._SDinlets.Clear();
            foreach (ushort id in Hydraulics.instance._SDoutlets)
            {
                Hydraulics.instance._buildingManager.ReleaseBuilding(id);
            }
            Hydraulics.instance._SDoutlets.Clear();
            foreach (ushort id in Hydraulics.instance._SDdetentionBasins)
            {
                Hydraulics.instance._buildingManager.ReleaseBuilding(id);
            }
            Hydraulics.instance._SDdetentionBasins.Clear();
            
        }

        
       
    }
}