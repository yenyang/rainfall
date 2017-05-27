using Rainfall.Redirection.Attributes;
using ColossalFramework;
using ColossalFramework.Math;
using System;

using UnityEngine;

namespace Rainfall
{

    [TargetType(typeof(RoadBaseAI))]
    public class RoadBaseAIDetour : RoadBaseAI
    {
        [RedirectMethod]
        public override void SimulationStep(ushort segmentID, ref NetSegment data)
        {
            //Start PlayerNEtAI.SimulationStep

            if (this.HasMaintenanceCost(segmentID, ref data))
            {
                NetManager playerNetAIinstance = Singleton<NetManager>.instance;
                Vector3 playerNetAIposition = playerNetAIinstance.m_nodes.m_buffer[(int)data.m_startNode].m_position;
                Vector3 playerNetAIposition2 = playerNetAIinstance.m_nodes.m_buffer[(int)data.m_endNode].m_position;
                int playerNetAInum = this.GetMaintenanceCost(playerNetAIposition, playerNetAIposition2);
                bool playerNetAIflag = (ulong)(Singleton<SimulationManager>.instance.m_currentFrameIndex >> 8 & 15u) == (ulong)((long)(segmentID & 15));
                if (playerNetAInum != 0)
                {
                    if (playerNetAIflag)
                    {
                        playerNetAInum = playerNetAInum * 16 / 100 - playerNetAInum / 100 * 15;
                    }
                    else
                    {
                        playerNetAInum /= 100;
                    }
                    Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Maintenance, playerNetAInum, this.m_info.m_class);
                }
                if (playerNetAIflag)
                {
                    float playerNetAInum2 = (float)playerNetAIinstance.m_nodes.m_buffer[(int)data.m_startNode].m_elevation;
                    float playerNetAInum3 = (float)playerNetAIinstance.m_nodes.m_buffer[(int)data.m_endNode].m_elevation;
                    if (this.IsUnderground())
                    {
                        playerNetAInum2 = -playerNetAInum2;
                        playerNetAInum3 = -playerNetAInum3;
                    }
                    int constructionCost = this.GetConstructionCost(playerNetAIposition, playerNetAIposition2, playerNetAInum2, playerNetAInum3);
                    if (constructionCost != 0)
                    {
                        StatisticBase statisticBase = Singleton<StatisticsManager>.instance.Acquire<StatisticInt64>(StatisticType.CityValue);
                        if (statisticBase != null)
                        {
                            statisticBase.Add(constructionCost);
                        }
                    }
                }
            }
            //End  PlayerNEtAI.SimulationStep


