using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using ColossalFramework.Globalization;
using HarmonyLib;
using System;
using System.Reflection;
using ICities;

namespace Rainfall
{
    [HarmonyPatch(typeof(CommonBuildingAI), "HandleCommonConsumption")]
    class CommonBuildingAIPatch
    {
        static void Postfix(CommonBuildingAI __instance, ref int __result, ushort buildingID, ref Building data, ref Building.Frame frameData, ref int electricityConsumption, ref int heatingConsumption, ref int waterConsumption, ref int sewageAccumulation, ref int garbageAccumulation, ref int mailAccumulation, int maxMail, DistrictPolicies.Services policies)
        {

            bool logging = false;
            bool timerLogging = false;

            StormDrainAI stormDrainAI = data.Info.m_buildingAI as StormDrainAI;
            if (stormDrainAI != null)
                if (stormDrainAI.m_filter != false &
                    (data.m_problems & Notification.Problem.LineNotConnected) == Notification.Problem.None
                    && (data.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None
                    && (data.m_problems & Notification.Problem.Electricity) == Notification.Problem.None) {

                    int pollutantAccumulation = Hydraulics.removePollutants(buildingID, Hydraulics.getPollutants(buildingID));
                    data.m_garbageBuffer += (ushort)(pollutantAccumulation);
                    if (logging)
                        Debug.Log("[RF]CommonBuildingAI.handleCommonConsumption garbagebuffer = " + data.m_garbageBuffer.ToString());
            }
            

            if ((data.m_problems & Notification.Problem.Flood) == Notification.Problem.Flood && (data.m_problems & Notification.Problem.MajorProblem) != Notification.Problem.None)
            {
                __result = FloodingTimers.instance.getLastHandleCommonConsumptionEfficiency(buildingID);
                if (logging)
                    Debug.Log("[RF]CommonBuildingAI.handleCommonConsumption initially flooded");
            } else if ((data.m_problems & Notification.Problem.Flood) == Notification.Problem.Flood && (data.m_problems & Notification.Problem.MajorProblem) == Notification.Problem.None)
            {
                __result *= 2;
                FloodingTimers.instance.setLastHandleCommonConsumptionEfficiency(buildingID, __result);
                if (logging)
                    Debug.Log("[RF]CommonBuildingAI.handleCommonConsumption initially flooding");
            } else
            {
                FloodingTimers.instance.setLastHandleCommonConsumptionEfficiency(buildingID, __result);
                return;
            }

            if (logging)
                Debug.Log("[RF]CommonBuildingAI.handleCommonConsumption initial __result = " + __result.ToString());
            data.m_problems = Notification.RemoveProblems(data.m_problems, Notification.Problem.Flood);
            float num21 = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(data.m_position));
            float buildingFloodedTolerance = OptionHandler.getSliderSetting("BuildingFloodedTolerance");
            float buildingFloodingTolerance = OptionHandler.getSliderSetting("BuildingFloodingTolerance");
            int gridx;
            int gridz;
            Singleton<GameAreaManager>.instance.GetTileXZ(data.m_position, out gridx, out gridz); //Not compatible with 81 tiles.
            bool tileUnlocked = Singleton<GameAreaManager>.instance.IsUnlocked(gridx, gridz);
            bool preventFlood = false;
            bool OnlyFloodOwnedTiles = OptionHandler.getCheckboxSetting("OnlyFloodOwnedtiles");
            if (OnlyFloodOwnedTiles && tileUnlocked) preventFlood = true;


            if (num21 > data.m_position.y + buildingFloodedTolerance && FloodingTimers.instance.getBuildingFloodedElapsedTime(buildingID) == -1f)
            {
                FloodingTimers.instance.setBuildingFloodedStartTime(buildingID);
                if (timerLogging)
                    Debug.Log("[RF]CBAIP Flooded Timer Set.");
            }
            else if (num21 > data.m_position.y + buildingFloodingTolerance && FloodingTimers.instance.getBuildingFloodingElapsedTime(buildingID) == -1f)
            {
                FloodingTimers.instance.setBuildingFloodingStartTime(buildingID);
                if (timerLogging)
                    Debug.Log("[RF]CBAIP Flooding Timer Set.");
            }
            if (num21 > data.m_position.y + buildingFloodedTolerance && FloodingTimers.instance.getBuildingFloodedElapsedTime(buildingID) >= OptionHandler.getSliderSetting("BuildingFloodedTimer") && OptionHandler.getCheckboxSetting("BuildingSufferFlooded") && !preventFlood)
            {
                __result = Mathf.RoundToInt((float)__result*OptionHandler.getSliderSetting("BuildingFloodedEfficiency"));
                data.m_problems = Notification.AddProblems(data.m_problems, Notification.Problem.Flood | Notification.Problem.MajorProblem);
            }
            //add flooding tolerance
            else if (num21 > data.m_position.y + buildingFloodingTolerance && FloodingTimers.instance.getBuildingFloodingElapsedTime(buildingID) >= OptionHandler.getSliderSetting("BuildingFloodingTimer") && OptionHandler.getCheckboxSetting("BuildingSufferFlooding") && !preventFlood)
            {
                __result = Mathf.RoundToInt((float)__result * OptionHandler.getSliderSetting("BuildingFloodingEfficiency")); 
                data.m_problems = Notification.AddProblems(data.m_problems, Notification.Problem.Flood);
                if (FloodingTimers.instance.getBuildingFloodedElapsedTime(buildingID) != -1f && num21 < data.m_position.y + buildingFloodedTolerance)
                {
                    FloodingTimers.instance.resetBuildingFloodedStartTime(buildingID);
                    if (timerLogging)
                        Debug.Log("[RF]CBAIP Flooded Timer RESet.");
                }
            }
            if (FloodingTimers.instance.getBuildingFloodingElapsedTime(buildingID) != -1f && num21 < data.m_position.y + buildingFloodingTolerance)
            {
                FloodingTimers.instance.resetBuildingFloodingStartTime(buildingID);
                if (timerLogging)
                    Debug.Log("[RF]CBAIP Flooding Timer RESet.");
            }
            if (FloodingTimers.instance.getBuildingFloodedElapsedTime(buildingID) != -1f && num21 < data.m_position.y + buildingFloodedTolerance)
            {
                FloodingTimers.instance.resetBuildingFloodedStartTime(buildingID);
                if (timerLogging)
                    Debug.Log("[RF]CBAIP Flooded Timer RESet.");
            }
        }
    }
    
}
