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
            if (!DrainageArea.ReviewSegment(segment) || !DrainageAreaGrid.areYouAwake())
            {
                return;
            }
            Vector3 centerPos = (__instance.m_nodes.m_buffer[data.m_startNode].m_position + __instance.m_nodes.m_buffer[data.m_endNode].m_position) * 0.5f;
            int gridX = Mathf.Clamp((int)(centerPos.x / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            int gridZ = Mathf.Clamp((int)(centerPos.z / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            int gridLocation = gridZ * DrainageAreaGrid.drainageAreaGridCoefficient + gridX;
            DrainageAreaGrid.RemoveSegmentFromDrainageArea(segment, gridLocation);
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
            if (!DrainageArea.ReviewSegment(segment) || !DrainageAreaGrid.areYouAwake())
            {
                return;
            }   
            Vector3 centerPos = (__instance.m_nodes.m_buffer[data.m_startNode].m_position + __instance.m_nodes.m_buffer[data.m_endNode].m_position) * 0.5f;
            int gridX = Mathf.Clamp((int)(centerPos.x / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            int gridZ = Mathf.Clamp((int)(centerPos.z / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            int gridLocation = gridZ * DrainageAreaGrid.drainageAreaGridCoefficient + gridX;
            DrainageAreaGrid.AddSegmentToDrainageArea(segment, gridLocation);
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
