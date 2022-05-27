using HarmonyLib;
using UnityEngine;

namespace Rainfall
{
    [HarmonyPatch(typeof(BuildingAI), "ReleaseBuilding")]
    class BuildingAIReleaseBuildingPatch
    {
       
        static void Postfix(ushort buildingID, ref Building data, bool __state)
        {

            int gridX = Mathf.Clamp((int)(data.m_position.x / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            int gridZ = Mathf.Clamp((int)(data.m_position.z / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            int gridLocation = gridZ * DrainageAreaGrid.drainageAreaGridCoefficient + gridX;

            bool flag = DrainageAreaGrid.recalculateCompositeRunoffCoefficentForBasinAtGridLocation(gridLocation);
            DrainageAreaGrid.EnableBuildingUncoveredDrainageAreas(buildingID);
            //Debug.Log("[RF]BuildingAIReleaseBuildingPatch.ReleaseBuilding flag = " + flag.ToString());
            bool logging = false;  
            if (logging)
            {
                Debug.Log("[RF]BuildingAIReleaseBuildingPatch.ReleaseBuilding recalculated compostie runoff coefficent for basin at grid location " + gridLocation.ToString());
            }
            return;
        }
    }
}
