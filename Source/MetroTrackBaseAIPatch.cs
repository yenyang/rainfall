using System;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using ColossalFramework.Globalization;
using HarmonyLib;
using System;

namespace Rainfall
{
    [HarmonyPatch(typeof(MetroTrackBaseAI), nameof(MetroTrackBaseAI.SimulationStep), new Type[] { typeof(ushort), typeof(NetSegment)}, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref } )]
    class c
    {
        [HarmonyPostfix]
        static void Postfix(ushort segmentID, ref NetSegment data, ref MetroTrackBaseAI __instance)
        {
            bool logging = false;
            bool timerLogging = false; 
            NetManager instance = Singleton<NetManager>.instance;
            Notification.Problem problem = Notification.RemoveProblems(data.m_problems, Notification.Problem.Flood);
            if (logging)
                Debug.Log("[RF]MetroTrackBaseAIPatch Patched!");
            
            NetManager _netManager = Singleton<NetManager>.instance;
            Vector3 startPosition = _netManager.m_nodes.m_buffer[data.m_startNode].m_position;
            Vector3 endPosition = _netManager.m_nodes.m_buffer[data.m_endNode].m_position;
            Vector3 midPosition = data.m_middlePosition;
            Vector3 quarterPosition = data.GetClosestPosition((startPosition + midPosition) / 2f);
            Vector3 threeQuarterPosition = data.GetClosestPosition((midPosition + endPosition) / 2f);
            Vector3 lowestPointOfConcentration = startPosition;
            List<Vector3> potentialPointsOfConcentration = new List<Vector3> { startPosition, endPosition, midPosition, quarterPosition, threeQuarterPosition };
            foreach (Vector3 potentialPOC in potentialPointsOfConcentration)
            {
                if (potentialPOC.y < lowestPointOfConcentration.y)
                {
                    lowestPointOfConcentration = potentialPOC;
                }
            }
            Vector3 vector = lowestPointOfConcentration;
            bool flag = false;
            float MetroTrackFloodedTolerance = OptionHandler.getSliderSetting("MetroFloodedTolerance");
            float MetroTrackFloodingTolerance = OptionHandler.getSliderSetting("MetroFloodingTolerance");
            bool MetroTrackSufferFlooding = OptionHandler.getCheckboxSetting("MetroSufferFlooding");
            bool MetroTrackSufferFlooded = OptionHandler.getCheckboxSetting("MetroSufferFlooded");
            float MetroTrackFloodingTimer = OptionHandler.getSliderSetting("MetroFloodingTimer");
            float MetroTrackFloodedTimer = OptionHandler.getSliderSetting("MetroFloodedTimer");

            int gridx;
            int gridz;
            Singleton<GameAreaManager>.instance.GetTileXZ(data.m_middlePosition, out gridx, out gridz); 
            bool tileUnlocked = Singleton<GameAreaManager>.instance.IsUnlocked(gridx, gridz);
            bool preventFlood = false;
            bool OnlyFloodOwnedTiles = OptionHandler.getCheckboxSetting("OnlyFloodOwnedtiles");
            if (OnlyFloodOwnedTiles && !tileUnlocked) preventFlood = true;

            if ((__instance.m_info.m_setVehicleFlags & Vehicle.Flags.Underground) == 0)
            {
                float num7 = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(vector));
                if (num7 > vector.y + MetroTrackFloodedTolerance && num7 > 0f && FloodingTimers.instance.getSegmentFloodedElapsedTime(segmentID) == -1f)
                {
                    FloodingTimers.instance.setSegmentFloodedStartTime(segmentID);
                    if (timerLogging)
                        Debug.Log("[RF]MTBAIP Flooded Timer Set.");
                } else if (num7 > vector.y + MetroTrackFloodingTolerance && num7 > 0f && FloodingTimers.instance.getSegmentFloodingElapsedTime(segmentID) == -1f)
                {
                    FloodingTimers.instance.setSegmentFloodingStartTime(segmentID);
                    if (timerLogging)
                        Debug.Log("[RF]MTBAIP Flooding Timer Set.");
                }
                if (num7 > vector.y + MetroTrackFloodedTolerance && num7 > 0f && FloodingTimers.instance.getSegmentFloodedElapsedTime(segmentID) >= MetroTrackFloodedTimer && MetroTrackSufferFlooded && !preventFlood)
                {
                    flag = true;
                    if (timerLogging)
                        Debug.Log("[RF]MetroTrackBaseAI Flooded");
                    data.m_flags |= NetSegment.Flags.Flooded;
                    problem = Notification.AddProblems(problem, Notification.Problem.Flood | Notification.Problem.MajorProblem);
                }
                else 
                {
                    data.m_flags &= ~NetSegment.Flags.Flooded;
                    if (logging)
                     Debug.Log("[RF]MetroTrackBaseAI vector.y = " + vector.y.ToString() + "ModSetting.MetroTrackFloodingTolerance = " + MetroTrackFloodingTolerance.ToString() + " num7 = " + num7.ToString());
                    if (num7 > vector.y + MetroTrackFloodingTolerance && num7 > 0f && FloodingTimers.instance.getSegmentFloodingElapsedTime(segmentID) >= MetroTrackFloodingTimer && MetroTrackSufferFlooding && !preventFlood)
                    {
                        if (timerLogging)
                        Debug.Log("[RF]MetroTrackBaseAI Flood");
                        flag = true;
                        problem = Notification.AddProblems(problem, Notification.Problem.Flood);
                    } else if (num7 < vector.y + MetroTrackFloodingTolerance  && FloodingTimers.instance.getSegmentFloodingElapsedTime(segmentID) != -1f)
                    {
                        FloodingTimers.instance.resetSegmentFloodingStartTime(segmentID);
                        if (timerLogging)
                            Debug.Log("[RF]MetroTrackBaseAI reset flooding timer");
                    } else if (num7 < vector.y + MetroTrackFloodedTolerance  && FloodingTimers.instance.getSegmentFloodedElapsedTime(segmentID) != -1f)
                    {
                        FloodingTimers.instance.resetSegmentFloodedStartTime(segmentID);
                        if (timerLogging)
                            Debug.Log("[RF]MetroTrackBaseAI reset flooded timer");
                    }
                } 
                int num8 = (int)data.m_wetness;
                if (!instance.m_treatWetAsSnow)
                {
                    if (flag)
                    {
                        num8 = 255;
                    }
                    else
                    {
                        int num9 = -(num8 + 63 >> 5);
                        float num10 = Singleton<WeatherManager>.instance.SampleRainIntensity(vector, false);
                        if (num10 != 0f)
                        {
                            int num11 = Mathf.RoundToInt(Mathf.Min(num10 * 4000f, 1000f));
                            num9 += Singleton<SimulationManager>.instance.m_randomizer.Int32(num11, num11 + 99) / 100;
                        }
                        num8 = Mathf.Clamp(num8 + num9, 0, 255);
                    }
                }
                if (num8 != (int)data.m_wetness)
                {
                    if (Mathf.Abs((int)data.m_wetness - num8) > 10)
                    {
                        data.m_wetness = (byte)num8;
                        InstanceID empty = InstanceID.Empty;
                        empty.NetSegment = segmentID;
                        instance.AddSmoothColor(empty);
                        empty.NetNode = data.m_startNode;
                        instance.AddSmoothColor(empty);
                        empty.NetNode = data.m_endNode;
                        instance.AddSmoothColor(empty);
                    }
                    else
                    {
                        data.m_wetness = (byte)num8;
                        instance.m_wetnessChanged = 256;
                    }
                }
            }
            data.m_problems = problem;
        }
    }
}
