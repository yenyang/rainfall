using Rainfall.Redirection.Attributes;
using ColossalFramework;
using ICities;
using ColossalFramework.Math;
using UnityEngine;

namespace Rainfall
{

    [TargetType(typeof(CommonBuildingAI))]
    internal class CommonBuildingAIDetour : CommonBuildingAI
    {
        [RedirectMethod]
        new protected int HandleCommonConsumption(ushort buildingID, ref Building data, ref Building.Frame frameData, ref int electricityConsumption, ref int heatingConsumption, ref int waterConsumption, ref int sewageAccumulation, ref int garbageAccumulation, DistrictPolicies.Services policies)
        {
            int num = 100;
            DistrictManager instance = Singleton<DistrictManager>.instance;
            Notification.Problem problem = Notification.RemoveProblems(data.m_problems, Notification.Problem.Electricity | Notification.Problem.Water | Notification.Problem.Sewage | Notification.Problem.Flood | Notification.Problem.Heating);
            bool flag = data.m_electricityProblemTimer != 0;
            bool flag2 = false;
            bool flag3 = false;
            int electricityUsage = 0;
            int heatingUsage = 0;
            int waterUsage = 0;
            int sewageUsage = 0;
            if (electricityConsumption != 0)
            {
                int num2 = Mathf.RoundToInt((20f - Singleton<WeatherManager>.instance.SampleTemperature(data.m_position, false)) * 8f);
                num2 = Mathf.Clamp(num2, 0, 400);
                int num3 = heatingConsumption;
                heatingConsumption = (num3 * num2 + Singleton<SimulationManager>.instance.m_randomizer.Int32(100u)) / 100;
                if ((policies & DistrictPolicies.Services.PowerSaving) != DistrictPolicies.Services.None)
                {
                    electricityConsumption = Mathf.Max(1, electricityConsumption * 90 / 100);
                    Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, 32, this.m_info.m_class);
                }
                bool flag4 = false;
                int num4 = heatingConsumption * 2 - (int)data.m_heatingBuffer;
                if (num4 > 0 && (policies & DistrictPolicies.Services.OnlyElectricity) == DistrictPolicies.Services.None)
                {
                    int num5 = Singleton<WaterManager>.instance.TryFetchHeating(data.m_position, heatingConsumption, num4, out flag4);
                    data.m_heatingBuffer += (ushort)num5;
                }
                if ((int)data.m_heatingBuffer < heatingConsumption)
                {
                    if ((policies & DistrictPolicies.Services.NoElectricity) != DistrictPolicies.Services.None)
                    {
                        flag3 = true;
                        data.m_heatingProblemTimer = (byte)Mathf.Min(255, (int)(data.m_heatingProblemTimer + 1));
                        if (data.m_heatingProblemTimer >= 65)
                        {
                            num = 0;
                            problem = Notification.AddProblems(problem, Notification.Problem.Heating | Notification.Problem.MajorProblem);
                        }
                        else if (data.m_heatingProblemTimer >= 3)
                        {
                            num /= 2;
                            problem = Notification.AddProblems(problem, Notification.Problem.Heating);
                        }
                    }
                    else
                    {
                        num2 = ((num2 + 50) * (heatingConsumption - (int)data.m_heatingBuffer) + heatingConsumption - 1) / heatingConsumption;
                        electricityConsumption += (num3 * num2 + Singleton<SimulationManager>.instance.m_randomizer.Int32(100u)) / 100;
                        if (flag4)
                        {
                            flag3 = true;
                            data.m_heatingProblemTimer = (byte)Mathf.Min(255, (int)(data.m_heatingProblemTimer + 1));
                            if (data.m_heatingProblemTimer >= 3)
                            {
                                problem = Notification.AddProblems(problem, Notification.Problem.Heating);
                            }
                        }
                    }
                    heatingUsage = (int)data.m_heatingBuffer;
                    data.m_heatingBuffer = 0;
                }
                else
                {
                    heatingUsage = heatingConsumption;
                    data.m_heatingBuffer -= (ushort)heatingConsumption;
                }
                int num6;
                int a;
                if (this.CanStockpileElectricity(buildingID, ref data, out num6, out a))
                {
                    num4 = num6 + electricityConsumption * 2 - (int)data.m_electricityBuffer;
                    if (num4 > 0)
                    {
                        int num7 = electricityConsumption;
                        if ((int)data.m_electricityBuffer < num6)
                        {
                            num7 += Mathf.Min(a, num6 - (int)data.m_electricityBuffer);
                        }
                        int num8 = Singleton<ElectricityManager>.instance.TryFetchElectricity(data.m_position, num7, num4);
                        data.m_electricityBuffer += (ushort)num8;
                        if (num8 < num4 && num8 < num7)
                        {
                            flag2 = true;
                            problem = Notification.AddProblems(problem, Notification.Problem.Electricity);
                            if (data.m_electricityProblemTimer < 64)
                            {
                                data.m_electricityProblemTimer = 64;
                            }
                        }
                    }
                }
                else
                {
                    num4 = electricityConsumption * 2 - (int)data.m_electricityBuffer;
                    if (num4 > 0)
                    {
                        int num9 = Singleton<ElectricityManager>.instance.TryFetchElectricity(data.m_position, electricityConsumption, num4);
                        data.m_electricityBuffer += (ushort)num9;
                    }
                }
                if ((int)data.m_electricityBuffer < electricityConsumption)
                {
                    flag2 = true;
                    data.m_electricityProblemTimer = (byte)Mathf.Min(255, (int)(data.m_electricityProblemTimer + 1));
                    if (data.m_electricityProblemTimer >= 65)
                    {
                        num = 0;
                        problem = Notification.AddProblems(problem, Notification.Problem.Electricity | Notification.Problem.MajorProblem);
                    }
                    else if (data.m_electricityProblemTimer >= 3)
                    {
                        num /= 2;
                        problem = Notification.AddProblems(problem, Notification.Problem.Electricity);
                    }
                    electricityUsage = (int)data.m_electricityBuffer;
                    data.m_electricityBuffer = 0;
                    if (Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.Electricity))
                    {
                        GuideController properties = Singleton<GuideManager>.instance.m_properties;
                        if (properties != null)
                        {
                            int publicServiceIndex = ItemClass.GetPublicServiceIndex(ItemClass.Service.Electricity);
                            int electricityCapacity = instance.m_districts.m_buffer[0].GetElectricityCapacity();
                            int electricityConsumption2 = instance.m_districts.m_buffer[0].GetElectricityConsumption();
                            if (electricityCapacity >= electricityConsumption2)
                            {
                                Singleton<GuideManager>.instance.m_serviceNeeded[publicServiceIndex].Activate(properties.m_serviceNeeded2, ItemClass.Service.Electricity);
                            }
                            else
                            {
                                Singleton<GuideManager>.instance.m_serviceNeeded[publicServiceIndex].Activate(properties.m_serviceNeeded, ItemClass.Service.Electricity);
                            }
                        }
                    }
                }
                else
                {
                    electricityUsage = electricityConsumption;
                    data.m_electricityBuffer -= (ushort)electricityConsumption;
                }
            }
            else
            {
                heatingConsumption = 0;
            }
            if (!flag2)
            {
                data.m_electricityProblemTimer = 0;
            }
            if (flag != flag2)
            {
                Singleton<BuildingManager>.instance.UpdateBuildingColors(buildingID);
            }
            if (!flag3)
            {
                data.m_heatingProblemTimer = 0;
            }
            bool flag5 = false;
            int num10 = sewageAccumulation;
            if (waterConsumption != 0)
            {
                if ((policies & DistrictPolicies.Services.WaterSaving) != DistrictPolicies.Services.None)
                {
                    waterConsumption = Mathf.Max(1, waterConsumption * 85 / 100);
                    if (sewageAccumulation != 0)
                    {
                        sewageAccumulation = Mathf.Max(1, sewageAccumulation * 85 / 100);
                    }
                    Singleton<EconomyManager>.instance.FetchResource(EconomyManager.Resource.PolicyCost, 32, this.m_info.m_class);
                }
                int num11;
                int a2;
                if (this.CanStockpileWater(buildingID, ref data, out num11, out a2))
                {
                    int num12 = num11 + waterConsumption * 2 - (int)data.m_waterBuffer;
                    if (num12 > 0)
                    {
                        int num13 = waterConsumption;
                        if ((int)data.m_waterBuffer < num11)
                        {
                            num13 += Mathf.Min(a2, num11 - (int)data.m_waterBuffer);
                        }
                        int num14 = Singleton<WaterManager>.instance.TryFetchWater(data.m_position, num13, num12, ref data.m_waterPollution);
                        data.m_waterBuffer += (ushort)num14;
                        if (num14 < num12 && num14 < num13)
                        {
                            flag5 = true;
                            problem = Notification.AddProblems(problem, Notification.Problem.Water);
                            if (data.m_waterProblemTimer < 64)
                            {
                                data.m_waterProblemTimer = 64;
                            }
                        }
                    }
                }
                else
                {
                    int num15 = waterConsumption * 2 - (int)data.m_waterBuffer;
                    if (num15 > 0)
                    {
                        int num16 = Singleton<WaterManager>.instance.TryFetchWater(data.m_position, waterConsumption, num15, ref data.m_waterPollution);
                        data.m_waterBuffer += (ushort)num16;
                    }
                }
                if ((int)data.m_waterBuffer < waterConsumption)
                {
                    flag5 = true;
                    data.m_waterProblemTimer = (byte)Mathf.Min(255, (int)(data.m_waterProblemTimer + 1));
                    if (data.m_waterProblemTimer >= 65)
                    {
                        num = 0;
                        problem = Notification.AddProblems(problem, Notification.Problem.Water | Notification.Problem.MajorProblem);
                    }
                    else if (data.m_waterProblemTimer >= 3)
                    {
                        num /= 2;
                        problem = Notification.AddProblems(problem, Notification.Problem.Water);
                    }
                    num10 = sewageAccumulation * (waterConsumption + (int)data.m_waterBuffer) / (waterConsumption << 1);
                    waterUsage = (int)data.m_waterBuffer;
                    data.m_waterBuffer = 0;
                    if (Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.Water))
                    {
                        GuideController properties2 = Singleton<GuideManager>.instance.m_properties;
                        if (properties2 != null)
                        {
                            int publicServiceIndex2 = ItemClass.GetPublicServiceIndex(ItemClass.Service.Water);
                            int waterCapacity = instance.m_districts.m_buffer[0].GetWaterCapacity();
                            int waterConsumption2 = instance.m_districts.m_buffer[0].GetWaterConsumption();
                            if (waterCapacity >= waterConsumption2)
                            {
                                Singleton<GuideManager>.instance.m_serviceNeeded[publicServiceIndex2].Activate(properties2.m_serviceNeeded2, ItemClass.Service.Water);
                            }
                            else
                            {
                                Singleton<GuideManager>.instance.m_serviceNeeded[publicServiceIndex2].Activate(properties2.m_serviceNeeded, ItemClass.Service.Water);
                            }
                        }
                    }
                }
                else
                {
                    num10 = sewageAccumulation;
                    waterUsage = waterConsumption;
                    data.m_waterBuffer -= (ushort)waterConsumption;
                }
            }
            int num17;
            int b;
            if (this.CanStockpileWater(buildingID, ref data, out num17, out b))
            {
                int num18 = Mathf.Max(0, num17 + num10 * 2 - (int)data.m_sewageBuffer);
                if (num18 < num10)
                {
                    if (!flag5 && (data.m_problems & Notification.Problem.Water) == Notification.Problem.None)
                    {
                        flag5 = true;
                        data.m_waterProblemTimer = (byte)Mathf.Min(255, (int)(data.m_waterProblemTimer + 1));
                        if (data.m_waterProblemTimer >= 65)
                        {
                            num = 0;
                            problem = Notification.AddProblems(problem, Notification.Problem.Sewage | Notification.Problem.MajorProblem);
                        }
                        else if (data.m_waterProblemTimer >= 3)
                        {
                            num /= 2;
                            problem = Notification.AddProblems(problem, Notification.Problem.Sewage);
                        }
                    }
                    sewageUsage = num18;
                    data.m_sewageBuffer = (ushort)(num17 + num10 * 2);
                }
                else
                {
                    sewageUsage = num10;
                    data.m_sewageBuffer += (ushort)num10;
                }
                int num19 = num10 + Mathf.Max(num10, b);
                num18 = Mathf.Min(num19, (int)data.m_sewageBuffer);
                if (num18 > 0)
                {
                    int num20 = Singleton<WaterManager>.instance.TryDumpSewage(data.m_position, num19, num18);
                    data.m_sewageBuffer -= (ushort)num20;
                    if (num20 < num19 && num20 < num18 && !flag5 && (data.m_problems & Notification.Problem.Water) == Notification.Problem.None)
                    {
                        flag5 = true;
                        problem = Notification.AddProblems(problem, Notification.Problem.Sewage);
                        if (data.m_waterProblemTimer < 64)
                        {
                            data.m_waterProblemTimer = 64;
                        }
                    }
                }
            }
            else if (num10 != 0)
            {
                int num21 = Mathf.Max(0, num10 * 2 - (int)data.m_sewageBuffer);
                if (num21 < num10)
                {
                    if (!flag5 && (data.m_problems & Notification.Problem.Water) == Notification.Problem.None)
                    {
                        flag5 = true;
                        data.m_waterProblemTimer = (byte)Mathf.Min(255, (int)(data.m_waterProblemTimer + 1));
                        if (data.m_waterProblemTimer >= 65)
                        {
                            num = 0;
                            problem = Notification.AddProblems(problem, Notification.Problem.Sewage | Notification.Problem.MajorProblem);
                        }
                        else if (data.m_waterProblemTimer >= 3)
                        {
                            num /= 2;
                            problem = Notification.AddProblems(problem, Notification.Problem.Sewage);
                        }
                    }
                    sewageUsage = num21;
                    data.m_sewageBuffer = (ushort)(num10 * 2);
                }
                else
                {
                    sewageUsage = num10;
                    data.m_sewageBuffer += (ushort)num10;
                }
                num21 = Mathf.Min(num10 * 2, (int)data.m_sewageBuffer);
                if (num21 > 0)
                {
                    int num22 = Singleton<WaterManager>.instance.TryDumpSewage(data.m_position, num10 * 2, num21);
                    data.m_sewageBuffer -= (ushort)num22;
                }
            }
            if (!flag5)
            {
                data.m_waterProblemTimer = 0;
            }
            if (garbageAccumulation != 0)
            {
                int num23 = (int)(65535 - data.m_garbageBuffer);
                if (num23 < garbageAccumulation)
                {
                    num = 0;
                    data.m_garbageBuffer = (ushort)num23;
                }
                else
                {
                    //start edit
                    StormDrainAI stormDrainAI = data.Info.m_buildingAI as StormDrainAI;
                    if (stormDrainAI == null)
                        data.m_garbageBuffer += (ushort)garbageAccumulation;
                    else if (stormDrainAI.m_filter == false)
                        data.m_garbageBuffer += (ushort)garbageAccumulation;
                    else
                    {
                        int pollutantAccumulation = Hydraulics.removePollutants(buildingID, Hydraulics.getPollutants(buildingID));
                        data.m_garbageBuffer += (ushort)pollutantAccumulation;
                        //Debug.Log("[RF]CommonBuildingAI.handleCommonConsumption garbagebuffer = " + data.m_garbageBuffer.ToString());
                    }
                    //end edit
                }
            }
            if (garbageAccumulation != 0)
            {
                int num24 = (int)data.m_garbageBuffer;
                if (num24 >= 200 && Singleton<SimulationManager>.instance.m_randomizer.Int32(5u) == 0 && Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.Garbage))
                {
                    int num25 = 0;
                    int num26 = 0;
                    int num27 = 0;
                    int num28 = 0;
                    this.CalculateGuestVehicles(buildingID, ref data, TransferManager.TransferReason.Garbage, ref num25, ref num26, ref num27, ref num28);
                    num24 -= num27 - num26;
                    if (num24 >= 200)
                    {
                        TransferManager.TransferOffer offer = default(TransferManager.TransferOffer);
                        offer.Priority = num24 / 1000;
                        offer.Building = buildingID;
                        offer.Position = data.m_position;
                        offer.Amount = 1;
                        Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Garbage, offer);
                    }
                }
            }
            bool flag6;
            if (this.CanSufferFromFlood(out flag6))
            {
                float num29 = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(data.m_position));
                if (num29 > data.m_position.y)
                {
                    bool flag7 = num29 > data.m_position.y + Mathf.Max(4f, this.m_info.m_collisionHeight);
                    if ((!flag6 || flag7) && (data.m_flags & Building.Flags.Flooded) == Building.Flags.None && data.m_fireIntensity == 0)
                    {
                        DisasterManager instance2 = Singleton<DisasterManager>.instance;
                        ushort num30 = instance2.FindDisaster<FloodBaseAI>(data.m_position);
                        if (num30 == 0)
                        {
                            DisasterInfo disasterInfo = DisasterManager.FindDisasterInfo<GenericFloodAI>();
                            if (disasterInfo != null && instance2.CreateDisaster(out num30, disasterInfo))
                            {
                                instance2.m_disasters.m_buffer[(int)num30].m_intensity = 10;
                                instance2.m_disasters.m_buffer[(int)num30].m_targetPosition = data.m_position;
                                disasterInfo.m_disasterAI.StartNow(num30, ref instance2.m_disasters.m_buffer[(int)num30]);
                            }
                        }
                        if (num30 != 0)
                        {
                            InstanceID srcID = default(InstanceID);
                            InstanceID dstID = default(InstanceID);
                            srcID.Disaster = num30;
                            dstID.Building = buildingID;
                            Singleton<InstanceManager>.instance.CopyGroup(srcID, dstID);
                            DisasterInfo info = instance2.m_disasters.m_buffer[(int)num30].Info;
                            info.m_disasterAI.ActivateNow(num30, ref instance2.m_disasters.m_buffer[(int)num30]);
                            if ((instance2.m_disasters.m_buffer[(int)num30].m_flags & DisasterData.Flags.Significant) != DisasterData.Flags.None)
                            {
                                instance2.DetectDisaster(num30, false);
                                instance2.FollowDisaster(num30);
                            }
                        }
                        data.m_flags |= Building.Flags.Flooded;
                    }
                    if (flag7)
                    {
                        frameData.m_constructState = (byte)Mathf.Max(0, (int)frameData.m_constructState - 1088 / this.GetCollapseTime());
                        data.SetFrameData(Singleton<SimulationManager>.instance.m_currentFrameIndex, frameData);
                        InstanceID id = default(InstanceID);
                        id.Building = buildingID;
                        InstanceManager.Group group = Singleton<InstanceManager>.instance.GetGroup(id);
                        if (group != null)
                        {
                            ushort disaster = group.m_ownerInstance.Disaster;
                            if (disaster != 0)
                            {
                                DisasterData[] expr_D18_cp_0 = Singleton<DisasterManager>.instance.m_disasters.m_buffer;
                                ushort expr_D18_cp_1 = disaster;
                                expr_D18_cp_0[(int)expr_D18_cp_1].m_collapsedCount = (ushort)(expr_D18_cp_0[(int)expr_D18_cp_1].m_collapsedCount + 1);
                            }
                        }
                        if (frameData.m_constructState == 0)
                        {
                            Singleton<InstanceManager>.instance.SetGroup(id, null);
                        }
                        data.m_levelUpProgress = 0;
                        data.m_fireIntensity = 0;
                        data.m_garbageBuffer = 0;
                        data.m_flags |= Building.Flags.Collapsed;
                        num = 0;
                        this.RemovePeople(buildingID, ref data, 90);
                        this.BuildingDeactivated(buildingID, ref data);
                        if (this.m_info.m_hasParkingSpaces != VehicleInfo.VehicleType.None)
                        {
                            Singleton<BuildingManager>.instance.UpdateParkingSpaces(buildingID, ref data);
                        }
                        Singleton<BuildingManager>.instance.UpdateBuildingRenderer(buildingID, true);
                        Singleton<BuildingManager>.instance.UpdateBuildingColors(buildingID);
                        GuideController properties3 = Singleton<GuideManager>.instance.m_properties;
                        if (properties3 != null)
                        {
                            Singleton<BuildingManager>.instance.m_buildingFlooded.Deactivate(buildingID, false);
                            Singleton<BuildingManager>.instance.m_buildingFlooded2.Deactivate(buildingID, false);
                        }
                    }
                    else if (!flag6)
                    {
                        if ((data.m_flags & Building.Flags.RoadAccessFailed) == Building.Flags.None)
                        {
                            int num31 = 0;
                            int num32 = 0;
                            int num33 = 0;
                            int num34 = 0;
                            this.CalculateGuestVehicles(buildingID, ref data, TransferManager.TransferReason.FloodWater, ref num31, ref num32, ref num33, ref num34);
                            if (num31 == 0)
                            {
                                TransferManager.TransferOffer offer2 = default(TransferManager.TransferOffer);
                                offer2.Priority = 5;
                                offer2.Building = buildingID;
                                offer2.Position = data.m_position;
                                offer2.Amount = 1;
                                Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.FloodWater, offer2);
                            }
                        }
                        if (num29 > data.m_position.y + (float)ModSettings.BuildingFloodedTolerance / 100f)
                        {
                            num = 0;
                            problem = Notification.AddProblems(problem, Notification.Problem.Flood | Notification.Problem.MajorProblem);
                        }
                        else if (num29 > data.m_position.y + (float)ModSettings.BuildingFloodingTolerance / 100f)
                        {
                            num /= 2;
                            problem = Notification.AddProblems(problem, Notification.Problem.Flood);
                        }
                        GuideController properties4 = Singleton<GuideManager>.instance.m_properties;
                        if (properties4 != null)
                        {
                            if (Singleton<LoadingManager>.instance.SupportsExpansion(Expansion.NaturalDisasters) && Singleton<UnlockManager>.instance.Unlocked(UnlockManager.Feature.WaterPumping))
                            {
                                Singleton<BuildingManager>.instance.m_buildingFlooded2.Activate(properties4.m_buildingFlooded2, buildingID);
                            }
                            else
                            {
                                Singleton<BuildingManager>.instance.m_buildingFlooded.Activate(properties4.m_buildingFlooded, buildingID);
                            }
                        }
                    }
                }
                else if ((data.m_flags & Building.Flags.Flooded) != Building.Flags.None)
                {
                    InstanceID id2 = default(InstanceID);
                    id2.Building = buildingID;
                    Singleton<InstanceManager>.instance.SetGroup(id2, null);
                    data.m_flags &= ~Building.Flags.Flooded;
                }
            }
            byte district = instance.GetDistrict(data.m_position);
            instance.m_districts.m_buffer[(int)district].AddUsageData(electricityUsage, heatingUsage, waterUsage, sewageUsage);
            data.m_problems = problem;
            return num;
        }
    }
}
