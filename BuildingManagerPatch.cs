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
            int gridX = Mathf.Clamp((int)(data.m_position.x / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            int gridZ = Mathf.Clamp((int)(data.m_position.z / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            int gridLocation = gridZ * DrainageAreaGrid.drainageAreaGridCoefficient + gridX;


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
            int gridX = Mathf.Clamp((int)(data.m_position.x / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            int gridZ = Mathf.Clamp((int)(data.m_position.z / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            int gridLocation = gridZ * DrainageAreaGrid.drainageAreaGridCoefficient + gridX;

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
