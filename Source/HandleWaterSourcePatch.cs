using ColossalFramework;
using HarmonyLib;
using UnityEngine;
using static ImmaterialResourceManager;

namespace Rainfall
{
    [HarmonyPatch(typeof(WaterFacilityAI), "HandleWaterSource")]
    class WaterFacilityAIHandleWaterSourcePatch
    {
       
        static bool Prefix(ushort buildingID, ref Building data, bool output, int pollution, int rate, int max, float radius)
        {
            bool logging = false;
            //if (data.Info.m_buildingAI is StormDrainAI) return true;
            if (logging) Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Prefix Hello!");
            
            if (WaterSourceManager.AreYouAwake())
            {
               if (data.m_waterSource != 0)
                {
                    WaterSourceEntry currentWaterSourceEntry = WaterSourceManager.GetWaterSourceEntry(data.m_waterSource);
                    WaterFacilityAI currentWaterFacilityAI = data.Info.m_buildingAI as WaterFacilityAI;
                    
                    if (currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined || currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Empty)
                    {
                        if (currentWaterFacilityAI.m_waterIntake > 0 || currentWaterFacilityAI.m_waterOutlet > 0)
                        {
                            WaterSourceManager.SetWaterSourceEntry(data.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.WaterFacility, buildingID));
                        } else if (currentWaterFacilityAI.m_sewageOutlet > 0)
                        {
                            WaterSourceManager.SetWaterSourceEntry(data.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.SewerFacility, buildingID));
                        } else
                        {
                            Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Prefix Error With WaterSourceID " + data.m_waterSource.ToString() + " WaterFacilityAI BuldingID = " + buildingID.ToString() + " Neither an inlet or outlet.");

                        }

                        if (logging) Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Prefix SetWaterSourceEntry for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " and is " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);

                    } else if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.WaterFacility && currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.SewerFacility || currentWaterSourceEntry.GetBuildingID() != buildingID)
                    {
                        /*if (logging)*/ Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Prefix Set data.m_waterSource = 0 for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                        data.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0
                        
                    }
                    if (data.m_waterSource != 0)
                    {
                        Vector2 buildingPositionXZ = new Vector2(data.m_position.x, data.m_position.z);
                        WaterSource currentWaterSource = Singleton<TerrainManager>.instance.WaterSimulation.m_waterSources[data.m_waterSource - 1];
                        Vector2 waterSourceOutputPositionXZ = new Vector2(currentWaterSource.m_outputPosition.x, currentWaterSource.m_outputPosition.z);
                        if (Vector2.Distance(buildingPositionXZ, waterSourceOutputPositionXZ) > 50f)
                        {
                            
                            Singleton<TerrainManager>.instance.WaterSimulation.LockWaterSource(data.m_waterSource);
                            Vector3 waterLocationOffsetPosition = data.CalculatePosition(currentWaterFacilityAI.m_waterLocationOffset);
                            Vector2 stormDrainPositionXZ = new Vector2(waterLocationOffsetPosition.x, waterLocationOffsetPosition.z);
                            if (Vector2.Distance(stormDrainPositionXZ, waterSourceOutputPositionXZ) > 50f)
                            {
                                currentWaterSource.m_outputPosition = waterLocationOffsetPosition;
                            }
                            if (logging) Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Prefix Moved WaterSource for buildingID " + buildingID.ToString() + " since BuildPositionXZ (" + buildingPositionXZ.x.ToString() + "," + buildingPositionXZ.y.ToString() + " is more than 50f " + " from WaterSourcePositionXZ (" + waterSourceOutputPositionXZ.x.ToString() + "," + waterSourceOutputPositionXZ.y.ToString());
                            Singleton<TerrainManager>.instance.WaterSimulation.UnlockWaterSource(data.m_waterSource, currentWaterSource);
                        }
                    }
                }
            }
            return true;
        }
        static void Postfix(ushort buildingID, ref Building data, bool output, int pollution, int rate, int max, float radius)
        {
            bool logging = false;
            //if (data.Info.m_buildingAI is StormDrainAI) return;
            if (logging) Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Postfix Hello!");

            if (WaterSourceManager.AreYouAwake())
            {
                if (data.m_waterSource != 0)
                {
                    WaterSourceEntry currentWaterSourceEntry = WaterSourceManager.GetWaterSourceEntry(data.m_waterSource);
                    WaterFacilityAI currentWaterFacilityAI = data.Info.m_buildingAI as WaterFacilityAI;
                   
                    if (currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined || currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Empty)
                    {
                        if (currentWaterFacilityAI.m_waterIntake > 0 || currentWaterFacilityAI.m_waterOutlet > 0)
                        {
                            WaterSourceManager.SetWaterSourceEntry(data.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.WaterFacility, buildingID));
                        }
                        else if (currentWaterFacilityAI.m_sewageOutlet > 0)
                        {
                            WaterSourceManager.SetWaterSourceEntry(data.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.SewerFacility, buildingID));
                        }
                        else
                        {
                            Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Prefix Error With WaterSourceID " + data.m_waterSource.ToString() + " WaterFacilityAI BuldingID = " + buildingID.ToString() + " Neither an inlet or outlet.");

                        }
                        if (logging) Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Postfix SetWaterSourceEntry for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " and is " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);

                    } else if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.WaterFacility && currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.SewerFacility || currentWaterSourceEntry.GetBuildingID() != buildingID)
                    {

                        /*if (logging)*/Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Postfix Set data.m_waterSource = 0 for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                        data.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0

                    }
                    if (data.m_waterSource != 0)
                    {
                        Vector2 buildingPositionXZ = new Vector2(data.m_position.x, data.m_position.z);
                        WaterSource currentWaterSource = Singleton<TerrainManager>.instance.WaterSimulation.m_waterSources[data.m_waterSource - 1];
                        Vector2 waterSourceOutputPositionXZ = new Vector2(currentWaterSource.m_outputPosition.x, currentWaterSource.m_outputPosition.z);
                        if (Vector2.Distance(buildingPositionXZ, waterSourceOutputPositionXZ) > 50f)
                        {
                            
                            Singleton<TerrainManager>.instance.WaterSimulation.LockWaterSource(data.m_waterSource);
                            Vector3 waterLocationOffsetPosition = data.CalculatePosition(currentWaterFacilityAI.m_waterLocationOffset);
                            Vector2 stormDrainPositionXZ = new Vector2(waterLocationOffsetPosition.x, waterLocationOffsetPosition.z);
                            if (Vector2.Distance(stormDrainPositionXZ, waterSourceOutputPositionXZ) > 50f)
                            {
                                currentWaterSource.m_outputPosition = waterLocationOffsetPosition;
                            }
                            if (logging) Debug.Log("[RF]WaterFacilityAIHandleWaterSourcePatch.Postfix Moved WaterSource for buildingID " + buildingID.ToString() + " since BuildPositionXZ (" + buildingPositionXZ.x.ToString() + "," + buildingPositionXZ.y.ToString() + " is more than 50f " + " from WaterSourcePositionXZ (" + waterSourceOutputPositionXZ.x.ToString() + "," + waterSourceOutputPositionXZ.y.ToString());
                            Singleton<TerrainManager>.instance.WaterSimulation.UnlockWaterSource(data.m_waterSource, currentWaterSource);
                        }
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
                        if (logging) Debug.Log("[RF]WaterCleanerAIHandleWaterSourcePatch.Prefix SetWaterSourceEntry for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " and is " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()] + " WaterSourceEntry.buildingID = " + WaterSourceManager.GetWaterSourceEntry(data.m_waterSource).GetBuildingID().ToString());
                        
                    }
                    else if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.WaterCleaner || currentWaterSourceEntry.GetBuildingID() != buildingID) { 
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
                        
                            Debug.Log("[RF]WaterCleanerAIHandleWaterSourcePatch.Prefix Set data.m_waterSource = 0 for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
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
            if (vehicleData.m_waterSource != 0)
            {
                Vector3 smoothVehiclePosition = vehicleData.GetSmoothPosition(vehicleID);
                Vector2 vehiclePositionXZ = new Vector2(smoothVehiclePosition.x, smoothVehiclePosition.z);
                WaterSource currentWaterSource = Singleton<TerrainManager>.instance.WaterSimulation.m_waterSources[vehicleData.m_waterSource - 1];
                Vector2 waterSourceOutputPositionXZ = new Vector2(currentWaterSource.m_outputPosition.x, currentWaterSource.m_outputPosition.z);
                if (Vector2.Distance(vehiclePositionXZ, waterSourceOutputPositionXZ) > 50f)
                {
                    
                    Singleton<TerrainManager>.instance.WaterSimulation.LockWaterSource(vehicleData.m_waterSource);
                    if (Vector2.Distance(smoothVehiclePosition, waterSourceOutputPositionXZ) > 50f)
                    {
                        currentWaterSource.m_outputPosition = smoothVehiclePosition;
                    }
                    if (logging) Debug.Log("[RF]WaterTruckAIHandleWaterSourcePatch.Prefix Moved WaterSource for vehicleID " + vehicleID.ToString() + " since smoothVehiclePosition (" + vehiclePositionXZ.x.ToString() + "," + vehiclePositionXZ.y.ToString() + " is more than 50f " + " from WaterSourcePositionXZ (" + waterSourceOutputPositionXZ.x.ToString() + "," + waterSourceOutputPositionXZ.y.ToString());
                    Singleton<TerrainManager>.instance.WaterSimulation.UnlockWaterSource(vehicleData.m_waterSource, currentWaterSource);
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
                if (vehicleData.m_waterSource != 0)
                {
                    Vector3 smoothVehiclePosition = vehicleData.GetSmoothPosition(vehicleID);
                    Vector2 vehiclePositionXZ = new Vector2(smoothVehiclePosition.x, smoothVehiclePosition.z);
                    WaterSource currentWaterSource = Singleton<TerrainManager>.instance.WaterSimulation.m_waterSources[vehicleData.m_waterSource - 1];
                    Vector2 waterSourceOutputPositionXZ = new Vector2(currentWaterSource.m_outputPosition.x, currentWaterSource.m_outputPosition.z);
                    if (Vector2.Distance(vehiclePositionXZ, waterSourceOutputPositionXZ) > 50f)
                    {
                       
                        Singleton<TerrainManager>.instance.WaterSimulation.LockWaterSource(vehicleData.m_waterSource);
                        if (Vector2.Distance(smoothVehiclePosition, waterSourceOutputPositionXZ) > 50f)
                        {
                            currentWaterSource.m_outputPosition = smoothVehiclePosition;
                        }
                        if (logging) Debug.Log("[RF]WaterTruckAIHandleWaterSourcePatch.Postfix Moved WaterSource for vehicleID " + vehicleID.ToString() + " since smoothVehiclePosition (" + vehiclePositionXZ.x.ToString() + "," + vehiclePositionXZ.y.ToString() + " is more than 50f " + " from WaterSourcePositionXZ (" + waterSourceOutputPositionXZ.x.ToString() + "," + waterSourceOutputPositionXZ.y.ToString());
                        Singleton<TerrainManager>.instance.WaterSimulation.UnlockWaterSource(vehicleData.m_waterSource, currentWaterSource);
                    }
                }
            }
        }
    }
}
