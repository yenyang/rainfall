using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICities;
using UnityEngine;
using ColossalFramework;
using System.Threading;

namespace Rainfall
{
    public class RFBuildingExtension : IBuildingExtension
    {
        public void OnCreated(IBuilding Building)
        {
        }

        public void OnReleased()
        {

        }

        public SpawnData OnCalculateSpawn(Vector3 location, SpawnData spawn)
        {


            return spawn;
        }

        public void OnBuildingCreated(ushort id)
        {
            //recalculate drainage coefficient for drainage basin
        }

        public void OnBuildingRelocated(ushort id)
        {
            //recalculate drainage coefficient for drainage basins
            // how do i know where it came from?
        }

        public void OnBuildingReleased(ushort id)
        {
            if (!DrainageArea.ReviewBuilding(id))
            {
                return;
            }
            else
            {
                Building data = Singleton<BuildingManager>.instance.m_buildings.m_buffer[id];
                int gridX = Mathf.Clamp((int)(data.m_position.x / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
                int gridZ = Mathf.Clamp((int)(data.m_position.z / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
                int gridLocation = gridZ * DrainageAreaGrid.drainageAreaGridCoefficient + gridX;

                DrainageAreaGrid.RemoveBuildingFromDrainageArea(id, gridLocation);
                DrainageAreaGrid.recalculateCompositeRunoffCoefficentForBasinAtGridLocation(gridLocation);

                bool logging = false;
                if (logging)
                {
                    Debug.Log("[RF]RFBuildingExtension.OnBuildingReleased recalculated compostie runoff coefficent for basin at grid location " + gridLocation.ToString());
                }
                DrainageAreaGrid.EnableBuildingUncoveredDrainageAreas(id);
            }
        }

    }
}
