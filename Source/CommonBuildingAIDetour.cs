using Rainfall.Redirection;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace Rainfall
{

    [TargetType(typeof(CommonBuildingAI))]
    internal class CommonBuildingAIDetour:CommonBuildingAI
    {
        [RedirectMethod]
        new int HandleCommonConsumption(ushort buildingID, ref Building data, ref int electricityConsumption, ref int heatingConsumption, ref int waterConsumption, ref int sewageAccumulation, ref int garbageAccumulation, DistrictPolicies.Services policies)
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
                num4 = electricityConsumption * 2 - (int)data.m_electricityBuffer;
                if (num4 > 0)
                {
                    int num6 = Singleton<ElectricityManager>.instance.TryFetchElectricity(data.m_position, electricityConsumption, num4);
                    data.m_electricityBuffer += (ushort)num6;
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
            int num7 = sewageAccumulation;
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
                int num8 = waterConsumption * 2 - (int)data.m_waterBuffer;
                if (num8 > 0)
                {
                    int num9 = Singleton<WaterManager>.instance.TryFetchWater(data.m_position, waterConsumption, num8, ref data.m_waterPollution);
                    data.m_waterBuffer += (ushort)num9;
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
                    num7 = sewageAccumulation * (waterConsumption + (int)data.m_waterBuffer) / (waterConsumption << 1);
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
                    num7 = sewageAccumulation;
                    waterUsage = waterConsumption;
                    data.m_waterBuffer -= (ushort)waterConsumption;
                }
            }
            if (num7 != 0)
            {
                int num10 = num7 * 2 - (int)data.m_sewageBuffer;
                if (num10 < num7)
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
                    sewageUsage = num10;
                    data.m_sewageBuffer = (ushort)(num7 * 2);
                }
                else
                {
                    sewageUsage = num7;
                    data.m_sewageBuffer += (ushort)num7;
                }
            }
            if (!flag5)
            {
                data.m_waterProblemTimer = 0;
            }
            if (garbageAccumulation != 0)
            {
                int num11 = (int)(65535 - data.m_garbageBuffer);
                if (num11 < garbageAccumulation)
                {
                    num = 0;
                    data.m_garbageBuffer = (ushort)num11;
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
            if (num7 != 0)
            {
                int num12 = Mathf.Min(num7 * 2, (int)data.m_sewageBuffer);
                if (num12 > 0)
                {
                    int num13 = Singleton<WaterManager>.instance.TryDumpSewage(data.m_position, num7 * 2, num12);
                    data.m_sewageBuffer -= (ushort)num13;
                }
            }
            if (garbageAccumulation != 0)
            {
                int num14 = (int)data.m_garbageBuffer;
                if (num14 >= 200 && Singleton<SimulationManager>.instance.m_randomizer.Int32(5u) == 0 && Singleton<UnlockManager>.instance.Unlocked(ItemClass.Service.Garbage))
                {
                    int num15 = 0;
                    int num16 = 0;
                    int num17 = 0;
                    int num18 = 0;
                    this.CalculateGuestVehicles(buildingID, ref data, TransferManager.TransferReason.Garbage, ref num15, ref num16, ref num17, ref num18);
                    num14 -= num17 - num16;
                    if (num14 >= 200)
                    {
                        TransferManager.TransferOffer offer = default(TransferManager.TransferOffer);
                        offer.Priority = num14 / 1000;
                        offer.Building = buildingID;
                        offer.Position = data.m_position;
                        offer.Amount = 1;
                        Singleton<TransferManager>.instance.AddOutgoingOffer(TransferManager.TransferReason.Garbage, offer);
                    }
                }
            }
            if (this.CanSufferFromFlood())
            {
                float num19 = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(data.m_position));
                if (num19 > data.m_position.y)
                {
                   
                    if (num19 > data.m_position.y + (float)ModSettings.BuildingFloodedTolerance/100f)
                    {
                        //Debug.Log("[RF].CBAId Detoured Flooded Notification");
                        num = 0;
                        problem = Notification.AddProblems(problem, Notification.Problem.Flood | Notification.Problem.MajorProblem);
                    }
                    else if (num19 > data.m_position.y + (float)ModSettings.BuildingFloodingTolerance/100f)
                    {
                        //Debug.Log("[RF].CBAId Detoured Flooding Notification");
                        num /= 2;
                        problem = Notification.AddProblems(problem, Notification.Problem.Flood);
                        GuideController properties3 = Singleton<GuideManager>.instance.m_properties;
                        if (properties3 != null)
                        {
                            Singleton<BuildingManager>.instance.m_buildingFlooded.Activate(properties3.m_buildingFlooded, buildingID);
                        }
                    }
                }
                //Debug.Log("[RF].CBAId Detouring was sucessful!");
            }
            
            byte district = instance.GetDistrict(data.m_position);
            instance.m_districts.m_buffer[(int)district].AddUsageData(electricityUsage, heatingUsage, waterUsage, sewageUsage);
            data.m_problems = problem;
            return num;
        }
        

    }
}
