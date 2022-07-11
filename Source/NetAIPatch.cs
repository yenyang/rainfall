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
    [HarmonyPatch(typeof(NetAI), "ReleaseSegment")]
    class NetAIReleaseSegmentPatch
    {
        
        static void Postfix(ushort segmentID, ref NetSegment data, bool __state)
        {
            Vector3 centerPos = (Singleton<NetManager>.instance.m_nodes.m_buffer[data.m_startNode].m_position + Singleton<NetManager>.instance.m_nodes.m_buffer[data.m_endNode].m_position) * 0.5f;
            int gridX = Mathf.Clamp((int)(centerPos.x / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient-1);
            int gridZ = Mathf.Clamp((int)(centerPos.z / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            int gridLocation = gridZ * DrainageAreaGrid.drainageAreaGridCoefficient + gridX;
            DrainageAreaGrid.RemoveSegmentFromDrainageArea(segmentID, gridLocation);  
            bool flag = DrainageAreaGrid.recalculateCompositeRunoffCoefficentForBasinAtGridLocation(gridLocation);  
            //Debug.Log("[RF]NetAIReleaseSegmentPatch.ReleaseSegment flag = " + flag.ToString());
            bool logging = false;
            if (logging) {
                Debug.Log("[RF]NetAIReleaseSegmentPatch.ReleaseSegment recalculated compostie runoff coefficent for basin at grid location " + gridLocation.ToString());
            }
            DrainageAreaGrid.EnableRoadwayUncoveredDrainageAreas(segmentID);
            return;
        }
    }
    
    
    
}
