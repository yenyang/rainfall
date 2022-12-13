using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;
using ColossalFramework.Math;

namespace Rainfall
{
    public static class WaterSourceManager 
    {
        private static bool awake = false;

        private static readonly int waterSourceLimit = 65536;

        private static WaterSourceEntry[] m_buffer = new WaterSourceEntry[waterSourceLimit];

        private static int m_entryCount = 0;
        public static void Awake()
        {
            GenerateWaterSourceTypeBuffer();
            awake = true;
        }
        public static bool AreYouAwake()
        {
            return awake;
        }
        public static void GenerateWaterSourceTypeBuffer()
        {
            InitializeBuffer();
            Debug.Log("[RF].WaterSourceManager.GenerateWaterSourceTypeBuffer Buffer Initialized.");

            GenerateEmptyNaturalAndPumpingTruckEntries();
            Debug.Log("[RF].WaterSourceManager.GenerateWaterSourceTypeBuffer GenerateEmptyNaturalAndPumpingTruckEntries Completed m_entryCount = " + m_entryCount);

            GenerateDrainageAreaEntries();
            Debug.Log("[RF].WaterSourceManager.GenerateWaterSourceTypeBuffer GenerateDrainageAreaEntries Completed m_entryCount = " + m_entryCount);

            GenerateFacilityEntries();
            Debug.Log("[RF].WaterSourceManager.GenerateWaterSourceTypeBuffer GenerateFacilityEntries Completed m_entryCount = " + m_entryCount);
        }

        private static void InitializeBuffer()
        {
            for (int i=0;i<waterSourceLimit;i++)
            {
                m_buffer[i] = new WaterSourceEntry(WaterSourceEntry.WaterSourceType.Undefined);
            }
        }
        
        public static void Clear()
        {
            InitializeBuffer();
            awake = false;
        }
        private static void GenerateDrainageAreaEntries()
        {
            foreach(KeyValuePair<int, DrainageArea> DrainageArea in DrainageAreaGrid.DrainageAreaDictionary)
            {
                WaterSourceEntry currentWaterSourceEntry = new WaterSourceEntry(DrainageArea.Key);
                ushort currentWaterSourceID = DrainageArea.Value.getWaterSourceID();
                if (currentWaterSourceID != 0)
                {
                    if (m_buffer[currentWaterSourceID].GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined)
                    {
                        m_buffer[currentWaterSourceID] = currentWaterSourceEntry;
                        m_entryCount++;
                    } else
                    {
                        Debug.Log("[RF].WaterSourceManager.GenerateDrainageAreaEntries Error With Drainage Area " + DrainageArea.Key.ToString() + " WaterSourceEntry already defined.");
                    }

                } else
                {
                    Debug.Log("[RF].WaterSourceManager.GenerateDrainageAreaEntries Error With Drainage Area " + DrainageArea.Key.ToString() + " WaterSource not created.");
                }
            }
        }
        private static void GenerateEmptyNaturalAndPumpingTruckEntries()
        {
            for (int i = 0; i < Singleton<TerrainManager>.instance.WaterSimulation.m_waterSources.m_size - 1; i++)
            {
                WaterSource ws = Singleton<TerrainManager>.instance.WaterSimulation.m_waterSources.m_buffer[i];
                if (ws.m_type == 0)
                {
                    if (m_buffer[i+1].GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined) {
                        m_buffer[i+1] = new WaterSourceEntry(WaterSourceEntry.WaterSourceType.Empty);
                        m_entryCount++;
                    }
                    else
                    {
                        Debug.Log("[RF].WaterSourceManager.GenerateEmptyNaturalAndPumpingTruckEntries Error With WaterSourceID " + (i+1).ToString() + " Empty WaterSourceEntry already defined.");
                    }
                } else if (ws.m_type == 1)
                {
                    if (m_buffer[i + 1].GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined)
                    {
                        m_buffer[i + 1] = new WaterSourceEntry(WaterSourceEntry.WaterSourceType.Natural);
                        m_entryCount++;
                    }
                    else
                    {
                        Debug.Log("[RF].WaterSourceManager.GenerateEmptyNaturalAndPumpingTruckEntries Error With WaterSourceID " + (i + 1).ToString() + " Natural WaterSourceEntry already defined.");
                    }
                }
                else if (ws.m_type == 3)
                {
                    if (m_buffer[i + 1].GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined)
                    {
                        m_buffer[i + 1] = new WaterSourceEntry(WaterSourceEntry.WaterSourceType.WaterCleaner);
                        m_entryCount++;
                    }
                    else
                    {
                        Debug.Log("[RF].WaterSourceManager.GenerateEmptyNaturalAndPumpingTruckEntries Error With WaterSourceID " + (i + 1).ToString() + " Cleaner WaterSourceEntry already defined.");
                    }
                }
            }

        }

