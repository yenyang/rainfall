using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;
using ColossalFramework.Math;

namespace Rainfall
{
	public class WaterSourceEntry
	{

        public enum WaterSourceType
        {
            Undefined,
            Empty,
            Natural,
            WaterFacility,
            SewerFacility,
            StormDrainInletFacility,
            StormDrainOutletFacility,
            DrainageArea,
            DamPowerHouseFacility,
            WaterCleaner,
            WaterTruck,
            FloodSpawner,
            RetentionBasin,
        }

        public static readonly Dictionary<WaterSourceType, string> waterSourceTypeNames = new Dictionary<WaterSourceType, string>()
        {
            {WaterSourceType.Undefined,                     "Undefined"},
            {WaterSourceType.Empty,                         "Empty Water Source"},
            {WaterSourceType.Natural,                       "Natural Water Source"},
            {WaterSourceType.WaterFacility,                 "Water Facility Water Source"},
            {WaterSourceType.SewerFacility,                 "Sewer Facility Water Source"},
            {WaterSourceType.StormDrainInletFacility,       "Storm Drain Inlet Water Source"},
            {WaterSourceType.StormDrainOutletFacility,      "Storm Drain Outlet Water Source"},
            {WaterSourceType.DrainageArea,                  "Drainage Area Water Source"},
            {WaterSourceType.DamPowerHouseFacility,         "Dam Water Source"},
            {WaterSourceType.WaterCleaner,                  "Water Cleaner"},
            {WaterSourceType.WaterTruck,                    "Water Truck"},
            {WaterSourceType.FloodSpawner,                  "Flood Spawner" },
            {WaterSourceType.RetentionBasin,                "Retention Basin" },
        };

        private WaterSourceType type = WaterSourceType.Undefined;
        private ushort buildingID = 0;
		private int drainageBasinID = 0;
        List<WaterSourceType> facilityWaterSourceTypes = new List<WaterSourceType>() { WaterSourceType.WaterFacility, WaterSourceType.SewerFacility, WaterSourceType.StormDrainInletFacility, WaterSourceType.StormDrainOutletFacility, WaterSourceType.DamPowerHouseFacility, WaterSourceType.WaterCleaner, WaterSourceType.WaterTruck, WaterSourceType.FloodSpawner, WaterSourceType.RetentionBasin};
		public WaterSourceEntry(WaterSourceType currentType, ushort currentBuildingID, int currentDrainageBasinID) //works with any type
        {
            type = currentType;
            if (facilityWaterSourceTypes.Contains(type)) {
                buildingID = currentBuildingID;
            }
            if (type == WaterSourceType.DrainageArea) {
                drainageBasinID = currentDrainageBasinID;
            }
        }
        public WaterSourceEntry(WaterSourceType currentType) //Not meant for Facility or Drainage Areas
        {
            type = currentType;
        }
        public WaterSourceEntry(int currentDrainageBasinID) //only for drainage areas
        {
            type = WaterSourceType.DrainageArea;
            drainageBasinID = currentDrainageBasinID;
        }
        public WaterSourceEntry(WaterSourceType currentType, ushort currentBuildingID) //Only for facilities
        {
            type = currentType;
            if (facilityWaterSourceTypes.Contains(type))
            {
                buildingID = currentBuildingID;
            }
        }
        public WaterSourceType GetWaterSourceType()
        {
            return type;
        }
        public ushort GetBuildingID()
        {
            if (facilityWaterSourceTypes.Contains(type))
            {
                return buildingID;
            }
            return 0;
        }
        public int GetDrainageBasinID()
        {
            if (type == WaterSourceType.DrainageArea)
            {
                return drainageBasinID;
            }
            return 0;
        }
	}
}

