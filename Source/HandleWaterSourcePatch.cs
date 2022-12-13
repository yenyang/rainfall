using HarmonyLib;
using UnityEngine;

namespace Rainfall
{
    [HarmonyPatch(typeof(WaterFacilityAI), "HandleWaterSource")]
    class WaterFacilityAIHandleWaterSourcePatch
    {
       
        static bool Prefix(ushort buildingID, ref Building data, bool output, int pollution, int rate, int max, float radius)
        {
            bool logging = false;
            if (logging) Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Prefix Hello!");
            if (WaterSourceManager.AreYouAwake())
            {
               if (data.m_waterSource != 0)
                {
                    WaterSourceEntry currentWaterSourceEntry = WaterSourceManager.GetWaterSourceEntry(data.m_waterSource);
                    if (currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined || currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Empty)
                    {
                        WaterSourceManager.SetWaterSourceEntry(data.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.WaterFacility, buildingID));
                        if (logging) Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Prefix SetWaterSourceEntry for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " and is " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);

                    } else if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.WaterFacility || currentWaterSourceEntry.GetBuildingID() != buildingID)
                    {
                        /*if (logging)*/ Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Prefix Set data.m_waterSource = 0 for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                        data.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0
                        
                    }
                }
            }
            return true;
        }
        static void Postfix(ushort buildingID, ref Building data, bool output, int pollution, int rate, int max, float radius)
        {
            bool logging = false;
            if (logging) Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Postfix Hello!");

            if (WaterSourceManager.AreYouAwake())
            {
                if (data.m_waterSource != 0)
                {
                    WaterSourceEntry currentWaterSourceEntry = WaterSourceManager.GetWaterSourceEntry(data.m_waterSource);
                    if (currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined || currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Empty)
                    {
                        WaterSourceManager.SetWaterSourceEntry(data.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.WaterFacility, buildingID));
                        if (logging) Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Postfix SetWaterSourceEntry for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " and is " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);

                    } else if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.WaterFacility || currentWaterSourceEntry.GetBuildingID() != buildingID)
                    {

                        /*if (logging)*/Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Postfix Set data.m_waterSource = 0 for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                        data.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0

                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(DamPowerHouseAI), "HandleWaterSource")]
    class DamPowerHouseAIHandleWaterSourcePatch
    {

        static bool Prefix(ushort buildingID, ref Building data, float radius, float target)
        {
            bool logging = false;
            if (logging) Debug.Log("[RF]DamPowerHouseAIHandleWaterSourcePatch.Prefix Hello!");
            if (WaterSourceManager.AreYouAwake())
            {
                if (data.m_waterSource != 0)
                {
                    WaterSourceEntry currentWaterSourceEntry = WaterSourceManager.GetWaterSourceEntry(data.m_waterSource);
                    if (currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined || currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Empty)
                    {
                        WaterSourceManager.SetWaterSourceEntry(data.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.DamPowerHouseFacility, buildingID));
                        if (logging) Debug.Log("[RF]DamPowerHouseAIHandleWaterSourcePatch.Prefix SetWaterSourceEntry for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " and is " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);

                    } else if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.DamPowerHouseFacility || currentWaterSourceEntry.GetBuildingID() != buildingID)
                    {
                        /*if (logging)*/Debug.Log("[RF]DamPowerHouseAIHandleWaterSourcePatch.Prefix Set data.m_waterSource = 0 for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                        data.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0

                    }
                }
            }
            return true;
        }
        static void Postfix(ushort buildingID, ref Building data, float radius, float target)
        {
            bool logging = false;
            if (logging) Debug.Log("[RF]DamPowerHouseAIHandleWaterSourcePatch.Postfix Hello!");

            if (WaterSourceManager.AreYouAwake())
            {
                if (data.m_waterSource != 0)
                {
                    WaterSourceEntry currentWaterSourceEntry = WaterSourceManager.GetWaterSourceEntry(data.m_waterSource);
                    if (currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined || currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Empty)
                    {
                        WaterSourceManager.SetWaterSourceEntry(data.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.DamPowerHouseFacility, buildingID));
                        if (logging) Debug.Log("[RF]DamPowerHouseAIHandleWaterSourcePatch.Postfix SetWaterSourceEntry for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " and is " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);

                    }
                    else if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.DamPowerHouseFacility || currentWaterSourceEntry.GetBuildingID() != buildingID)
                    {
                        /*if (logging)*/
                        Debug.Log("[RF]DamPowerHouseAIHandleWaterSourcePatch.Postfix Set data.m_waterSource = 0 for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                        data.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0

                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(WaterCleanerAI), "HandleWaterSource")]
    class WaterCleanerAIHandleWaterSourcePatch
    {

        static bool Prefix(ushort buildingID, ref Building data, int rate, float radius)
        {
            bool logging = false;
            if (logging) Debug.Log("[RF]WaterCleanerAIHandleWaterSourcePatch.Prefix Hello!");
            if (WaterSourceManager.AreYouAwake())
            {
                if (data.m_waterSource != 0)
                {
                    WaterSourceEntry currentWaterSourceEntry = WaterSourceManager.GetWaterSourceEntry(data.m_waterSource);
                    if (currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined || currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Empty)
                    {
                        WaterSourceManager.SetWaterSourceEntry(data.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.WaterCleaner, buildingID));
                        if (logging) Debug.Log("[RF]WaterCleanerAIHandleWaterSourcePatch.Postfix SetWaterSourceEntry for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " and is " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);

                    }
                    else if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.WaterCleaner || currentWaterSourceEntry.GetBuildingID() != buildingID)
                    {
                        /*if (logging)*/
                        Debug.Log("[RF]WaterCleanerAIHandleWaterSourcePatch.Prefix Set data.m_waterSource = 0 for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                        data.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0

                    }
                }
            }
            return true;
        }
        static void Postfix(ushort buildingID, ref Building data, int rate, float radius)
        {
            bool logging = false;
            if (logging) Debug.Log("[RF]WaterCleanerAIHandleWaterSourcePatch.Postfix Hello!");

            if (WaterSourceManager.AreYouAwake())
            {
                if (data.m_waterSource != 0)
                {
                    WaterSourceEntry currentWaterSourceEntry = WaterSourceManager.GetWaterSourceEntry(data.m_waterSource);
                    if (currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined || currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Empty)
                    {
                        WaterSourceManager.SetWaterSourceEntry(data.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.WaterCleaner, buildingID));
                        if (logging) Debug.Log("[RF]WaterCleanerAIHandleWaterSourcePatch.Postfix SetWaterSourceEntry for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " and is " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);

                    }
                    else if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.WaterCleaner || currentWaterSourceEntry.GetBuildingID() != buildingID)
                    {
                        /*if (logging)*/
                        Debug.Log("[RF]WaterCleanerAIHandleWaterSourcePatch.Postfix Set data.m_waterSource = 0 for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                        data.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0

                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(WaterTruckAI), "HandleWaterSource")]
    class WaterTruckAIHandleWaterSourcePatch
    {

        static bool Prefix(ushort vehicleID, ref Vehicle vehicleData, Vector3 refPos, float radius, int amount)
        {
            bool logging = false;
            if (logging) Debug.Log("[RF]WaterTruckAIHandleWaterSourcePatch.Prefix Hello!");
            if (WaterSourceManager.AreYouAwake())
            {
                if (vehicleData.m_waterSource != 0)
                {
                    WaterSourceEntry currentWaterSourceEntry = WaterSourceManager.GetWaterSourceEntry(vehicleData.m_waterSource);
                    if (currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined || currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Empty)
                    {
                        WaterSourceManager.SetWaterSourceEntry(vehicleData.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.WaterTruck, vehicleID));
                        if (logging) Debug.Log("[RF]WaterTruckAIHandleWaterSourcePatch.Postfix SetWaterSourceEntry for buildingID " + vehicleID.ToString() + " since WSM says WaterSource " + vehicleData.m_waterSource.ToString() + " and is " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);

                    } else if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.WaterTruck || currentWaterSourceEntry.GetBuildingID() != vehicleID)
                    {
                        /*if (logging)*/
                        Debug.Log("[RF]WaterTruckAIHandleWaterSourcePatch.Prefix Set data.m_waterSource = 0 for buildingID " + vehicleID.ToString() + " since WSM says WaterSource " + vehicleData.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                        vehicleData.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0

                    }
                }
            }
            return true;
        }
        static void Postfix(ushort vehicleID, ref Vehicle vehicleData, Vector3 refPos, float radius, int amount)
        {
            bool logging = false;
            if (logging) Debug.Log("[RF]WaterTruckAIHandleWaterSourcePatch.Postfix Hello!");

            if (WaterSourceManager.AreYouAwake())
            {
                if (vehicleData.m_waterSource != 0)
                {
                    WaterSourceEntry currentWaterSourceEntry = WaterSourceManager.GetWaterSourceEntry(vehicleData.m_waterSource);
                    if (currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined || currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Empty)
                    {
                        WaterSourceManager.SetWaterSourceEntry(vehicleData.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.WaterTruck, vehicleID));
                        if (logging) Debug.Log("[RF]WaterTruckAIHandleWaterSourcePatch.Postfix SetWaterSourceEntry for buildingID " + vehicleID.ToString() + " since WSM says WaterSource " + vehicleData.m_waterSource.ToString() + " and is " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);

                    }
                    else if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.WaterTruck || currentWaterSourceEntry.GetBuildingID() != vehicleID)
                    {
                        /*if (logging)*/
                        Debug.Log("[RF]WaterTruckAIHandleWaterSourcePatch.Postfix Set data.m_waterSource = 0 for buildingID " + vehicleID.ToString() + " since WSM says WaterSource " + vehicleData.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                        vehicleData.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0

                    }
                }
            }
        }
    }
}