        private static void GenerateFacilityEntries()
        {
            for (ushort i=0; i<Singleton<BuildingManager>.instance.m_buildings.m_size; i++)
            {
                Building currentBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[i];
                ushort waterSourceID = currentBuilding.m_waterSource;
                if (waterSourceID != 0) { 
                    BuildingAI currentBuildingAI = currentBuilding.Info.m_buildingAI;
                    if (currentBuildingAI is DamPowerHouseAI)
                    {
                        if (m_buffer[waterSourceID].GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined)
                        {
                            m_buffer[waterSourceID] = new WaterSourceEntry(WaterSourceEntry.WaterSourceType.DamPowerHouseFacility, i);
                            m_entryCount++;
                        }
                        else
                        {
                            Debug.Log("[RF].WaterSourceManager.GenerateFacilityEntries Error With WaterSourceID " + waterSourceID.ToString() + " DamPowerHouseAI BuldingID = " + i.ToString() + " WaterSourceEntry already defined.");
                        }
                    } else if (currentBuildingAI is StormDrainAI)
                    {
                        StormDrainAI currentStormDrainAI = currentBuildingAI as StormDrainAI;
                        if (currentStormDrainAI.m_stormWaterIntake > 0)
                        {
                            if (m_buffer[waterSourceID].GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined)
                            {
                                m_buffer[waterSourceID] = new WaterSourceEntry(WaterSourceEntry.WaterSourceType.StormDrainInletFacility, i);
                                m_entryCount++;
                            }
                            else
                            {
                                Debug.Log("[RF].WaterSourceManager.GenerateFacilityEntries Error With WaterSourceID " + waterSourceID.ToString() + " StormDrainAI StormDrainInletFacility BuldingID = " + i.ToString() + " WaterSourceEntry already defined.");
                            }
                        } else if (currentStormDrainAI.m_stormWaterOutlet > 0)
                        {
                            if (m_buffer[waterSourceID].GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined)
                            {
                                m_buffer[waterSourceID] = new WaterSourceEntry(WaterSourceEntry.WaterSourceType.StormDrainOutletFacility, i);
                                m_entryCount++;
                            }
                            else
                            {
                                Debug.Log("[RF].WaterSourceManager.GenerateFacilityEntries Error With WaterSourceID " + waterSourceID.ToString() + " StormDrainAI StormDrainInletFacility BuldingID = " + i.ToString() + " WaterSourceEntry already defined.");
                            }
                        } else
                        {
                            Debug.Log("[RF].WaterSourceManager.GenerateFacilityEntries Error With WaterSourceID " + waterSourceID.ToString() + " StormDrainAI BuldingID = " + i.ToString() + " Neither an inlet or outlet.");
                        }
                    } else if (currentBuildingAI is WaterFacilityAI)
                    {
                        WaterFacilityAI currentWaterFacilityAI = currentBuildingAI as WaterFacilityAI;
                        if (currentWaterFacilityAI.m_waterIntake > 0)
                        {
                            if (m_buffer[waterSourceID].GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined)
                            {
                                m_buffer[waterSourceID] = new WaterSourceEntry(WaterSourceEntry.WaterSourceType.WaterFacility, i);
                                m_entryCount++;
                            }
                            else
                            {
                                Debug.Log("[RF].WaterSourceManager.GenerateFacilityEntries Error With WaterSourceID " + waterSourceID.ToString() + " WaterFacilityAI WaterFacility BuldingID = " + i.ToString() + " WaterSourceEntry already defined.");
                            }
                        }
                        else if (currentWaterFacilityAI.m_waterOutlet > 0)
                        {
                            if (m_buffer[waterSourceID].GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined)
                            {
                                m_buffer[waterSourceID] = new WaterSourceEntry(WaterSourceEntry.WaterSourceType.SewerFacility, i);
                                m_entryCount++;
                            }
                            else
                            {
                                Debug.Log("[RF].WaterSourceManager.GenerateFacilityEntries Error With WaterSourceID " + waterSourceID.ToString() + " WaterFacilityAI SewerFacility BuldingID = " + i.ToString() + " WaterSourceEntry already defined.");
                            }
                        }
                        else
                        {
                            Debug.Log("[RF].WaterSourceManager.GenerateFacilityEntries Error With WaterSourceID " + waterSourceID.ToString() + " WaterFacilityAI BuldingID = " + i.ToString() + " Neither an inlet or outlet.");
                        }
                    }
                }
            }
        }

        public static int GetEntryCount()
        {
            return m_entryCount;
        }

        public static WaterSourceEntry GetWaterSourceEntry(int waterSourceID)
        {
            if (m_buffer[waterSourceID] != null)
            {
                return m_buffer[waterSourceID];
            }
            return new WaterSourceEntry(WaterSourceEntry.WaterSourceType.Empty);
        }

        public static bool SetWaterSourceEntry(int waterSourceID, WaterSourceEntry waterSourceEntry)
        {
            if (waterSourceID > 0 && waterSourceID < waterSourceLimit)
            {
                m_buffer[waterSourceID] = waterSourceEntry;
                return true;
            }
            return false;
        }
    }
}
