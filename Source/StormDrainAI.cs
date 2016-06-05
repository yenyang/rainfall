using ColossalFramework;
using ColossalFramework.DataBinding;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using System.Text;
using System;

using UnityEngine;

namespace Rainfall
{
    public class StormDrainAI : PlayerBuildingAI
    {
        [CustomizableProperty("Uneducated Workers", "Workers", 0)]
        public int m_workPlaceCount0 = 10;

        [CustomizableProperty("Educated Workers", "Workers", 1)]
        public int m_workPlaceCount1 = 10;

        [CustomizableProperty("Well Educated Workers", "Workers", 2)]
        public int m_workPlaceCount2 = 10;

        [CustomizableProperty("Highly Educated Workers", "Workers", 3)]
        public int m_workPlaceCount3 = 10;

        [CustomizableProperty("Storm Water Intake", "Water")]
        public int m_stormWaterIntake = 1000;

        [CustomizableProperty("Storm Water Outlet", "Water")]
        public int m_stormWaterOutlet = 1000;

        [CustomizableProperty("Storm Water Detention", "Water")]
        public int m_stormWaterDetention = 1000;

        [CustomizableProperty("Storm Water Infiltration", "Water")]
        public int m_stormWaterInfiltration = 1000;

        public Vector3 m_waterLocationOffset = Vector3.zero;

        [CustomizableProperty("Max Water Placement Distance", "Water")]
        public float m_maxWaterDistance = 100f;

        [CustomizableProperty("Water Effect Distance", "Water")]
        public float m_waterEffectDistance = 100f;

        [CustomizableProperty("Outlet Pollution", "Pollution")]
        public int m_outletPollution = 100;

        [CustomizableProperty("Noise Accumulation", "Pollution")]
        public int m_noiseAccumulation = 50;

        [CustomizableProperty("Noise Radius", "Pollution")]
        public float m_noiseRadius = 100f;

        public override void GetNaturalResourceRadius(ushort buildingID, ref Building data, out NaturalResourceManager.Resource resource1, out Vector3 position1, out float radius1, out NaturalResourceManager.Resource resource2, out Vector3 position2, out float radius2)
        {
            resource1 = NaturalResourceManager.Resource.Water;
            position1 = data.CalculatePosition(this.m_waterLocationOffset);
            radius1 = this.m_maxWaterDistance;
            resource2 = NaturalResourceManager.Resource.None;
            position2 = data.m_position;
            radius2 = 0f;
        }

        public override void GetImmaterialResourceRadius(ushort buildingID, ref Building data, out ImmaterialResourceManager.Resource resource1, out float radius1, out ImmaterialResourceManager.Resource resource2, out float radius2)
        {
            if (this.m_noiseAccumulation != 0)
            {
                resource1 = ImmaterialResourceManager.Resource.NoisePollution;
                radius1 = this.m_noiseRadius;
            }
            else
            {
                resource1 = ImmaterialResourceManager.Resource.None;
                radius1 = 0f;
            }
            resource2 = ImmaterialResourceManager.Resource.None;
            radius2 = 0f;
        }

        public override Color GetColor(ushort buildingID, ref Building data, InfoManager.InfoMode infoMode)
        {
            if (infoMode == InfoManager.InfoMode.Water)
            {
                if ((data.m_flags & Building.Flags.Active) == Building.Flags.None)
                {
                    return Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_inactiveColor;
                }
                if (this.m_stormWaterIntake != 0)
                {
                    return Color.magenta;
                }
                if (this.m_stormWaterOutlet != 0)
                {
                    return Color.red;
                }
                if (this.m_stormWaterDetention != 0)
                {
                    return Color.green;
                }
                return base.GetColor(buildingID, ref data, infoMode);
            }
            else
            {
                if (infoMode == InfoManager.InfoMode.NoisePollution)
                {
                    int noiseAccumulation = this.m_noiseAccumulation;
                    return CommonBuildingAI.GetNoisePollutionColor((float)noiseAccumulation);
                }
                if (infoMode == InfoManager.InfoMode.TerrainHeight)
                {
                    if ((data.m_flags & Building.Flags.Active) == Building.Flags.None)
                    {
                        return Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_inactiveColor;
                    }
                    if (this.m_stormWaterIntake != 0)
                    {
                        return Color.magenta;
                    }
                }
                if (infoMode != InfoManager.InfoMode.Pollution)
                {
                    return base.GetColor(buildingID, ref data, infoMode);
                }
                if (this.m_stormWaterIntake != 0)
                {
                    float t = Mathf.Clamp01((float)data.m_waterPollution * 0.0117647061f);
                    return ColorUtils.LinearLerp(Singleton<InfoManager>.instance.m_properties.m_neutralColor, Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_activeColor, t);
                }
                if (this.m_stormWaterOutlet != 0)
                {
                    return Color.red;
                }
                return base.GetColor(buildingID, ref data, infoMode);
            }
        }

