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
            //Debug.Log("[RF]RFBuildingExtension reviewBuilding id" + id.ToString() + " is " + Hydrology.instance.reviewBuilding(id).ToString());
            if (Hydrology.instance.reviewBuilding(id))
            {
                Hydrology.instance._newBuildingIDs.Add(id);
                //Debug.Log("[RF]RFbuildingExtension Successfully added building " + id.ToString());
            }
        }

        public void OnBuildingRelocated(ushort id)
        {

        }

        public void OnBuildingReleased(ushort id)
        {
            //Debug.Log("[RF]RFBuildingExtension release id" + id.ToString());
            if (Hydrology.instance._buildingIDs.Contains(id))
                Hydrology.instance._removeBuildingIDs.Add(id);
        }

    }
}
