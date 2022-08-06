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
            if (data.Info.m_buildingAI is WaterFacilityAI)
            {
                WaterFacilityAI waterFacilityAI = (WaterFacilityAI)data.Info.m_buildingAI;
                if (waterFacilityAI.m_sewageOutlet > 0f)
                {
                    if (Hydraulics.instance._existingSewageOutlets != null)
                    {
                        Hydraulics.instance._existingSewageOutlets.Add(building);
                        //Debug.Log("[RF]BuildingManagerAddToGrid Adding existing sewage outlet " + building.ToString());
                    }
                }
            }
            if (!DrainageArea.ReviewBuilding(building))
            {
                //Debug.Log("[RF]BuildingManagerAddToGrid !ReviewBuilding. buildingID = " + building);
            }
            else
            {
                BuildingAI buildingAI = data.Info.m_buildingAI;
                if (OptionHandler.PublicBuildingAICatalog.ContainsKey(buildingAI.GetType()) || OptionHandler.PublicBuildingAISpecialCatalog.Contains(buildingAI.GetType()) || buildingAI is PrivateBuildingAI)
                {
                    //Debug.Log("[RF]BuildingManagerAddToGrid ReviewBuilding == true buildingID = " + building + " buildingAI = " + data.Info.m_buildingAI.GetType().ToString());
                    int gridX = Mathf.Clamp((int)(data.m_position.x / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
                    int gridZ = Mathf.Clamp((int)(data.m_position.z / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
                    int gridLocation = gridZ * DrainageAreaGrid.drainageAreaGridCoefficient + gridX;

                    DrainageAreaGrid.AddBuildingToDrainageArea(building, gridLocation);
                    DrainageAreaGrid.recalculateCompositeRunoffCoefficentForBasinAtGridLocation(gridLocation);
                    bool logging = false;
                    if (logging)
                    {
                        Debug.Log("[RF]BuildignManagerPatch.AddToGrid recalculated compostie runoff coefficent for basin at grid location " + gridLocation.ToString());
                    }
                    DrainageAreaGrid.DisableBuildingCoveredDrainageAreas(building);
                } else
                {
                    if (Hydrology.instance.buildingToReviewAndAdd != null)
                    {
                        Hydrology.instance.buildingToReviewAndAdd.Add(building);
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(BuildingManager), "RemoveFromGrid")]
    class BuildingManagerRemoveFromGridPatch
    {
        static void Postfix(ushort building, ref Building data)
        {
            if (data.Info.m_buildingAI is WaterFacilityAI)
            {
                WaterFacilityAI waterFacilityAI = data.Info.m_buildingAI as WaterFacilityAI;
                if (waterFacilityAI.m_sewageOutlet > 0f)
                {
                    if (Hydraulics.instance._existingSewageOutlets != null)
                    {
                        if (Hydraulics.instance._existingSewageOutlets.Contains(building))
                        {
                            Hydraulics.instance._existingSewageOutlets.Remove(building);
                        }
                    }
                }
            }
            if (!DrainageArea.ReviewBuilding(building))
            {
                return;
            }
            else
            {
                int gridX = Mathf.Clamp((int)(data.m_position.x / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
                int gridZ = Mathf.Clamp((int)(data.m_position.z / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
                int gridLocation = gridZ * DrainageAreaGrid.drainageAreaGridCoefficient + gridX;
                DrainageAreaGrid.RemoveBuildingFromDrainageArea(building, gridLocation);
                DrainageAreaGrid.recalculateCompositeRunoffCoefficentForBasinAtGridLocation(gridLocation);

                bool logging = false;
                if (logging)
                {
                    Debug.Log("[RF]BuildignManagerPatch.RemoveFromGrid recalculated compostie runoff coefficent for basin at grid location " + gridLocation.ToString());
                }
                DrainageAreaGrid.EnableBuildingUncoveredDrainageAreas(building);
            }
        }
    }
}