            SimulationManager instance = Singleton<SimulationManager>.instance;
            NetManager instance2 = Singleton<NetManager>.instance;
            Notification.Problem problem = Notification.RemoveProblems(data.m_problems, Notification.Problem.Flood | Notification.Problem.Snow);
            if ((data.m_flags & NetSegment.Flags.AccessFailed) != NetSegment.Flags.None && Singleton<SimulationManager>.instance.m_randomizer.Int32(16u) == 0)
            {
                data.m_flags &= ~NetSegment.Flags.AccessFailed;
            }
            float num = 0f;
            uint num2 = data.m_lanes;
            int num3 = 0;
            while (num3 < this.m_info.m_lanes.Length && num2 != 0u)
            {
                NetInfo.Lane lane = this.m_info.m_lanes[num3];
                if ((byte)(lane.m_laneType & (NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle)) != 0 && (lane.m_vehicleType & ~VehicleInfo.VehicleType.Bicycle) != VehicleInfo.VehicleType.None)
                {
                    num += instance2.m_lanes.m_buffer[(int)((UIntPtr)num2)].m_length;
                }
                num2 = instance2.m_lanes.m_buffer[(int)((UIntPtr)num2)].m_nextLane;
                num3++;
            }
            int num4 = 0;
            if (data.m_trafficBuffer == 65535)
            {
                if ((data.m_flags & NetSegment.Flags.Blocked) == NetSegment.Flags.None)
                {
                    data.m_flags |= NetSegment.Flags.Blocked;
                    data.m_modifiedIndex = instance.m_currentBuildIndex++;
                }
            }
            else
            {
                data.m_flags &= ~NetSegment.Flags.Blocked;
                int num5 = Mathf.RoundToInt(num) << 4;
                if (num5 != 0)
                {
                    num4 = (int)((byte)Mathf.Min((int)(data.m_trafficBuffer * 100) / num5, 100));
                }
            }
            data.m_trafficBuffer = 0;
            if (num4 > (int)data.m_trafficDensity)
            {
                data.m_trafficDensity = (byte)Mathf.Min((int)(data.m_trafficDensity + 5), num4);
            }
            else if (num4 < (int)data.m_trafficDensity)
            {
                data.m_trafficDensity = (byte)Mathf.Max((int)(data.m_trafficDensity - 5), num4);
            }
            Vector3 position = instance2.m_nodes.m_buffer[(int)data.m_startNode].m_position;
            Vector3 position2 = instance2.m_nodes.m_buffer[(int)data.m_endNode].m_position;
            //edit
            float length = Mathf.Sqrt((float)(Math.Pow((position.x - position2.x),2.0)+ Math.Pow((position.z - position2.z), 2.0)))/8f;
            float slope = (position.y - position2.y) / length;
            Vector3 vector = (position + position2) * 0.5f;
            float additionalToleranceForSlope = 0;
            if (ModSettings.AdditionalToleranceOnSlopes == true)
            {
                
                additionalToleranceForSlope += Mathf.Abs(slope);
            }
            //end edit
            bool flag = false;
            if ((this.m_info.m_setVehicleFlags & Vehicle.Flags.Underground) == (Vehicle.Flags)0)
            {
                float num6 = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(vector));
                // NON-STOCK CODE START
                if (num6 > vector.y + (float)ModSettings.RoadwayFloodedTolerance / 100 + additionalToleranceForSlope*2)
                {
                    flag = true;
                    //Debug.Log("[RF]RoadBaseAIDetour segmentID " + segmentID.ToString() + " slope = " + slope.ToString() + " wse = " + num6.ToString() + " vector.y " + vector.y.ToString() + " tolerance " + ((float)ModSettings.RoadwayFloodedTolerance / 100 + additionalToleranceForSlope * 2).ToString());
                    data.m_flags |= NetSegment.Flags.Flooded;
                    //Debug.Log("[RF] Successfully detoured roadway flooded tolerance");
                    problem = Notification.AddProblems(problem, Notification.Problem.Flood | Notification.Problem.MajorProblem);
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
                    float add = (float)ModSettings.RoadwayFloodingTolerance / 100 + additionalToleranceForSlope;
                    //Debug.Log("[RF] Successfully detoured roadway flooding tolerance");
                    if (num6 > vector.y + add)
                    {
                        flag = true;
                        problem = Notification.AddProblems(problem, Notification.Problem.Flood);
                    }
                }
                //Debug.Log("[RF] Successfully detoured roadway flooding tolerance: not flooding");
                // NON-STOCK CODE END
            }
            DistrictManager instance3 = Singleton<DistrictManager>.instance;
            byte district = instance3.GetDistrict(vector);
            DistrictPolicies.CityPlanning cityPlanningPolicies = instance3.m_districts.m_buffer[(int)district].m_cityPlanningPolicies;
            int num7 = (int)(100 - (data.m_trafficDensity - 100) * (data.m_trafficDensity - 100) / 100);
            if ((this.m_info.m_vehicleTypes & VehicleInfo.VehicleType.Car) != VehicleInfo.VehicleType.None)
            {
                if ((this.m_info.m_setVehicleFlags & Vehicle.Flags.Underground) == (Vehicle.Flags)0)
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
                    int num8 = (int)data.m_wetness;
                    if (!instance2.m_treatWetAsSnow)
                    {
                        if (flag)
                        {
                            num8 = 255;
                        }
                        else
                        {
                            int num9 = -(num8 + 63 >> 5);
                            float num10 = Singleton<WeatherManager>.instance.SampleRainIntensity(vector, false);
                            if (num10 != 0f)
                            {
                                int num11 = Mathf.RoundToInt(Mathf.Min(num10 * 4000f, 1000f));
                                num9 += instance.m_randomizer.Int32(num11, num11 + 99) / 100;
                            }
                            num8 = Mathf.Clamp(num8 + num9, 0, 255);
                        }
                    }
                    else if (this.m_accumulateSnow)
                    {
                        if (flag)
                        {
                            num8 = 128;
                        }
                        else
                        {
                            float num12 = Singleton<WeatherManager>.instance.SampleRainIntensity(vector, false);
                            if (num12 != 0f)
                            {
                                int num13 = Mathf.RoundToInt(num12 * 400f);
                                int num14 = instance.m_randomizer.Int32(num13, num13 + 99) / 100;
                                if (Singleton<UnlockManager>.instance.Unlocked(UnlockManager.Feature.Snowplow))
                                {
                                    num8 = Mathf.Min(num8 + num14, 255);
                                }
                                else
                                {
                                    num8 = Mathf.Min(num8 + num14, 128);
                                }
                            }
                            else if (Singleton<SimulationManager>.instance.m_randomizer.Int32(4u) == 0)
                            {
                                num8 = Mathf.Max(num8 - 1, 0);
                            }
                            if (num8 >= 64 && (data.m_flags & (NetSegment.Flags.AccessFailed | NetSegment.Flags.Blocked | NetSegment.Flags.Flooded)) == NetSegment.Flags.None && instance.m_randomizer.Int32(10u) == 0)
                            {
                                TransferManager.TransferOffer offer2 = default(TransferManager.TransferOffer);
                                offer2.Priority = num8 / 50;
                                offer2.NetSegment = segmentID;
                                offer2.Position = vector;
                                offer2.Amount = 1;
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Snow, offer2);
                            }
                            if (num8 >= 192)
                            {
                                problem = Notification.AddProblems(problem, Notification.Problem.Snow);
                            }
                            District[] expr_5B7_cp_0_cp_0 = instance3.m_districts.m_buffer;
                            byte expr_5B7_cp_0_cp_1 = district;
                            expr_5B7_cp_0_cp_0[(int)expr_5B7_cp_0_cp_1].m_productionData.m_tempSnowCover = expr_5B7_cp_0_cp_0[(int)expr_5B7_cp_0_cp_1].m_productionData.m_tempSnowCover + (uint)num8;
                        }
                    }
                    if (num8 != (int)data.m_wetness)
                    {
                        if (Mathf.Abs((int)data.m_wetness - num8) > 10)
                        {
                            data.m_wetness = (byte)num8;
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
                            data.m_wetness = (byte)num8;
                            instance2.m_wetnessChanged = 256;
                        }
                    }
                }
                int num15;
                if ((cityPlanningPolicies & DistrictPolicies.CityPlanning.StuddedTires) != DistrictPolicies.CityPlanning.None)
                {
                    num7 = num7 * 3 + 1 >> 1;
                    num15 = Mathf.Min(700, (int)(50 + data.m_trafficDensity * 6));
                }
                else
                {
                    num15 = Mathf.Min(500, (int)(50 + data.m_trafficDensity * 4));
                }
                if (!this.m_highwayRules)
                {
                    int num16 = instance.m_randomizer.Int32(num15, num15 + 99) / 100;
                    data.m_condition = (byte)Mathf.Max((int)data.m_condition - num16, 0);
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
            if (!this.m_highwayRules)
            {
                if ((cityPlanningPolicies & DistrictPolicies.CityPlanning.HeavyTrafficBan) != DistrictPolicies.CityPlanning.None)
                {
                    data.m_flags |= NetSegment.Flags.HeavyBan;
                }
                else
                {
                    data.m_flags &= ~NetSegment.Flags.HeavyBan;
                }
                if ((cityPlanningPolicies & DistrictPolicies.CityPlanning.BikeBan) != DistrictPolicies.CityPlanning.None)
                {
                    data.m_flags |= NetSegment.Flags.BikeBan;
                }
                else
                {
                    data.m_flags &= ~NetSegment.Flags.BikeBan;
                }
                if ((cityPlanningPolicies & DistrictPolicies.CityPlanning.OldTown) != DistrictPolicies.CityPlanning.None)
                {
                    data.m_flags |= NetSegment.Flags.CarBan;
                }
                else
                {
                    data.m_flags &= ~NetSegment.Flags.CarBan;
                }
            }
            int num17 = this.m_noiseAccumulation * num7 / 100;
            if (num17 != 0)
            {
                float num18 = Vector3.Distance(position, position2);
                int num19 = Mathf.FloorToInt(num18 / this.m_noiseRadius);
                for (int i = 0; i < num19; i++)
                {
                    Vector3 position3 = Vector3.Lerp(position, position2, (float)(i + 1) / (float)(num19 + 1));
                    Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.NoisePollution, num17, position3, this.m_noiseRadius);
                }
            }
            if (data.m_trafficDensity >= 50 && data.m_averageLength < 25f && (instance2.m_nodes.m_buffer[(int)data.m_startNode].m_flags & (NetNode.Flags.LevelCrossing | NetNode.Flags.TrafficLights)) == NetNode.Flags.TrafficLights && (instance2.m_nodes.m_buffer[(int)data.m_endNode].m_flags & (NetNode.Flags.LevelCrossing | NetNode.Flags.TrafficLights)) == NetNode.Flags.TrafficLights)
            {
                GuideController properties = Singleton<GuideManager>.instance.m_properties;
                if (properties != null)
                {
                    Singleton<NetManager>.instance.m_shortRoadTraffic.Activate(properties.m_shortRoadTraffic, segmentID, false);
                }
            }
            if ((data.m_flags & NetSegment.Flags.Collapsed) != NetSegment.Flags.None)
            {
                GuideController properties2 = Singleton<GuideManager>.instance.m_properties;
                if (properties2 != null)
                {
                    Singleton<NetManager>.instance.m_roadDestroyed.Activate(properties2.m_roadDestroyed, segmentID, false);
                    Singleton<NetManager>.instance.m_roadDestroyed2.Activate(properties2.m_roadDestroyed2, this.m_info.m_class.m_service);
                }
                if ((ulong)(instance.m_currentFrameIndex >> 8 & 15u) == (ulong)((long)(segmentID & 15)))
                {
                    int delta = Mathf.RoundToInt(data.m_averageLength);
                    StatisticBase statisticBase = Singleton<StatisticsManager>.instance.Acquire<StatisticInt32>(StatisticType.DestroyedLength);
                    statisticBase.Add(delta);
                }
            }
            data.m_problems = problem;

        }
        
    }
}