        public override int GetResourceRate(ushort buildingID, ref Building data, ImmaterialResourceManager.Resource resource)
        {
            if (resource == ImmaterialResourceManager.Resource.NoisePollution)
            {
                return this.m_noiseAccumulation;
            }
            return base.GetResourceRate(buildingID, ref data, resource);
        }

        public override int GetWaterRate(ushort buildingID, ref Building data)
        {
            int productionRate = (int)data.m_productionRate;
            int budget = Singleton<EconomyManager>.instance.GetBudget(this.m_info.m_class);
            productionRate = PlayerBuildingAI.GetProductionRate(productionRate, budget);
            if (this.m_stormWaterDetention != 0)
            {
                return productionRate * (this.m_stormWaterDetention) / 100;
            }
            return productionRate * (this.m_stormWaterIntake - this.m_stormWaterOutlet) / 100;
        }
      
        public override void GetPlacementInfoMode(out InfoManager.InfoMode mode, out InfoManager.SubInfoMode subMode, float elevation)
        {
            if (this.m_stormWaterIntake > 0 && this.m_electricityConsumption == 0)
            {
                mode = InfoManager.InfoMode.TerrainHeight;
                subMode = InfoManager.SubInfoMode.NormalWater;
            }
            else
            {
                mode = InfoManager.InfoMode.Water;
                subMode = InfoManager.SubInfoMode.WaterPower;
            }


        }

        public override void CreateBuilding(ushort buildingID, ref Building data)
        {
            base.CreateBuilding(buildingID, ref data);
            int workCount = this.m_workPlaceCount0 + this.m_workPlaceCount1 + this.m_workPlaceCount2 + this.m_workPlaceCount3;
            Singleton<CitizenManager>.instance.CreateUnits(out data.m_citizenUnits, ref Singleton<SimulationManager>.instance.m_randomizer, buildingID, 0, 0, workCount, 0, 0, 0);
        }

        public override void ReleaseBuilding(ushort buildingID, ref Building data)
        {
            base.ReleaseBuilding(buildingID, ref data);
        }

        protected override void ManualActivation(ushort buildingID, ref Building buildingData)
        {
            Vector3 position = buildingData.m_position;
            position.y += this.m_info.m_size.y;
            Singleton<NotificationManager>.instance.AddEvent(NotificationEvent.Type.GainWater, position, 1.5f);
            if (this.m_noiseAccumulation != 0)
            {
                Singleton<NotificationManager>.instance.AddWaveEvent(buildingData.m_position, NotificationEvent.Type.Sad, ImmaterialResourceManager.Resource.NoisePollution, (float)this.m_noiseAccumulation, this.m_noiseRadius);
            }
        }

        protected override void ManualDeactivation(ushort buildingID, ref Building buildingData)
        {
            Vector3 position = buildingData.m_position;
            position.y += this.m_info.m_size.y;
            Singleton<NotificationManager>.instance.AddEvent(NotificationEvent.Type.LoseWater, position, 1.5f);
            if (this.m_noiseAccumulation != 0)
            {
                Singleton<NotificationManager>.instance.AddWaveEvent(buildingData.m_position, NotificationEvent.Type.Happy, ImmaterialResourceManager.Resource.NoisePollution, (float)(-(float)this.m_noiseAccumulation), this.m_noiseRadius);
            }
        }

        public override ToolBase.ToolErrors CheckBuildPosition(ushort relocateID, ref Vector3 position, ref float angle, float waterHeight, float elevation, ref Segment3 connectionSegment, out int productionRate, out int constructionCost)
        {
            if (this.m_stormWaterDetention != 0 || this.m_stormWaterIntake > 0 && this.m_electricityConsumption == 0 || this.m_stormWaterOutlet > 0)
            {
                bool flag;
                this.GetConstructionCost(relocateID != 0, out constructionCost, out flag);
                productionRate = 0;
                //Debug.Log("[RF]StormDrainAI checkBuildPosition should work but doesn't");
                return ToolBase.ToolErrors.None;
            }
            ToolBase.ToolErrors toolErrors = base.CheckBuildPosition(relocateID, ref position, ref angle, waterHeight, elevation, ref connectionSegment, out productionRate, out constructionCost);
            Vector3 vector = Building.CalculatePosition(position, angle, this.m_waterLocationOffset);
            Vector3 a;
            Vector3 a2;
            if (BuildingTool.SnapToCanal(position, out a, out a2, 40f, true))
            {
                angle = Mathf.Atan2(a2.x, -a2.z);
                a -= a2 * this.m_waterLocationOffset.z;
                position.x = a.x;
                position.z = a.z;
            }
            else if (!Singleton<TerrainManager>.instance.GetClosestWaterPos(ref vector, this.m_maxWaterDistance * 0.5f))
            {
                toolErrors |= ToolBase.ToolErrors.WaterNotFound;
            }
            return toolErrors;
        }
        
