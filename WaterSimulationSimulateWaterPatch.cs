using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using ColossalFramework.Globalization;
using HarmonyLib;
using System;
using System.Reflection;
using ICities;
using System.Threading;
using System.Collections.Generic;

namespace Rainfall
{
    [HarmonyPatch(typeof(WaterSimulation), "SimulateWater")]
    class WaterSimulationSimulateWaterPatch
    {
        static bool Prefix(int pollutionDisposeRate)
        {
            
            foreach (KeyValuePair<int, DrainageArea> currentDrainageArea in DrainageAreaGrid.DrainageAreaDictionary)
            {
                float currentOutputRate = 0f;
                ushort currentWaterSourceID = 0;
                float currentPollutantRate = 0f; //Need to calculate pollution runoff here for entrie drainage basin.
                 
                if (currentDrainageArea.Value.m_enabled == false) continue;
                if (Singleton<WeatherManager>.instance.m_currentRain == 0)
                {
                    if (currentDrainageArea.Value.m_outputRate > 0)
                    {
                        currentDrainageArea.Value.m_outputRate = 0;
                        currentOutputRate = 0f;
                        currentWaterSourceID = currentDrainageArea.Value.getWaterSourceID();
                        if (currentWaterSourceID != 0)
                        {
                            WaterSource currentWaterSource = Singleton<TerrainManager>.instance.WaterSimulation.LockWaterSource(currentWaterSourceID);
                            currentWaterSource.m_type = WaterSource.TYPE_FACILITY;
                            currentWaterSource.m_inputPosition = currentDrainageArea.Value.m_outputPosition;
                            currentWaterSource.m_outputPosition = currentDrainageArea.Value.m_outputPosition;
                            currentWaterSource.m_target = Math.Min((ushort)65535, (ushort)(Mathf.CeilToInt(currentDrainageArea.Value.m_outputPosition.y * 64f) + 100));
                            currentWaterSource.m_inputRate = 0u;
                            currentWaterSource.m_outputRate = Math.Min((ushort)65535, (ushort)Mathf.CeilToInt(currentOutputRate));
                            currentWaterSource.m_pollution = Math.Min((ushort)65535, (ushort)Mathf.CeilToInt(currentPollutantRate));
                            currentWaterSource.m_water = 65535u;
                            currentWaterSource.m_flow = Math.Min((ushort)65535, (ushort)Mathf.CeilToInt(currentOutputRate));
                            Singleton<TerrainManager>.instance.WaterSimulation.UnlockWaterSource(currentWaterSourceID, currentWaterSource);
                        }
                    } else
                    {
                        currentWaterSourceID = currentDrainageArea.Value.getWaterSourceID();
                        
                        if (currentWaterSourceID != 0)
                        {
                            if (Singleton<TerrainManager>.instance.WaterSimulation.m_waterSources[currentWaterSourceID-1].m_outputRate > 0)
                            {
                                currentOutputRate = 0f;
                                WaterSource currentWaterSource = Singleton<TerrainManager>.instance.WaterSimulation.LockWaterSource(currentWaterSourceID);
                                currentWaterSource.m_type = WaterSource.TYPE_FACILITY;
                                currentWaterSource.m_inputPosition = currentDrainageArea.Value.m_outputPosition;
                                currentWaterSource.m_outputPosition = currentDrainageArea.Value.m_outputPosition;
                                currentWaterSource.m_target = Math.Min((ushort)65535, (ushort)(Mathf.CeilToInt(currentDrainageArea.Value.m_outputPosition.y * 64f) + 100));
                                currentWaterSource.m_inputRate = 0u;
                                currentWaterSource.m_outputRate = Math.Min((ushort)65535, (ushort)Mathf.CeilToInt(currentOutputRate));
                                currentWaterSource.m_pollution = Math.Min((ushort)65535, (ushort)Mathf.CeilToInt(currentPollutantRate));
                                currentWaterSource.m_water = 65535u;
                                currentWaterSource.m_flow = Math.Min((ushort)65535, (ushort)Mathf.CeilToInt(currentOutputRate));
                                Singleton<TerrainManager>.instance.WaterSimulation.UnlockWaterSource(currentWaterSourceID, currentWaterSource);
                            }
                        }
                    }
                    continue;
                }
                if (currentDrainageArea.Value.DoesOutputPositionEqualPosition())
                {
                    if (Mathf.Abs(Singleton<TerrainManager>.instance.SampleRawHeightSmooth(currentDrainageArea.Value.m_outputPosition) - currentDrainageArea.Value.m_outputPosition.y) > 0f)
                    {
                        //Debug.Log("[RF]WaterSimulationSimulateWaterPatch Thinks terrain has been altered near drainage area " + currentDrainageArea.Key.ToString());
                        currentDrainageArea.Value.recalculateCompositeRunoffCoefficent(false);
                    }
                }
                if (Singleton<TerrainManager>.instance.WaterSimulation.m_waterSources.m_size > 65000) continue;
                
                float currentCompositeRunoffCoefficent = DrainageAreaGrid.getCompositeRunoffCoefficientForDrainageArea(currentDrainageArea.Key);
                currentOutputRate = Mathf.CeilToInt(Singleton<WeatherManager>.instance.m_currentRain * currentCompositeRunoffCoefficent * OptionHandler.getSliderSetting("GlobalRunoffScalar"));
                //if (logging) Debug.Log("[RF]WaterSimulationSimulateWaterPatchPostfix currentOutputRate = " + currentOutputRate.ToString());

                float MinimumDrainageAreaRunoff = OptionHandler.getSliderSetting("MinimumDrainageAreaRunoff");
                float MaximumDrainageAreaRunoff = OptionHandler.getSliderSetting("MaximumDrainageAreaRunoff");
                if (currentOutputRate < MinimumDrainageAreaRunoff)
                {
                    currentOutputRate = (int)MinimumDrainageAreaRunoff;
                }
                if (currentOutputRate > MaximumDrainageAreaRunoff)
                {
                    currentOutputRate = (int)MaximumDrainageAreaRunoff;
                }
                currentWaterSourceID = currentDrainageArea.Value.getWaterSourceID();
                if (currentWaterSourceID != 0) {
                    WaterSource currentWaterSource = Singleton<TerrainManager>.instance.WaterSimulation.LockWaterSource(currentWaterSourceID);
                    currentWaterSource.m_type = WaterSource.TYPE_FACILITY;
                    currentWaterSource.m_inputPosition = currentDrainageArea.Value.m_outputPosition;
                    currentWaterSource.m_outputPosition = currentDrainageArea.Value.m_outputPosition;
                    currentWaterSource.m_target = Math.Min((ushort)65535, (ushort)(Mathf.CeilToInt(currentDrainageArea.Value.m_outputPosition.y * 64f) + 100));
                    currentWaterSource.m_inputRate = 0u;
                    currentWaterSource.m_outputRate = Math.Min((ushort)65535, (ushort)Mathf.CeilToInt(currentOutputRate));
                    currentWaterSource.m_pollution = Math.Min((ushort)65535, (ushort)Mathf.CeilToInt(currentPollutantRate));
                    currentWaterSource.m_water = 65535u;
                    currentWaterSource.m_flow = Math.Min((ushort)65535, (ushort)Mathf.CeilToInt(currentOutputRate));
                    Singleton<TerrainManager>.instance.WaterSimulation.UnlockWaterSource(currentWaterSourceID, currentWaterSource);
                }
            }

            return true;
        }
        static void Postfix(int pollutionDisposeRate)
        {
            if (Hydraulics.instance._SDoutletsToReleaseWaterSources != null)
            {
                HashSet<ushort> SDOutletsToReleaseWaterSourcesIterator = Hydraulics.instance._SDoutletsToReleaseWaterSources;
                foreach (ushort currentBuildingID in SDOutletsToReleaseWaterSourcesIterator)
                {
                    Building currentBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[currentBuildingID];
                    ushort waterSourceID = currentBuilding.m_waterSource;
                    if (waterSourceID != 0)
                    {
                        Singleton<TerrainManager>.instance.WaterSimulation.ReleaseWaterSource(waterSourceID);
                        Singleton<BuildingManager>.instance.m_buildings.m_buffer[currentBuildingID].m_waterSource = 0;
                    }
                    if (Hydraulics.instance._SDoutletsToReleaseWaterSources.Contains(currentBuildingID))
                    {
                        Hydraulics.instance._SDoutletsToReleaseWaterSources.Remove(waterSourceID);
                    }
                }
            }
            /*
            if (Hydraulics.instance._existingSewageOutlets != null && Hydraulics.instance._existingSewageOutlets.Count > 0)
            {
                HashSet<ushort> _existingSewageOutletsIterator = new HashSet<ushort>(Hydraulics.instance._existingSewageOutlets);

                foreach (ushort id in _existingSewageOutletsIterator)
                {
                    Building currentSewageOutlet = Singleton<BuildingManager>.instance.m_buildings.m_buffer[id];
                    WaterFacilityAI currentSewageOutletAI = currentSewageOutlet.Info.m_buildingAI as WaterFacilityAI;
                    if (currentSewageOutlet.m_waterSource != 0 && currentSewageOutlet.m_waterSource < Singleton<TerrainManager>.instance.WaterSimulation.m_waterSources.m_size)
                    {
                        bool flag = false;
                        WaterSource sourceData = Singleton<TerrainManager>.instance.WaterSimulation.LockWaterSource(currentSewageOutlet.m_waterSource);
                        Vector2 waterSourceOutputPositionXZ = new Vector2(sourceData.m_outputPosition.x, sourceData.m_outputPosition.z);
                        Vector3 waterLocationOffsetPosition = currentSewageOutlet.CalculatePosition(currentSewageOutletAI.m_waterLocationOffset);
                        Vector2 stormDrainPositionXZ = new Vector2(waterLocationOffsetPosition.x, waterLocationOffsetPosition.z);
                        if (Vector2.Distance(stormDrainPositionXZ, waterSourceOutputPositionXZ) > 50f)
                        {
                            Debug.Log("[RF]Hydraulics.update existing sewage outlet removed from position X " + sourceData.m_outputPosition.x.ToString() + " Z " + sourceData.m_outputPosition.z.ToString());
                            //sourceData.m_outputPosition = waterLocationOffsetPosition;
                            flag = true;

                        }

                        Singleton<TerrainManager>.instance.WaterSimulation.UnlockWaterSource(currentSewageOutlet.m_waterSource, sourceData);
                        if (flag)
                        {
                            Singleton<TerrainManager>.instance.WaterSimulation.ReleaseWaterSource(currentSewageOutlet.m_waterSource);
                            currentSewageOutlet.m_waterSource = 0;
                        }
                    }
                    else if (currentSewageOutlet.m_waterSource < Singleton<TerrainManager>.instance.WaterSimulation.m_waterSources.m_size)
                    {
                        currentSewageOutlet.m_waterSource = 0;
                    }
                    //Debug.Log("[RF]Hydraulics.update looking into sewage outlets");
                }
            }*/
        }

    }

}