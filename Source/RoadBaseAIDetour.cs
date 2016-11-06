using Rainfall.Redirection;
using ColossalFramework;
using ColossalFramework.Math;
using System;

using UnityEngine;

namespace Rainfall
{

    [TargetType(typeof(RoadBaseAI))]
    public class RoadBaseAIDetour:RoadBaseAI
    {
        [RedirectMethod]
        public override void SimulationStep(ushort segmentID, ref NetSegment data)
        {
            if ((data.m_flags & NetSegment.Flags.Original) == NetSegment.Flags.None)
            {
                NetManager netManager = Singleton<NetManager>.instance;
                Vector3 pos = netManager.m_nodes.m_buffer[(int)data.m_startNode].m_position;
                Vector3 pos2 = netManager.m_nodes.m_buffer[(int)data.m_endNode].m_position;
                int n = this.GetMaintenanceCost(pos, pos2);
                bool f = (ulong)(Singleton<SimulationManager>.instance.m_currentFrameIndex >> 8 & 15u) == (ulong)((long)(segmentID & 15));
                if (n != 0)
                {
                    if (f)
                    {
                        n = n * 16 / 100 - n / 100 * 15;
                    }
                    else {
                        n /= 100;
                    }
                    Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.Maintenance, n, this.m_info.m_class);
                }
                if (f)
                {
                    float n2 = (float)netManager.m_nodes.m_buffer[(int)data.m_startNode].m_elevation;
                    float n3 = (float)netManager.m_nodes.m_buffer[(int)data.m_endNode].m_elevation;
                    if (this.IsUnderground())
                    {
                        n2 = -n2;
                        n3 = -n3;
                    }
                    int constructionCost = this.GetConstructionCost(pos, pos2, n2, n3);
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

            SimulationManager instance = Singleton<SimulationManager>.instance;
            NetManager instance2 = Singleton<NetManager>.instance;
            Notification.Problem problem = Notification.RemoveProblems(data.m_problems, Notification.Problem.Flood | Notification.Problem.Snow);
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
            else {
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
            Vector3 vector = (position + position2) * 0.5f;
            bool flag = false;
            if ((this.m_info.m_setVehicleFlags & Vehicle.Flags.Underground) == 0)
            {
                float num6 = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(vector));
                // NON-STOCK CODE START
                if (num6 > vector.y + (float)ModSettings.RoadwayFloodedTolerance/100)
                {
                    flag = true;
                    data.m_flags |= NetSegment.Flags.Flooded;
                    problem = Notification.AddProblems(problem, Notification.Problem.Flood | Notification.Problem.MajorProblem);
                }
                else {
                    data.m_flags &= ~NetSegment.Flags.Flooded;
                   
                    // Rainfall compatibility
                    float add = (float)ModSettings.RoadwayFloodingTolerance/100; 
                 
                    if (num6 > vector.y + add)
                    {
                        flag = true;
                        problem = Notification.AddProblems(problem, Notification.Problem.Flood);
                    }
                }
                // NON-STOCK CODE END
            }
            DistrictManager instance3 = Singleton<DistrictManager>.instance;
            byte district = instance3.GetDistrict(vector);
            DistrictPolicies.CityPlanning cityPlanningPolicies = instance3.m_districts.m_buffer[(int)district].m_cityPlanningPolicies;
            int num7 = (int)(100 - (data.m_trafficDensity - 100) * (data.m_trafficDensity - 100) / 100);
            if ((this.m_info.m_vehicleTypes & VehicleInfo.VehicleType.Car) != VehicleInfo.VehicleType.None)
            {
                if ((this.m_info.m_setVehicleFlags & Vehicle.Flags.Underground) == 0)
                {
                    int num8 = (int)data.m_wetness;
                    if (!instance2.m_treatWetAsSnow)
                    {
                        if (flag)
                        {
                            num8 = 255;
                        }
                        else {
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
                        else {
                            float num12 = Singleton<WeatherManager>.instance.SampleRainIntensity(vector, false);
                            if (num12 != 0f)
                            {
                                int num13 = Mathf.RoundToInt(num12 * 400f);
                                int num14 = instance.m_randomizer.Int32(num13, num13 + 99) / 100;
                                if (Singleton<UnlockManager>.instance.Unlocked(UnlockManager.Feature.Snowplow))
                                {
                                    num8 = Mathf.Min(num8 + num14, 255);
                                }
                                else {
                                    num8 = Mathf.Min(num8 + num14, 128);
                                }
                            }
                            else if (Singleton<SimulationManager>.instance.m_randomizer.Int32(4u) == 0)
                            {
                                num8 = Mathf.Max(num8 - 1, 0);
                            }
                            if (num8 >= 64 && (data.m_flags & (NetSegment.Flags.Blocked | NetSegment.Flags.Flooded)) == NetSegment.Flags.None && instance.m_randomizer.Int32(10u) == 0)
                            {
                                TransferManager.TransferOffer offer = default(TransferManager.TransferOffer);
                                offer.Priority = num8 / 50;
                                offer.NetSegment = segmentID;
                                offer.Position = vector;
                                offer.Amount = 1;
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Snow, offer);
                            }
                            if (num8 >= 192)
                            {
                                problem = Notification.AddProblems(problem, Notification.Problem.Snow);
                            }
                            District[] expr_4E2_cp_0_cp_0 = instance3.m_districts.m_buffer;
                            byte expr_4E2_cp_0_cp_1 = district;
                            expr_4E2_cp_0_cp_0[(int)expr_4E2_cp_0_cp_1].m_productionData.m_tempSnowCover = expr_4E2_cp_0_cp_0[(int)expr_4E2_cp_0_cp_1].m_productionData.m_tempSnowCover + (uint)num8;
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
                        else {
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
                else {
                    num15 = Mathf.Min(500, (int)(50 + data.m_trafficDensity * 4));
                }
                if (!this.m_highwayRules)
                {
                    int num16 = instance.m_randomizer.Int32(num15, num15 + 99) / 100;
                    data.m_condition = (byte)Mathf.Max((int)data.m_condition - num16, 0);
                    if (data.m_condition < 192 && (data.m_flags & (NetSegment.Flags.Blocked | NetSegment.Flags.Flooded)) == NetSegment.Flags.None && instance.m_randomizer.Int32(20u) == 0)
                    {
                        TransferManager.TransferOffer offer2 = default(TransferManager.TransferOffer);
                        offer2.Priority = (int)((255 - data.m_condition) / 50);
                        offer2.NetSegment = segmentID;
                        offer2.Position = vector;
                        offer2.Amount = 1;
                        Singleton<TransferManager>.instance.AddIncomingOffer(TransferManager.TransferReason.RoadMaintenance, offer2);
                    }
                }
            }
            if (!this.m_highwayRules)
            {
                if ((cityPlanningPolicies & DistrictPolicies.CityPlanning.HeavyTrafficBan) != DistrictPolicies.CityPlanning.None)
                {
                    data.m_flags |= NetSegment.Flags.HeavyBan;
                }
                else {
                    data.m_flags &= ~NetSegment.Flags.HeavyBan;
                }
                if ((cityPlanningPolicies & DistrictPolicies.CityPlanning.BikeBan) != DistrictPolicies.CityPlanning.None)
                {
                    data.m_flags |= NetSegment.Flags.BikeBan;
                }
                else {
                    data.m_flags &= ~NetSegment.Flags.BikeBan;
                }
                if ((cityPlanningPolicies & DistrictPolicies.CityPlanning.OldTown) != DistrictPolicies.CityPlanning.None)
                {
                    data.m_flags |= NetSegment.Flags.CarBan;
                }
                else {
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
                    Singleton<NetManager>.instance.m_shortRoadTraffic.Activate(properties.m_shortRoadTraffic, segmentID);
                }
            }
            data.m_problems = problem;
        }

    }
}
