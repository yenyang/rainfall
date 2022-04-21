using HarmonyLib;
using UnityEngine;

namespace Rainfall
{
    [HarmonyPatch(typeof(BuildingAI), "ReleaseBuilding")]
    class BuildingAIReleaseBuildingPatch
    {
       
        static void Postfix(ushort buildingID, ref Building data, bool __state)
        {
           
            int gridX = Mathf.Clamp((int)(data.m_position.x / 64f + 135f), 0, 269);
            int gridZ = Mathf.Clamp((int)(data.m_position.z / 64f + 135f), 0, 269);
            int gridLocation = gridZ * 270 + gridX;

            bool flag = DrainageBasinGrid.recalculateCompositeRunoffCoefficentForBasinAtGridLocation(gridLocation);
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
