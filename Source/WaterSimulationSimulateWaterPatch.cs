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
            System.Random random = new System.Random();
            foreach (KeyValuePair<int, DrainageArea> currentDrainageArea in DrainageAreaGrid.DrainageAreaDictionary)
            {
                if (currentDrainageArea.Value.m_disabled == true) continue;
                if (Singleton<WeatherManager>.instance.m_currentRain == 0)
                {
                    if (currentDrainageArea.Value.m_outputRate > 0)
                    {
                        currentDrainageArea.Value.m_outputRate = 0;
                    }
                    continue;
                }
                if (random.Next(0, 2) == 0) continue;
                if (Singleton<TerrainManager>.instance.WaterSimulation.m_waterSources.m_size > 65000) continue;
                float DrainageAreaArea = DrainageAreaGrid.drainageAreaGridQuotient * DrainageAreaGrid.drainageAreaGridQuotient;
                float currentCompositeRunoffCoefficent = DrainageAreaGrid.getCompositeRunoffCoefficientForDrainageArea(currentDrainageArea.Key);
                float currentOutputRate = Mathf.CeilToInt(Singleton<WeatherManager>.instance.m_currentRain * currentCompositeRunoffCoefficent * OptionHandler.getSliderSetting("GlobalRunoffScalar"));
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
                float currentPollutantRate = 0f; //Need to calculate pollution runoff here for entrie drainage basin.
                WaterSource currentWaterSource = default(WaterSource);
                currentWaterSource.m_type = WaterSource.TYPE_FACILITY;
                currentWaterSource.m_inputPosition = currentDrainageArea.Value.m_outputPosition;
                currentWaterSource.m_outputPosition = currentDrainageArea.Value.m_outputPosition;
                currentWaterSource.m_target = Math.Min((ushort)65535,(ushort)(Mathf.CeilToInt(currentDrainageArea.Value.m_outputPosition.y*64f) + 100));
                currentWaterSource.m_inputRate = 0u;
                currentWaterSource.m_outputRate = Math.Min((ushort)65535, (ushort)Mathf.CeilToInt(currentOutputRate));
                currentWaterSource.m_pollution = Math.Min((ushort)65535, (ushort)Mathf.CeilToInt(currentPollutantRate));
                currentWaterSource.m_water = 65535u;
                currentWaterSource.m_flow = Math.Min((ushort)65535, (ushort)Mathf.CeilToInt(currentOutputRate));
                ushort currentWaterSourceID;
                Singleton<TerrainManager>.instance.WaterSimulation.CreateWaterSource(out currentWaterSourceID, currentWaterSource);
                Hydrology.instance._waterSourceIDs.Add(currentWaterSourceID);
            }

            return true;
        }
        static void Postfix(int pollutionDisposeRate)
        {
			foreach(ushort currentWaterSourceID in Hydrology.instance._waterSourceIDs)
            {
                Singleton<TerrainManager>.instance.WaterSimulation.ReleaseWaterSource(currentWaterSourceID);
            }
            Hydrology.instance._waterSourceIDs.Clear();
        }
		
	}

}