        protected override void HandleWorkAndVisitPlaces(ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveWorkerCount, ref int totalWorkerCount, ref int workPlaceCount, ref int aliveVisitorCount, ref int totalVisitorCount, ref int visitPlaceCount)
        {
            workPlaceCount += this.m_workPlaceCount0 + this.m_workPlaceCount1 + this.m_workPlaceCount2 + this.m_workPlaceCount3;
            base.GetWorkBehaviour(buildingID, ref buildingData, ref behaviour, ref aliveWorkerCount, ref totalWorkerCount);
            base.HandleWorkPlaces(buildingID, ref buildingData, this.m_workPlaceCount0, this.m_workPlaceCount1, this.m_workPlaceCount2, this.m_workPlaceCount3, ref behaviour, aliveWorkerCount, totalWorkerCount);
        }


        protected override void ProduceGoods(ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, ref Citizen.BehaviourData behaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveVisitorCount, int totalVisitorCount, int visitPlaceCount)
        {
            bool flag = false;
            int districtInletCapacity;
            if (buildingData.m_netNode != 0)
            {
                NetManager instance = Singleton<NetManager>.instance;
                for (int i = 0; i < 8; i++)
                {
                    if (instance.m_nodes.m_buffer[(int)buildingData.m_netNode].GetSegment(i) != 0)
                    {
                        flag = true;
                        break;
                    }
                }
            }
            if (!flag)
            {
                productionRate = 0;
            }
            //Debug.Log("[RF] Starting Produce Goods for Building " + buildingID.ToString());
            DistrictManager instance2 = Singleton<DistrictManager>.instance;
            byte district = instance2.GetDistrict(buildingData.m_position);
            int stormWaterIntake = this.m_stormWaterIntake * productionRate / 100;
            //Debug.Log("[RF].StormDrainAI  Num = " + num.ToString());
            if (stormWaterIntake != 0)
            {


                //Debug.Log("[RF].StormDrainAI  Num3 = " + num3.ToString());

                int districtOutletCapacity = Hydraulics.getDistrictOutletCapacity(district);
                int districtDetentionCapacity = Hydraulics.getDistrictDetentionCapacity(district);
                int usedDetentionCapacity = Hydraulics.getDistrictDetainedStormwater(district);
                if (districtOutletCapacity == 0 && districtDetentionCapacity == 0)
                {
                    if ((buildingData.m_flags & Building.Flags.Outgoing) != Building.Flags.Outgoing)
                    {
                        buildingData.m_flags |= Building.Flags.Outgoing;
                        buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem.LineNotConnected);
                    }
                    productionRate = 0;
                }
                else if ((buildingData.m_flags & Building.Flags.Outgoing) == Building.Flags.Outgoing)
                {
                    buildingData.m_flags &= ~Building.Flags.Outgoing;
                    buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem.LineNotConnected);
                }
                if (productionRate > 0)
                {
                    if (Hydraulics.checkGravityFlowForInlet(buildingID) == false)
                    {
                        if ((buildingData.m_flags & Building.Flags.Loading1) != Building.Flags.Loading1)
                        {
                            buildingData.m_flags |= Building.Flags.Loading1;
                            buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem.NoPlaceforGoods);
                        }
                        productionRate = 0;
                    }
                    else if ((buildingData.m_flags & Building.Flags.Loading1) == Building.Flags.Loading1)
                    {
                        buildingData.m_flags &= ~Building.Flags.Loading1;
                        buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem.NoPlaceforGoods);
                    }
                }
                int outletCapacity = Hydraulics.getOutletCapacityForInlet(buildingID);
                int stormWaterAccumulation = Hydraulics.getStormwaterAccumulation(buildingID);
                int systemCapacity = outletCapacity + districtDetentionCapacity - stormWaterAccumulation - usedDetentionCapacity;
                if (productionRate > 0)
                {
                    if (systemCapacity <= 0)
                    {
                        if ((buildingData.m_flags & Building.Flags.CapacityFull) != Building.Flags.CapacityFull)
                        {
                            buildingData.m_flags |= Building.Flags.CapacityFull;
                            buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem.LandfillFull);
                        }
                        productionRate = 0;
                    }
                    else if ((buildingData.m_flags & Building.Flags.CapacityFull) == Building.Flags.CapacityFull)
                    {
                        buildingData.m_flags &= ~Building.Flags.CapacityFull;
                        buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem.LandfillFull);
                    }
                }
                int capturedWater;
                bool flag1 = (buildingData.m_problems & Notification.Problem.NoPlaceforGoods) == Notification.Problem.None;
                bool flag2 = (buildingData.m_problems & Notification.Problem.LineNotConnected) == Notification.Problem.None;
                bool flag3 = (buildingData.m_problems & Notification.Problem.WaterNotConnected) == Notification.Problem.None;
                bool flag4 = (buildingData.m_problems & Notification.Problem.LandfillFull) == Notification.Problem.None;
                bool flag5 = productionRate > 0;
                if (flag1 && flag2 && flag3 && flag4 && flag5)
                {
                    capturedWater = this.HandleWaterSource(buildingID, ref buildingData, false, stormWaterIntake, systemCapacity, this.m_waterEffectDistance);
                    if (this.m_electricityConsumption == 0)
                    {
                        Vector3 pos = buildingData.CalculatePosition(this.m_waterLocationOffset);
                        Singleton<NaturalResourceManager>.instance.CheckPollution(pos, out buildingData.m_waterPollution);
                     }
                }
                else
                {
                    capturedWater = this.HandleWaterSource(buildingID, ref buildingData, false, 0, 0, this.m_waterEffectDistance); ;
                }
                //Debug.Log("[RF].StormDrainAI  Num4 = " + num4.ToString());

                if (capturedWater == 0)
                {
                    productionRate = 0;
                }
                else if (capturedWater > 0)
                {

                    int num6 = Mathf.Min(capturedWater, stormWaterIntake, systemCapacity);
                    //Debug.Log("[RF].StormDrainAI  Num6 = " + num6.ToString());
                    //Debug.Log("[RF].StormDrainAI  StormwaterAccumulation = " + this.m_stormWaterAccumulation.ToString());

                    if (num6 > 0)
                    {
                        Hydraulics.addStormwaterAccumulation(buildingID, num6);
                        if (num6 == systemCapacity)
                        {
                            //Debug.Log("[RF].StormDrainAI Flooded Inlet " + buildingID);

                            if ((buildingData.m_flags & Building.Flags.CapacityFull) != Building.Flags.CapacityFull)
                            {
                                //Debug.Log("[RF].StormDrainAI Full Outlet problem true for " + buildingID);
                                buildingData.m_flags |= Building.Flags.CapacityFull;
                                //Debug.Log("[RF].StormDrainAI building " + buildingID + " now has flags " + buildingData.m_flags.ToString());
                                //Debug.Log("[RF].StormDrainAI building " + buildingID + " has problems " + buildingData.m_problems.ToString());
                                buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem.LandfillFull);
                                // Debug.Log("[RF].StormDrainAI building " + buildingID + " now has problems " + buildingData.m_problems.ToString());
                            }
                        }
                        else
                        {
                            if ((buildingData.m_flags & Building.Flags.CapacityFull) == Building.Flags.CapacityFull)
                            {
                                // Debug.Log("[RF].StormDrainAI No longer Full");
                                buildingData.m_flags &= ~Building.Flags.CapacityFull;
                                buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem.LandfillFull);
                            }
                        }
                     
                    }
                }

            }
            
                //Debug.Log("[RF].StormDrainAI  production rate = " + productionRate.ToString());


                //Debug.Log("[RF].StormDrainAI  StormwaterAccumulation = " + Hydraulics.getStormwaterAccumulation(buildingID).ToString() +" at building " + buildingID.ToString());
                // Debug.Log("[RF].StormDrainAI  TotalStormwaterAccumulation = " + calculateTotalStormDrainAccumulation().ToString());
                // Debug.Log("[RF].StormDrainAI  StormDrainCapacity = " + calculateStormDrainCapacity().ToString());

            

            int num7 = this.m_stormWaterOutlet * productionRate / 100;
            // Debug.Log("[RF].StormDrainAI  Num7 = " + num7.ToString());
            if (num7 != 0)
            {
                int currentStormWaterAccumulation = Hydraulics.getStormWaterAccumulationForOutlet(buildingID);
                int districtDetentionCapacity = Hydraulics.getDistrictDetentionCapacity(district) * productionRate / 100;
                int districtDetainedStormwater = Hydraulics.getDistrictDetainedStormwater(district);
                districtInletCapacity = Hydraulics.getDistrictInletCapacity(district);
                if (districtInletCapacity == 0)
                {
                    if ((buildingData.m_flags & Building.Flags.Incoming) != Building.Flags.Incoming)
                    {
                        buildingData.m_flags |= Building.Flags.Incoming;
                        buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem.LineNotConnected);
                    }
                }
                else if ((buildingData.m_flags & Building.Flags.Incoming) == Building.Flags.Incoming)
                {
                    buildingData.m_flags &= ~Building.Flags.Incoming;
                    buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem.LineNotConnected);
                }
                // Debug.Log("[RF].StormDrainAI  Total StormwaterAccumulation = " + currentStormWaterAccumulation.ToString());
                if (this.m_waterLocationOffset.z == 0)
                {
                    this.m_waterLocationOffset.z = 38;
                }
                if (currentStormWaterAccumulation > 0 && districtDetainedStormwater >= districtDetentionCapacity * 0.8 || currentStormWaterAccumulation > 0 && districtDetentionCapacity == 0)
                {
                    int num10 = Mathf.Min(currentStormWaterAccumulation, Hydraulics.getDistrictOutletCapacity(district));


                    if (num10 > 0)
                    {
                        int num12;

                        num12 = this.HandleWaterSource(buildingID, ref buildingData, true, num7, num10, this.m_waterEffectDistance);
                        //Debug.Log("[RF].StormDrainAI  Num12 = " + num12.ToString());
                        if (num12 > 0)
                        {
                            int dumpedQuantity;
                            if (this.m_electricityConsumption == 0)
                            {
                               dumpedQuantity = Hydraulics.removeDistrictStormwaterAccumulation(district, num12, buildingID, true);
                            } else
                            {
                               dumpedQuantity = Hydraulics.removeDistrictStormwaterAccumulation(district, num12, buildingID, false);
                            }
                            if (dumpedQuantity >= num7)
                            {

                                if ((buildingData.m_flags & Building.Flags.CapacityFull) != Building.Flags.CapacityFull)
                                {
                                    //Debug.Log("[RF].StormDrainAI Full Outlet problem true for " + buildingID);
                                    buildingData.m_flags |= Building.Flags.CapacityFull;
                                    //Debug.Log("[RF].StormDrainAI building " + buildingID + " now has flags " + buildingData.m_flags.ToString());
                                    //Debug.Log("[RF].StormDrainAI building " + buildingID + " has problems " + buildingData.m_problems.ToString());
                                    buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem.LandfillFull);
                                    // Debug.Log("[RF].StormDrainAI building " + buildingID + " now has problems " + buildingData.m_problems.ToString());
                                }

                            }
                            else
                            {

                                if ((buildingData.m_flags & Building.Flags.CapacityFull) == Building.Flags.CapacityFull)
                                {
                                    // Debug.Log("[RF].StormDrainAI No longer Full");
                                    buildingData.m_flags &= ~Building.Flags.CapacityFull;
                                    buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem.LandfillFull);
                                }
                            }
                            //Debug.Log("[RF].StormDrainAI  dumpedQuantity = " + dumpedQuantity.ToString());
                        }


                        if (num12 == 0)
                        {
                            productionRate = 0;
                        }
                    }
                    else
                    {
                        productionRate = 0;
                    }
                    //Debug.Log("[RF].StormDrainAI  StormwaterAccumulation = " + Hydraulics.getDistrictStormwaterAccumulation(district).ToString() + " at district " + district.ToString());
                }
                else
                {
                    productionRate = 0;
                    if ((buildingData.m_flags & Building.Flags.CapacityFull) == Building.Flags.CapacityFull)
                    {
                        // Debug.Log("[RF].StormDrainAI No longer Full");
                        buildingData.m_flags &= ~Building.Flags.CapacityFull;
                        buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem.LandfillFull);
                    }
                }


                //Debug.Log("[RF].StormDrainAI  production rate = " + productionRate.ToString());

            }
            int detentionCapacity = this.m_stormWaterDetention * productionRate / 100;
            //Debug.Log("[RF].StormdrainAI detention capacity =" + detentionCapacity.ToString());
            if (detentionCapacity != 0)
            {
                int remainingCapacity = detentionCapacity - Hydraulics.getDetainedStormwater(buildingID);
                //Debug.Log("[RF].StormdrainAI remainingCapacity = " + remainingCapacity.ToString() + " detainedStormwater = " + Hydraulics.getDetainedStormwater(buildingID).ToString());
                districtInletCapacity = Hydraulics.getDistrictInletCapacity(district);
                if (districtInletCapacity == 0)
                {
                    if ((buildingData.m_flags & Building.Flags.Incoming) != Building.Flags.Incoming)
                    {
                        buildingData.m_flags |= Building.Flags.Incoming;
                        buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem.LineNotConnected);
                    }
                }
                else if ((buildingData.m_flags & Building.Flags.Incoming) == Building.Flags.Incoming)
                {
                    buildingData.m_flags &= ~Building.Flags.Incoming;
                    buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem.LineNotConnected);
                }
                int detainedQuantitity = Mathf.Min(Hydraulics.removeDistrictStormwaterAccumulation(district, remainingCapacity, buildingID, false), remainingCapacity);
                //Debug.Log("[RF].StormdrainAI detained quantitiy =" + detainedQuantitity.ToString());
                Hydraulics.addDetainedStormwater(buildingID, detainedQuantitity);
                //Debug.Log("[RF].StormdrainAI total detained stormwater =" + totalDetainedStormwater.ToString());
                remainingCapacity = detentionCapacity - Hydraulics.getDetainedStormwater(buildingID);
                // Debug.Log("[RF].StormdrainAI remainingCapacity = " + remainingCapacity.ToString());
                if (remainingCapacity <= 0)
                {
                    if ((buildingData.m_flags & Building.Flags.CapacityFull) != Building.Flags.CapacityFull)
                    {
                        //Debug.Log("[RF].StormDrainAI Full Outlet problem true for " + buildingID);
                        buildingData.m_flags |= Building.Flags.CapacityFull;
                        //Debug.Log("[RF].StormDrainAI building " + buildingID + " now has flags " + buildingData.m_flags.ToString());
                        //Debug.Log("[RF].StormDrainAI building " + buildingID + " has problems " + buildingData.m_problems.ToString());
                        buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem.LandfillFull);
                        // Debug.Log("[RF].StormDrainAI building " + buildingID + " now has problems " + buildingData.m_problems.ToString());
                    }
                }
                else
                {
                    if ((buildingData.m_flags & Building.Flags.CapacityFull) == Building.Flags.CapacityFull)
                    {
                        // Debug.Log("[RF].StormDrainAI No longer Full");
                        buildingData.m_flags &= ~Building.Flags.CapacityFull;
                        buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem.LandfillFull);
                    }
                    if (remainingCapacity >= detentionCapacity)
                    {
                        productionRate = 0;
                    }
                }


            }
            base.HandleDead(buildingID, ref buildingData, ref behaviour, totalWorkerCount);
            int num14 = productionRate * this.m_noiseAccumulation / 100;
            if (num14 != 0)
            {
                Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.NoisePollution, num14, buildingData.m_position, this.m_noiseRadius);
            }
            base.ProduceGoods(buildingID, ref buildingData, ref frameData, productionRate, ref behaviour, aliveWorkerCount, totalWorkerCount, workPlaceCount, aliveVisitorCount, totalVisitorCount, visitPlaceCount);
        }

        protected override bool CanSufferFromFlood()
        {

            return false;
        }
        public override float ElectricityGridRadius()
        {
            if (this.m_electricityConsumption > 0)
                return base.ElectricityGridRadius();
            else
                return 0f;
        }
        private int HandleWaterSource(ushort buildingID, ref Building data, bool output, int rate, int max, float radius)
        {
            uint num = (uint)(Mathf.Min(rate, max) >> 1);
            if (num == 0u)
            {
                return 0;
            }
            TerrainManager instance = Singleton<TerrainManager>.instance;
            WaterSimulation waterSimulation = instance.WaterSimulation;
            if (data.m_waterSource != 0)
            {
                bool flag = false;
                WaterSource sourceData = waterSimulation.LockWaterSource(data.m_waterSource);
                try
                {
                    if (output)
                    {
                        uint num2 = num;
                        if (num2 < sourceData.m_water >> 3)
                        {
                            num2 = sourceData.m_water >> 3;
                        }
                        sourceData.m_outputRate = num2 + 3u >> 2;
                        sourceData.m_water += num;
                        sourceData.m_pollution += num * (uint)this.m_outletPollution / (uint)Mathf.Max(100, waterSimulation.GetPollutionDisposeRate() * 100);
                        //Debug.Log("[RF].StormDrainAI  watersource + output ");
                    }
                    else
                    {
                        uint num3 = num;

                        if (num3 < sourceData.m_water >> 3)
                        {
                            num3 >>= 1;
                        }
                        sourceData.m_inputRate = num3 + 3u >> 2;
                        if (num > sourceData.m_water)
                        {
                            num = sourceData.m_water;
                        }
                        //Debug.Log("[RF] num is " + num.ToString() + " souce water is " + sourceData.m_water.ToString());

                        if (sourceData.m_water != 0u)
                        {
                            data.m_waterPollution = (byte)Mathf.Min(255f, 255u * sourceData.m_pollution / sourceData.m_water);
                            sourceData.m_pollution = (uint)((ulong)sourceData.m_pollution * (ulong)(sourceData.m_water - num) / (ulong)sourceData.m_water);
                        }
                        else
                        {
                            data.m_waterPollution = 0;
                        }
                        sourceData.m_water -= num;

                        Vector3 vector = sourceData.m_inputPosition;
                        if (!instance.HasWater(VectorUtils.XZ(vector)))
                        {

                            vector = data.CalculatePosition(this.m_waterLocationOffset);
                            //Debug.Log("[RF] vector has water");
                            if (instance.GetClosestWaterPos(ref vector, radius))
                            {
                                //Debug.Log("[RF] inputs are vector");
                                sourceData.m_inputPosition = vector;
                                sourceData.m_outputPosition = vector;
                            }
                            else
                            {
                                flag = true;
                            }
                        }
                        // Debug.Log("[RF].StormDrainAI  watersource + !output ");
                    }
                }
                finally
                {
                    waterSimulation.UnlockWaterSource(data.m_waterSource, sourceData);
                }
                if (flag)
                {
                    waterSimulation.ReleaseWaterSource(data.m_waterSource);
                    data.m_waterSource = 0;
                    //num = 0u;
                    // Debug.Log("[RF].StormDrainAI  flag");
                }
            }
            else
            {
                bool flag2 = false;
                Vector3 vector2 = data.CalculatePosition(this.m_waterLocationOffset);
                Vector3 vector3;
                Vector3 vector4;
                //Debug.Log("[RF].StormDrainAI  !watersource");
                if (BuildingTool.SnapToCanal(vector2, out vector3, out vector4, 0f, true))
                {
                    vector2 = vector3;
                    flag2 = true;
                }
                if (!flag2)
                {
                    flag2 = instance.GetClosestWaterPos(ref vector2, radius);
                }
                if (flag2 || output)
                {
                    WaterSource sourceData2 = default(WaterSource);
                    sourceData2.m_type = 2;
                    sourceData2.m_inputPosition = vector2;
                    sourceData2.m_outputPosition = vector2;
                    if (output)
                    {
                        sourceData2.m_outputRate = num + 3u >> 2;
                        sourceData2.m_water = num;
                        sourceData2.m_pollution = num * (uint)this.m_outletPollution / (uint)Mathf.Max(100, waterSimulation.GetPollutionDisposeRate() * 100);
                        if (!waterSimulation.CreateWaterSource(out data.m_waterSource, sourceData2))
                        {
                            num = 0u;
                        }
                        //Debug.Log("[RF] !watersource + output ");
                    }
                    else
                    {
                        sourceData2.m_inputRate = num + 3u >> 2;
                        waterSimulation.CreateWaterSource(out data.m_waterSource, sourceData2);
                        num = 0u;
                    }
                }
                else {
                    //Debug.Log("[RF] 0u ");
                    num = 0u;
                }
            }
            //Debug.Log("[RF] num is " + ((int)num << 1).ToString());
            return (int)((int)num << 1);
        }
        public override void PlacementFailed()
        {

            GuideController properties = Singleton<GuideManager>.instance.m_properties;
            if (properties != null)
            {
                Singleton<BuildingManager>.instance.m_buildNextToWater.Activate(properties.m_buildNextToWater);
            }

        }

        public override void PlacementSucceeded()
        {

            GenericGuide buildNextToWater = Singleton<BuildingManager>.instance.m_buildNextToWater;
            if (buildNextToWater != null)
            {
                buildNextToWater.Deactivate();
            }

            if (this.m_stormWaterIntake != 0)
            {
                BuildingTypeGuide waterPumpMissingGuide = Singleton<WaterManager>.instance.m_waterPumpMissingGuide;
                if (waterPumpMissingGuide != null)
                {
                    waterPumpMissingGuide.Deactivate();
                }
            }
            if (this.m_stormWaterOutlet != 0)
            {
                BuildingTypeGuide drainPipeMissingGuide = Singleton<WaterManager>.instance.m_drainPipeMissingGuide;
                if (drainPipeMissingGuide != null)
                {
                    drainPipeMissingGuide.Deactivate();
                }
            }
        }

        public override void UpdateGuide(GuideController guideController)
        {
            if (this.m_stormWaterIntake != 0)
            {
                BuildingTypeGuide waterPumpMissingGuide = Singleton<WaterManager>.instance.m_waterPumpMissingGuide;
                if (waterPumpMissingGuide != null)
                {
                    int waterCapacity = Singleton<DistrictManager>.instance.m_districts.m_buffer[0].GetWaterCapacity();
                    int sewageCapacity = Singleton<DistrictManager>.instance.m_districts.m_buffer[0].GetSewageCapacity();
                    if (waterCapacity == 0 && sewageCapacity != 0)
                    {
                        waterPumpMissingGuide.Activate(guideController.m_waterPumpMissing, this.m_info);
                    }
                    else
                    {
                        waterPumpMissingGuide.Deactivate();
                    }
                }
            }
            if (this.m_stormWaterOutlet != 0)
            {
                BuildingTypeGuide drainPipeMissingGuide = Singleton<WaterManager>.instance.m_drainPipeMissingGuide;
                if (drainPipeMissingGuide != null)
                {
                    int waterCapacity2 = Singleton<DistrictManager>.instance.m_districts.m_buffer[0].GetWaterCapacity();
                    int sewageCapacity2 = Singleton<DistrictManager>.instance.m_districts.m_buffer[0].GetSewageCapacity();
                    if (waterCapacity2 != 0 && sewageCapacity2 == 0)
                    {
                        drainPipeMissingGuide.Activate(guideController.m_drainPipeMissing, this.m_info);
                    }
                    else
                    {
                        drainPipeMissingGuide.Deactivate();
                    }
                }
            }
            base.UpdateGuide(guideController);
        }

        public override void GetPollutionAccumulation(out int ground, out int noise)
        {
            ground = 0;
            noise = this.m_noiseAccumulation;
        }

        public override string GetLocalizedTooltip()
        {
            string text = LocaleFormatter.FormatGeneric("AIINFO_WATER_CONSUMPTION", new object[]
            {
            this.GetWaterConsumption() * 16
            }) + Environment.NewLine + LocaleFormatter.FormatGeneric("AIINFO_ELECTRICITY_CONSUMPTION", new object[]
            {
            this.GetElectricityConsumption() * 16
            });
            if (this.m_stormWaterIntake > 0)
            {
                return TooltipHelper.Append(base.GetLocalizedTooltip(), TooltipHelper.Format(new string[]
                {
                LocaleFormatter.Info1,
                text,
                LocaleFormatter.Info2,
                LocaleFormatter.FormatGeneric("AIINFO_WATER_INTAKE", new object[]
                {
                    this.m_stormWaterIntake * 16
                })
                }));
            }
            return TooltipHelper.Append(base.GetLocalizedTooltip(), TooltipHelper.Format(new string[]
            {
            LocaleFormatter.Info1,
            text,
            LocaleFormatter.Info2,
            LocaleFormatter.FormatGeneric("AIINFO_WATER_OUTLET", new object[]
            {
                this.m_stormWaterOutlet * 16
            })
            }));
        }

        public override string GetLocalizedStats(ushort buildingID, ref Building data)
        {
            StringBuilder sb = new StringBuilder();
            if (this.m_stormWaterIntake > 0)
            {
                int InletStormwaterAccululation = (int)Hydraulics.getStormwaterAccumulation(buildingID);
                int DistrictInletCapacity = (int)Hydraulics.getDistrictInletCapacity(buildingID);
                int DistrictStormwaterAccumulation = (int)Hydraulics.getDistrictStormwaterAccumulation(buildingID);
                int inletHeight = (int)data.m_position.y;
                if ((data.m_problems & Notification.Problem.LineNotConnected) != Notification.Problem.None)
                {
                    sb.AppendLine("No outlet in distrct!");
                    sb.AppendLine("No detention basin in distrct!");
                }
                else if ((data.m_problems & Notification.Problem.NoPlaceforGoods) != Notification.Problem.None)
                {
                    sb.AppendLine("Cannot gravity drain to outlet!");
                    sb.AppendLine("Inlet Elevation: " + inletHeight);
                }
                else if ((data.m_problems & Notification.Problem.LandfillFull) != Notification.Problem.None)
                {
                    sb.AppendLine("Not enough outlet capacity!");
                    sb.AppendLine("Not enough detention capacity!");
                } else
                {
                    sb.AppendLine("Stormwater Accumulation: " + InletStormwaterAccululation);
                    sb.AppendLine("Inlet Elevation: " + inletHeight);
                }
               
                sb.AppendLine("District SWA: " + DistrictStormwaterAccumulation);
                sb.AppendLine("District Inlet Cap: " + DistrictInletCapacity);
                sb.AppendLine("Inlet Capacity: " + this.m_stormWaterIntake);
            }
            else if (this.m_stormWaterOutlet > 0)
            {

                int DistrictOutletCapacity = (int)Hydraulics.getDistrictOutletCapacity(buildingID);
                int OutletStormwaterAccumulation = (int)Hydraulics.getStormWaterAccumulationForOutlet(buildingID);
                int outletHeight = (int)data.m_position.y;
                if ((data.m_problems & Notification.Problem.LineNotConnected) != Notification.Problem.None)
                {
                    sb.AppendLine("No inlets in distrct!");
                } else if ((data.m_problems & Notification.Problem.LandfillFull) != Notification.Problem.None)
                {
                    sb.AppendLine("Outlet at maximum capacity!");
                }
                sb.AppendLine("Outlet Elevation: " + outletHeight);
                sb.AppendLine("District Outlet Capacity: " + DistrictOutletCapacity);
                sb.AppendLine("Available SWA: " + OutletStormwaterAccumulation);
                sb.AppendLine("Outlet Capacity: " + this.m_stormWaterOutlet);
            } else if (this.m_stormWaterDetention > 0 )
            {
                int DetainedStormwater = (int)Hydraulics.getDetainedStormwater(buildingID);
                int RemainingCapacity = this.m_stormWaterDetention - DetainedStormwater;
                int InfiltrationRate = this.m_stormWaterInfiltration;
                int DistrictDetentionCapcity = (int)Hydraulics.getDistrictDetentionCapacity(buildingID);
                if ((data.m_problems & Notification.Problem.LineNotConnected) != Notification.Problem.None)
                {
                    sb.AppendLine("No inlets in distrct!");
                } else
                {
                    sb.AppendLine("Detained Stormwater: " + DetainedStormwater);
                }
                if ((data.m_problems & Notification.Problem.LandfillFull) != Notification.Problem.None)
                {
                    sb.AppendLine("Detention basin is full!");
                } else
                {
                    sb.AppendLine("Remaining Capacity: " + RemainingCapacity);
                }                            
                sb.AppendLine("Infiltration Rate: " + InfiltrationRate);
                sb.AppendLine("District Detention Capacity: " + DistrictDetentionCapcity);
                sb.AppendLine("Detention Basin Capacity: " + this.m_stormWaterDetention);
            }
           
            return sb.ToString();
        }

        public override bool RequireRoadAccess()
        {
            return base.RequireRoadAccess() || this.m_workPlaceCount0 != 0 || this.m_workPlaceCount1 != 0 || this.m_workPlaceCount2 != 0 || this.m_workPlaceCount3 != 0;
        }

        public override ToolBase.ToolErrors CheckBulldozing(ushort buildingID, ref Building data) {
            if (this.m_stormWaterDetention > 0)
            {
                if (Hydraulics.getDetainedStormwater(buildingID) > 0)
                {
                    return ToolBase.ToolErrors.NotEmpty;
                }
            }
            return base.CheckBulldozing(buildingID, ref data);
        }
        public override bool CanBeRelocated(ushort buildingID, ref Building data)
        {
            if (this.m_stormWaterDetention > 0)
            {
                int num = Hydraulics.getDetainedStormwater(buildingID);
                return num == 0;
            }
            return true;
        }

    }
}
