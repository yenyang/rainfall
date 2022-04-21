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
    [HarmonyPatch(typeof(NetManager), "FinalizeSegment")]
    class NetManagerFinalizeSegmentPatch
    {
        static void Postfix(ushort segment, ref NetSegment data, ref NetManager __instance)
        {
            if (!DrainageArea.reviewSegment(segment) || !DrainageAreaGrid.areYouAwake())
            {
                return;
            }
            Vector3 centerPos = (__instance.m_nodes.m_buffer[data.m_startNode].m_position + __instance.m_nodes.m_buffer[data.m_endNode].m_position) * 0.5f;
            int gridX = Mathf.Clamp((int)(centerPos.x / 64f + 135f), 0, 269);
            int gridZ = Mathf.Clamp((int)(centerPos.z / 64f + 135f), 0, 269);
            int gridLocation = gridZ * 270 + gridX;
            DrainageAreaGrid.recalculateCompositeRunoffCoefficentForBasinAtGridLocation(gridLocation);
            bool logging = false;
            if (logging) {
                Debug.Log("[RF]NetManagerPatch.FinalizeSegment recalculated compostie runoff coefficent for basin at grid location " + gridLocation.ToString());
            }
            DrainageAreaGrid.EnableRoadwayUncoveredDrainageAreas(segment);
        }
    }
    
    [HarmonyPatch(typeof(NetManager), "InitializeSegment")]
    class NetManagerInitializeSegmentPatch
    {
        static void Postfix(ushort segment, ref NetSegment data, ref NetManager __instance)
        {
            if (!DrainageArea.reviewSegment(segment) || !DrainageAreaGrid.areYouAwake())
            {
                return;
            }   
            Vector3 centerPos = (__instance.m_nodes.m_buffer[data.m_startNode].m_position + __instance.m_nodes.m_buffer[data.m_endNode].m_position) * 0.5f;
            int gridX = Mathf.Clamp((int)(centerPos.x / 64f + 135f), 0, 269);
            int gridZ = Mathf.Clamp((int)(centerPos.z / 64f + 135f), 0, 269);
            int gridLocation = gridZ * 270 + gridX;
            DrainageAreaGrid.recalculateCompositeRunoffCoefficentForBasinAtGridLocation(gridLocation);
            bool logging = false;
            if (logging)
            {
                Debug.Log("[RF]NetManagerPatch.InitializeSegment recalculated compostie runoff coefficent for basin at grid location " + gridLocation.ToString());
            }
            DrainageAreaGrid.DisableRoadwayCoveredDrainageAreas(segment);
        }
    }
    
}
