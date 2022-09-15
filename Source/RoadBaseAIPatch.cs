using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using ColossalFramework.Globalization;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace Rainfall
{
    [HarmonyPatch(typeof(RoadBaseAI), nameof(RoadBaseAI.SimulationStep), new Type[] { typeof(ushort), typeof(NetSegment) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref })]
 
    class RoadBaseAIPatch
    {
        [HarmonyPostfix]
        static void Postfix(ushort segmentID, ref NetSegment data, ref RoadBaseAI __instance)
        {
            SimulationManager instance = Singleton<SimulationManager>.instance;
            NetManager instance2 = Singleton<NetManager>.instance;
            Notification.Problem1 problem = Notification.RemoveProblems(data.m_problems, Notification.Problem1.Flood);
            //Debug.Log("[RF]RoadBaseAIPatch Postfix");
            bool timerLogging = false;
            Vector3 position = instance2.m_nodes.m_buffer[(int)data.m_startNode].m_position;
            Vector3 position2 = instance2.m_nodes.m_buffer[(int)data.m_endNode].m_position;
            float length = Mathf.Sqrt((float)(Math.Pow((position.x - position2.x), 2.0) + Math.Pow((position.z - position2.z), 2.0))) / 8f;
            float slope = (position.y - position2.y) / length;
            NetManager _netManager = Singleton<NetManager>.instance;
            Vector3 startPosition = _netManager.m_nodes.m_buffer[data.m_startNode].m_position;
            Vector3 endPosition = _netManager.m_nodes.m_buffer[data.m_endNode].m_position;
            Vector3 midPosition = data.m_middlePosition;
            Vector3 quarterPosition = data.GetClosestPosition((startPosition + midPosition) / 2f);
            Vector3 threeQuarterPosition = data.GetClosestPosition((midPosition + endPosition) / 2f);
            Vector3 lowestPointOfConcentration = startPosition;
            List<Vector3> potentialPointsOfConcentration = new List<Vector3> { startPosition, endPosition, midPosition, quarterPosition, threeQuarterPosition };
            foreach (Vector3 potentialPOC in potentialPointsOfConcentration)
            {
                if (potentialPOC.y < lowestPointOfConcentration.y)
                {
                    lowestPointOfConcentration = potentialPOC;
                }
            }
            Vector3 vector = lowestPointOfConcentration;
            
            //end edit
            bool flag = false;
            float RoadwayFloodedTolerance = OptionHandler.getSliderSetting("RoadwayFloodedTolerance");
            float RoadwayFloodingTolerance = OptionHandler.getSliderSetting("RoadwayFloodingTolerance");
            bool RoadwaySufferFlooded = OptionHandler.getCheckboxSetting("RoadwaySufferFlooded");
            bool RoadwaySufferFlooding = OptionHandler.getCheckboxSetting("RoadwaySufferFlooding");
            float RoadwayFloodingTimer = OptionHandler.getSliderSetting("RoadwayFloodingTimer");
            float RoadwayFloodedTimer = OptionHandler.getSliderSetting("RoadwayFloodedTimer");
            int gridx;
            int gridz;
            Singleton<GameAreaManager>.instance.GetTileXZ(data.m_middlePosition, out gridx, out gridz); 
            bool tileUnlocked = Singleton<GameAreaManager>.instance.IsUnlocked(gridx, gridz);
            bool preventFlood = false;
            bool OnlyFloodOwnedTiles = OptionHandler.getCheckboxSetting("OnlyFloodOwnedtiles");
            if (OnlyFloodOwnedTiles && !tileUnlocked) preventFlood = true;

            if ((__instance.m_info.m_setVehicleFlags & Vehicle.Flags.Underground) == (Vehicle.Flags)0)
            {
                float num6 = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(vector));
                // NON-STOCK CODE START
                if (num6 > vector.y + RoadwayFloodedTolerance && num6 > 0f && FloodingTimers.instance.getSegmentFloodedElapsedTime(segmentID) == -1f)
                {
                    FloodingTimers.instance.setSegmentFloodedStartTime(segmentID);
                    if (timerLogging)
                        Debug.Log("[RF]RBAIP Flooded Timer Set.");
                }
                else if (num6 > vector.y + RoadwayFloodingTolerance  && num6 > 0f && FloodingTimers.instance.getSegmentFloodingElapsedTime(segmentID) == -1f)
                {
                    FloodingTimers.instance.setSegmentFloodingStartTime(segmentID);
                    if (timerLogging)
                        Debug.Log("[RF]RBAIP Flooding Timer Set.");
                }

                if (num6 > vector.y + RoadwayFloodedTolerance && FloodingTimers.instance.getSegmentFloodedElapsedTime(segmentID) >= RoadwayFloodedTimer && RoadwaySufferFlooded && !preventFlood)
                {
                    flag = true;
                    //Debug.Log("[RF]RoadBaseAIDetour segmentID " + segmentID.ToString() + " slope = " + slope.ToString() + " wse = " + num6.ToString() + " vector.y " + vector.y.ToString() + " tolerance " + ((float)ModSettings.RoadwayFloodedTolerance / 100 + additionalToleranceForSlope * 2).ToString());
                    data.m_flags |= NetSegment.Flags.Flooded;
                    //Debug.Log("[RF] Successfully detoured roadway flooded tolerance");
                    problem = Notification.AddProblems(problem, Notification.Problem1.Flood | Notification.Problem1.MajorProblem);
                    /*DisasterData floodedSinkHoleData = new DisasterData();
                    floodedSinkHoleData.m_targetPosition = data.m_middlePosition;
                    floodedSinkHoleData.m_intensity = (byte)instance.m_randomizer.Int32(100u);
                    */
                    Vector3 min = data.m_bounds.min;
                    Vector3 max = data.m_bounds.max;
                    RoadBaseAI.FloodParkedCars(min.x, min.z, max.x, max.z);
                }

                else {
                    data.m_flags &= ~NetSegment.Flags.Flooded;

                    // Rainfall compatibility
                    float add = RoadwayFloodingTolerance;
                    //Debug.Log("[RF] Successfully detoured roadway flooding tolerance");
                    if (num6 > vector.y + add && FloodingTimers.instance.getSegmentFloodingElapsedTime(segmentID) >= RoadwayFloodingTimer && RoadwaySufferFlooding && !preventFlood)
                    {
                        if (timerLogging)
                            Debug.Log("[RF]RoadwayBaseAI Flood");
                        flag = true;
                        problem = Notification.AddProblems(problem, Notification.Problem1.Flood);
                    }
                    else if (num6 < vector.y + RoadwayFloodingTolerance && FloodingTimers.instance.getSegmentFloodingElapsedTime(segmentID) != -1f)
                    {
                        FloodingTimers.instance.resetSegmentFloodingStartTime(segmentID);
                        if (timerLogging)
                            Debug.Log("[RF]RoadwayBaseAI reset flooding timer");
                    }
                    else if (num6 < vector.y + RoadwayFloodedTolerance && FloodingTimers.instance.getSegmentFloodedElapsedTime(segmentID) != -1f)
                    {
                        FloodingTimers.instance.resetSegmentFloodedStartTime(segmentID);
                        if (timerLogging)
                            Debug.Log("[RF]RoadwayBaseAI reset flooded timer");
                    }
                }
                //Debug.Log("[RF] Successfully detoured roadway flooding tolerance: not flooding");
                // NON-STOCK CODE END
            }
            
            DistrictManager instance3 = Singleton<DistrictManager>.instance;
            byte district = instance3.GetDistrict(vector);
            DistrictPolicies.CityPlanning cityPlanningPolicies = instance3.m_districts.m_buffer[(int)district].m_cityPlanningPolicies;
            int num8 = (int)(100 - (data.m_noiseDensity - 100) * (data.m_noiseDensity - 100) / 100);
            if ((__instance.m_info.m_vehicleTypes & VehicleInfo.VehicleType.Car) != VehicleInfo.VehicleType.None)
            {
                if ((__instance.m_info.m_setVehicleFlags & Vehicle.Flags.Underground) == (Vehicle.Flags)0)
                {
                    if (flag && (data.m_flags & (NetSegment.Flags.AccessFailed | NetSegment.Flags.Blocked)) == NetSegment.Flags.None && instance.m_randomizer.Int32(10u) == 0)
                    {
                        TransferManager.TransferOffer offer = default(TransferManager.TransferOffer);
                        offer.Priority = 4;
                        offer.NetSegment = segmentID;
                        offer.Position = vector;
                        offer.Amount = 1;
                        Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.FloodWater, offer);
                    }
                    int num9 = (int)data.m_wetness;
                    if (!instance2.m_treatWetAsSnow)
                    {
                        if (flag)
                        {
                            num9 = 255;
                        }
                        else
                        {
                            int num10 = -(num9 + 63 >> 5);
                            float num11 = Singleton<WeatherManager>.instance.SampleRainIntensity(vector, false);
                            if (num11 != 0f)
                            {
                                int num12 = Mathf.RoundToInt(Mathf.Min(num11 * 4000f, 1000f));
                                num10 += instance.m_randomizer.Int32(num12, num12 + 99) / 100;
                            }
                            num9 = Mathf.Clamp(num9 + num10, 0, 255);
                        }
                    }
                    else if (__instance.m_accumulateSnow)
                    {
                        if (flag)
                        {
                            num9 = 128;
                        }
                        else
                        {
                            float num13 = Singleton<WeatherManager>.instance.SampleRainIntensity(vector, false);
                            if (num13 != 0f)
                            {
                                int num14 = Mathf.RoundToInt(num13 * 400f);
                                int num15 = instance.m_randomizer.Int32(num14, num14 + 99) / 100;
                                if (Singleton<UnlockManager>.instance.Unlocked(UnlockManager.Feature.Snowplow))
                                {
                                    num9 = Mathf.Min(num9 + num15, 255);
                                }
                                else
                                {
                                    num9 = Mathf.Min(num9 + num15, 128);
                                }
                            }
                            else if (Singleton<SimulationManager>.instance.m_randomizer.Int32(4u) == 0)
                            {
                                num9 = Mathf.Max(num9 - 1, 0);
                            }
                            if (num9 >= 64 && (data.m_flags & (NetSegment.Flags.AccessFailed | NetSegment.Flags.Blocked | NetSegment.Flags.Flooded)) == NetSegment.Flags.None && instance.m_randomizer.Int32(10u) == 0)
                            {
                                TransferManager.TransferOffer offer2 = default(TransferManager.TransferOffer);
                                offer2.Priority = num9 / 50;
                                offer2.NetSegment = segmentID;
                                offer2.Position = vector;
                                offer2.Amount = 1;
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Snow, offer2);
                            }
                            if (num9 >= 192)
                            {
                                problem = Notification.AddProblems(problem, Notification.Problem1.Snow);
                            }
                            District[] expr_63D_cp_0_cp_0 = instance3.m_districts.m_buffer;
                            byte expr_63D_cp_0_cp_1 = district;
                            expr_63D_cp_0_cp_0[(int)expr_63D_cp_0_cp_1].m_productionData.m_tempSnowCover = expr_63D_cp_0_cp_0[(int)expr_63D_cp_0_cp_1].m_productionData.m_tempSnowCover + (uint)num9;
                        }
                    }
                    if (num9 != (int)data.m_wetness)
                    {
                        if (Mathf.Abs((int)data.m_wetness - num9) > 10)
                        {
                            data.m_wetness = (byte)num9;
                            InstanceID empty = InstanceID.Empty;
                            empty.NetSegment = segmentID;
                            instance2.AddSmoothColor(empty);
                            empty.NetNode = data.m_startNode;
                            instance2.AddSmoothColor(empty);
                            empty.NetNode = data.m_endNode;
                            instance2.AddSmoothColor(empty);
                        }
                        else
                        {
                            data.m_wetness = (byte)num9;
                            instance2.m_wetnessChanged = 256;
                        }
                    }
                }
                int num16;
                if ((cityPlanningPolicies & DistrictPolicies.CityPlanning.StuddedTires) != DistrictPolicies.CityPlanning.None)
                {
                    num8 = num8 * 3 + 1 >> 1;
                    num16 = Mathf.Min(700, (int)(50 + data.m_trafficDensity * 6));
                }
                else
                {
                    num16 = Mathf.Min(500, (int)(50 + data.m_trafficDensity * 4));
                }
                if (!__instance.m_highwayRules)
                {
                    int num17 = instance.m_randomizer.Int32(num16, num16 + 99) / 100;
                    data.m_condition = (byte)Mathf.Max((int)data.m_condition - num17, 0);
                    if (data.m_condition < 192 && (data.m_flags & (NetSegment.Flags.AccessFailed | NetSegment.Flags.Blocked | NetSegment.Flags.Flooded)) == NetSegment.Flags.None && instance.m_randomizer.Int32(20u) == 0)
                    {
                        TransferManager.TransferOffer offer3 = default(TransferManager.TransferOffer);
                        offer3.Priority = (int)((255 - data.m_condition) / 50);
                        offer3.NetSegment = segmentID;
                        offer3.Position = vector;
                        offer3.Amount = 1;
                        Singleton<TransferManager>.instance.AddIncomingOffer(TransferManager.TransferReason.RoadMaintenance, offer3);
                    }
                }
            }
            data.m_problems = problem;
        }
    }
}
