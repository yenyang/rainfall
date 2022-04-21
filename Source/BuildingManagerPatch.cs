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
    [HarmonyPatch(typeof(BuildingManager), "AddToGrid")]
    class BuildingManagerAddToGridPatch
    {
        static void Postfix(ushort building, ref Building data)
        {
            if (!DrainageArea.reviewBuilding(building))
            {
                return;
            } 
            int gridX = Mathf.Clamp((int)(data.m_position.x / 64f + 135f), 0, 269);
            int gridZ = Mathf.Clamp((int)(data.m_position.z / 64f + 135f), 0, 269);
            int gridLocation = gridZ * 270 + gridX;

            DrainageAreaGrid.recalculateCompositeRunoffCoefficentForBasinAtGridLocation(gridLocation);
            bool logging = false;
            if (logging)
            {
                Debug.Log("[RF]BuildignManagerPatch.AddToGrid recalculated compostie runoff coefficent for basin at grid location " + gridLocation.ToString());
            }
            DrainageAreaGrid.DisableBuildingCoveredDrainageAreas(building);
            
            
        }
    }
    [HarmonyPatch(typeof(BuildingManager), "RemoveFromGrid")]
    class BuildingManagerRemoveFromGridPatch
    {
        static void Postfix(ushort building, ref Building data)
        {
            if (!DrainageArea.reviewBuilding(building))
            {
                return;
            }
            int gridX = Mathf.Clamp((int)(data.m_position.x / 64f + 135f), 0, 269);
            int gridZ = Mathf.Clamp((int)(data.m_position.z / 64f + 135f), 0, 269);
            int gridLocation = gridZ * 270 + gridX;
            DrainageAreaGrid.recalculateCompositeRunoffCoefficentForBasinAtGridLocation(gridLocation);
            
            bool logging = false;
            if (logging)
            {
                Debug.Log("[RF]BuildignManagerPatch.RemoveFromGrid recalculated compostie runoff coefficent for basin at grid location " + gridLocation.ToString());
            }
            DrainageAreaGrid.DisableBuildingCoveredDrainageAreas(building);
        }
    }
}
