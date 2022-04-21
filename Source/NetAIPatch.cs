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
            int gridX = Mathf.Clamp((int)(centerPos.x / 64f + 135f), 0, 269);
            int gridZ = Mathf.Clamp((int)(centerPos.z / 64f + 135f), 0, 269);
            int gridLocation = gridZ * 270 + gridX;
            
            bool flag = DrainageBasinGrid.recalculateCompositeRunoffCoefficentForBasinAtGridLocation(gridLocation);
            //Debug.Log("[RF]NetAIReleaseSegmentPatch.ReleaseSegment flag = " + flag.ToString());
            bool logging = false;
            if (logging) {
                Debug.Log("[RF]NetAIReleaseSegmentPatch.ReleaseSegment recalculated compostie runoff coefficent for basin at grid location " + gridLocation.ToString());
            }
            return;
        }
    }
    
    
    
}
