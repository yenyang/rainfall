using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using ColossalFramework.Globalization;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace Rainfall
{
    [HarmonyPatch(typeof(PedestrianWayAI), nameof(PedestrianWayAI.SimulationStep), new Type[] { typeof(ushort), typeof(NetSegment)}, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref } )]
    class PedestrianWayAIPatch
    {
        
        static void Postfix(ushort segmentID, ref NetSegment data, ref PedestrianWayAI __instance)
        {
           
            bool logging = false;
            if (logging)
                Debug.Log("[RF]PedestrianWayAIPatch Patched!");

            if (!__instance.m_invisible)
            {
                NetManager instance = Singleton<NetManager>.instance;
                Notification.Problem problem = Notification.RemoveProblems(data.m_problems, Notification.Problem.Flood);
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
                float num = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(vector));

                float PedestrianPathFloodedTolerance = OptionHandler.getSliderSetting("PathwayFloodedTolerance");
                float PedestrianPathFloodingTolerance = OptionHandler.getSliderSetting("PathwayFloodingTolerance");
                bool PathwaySufferFlooding = OptionHandler.getCheckboxSetting("PathwaySufferFlooding");
                bool PathwaySufferFlooded = OptionHandler.getCheckboxSetting("PathwaySufferFlooded");
                float PedestrianPathFloodingTimer = OptionHandler.getSliderSetting("PathwayFloodingTimer");
                float PedestrianPathFloodedTimer = OptionHandler.getSliderSetting("PathwayFloodedTimer");
                int gridx;
                int gridz;
                Singleton<GameAreaManager>.instance.GetTileXZ(data.m_middlePosition, out gridx, out gridz); 
                bool tileUnlocked = Singleton<GameAreaManager>.instance.IsUnlocked(gridx, gridz);
                bool preventFlood = false;
                bool OnlyFloodOwnedTiles = OptionHandler.getCheckboxSetting("OnlyFloodOwnedtiles");
                if (OnlyFloodOwnedTiles && !tileUnlocked) preventFlood = true;


                if (num > vector.y + PedestrianPathFloodedTolerance && num > 0f && FloodingTimers.instance.getSegmentFloodedElapsedTime(segmentID) == -1f)
                {
                    FloodingTimers.instance.setSegmentFloodedStartTime(segmentID);

                }
                else if (num > vector.y + PedestrianPathFloodedTolerance && num > 0f && FloodingTimers.instance.getSegmentFloodingElapsedTime(segmentID) == -1f)
                {
                    FloodingTimers.instance.setSegmentFloodingStartTime(segmentID);

                }
                if (num > vector.y + PedestrianPathFloodedTolerance && FloodingTimers.instance.getSegmentFloodedElapsedTime(segmentID) >= PedestrianPathFloodedTimer && PathwaySufferFlooded && !preventFlood)
                {
                    if ((data.m_flags & NetSegment.Flags.Flooded) == NetSegment.Flags.None)
                    {
                        data.m_flags |= NetSegment.Flags.Flooded;
                        if (logging)
                            Debug.Log("[RF]PedestrianWayAIPatch Really Flooded");
                        data.m_modifiedIndex = Singleton<SimulationManager>.instance.m_currentBuildIndex++;
                    }
                    problem = Notification.AddProblems(problem, Notification.Problem.Flood | Notification.Problem.MajorProblem);
                }
                else
                {
                    data.m_flags &= ~NetSegment.Flags.Flooded;
                    if (num > vector.y + PedestrianPathFloodingTolerance && FloodingTimers.instance.getSegmentFloodingElapsedTime(segmentID) >= PedestrianPathFloodingTimer && PathwaySufferFlooding && !preventFlood)
                    {
                        problem = Notification.AddProblems(problem, Notification.Problem.Flood);
                        if (logging)
                            Debug.Log("[RF]PedestrianWayAIPatch Flooding");
                    }
                    else if (num < vector.y + PedestrianPathFloodingTolerance  && FloodingTimers.instance.getSegmentFloodingElapsedTime(segmentID) != -1f)
                    {
                        FloodingTimers.instance.resetSegmentFloodingStartTime(segmentID);
                    }
                    else if (num < vector.y + PedestrianPathFloodedTolerance && FloodingTimers.instance.getSegmentFloodedElapsedTime(segmentID) != -1f)
                    {
                        FloodingTimers.instance.resetSegmentFloodedStartTime(segmentID);
                    }
                }
                data.m_problems = problem;
            }
            
            //}
       }
    }
}
