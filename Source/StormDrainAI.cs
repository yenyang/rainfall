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
        public int m_workPlaceCount0 = 0;

        [CustomizableProperty("Educated Workers", "Workers", 1)]
        public int m_workPlaceCount1 = 0;

        [CustomizableProperty("Well Educated Workers", "Workers", 2)]
        public int m_workPlaceCount2 = 0;

        [CustomizableProperty("Highly Educated Workers", "Workers", 3)]
        public int m_workPlaceCount3 = 0;

        [CustomizableProperty("Storm Water Intake", "Water")]
        public int m_stormWaterIntake = 0;

        [CustomizableProperty("Storm Water Outlet", "Water")]
        public int m_stormWaterOutlet = 0;

        [CustomizableProperty("Storm Water Detention", "Water")]
        public int m_stormWaterDetention = 0;

        [CustomizableProperty("Storm Water Infiltration", "Water")]
        public int m_stormWaterInfiltration = 0;

        [CustomizableProperty("Water Location Offset", "Water")]
        public Vector3 m_waterLocationOffset = Vector3.zero;

        [CustomizableProperty("Max Water Placement Distance", "Water")]
        public float m_maxWaterDistance = 1000000f;

        [CustomizableProperty("Water Effect Distance", "Water")]
        public float m_waterEffectDistance = 100f;

        [CustomizableProperty("Outlet Pollution", "Pollution")]
        public int m_outletPollution = 100;

        [CustomizableProperty("Noise Accumulation", "Pollution")]
        public int m_noiseAccumulation = 0;

        [CustomizableProperty("Noise Radius", "Pollution")]
        public float m_noiseRadius = 0f;

        [CustomizableProperty("Invert", "Water")]
        public float m_invert = 10f;

        [CustomizableProperty("Soffit", "Water")]
        public float m_soffit = 10f;

        [CustomizableProperty("Filtration", "Water")]
        public bool m_filter = false;

        [CustomizableProperty("Culvert", "Water")]
        public bool m_culvert = false;


        [CustomizableProperty("Milestone", "Water")]
        public int m_milestone = 0;

        [CustomizableProperty("Procedurally Generate Inlets", "Water")]
        public bool m_procedurally_generate_inlets = false;

        [CustomizableProperty("Placement Mode", "Water")]
        public BuildingInfo.PlacementMode m_placementMode = BuildingInfo.PlacementMode.OnTerrain;

        [CustomizableProperty("Placement Mode Alt", "Water")]
        public BuildingInfo.PlacementMode m_placementModeAlt = BuildingInfo.PlacementMode.Shoreline;


        string currentElevation = "0";
        //private bool buildingToolIsDetoured = false;

        public override void GetNaturalResourceRadius(ushort buildingID, ref Building data, out NaturalResourceManager.Resource resource1, out Vector3 position1, out float radius1, out NaturalResourceManager.Resource resource2, out Vector3 position2, out float radius2)
        {
            resource1 = NaturalResourceManager.Resource.Water;
            position1 = data.CalculatePosition(this.m_waterLocationOffset);
            radius1 = this.m_maxWaterDistance;
            resource2 = NaturalResourceManager.Resource.None;
            position2 = data.m_position;
            radius2 = 0f;
        }

        public override ImmaterialResourceManager.ResourceData[] GetImmaterialResourceRadius(ushort buildingID, ref Building data)
        {
            return new ImmaterialResourceManager.ResourceData[1]
            {
            new ImmaterialResourceManager.ResourceData
            {
                m_resource = ImmaterialResourceManager.Resource.NoisePollution,
                m_radius = ((m_noiseAccumulation == 0) ? 0f : m_noiseRadius)
            }
            };
        }

        public override Color GetColor(ushort buildingID, ref Building data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode)
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

                if (this.m_filter != false)
                {
                    return Color.yellow;
                }
                return base.GetColor(buildingID, ref data, infoMode, subInfoMode);
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
                    return base.GetColor(buildingID, ref data, infoMode, subInfoMode);
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
                return base.GetColor(buildingID, ref data, infoMode, subInfoMode);
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
            } else if (this.m_stormWaterIntake != 0)
            {
                return productionRate * Mathf.RoundToInt(OptionHandler.getSliderSetting("InletRateMultiplier") * (float)this.m_stormWaterIntake) / 100;
            }

            return productionRate * Mathf.RoundToInt(OptionHandler.getSliderSetting("OutletRateMultiplier")*(float)this.m_stormWaterOutlet) / 100; 
        }

        public override void GetPlacementInfoMode(out InfoManager.InfoMode mode, out InfoManager.SubInfoMode subMode, float elevation)
        {
            /*if (this.m_stormWaterIntake > 0 && this.m_electricityConsumption == 0)
            {
                mode = InfoManager.InfoMode.TerrainHeight;
                subMode = InfoManager.SubInfoMode.NormalWater;
            }
            else
            {*/
                mode = InfoManager.InfoMode.Water;
                subMode = InfoManager.SubInfoMode.WaterPower;
            //}


        }

        public override void CreateBuilding(ushort buildingID, ref Building data)
        {
            base.CreateBuilding(buildingID, ref data);
            int workCount = this.m_workPlaceCount0 + this.m_workPlaceCount1 + this.m_workPlaceCount2 + this.m_workPlaceCount3;
            Singleton<CitizenManager>.instance.CreateUnits(out data.m_citizenUnits, ref Singleton<SimulationManager>.instance.m_randomizer, buildingID, 0, 0, workCount, 0, 0, 0);
            if (this.m_stormWaterIntake > 0)
            {
                Hydraulics.addInlet(buildingID);
                Building currentBuilding = BuildingManager.instance.m_buildings.m_buffer[buildingID];

                //currentBuilding.Info.m_autoRemove = true;

            }
            else if (this.m_stormWaterOutlet > 0)
            {
                Hydraulics.addOutlet(buildingID);
            }
            else if (this.m_stormWaterDetention > 0)
            {
                Hydraulics.addDetentionBasin(buildingID);
            }
            if (OptionHandler.getCheckboxSetting("AutomaticPipeLaterals"))
            {
                Building currentBuilding = BuildingManager.instance.m_buildings.m_buffer[buildingID];
                //Debug.Log("[RF]SDAI.createBuilding buildign netnode = " + currentBuilding.m_netNode.ToString());
                ushort inletNodeID = currentBuilding.m_netNode;
                Vector3 inletPosition = currentBuilding.m_position;
                Vector3 pipePosition = new Vector3();
                Vector3 lateralDirection = new Vector3();
                ushort targetPipeID = 0;

                ushort newNodeID = 0;
                bool flag = SnapToSegment(inletPosition, out pipePosition, out lateralDirection, 8f, 90f, ItemClass.Service.Water, out targetPipeID);
                if (flag == true)
                {
                    bool flag2 = SplitSegment(targetPipeID, out newNodeID, pipePosition);
                    if (flag2)
                    {
                        //Debug.Log("[RF]SDAI.createBuilding split segment!");
                        ushort newPipeLateralID = 0;
                        NetSegment targetPipe = Singleton<NetManager>.instance.m_segments.m_buffer[targetPipeID];
                        NetInfo info = targetPipe.Info;
                        uint buildIndex = targetPipe.m_buildIndex;
                        bool flag3 = Singleton<NetManager>.instance.CreateSegment(out newPipeLateralID, ref Singleton<SimulationManager>.instance.m_randomizer, info, targetPipe.TreeInfo, inletNodeID, newNodeID, lateralDirection, -lateralDirection, buildIndex, Singleton<SimulationManager>.instance.m_currentBuildIndex, (targetPipe.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None);
                        if (flag3)
                        {
                            // Debug.Log("[RF]SDAI.createBuilding created pipe lateral!");
                        }
                    }
                } else if (SnapToSegment(inletPosition, out pipePosition, out lateralDirection, 0f, 8f, ItemClass.Service.Water, out targetPipeID))
                {
                    //Debug.Log("[RF]SDAI.CreateBuilding snap to segment close");
                    NetSegment targetPipe = Singleton<NetManager>.instance.m_segments.m_buffer[targetPipeID];
                    NetNode startNode = Singleton<NetManager>.instance.m_nodes.m_buffer[targetPipe.m_startNode];
                    NetNode endNode = Singleton<NetManager>.instance.m_nodes.m_buffer[targetPipe.m_endNode];
                    Vector3 startPosition = startNode.m_position;
                    float firstSegmentLength = Vector3.Distance(startPosition, inletPosition);//Mathf.Sqrt(Mathf.Pow(inletPosition.x - startPosition.x,2) + Mathf.Pow(inletPosition.y - startPosition.y,2)+ Mathf.Pow(inletPosition.z - startPosition.z,2));
                    Vector3 endPosition = endNode.m_position;
                    float secondSegmentLength = Vector3.Distance(inletPosition, endPosition);//Mathf.Sqrt(Mathf.Pow(endPosition.x - inletPosition.x, 2) + Mathf.Pow(endPosition.y - inletPosition.y, 2) + Mathf.Pow(endPosition.z - inletPosition.z, 2));
                    
                    if (firstSegmentLength > 8f && secondSegmentLength > 8f) {
                        ushort firstPipeLateralID = 0;
                        ushort secondPipeLateralID = 0;
                        NetInfo info = targetPipe.Info;
                        ushort startNodeID = targetPipe.m_startNode;
                        ushort endNodeID = targetPipe.m_endNode;
                        uint buildIndex = targetPipe.m_buildIndex;
                        Singleton<NetManager>.instance.ReleaseSegment(targetPipeID, true);
                        Vector3 firstSegmentDirection = new Vector3(inletPosition.x - startPosition.x, 0, inletPosition.z - startPosition.z);
                        Vector3 secondSegmentDirection = new Vector3(endPosition.x - inletPosition.x, 0, endPosition.z - inletPosition.z);
                        firstSegmentDirection = firstSegmentDirection.normalized;
                        secondSegmentDirection = secondSegmentDirection.normalized;
                        
                        bool flag4 = Singleton<NetManager>.instance.CreateSegment(out firstPipeLateralID, ref Singleton<SimulationManager>.instance.m_randomizer, info, targetPipe.TreeInfo, startNodeID, inletNodeID, firstSegmentDirection, -firstSegmentDirection, buildIndex, Singleton<SimulationManager>.instance.m_currentBuildIndex, (targetPipe.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None);
                        bool flag5 = Singleton<NetManager>.instance.CreateSegment(out secondPipeLateralID, ref Singleton<SimulationManager>.instance.m_randomizer, info, targetPipe.TreeInfo, inletNodeID, endNodeID, secondSegmentDirection, -secondSegmentDirection, buildIndex, Singleton<SimulationManager>.instance.m_currentBuildIndex, (targetPipe.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None);
                        if (flag4 && flag5)
                        {
                           // Debug.Log("[RF]SDAI.createBuilding split pipe main");
                        }
                    }

                }
                // add in split pipes if node is too close to existing pipe.
            }
            
           
        }
        /*
        public override string GenerateName(ushort buildingID, InstanceID caller)
        {
                if (this.m_culvert == true && this.m_stormWaterIntake > 0)
                    return Hydraulics.generateCulvertName(buildingID, true);
                else if (this.m_culvert == true && this.m_stormWaterOutlet > 0)
                    return Hydraulics.generateCulvertName(buildingID, false);
                else
                    return base.GenerateName(buildingID, caller);
        }*/
        public override void ReleaseBuilding(ushort buildingID, ref Building data)
        {
            if (this.m_stormWaterIntake > 0)
            {
                Hydraulics.removeInlet(buildingID);
            }
            else if (this.m_stormWaterOutlet > 0)
            {
                Hydraulics.removeOutlet(buildingID);
            }
            else if (this.m_stormWaterDetention > 0)
            {
                Hydraulics.removeDetentionBasin(buildingID);
            }
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
            if ((buildingData.m_flags & Building.Flags.Collapsed) != Building.Flags.None)
            {
                Singleton<NotificationManager>.instance.AddWaveEvent(buildingData.m_position, NotificationEvent.Type.Happy, ImmaterialResourceManager.Resource.Abandonment, (float)(-(float)buildingData.Width * buildingData.Length), 64f);
            }
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
            //BuildingDecoration.LoadProps(this.m_info, 0, ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[relocateID]);
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                if (this.m_info.m_placementMode != this.m_placementModeAlt)
                {
                    this.m_info.m_placementMode = this.m_placementModeAlt;
                }
                bool flag;
                this.GetConstructionCost(relocateID != 0, out constructionCost, out flag);
                productionRate = 0;

            }
            else if (this.m_info.m_placementMode != this.m_placementMode)
            {
                this.m_info.m_placementMode = this.m_placementMode;
            }
            if (this.m_stormWaterIntake > 0 && this.m_electricityConsumption == 0)
            {
                Vector3 finalPosition;
                Vector3 finalAngle;
                if (this.SnapToRoad(position, out finalPosition, out finalAngle, 20f))
                {
                    //Debug.Log("[RF].StormDrainAI.CheckBuildPosition angle = " + angle.ToString());
                    angle = Mathf.Atan2(finalAngle.x, -finalAngle.z);
                    //Debug.Log("[RF].StormDrainAI.CheckBuildPosition new angle = " + angle.ToString());
                    //Debug.Log("[RF].StormDrainAI.CheckBuildPosition position x,z = " + position.x.ToString() + "," + position.z.ToString());
                    position.x = finalPosition.x;
                    position.z = finalPosition.z;
                    //Debug.Log("[RF].StormDrainAI.CheckBuildPosition new position x,z = " + position.x.ToString() + "," + position.z.ToString());
                }
                bool flag;
                this.GetConstructionCost(relocateID != 0, out constructionCost, out flag);
                productionRate = 0;
                this.currentElevation = position.y.ToString("0.00");
                return ToolBase.ToolErrors.None;

            }
            /*
            if (this.m_stormWaterDetention != 0 || this.m_stormWaterIntake > 0 && this.m_electricityConsumption == 0 || this.m_stormWaterOutlet > 0 || this.m_culvert == true)
            {
                bool flag;
                this.GetConstructionCost(relocateID != 0, out constructionCost, out flag);
                productionRate = 0;
                this.currentElevation = position.y.ToString("0.00");
                return ToolBase.ToolErrors.None;
            }*/
            ToolBase.ToolErrors toolErrors = base.CheckBuildPosition(relocateID, ref position, ref angle, waterHeight, elevation, ref connectionSegment, out productionRate, out constructionCost);
            Vector3 position2 = Building.CalculatePosition(position, angle, m_waterLocationOffset);
            position2.y = 0f;
            Vector3 pos;
            Vector3 dir;
            bool flag2;

            if (BuildingTool.SnapToCanal(position, out pos, out dir, out flag2, 40f, center: true))
            {
                position2 = pos;
                position2.y = 0f;
                if (this.m_stormWaterOutlet > 0 || Singleton<TerrainManager>.instance.GetClosestWaterPos(ref position2, m_maxWaterDistance * 0.5f))
                {
                    productionRate = 100;
                }
                else
                {
                    productionRate = 0;
                }
                angle = Mathf.Atan2(dir.x, 0f - dir.z);
                pos -= dir * this.m_waterLocationOffset.z;
                //Debug.Log("[RF]StormDrainAI WLO = " + this.m_waterLocationOffset.z.ToString());
                position.x = pos.x;
                position.z = pos.z;
                if (this.m_info.m_placementMode == BuildingInfo.PlacementMode.Roadside)
                {
                    if (this.m_info.m_placementMode == this.m_placementMode)
                        this.m_info.m_placementMode = this.m_placementModeAlt;
                    else
                        this.m_info.m_placementMode = this.m_placementMode;
                }
            }
            else if (Singleton<TerrainManager>.instance.GetClosestWaterPos(ref position2, this.m_maxWaterDistance * 0.5f))
            {
                productionRate = 100;
            }
            else
            {
                productionRate = 0;
            }

            this.currentElevation = position.y.ToString("0.00");
            return toolErrors;
        }
        /*
        private bool getStormDrainLateral(Vector3 inletPos, out Vector3 lateralPos, out Vector3 lateralAngle, float maxDistance)
        {
            NetManager _netManager = Singleton<NetManager>.instance;
            bool result = false;
            lateralPos = inletPos;
            lateralAngle = Vector3.forward;
            float minX = inletPos.x - maxDistance - 100f;
            float minZ = inletPos.z - maxDistance - 100f;
            float maxX = inletPos.x + maxDistance + 100f;
            float maxZ = inletPos.z + maxDistance + 100f;
            int minXint = Mathf.Max((int)(minX / 64f + 135f), 0);
            int minZint = Mathf.Max((int)(minZ / 64f + 135f), 0);
            int maxXint = Mathf.Max((int)(maxX / 64f + 135f), 269);
            int maxZint = Mathf.Max((int)(maxZ / 64f + 135f), 269);

            Array16<NetSegment> segments = Singleton<NetManager>.instance.m_segments;
            ushort[] segmentGrid = Singleton<NetManager>.instance.m_segmentGrid;
            for (int i = minZint; i <= maxZint; i++)
            {
                for (int j = minXint; j <= maxXint; j++)
                {
                    ushort segmentGridZX = segmentGrid[i * 270 + j];
                    int iterator = 0;
                    while (segmentGridZX != 0)
                    {
                        NetSegment.Flags flags = segments.m_buffer[(int)segmentGridZX].m_flags;
                        if ((flags & (NetSegment.Flags.Created | NetSegment.Flags.Deleted)) == NetSegment.Flags.Created)
                        {
                            NetInfo info = segments.m_buffer[(int)segmentGridZX].Info;
                            if (info.m_class.m_service == ItemClass.Service.Water)
                            {
                                Vector3 min = segments.m_buffer[(int)segmentGridZX].m_bounds.min;
                                Vector3 max = segments.m_buffer[(int)segmentGridZX].m_bounds.max;
                                if (min.x < maxX && min.z < maxZ && max.x > minX && max.z > minZ)
                                {
                                    Vector3 centerPos;
                                    Vector3 centerDirection;
                                    segments.m_buffer[(int)segmentGridZX].GetClosestPositionAndDirection(inletPos, out centerPos, out centerDirection);
                                  
                                    float distanceToPipe = Vector3.Distance(centerPos, inletPos);

                                    if (distanceToPipe < maxDistance)
                                    {
                                        Vector3 vector2 = new Vector3(centerDirection.z, 0f, -centerDirection.x);
                                        lateralAngle = vector2.normalized;
                                        if (Vector3.Dot(centerPos - inletPos, lateralAngle) < 0f)
                                        {
                                            lateralAngle = -lateralAngle;
                                        }
                                        lateralPos = centerPos;
                                        maxDistance = distanceToPipe;
                                        result = true;
                                    }


                                }
                            }
                        }
                        segmentGridZX = segments.m_buffer[(int)segmentGridZX].m_nextGridSegment;
                        if (++iterator >= 32768)
                        {
                            Debug.Log("[RF].StormDrainAI.SnapToRoad Invalid List Detected!!!");
                            break;
                        }
                    }
                }
            }
          
            return result;
        }  */

        private bool SnapToRoad(Vector3 refPos, out Vector3 pos, out Vector3 dir, float maxDistance)
        {
            bool result = false;
            pos = refPos;
            dir = Vector3.forward;
            float minX = refPos.x - maxDistance - 100f;
            float minZ = refPos.z - maxDistance - 100f;
            float maxX = refPos.x + maxDistance + 100f;
            float maxZ = refPos.z + maxDistance + 100f;
            int minXint = Mathf.Max((int)(minX / 64f + 135f), 0);
            int minZint = Mathf.Max((int)(minZ / 64f + 135f), 0);
            int maxXint = Mathf.Min((int)(maxX / 64f + 135f), 269);
            int maxZint = Mathf.Min((int)(maxZ / 64f + 135f), 269);

            Array16<NetSegment> segments = Singleton<NetManager>.instance.m_segments;
            ushort[] segmentGrid = Singleton<NetManager>.instance.m_segmentGrid;
            for (int i = minZint; i <= maxZint; i++)
            {
                for (int j = minXint; j <= maxXint; j++)
                {
                    ushort segmentGridZX = segmentGrid[i * 270 + j];
                    int iterator = 0;
                    while (segmentGridZX != 0)
                    {
                        NetSegment.Flags flags = segments.m_buffer[(int)segmentGridZX].m_flags;
                        if ((flags & (NetSegment.Flags.Created | NetSegment.Flags.Deleted)) == NetSegment.Flags.Created)
                        {
                            NetInfo info = segments.m_buffer[(int)segmentGridZX].Info;
                            if (info.m_class.m_service == ItemClass.Service.Road)
                            {
                                Vector3 min = segments.m_buffer[(int)segmentGridZX].m_bounds.min;
                                Vector3 max = segments.m_buffer[(int)segmentGridZX].m_bounds.max;
                                if (min.x < maxX && min.z < maxZ && max.x > minX && max.z > minZ)
                                {
                                    Vector3 centerPos;
                                    Vector3 centerDirection;
                                    segments.m_buffer[(int)segmentGridZX].GetClosestPositionAndDirection(refPos, out centerPos, out centerDirection);
                                    //Debug.Log("[RF]StormDrainAI.SnapToRoad refPos = " + refPos.ToString());
                                    //Debug.Log("[RF]StormDrainAI.SnapToRoad CenterPos = " + centerPos.ToString());
                                    float distanceToRoad = Vector3.Distance(centerPos, refPos) - info.m_halfWidth;
                                    if (distanceToRoad < maxDistance)
                                    {
                                        //Debug.Log("[RF]StormDrainAI.SnapToRoad distance to road = " + distanceToRoad);
                                        Vector3 vector2 = new Vector3(centerDirection.z, 0f, -centerDirection.x);
                                        dir = vector2.normalized;
                                        if (Vector3.Dot(centerPos - refPos, dir) < 0f)
                                        {
                                            dir = -dir;
                                        }
                                        pos = centerPos;
                                        maxDistance = distanceToRoad;
                                        result = true;
                                        //Debug.Log("[RF].StormDrainAI.SnapToRoad = true");
                                    }


                                }
                            }
                        }
                        segmentGridZX = segments.m_buffer[(int)segmentGridZX].m_nextGridSegment;
                        if (++iterator >= 32768)
                        {
                            Debug.Log("[RF].StormDrainAI.SnapToRoad Invalid List Detected!!!");
                            break;
                        }
                    }
                }
            }
            //if (result == false)
            //Debug.Log("[RF].StormDrainAI.SnapToRoad = false");
            return result;
        }
        private bool SnapToSegment(Vector3 refPos, out Vector3 pos, out Vector3 dir, float minDistance, float maxDistance, ItemClass.Service segmentType, out ushort segmentID)
        {
            bool result = false;
            pos = refPos;
            dir = Vector3.forward;
            float minX = refPos.x - maxDistance - 100f;
            float minZ = refPos.z - maxDistance - 100f;
            float maxX = refPos.x + maxDistance + 100f;
            float maxZ = refPos.z + maxDistance + 100f;
            int minXint = Mathf.Max((int)(minX / 64f + 135f), 0);
            int minZint = Mathf.Max((int)(minZ / 64f + 135f), 0);
            int maxXint = Mathf.Min((int)(maxX / 64f + 135f), 269);
            int maxZint = Mathf.Min((int)(maxZ / 64f + 135f), 269);
            segmentID = 0;

            Array16<NetSegment> segments = Singleton<NetManager>.instance.m_segments;
            ushort[] segmentGrid = Singleton<NetManager>.instance.m_segmentGrid;
            for (int i = minZint; i <= maxZint; i++)
            {
                for (int j = minXint; j <= maxXint; j++)
                {
                    ushort segmentGridZX = segmentGrid[i * 270 + j];
                    int iterator = 0;
                    while (segmentGridZX != 0)
                    {
                        NetSegment.Flags flags = segments.m_buffer[(int)segmentGridZX].m_flags;
                        if ((flags & (NetSegment.Flags.Created | NetSegment.Flags.Deleted)) == NetSegment.Flags.Created)
                        {
                            NetInfo info = segments.m_buffer[(int)segmentGridZX].Info;
                            if (info.m_class.m_service == segmentType)
                            {
                                Vector3 min = segments.m_buffer[(int)segmentGridZX].m_bounds.min;
                                Vector3 max = segments.m_buffer[(int)segmentGridZX].m_bounds.max;
                                if (min.x < maxX && min.z < maxZ && max.x > minX && max.z > minZ)
                                {
                                    Vector3 centerPos;
                                    Vector3 centerDirection;
                                    segments.m_buffer[(int)segmentGridZX].GetClosestPositionAndDirection(refPos, out centerPos, out centerDirection);
                                    //Debug.Log("[RF]StormDrainAI.SnapToRoad refPos = " + refPos.ToString());
                                    //Debug.Log("[RF]StormDrainAI.SnapToRoad CenterPos = " + centerPos.ToString());
                                    float distanceToSegment = Vector3.Distance(centerPos, refPos) - info.m_halfWidth;
                                    if (distanceToSegment < maxDistance)
                                    {
                                        //Debug.Log("[RF]StormDrainAI.SnapToRoad distance to road = " + distanceToRoad);
                                        NetSegment currentSegment = segments.m_buffer[(int)segmentGridZX];
                                        NetNode startNode = Singleton<NetManager>.instance.m_nodes.m_buffer[currentSegment.m_startNode];
                                        NetNode endNode = Singleton<NetManager>.instance.m_nodes.m_buffer[currentSegment.m_endNode];
                                        if (Vector3.Distance(startNode.m_position, centerPos) < minDistance && Vector3.Distance(startNode.m_position, centerPos) < Vector3.Distance(endNode.m_position, centerPos))
                                            centerPos = startNode.m_position;
                                        if (Vector3.Distance(endNode.m_position, centerPos) < minDistance && Vector3.Distance(endNode.m_position, centerPos) < Vector3.Distance(startNode.m_position, centerPos))
                                            centerPos = endNode.m_position;
                                        if (centerPos != startNode.m_position && centerPos != endNode.m_position)
                                        {
                                            Vector3 vector2 = new Vector3(centerDirection.z, 0f, -centerDirection.x);
                                            dir = vector2.normalized;
                                            if (Vector3.Dot(centerPos - refPos, dir) < 0f)
                                            {
                                                dir = -dir;
                                            }
                                        } else
                                        {
                                            Vector3 vector2 = new Vector3(centerPos.x - refPos.x, 0f, centerPos.z - refPos.z);
                                            dir = vector2.normalized;
                                            if (Vector3.Dot(centerPos - refPos, dir) < 0f)
                                            {
                                                dir = -dir;
                                            }
                                        }

                                        pos = centerPos;
                                        maxDistance = distanceToSegment;
                                        segmentID = segmentGridZX;
                                        result = true;
                                        if (distanceToSegment < minDistance)
                                            return false;

                                        //Debug.Log("[RF].StormDrainAI.SnapToRoad = true");
                                    }


                                }
                            }
                        }
                        segmentGridZX = segments.m_buffer[(int)segmentGridZX].m_nextGridSegment;
                        if (++iterator >= 32768)
                        {
                            Debug.Log("[RF].StormDrainAI.SnapToRoad Invalid List Detected!!!");
                            break;
                        }
                    }
                }
            }
            //if (result == false)
            //Debug.Log("[RF].StormDrainAI.SnapToRoad = false");
            return result;
        }
        protected override void HandleWorkAndVisitPlaces(ushort buildingID, ref Building buildingData, ref Citizen.BehaviourData behaviour, ref int aliveWorkerCount, ref int totalWorkerCount, ref int workPlaceCount, ref int aliveVisitorCount, ref int totalVisitorCount, ref int visitPlaceCount)
        {
            workPlaceCount += this.m_workPlaceCount0 + this.m_workPlaceCount1 + this.m_workPlaceCount2 + this.m_workPlaceCount3;
            base.GetWorkBehaviour(buildingID, ref buildingData, ref behaviour, ref aliveWorkerCount, ref totalWorkerCount);
            base.HandleWorkPlaces(buildingID, ref buildingData, this.m_workPlaceCount0, this.m_workPlaceCount1, this.m_workPlaceCount2, this.m_workPlaceCount3, ref behaviour, aliveWorkerCount, totalWorkerCount);
        }


        protected override void ProduceGoods(ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, int finalProductionRate, ref Citizen.BehaviourData behaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveVisitorCount, int totalVisitorCount, int visitPlaceCount)
        {
            bool logging = false;
            if (Hydraulics.instance.halted)
            {
                return;
            }
            if (finalProductionRate != 0)
            {
                Hydraulics.updateDistrictAndDrainageGroup(buildingID);
                bool flag = false;
                int districtInletCapacity;
                TerrainManager terrainManager = Singleton<TerrainManager>.instance;
                WaterSimulation waterSimulation = terrainManager.WaterSimulation;
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
                    finalProductionRate = 0;
                }
                //Debug.Log("[RF] Starting Produce Goods for Building " + buildingID.ToString());
                DistrictManager instance2 = Singleton<DistrictManager>.instance;
                byte district = instance2.GetDistrict(buildingData.m_position);
                int stormWaterIntake = Mathf.RoundToInt(OptionHandler.getSliderSetting("InletRateMultiplier")*(float)this.m_stormWaterIntake) * finalProductionRate / 100;
                //Debug.Log("[RF].StormDrainAI  Num = " + num.ToString());
                if (stormWaterIntake != 0)
                {


                    //Debug.Log("[RF].StormDrainAI  Num3 = " + num3.ToString());

                    int districtOutletCapacity = Hydraulics.getAreaVariableCapacity(buildingID, Hydraulics.getOutletList());
                    int districtDetentionCapacity = Hydraulics.getAreaDetentionCapacity(buildingID);
                    int usedDetentionCapacity = Hydraulics.getAreaDetainedStormwater(buildingID);
                    if (districtOutletCapacity == 0 && districtDetentionCapacity == 0)
                    {
                        if ((buildingData.m_flags & Building.Flags.Outgoing) != Building.Flags.Outgoing)
                        {
                            buildingData.m_flags |= Building.Flags.Outgoing;
                            buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem1.LineNotConnected);
                        }
                        finalProductionRate = 0;
                    }
                    else if ((buildingData.m_flags & Building.Flags.Outgoing) == Building.Flags.Outgoing)
                    {
                        buildingData.m_flags &= ~Building.Flags.Outgoing;
                        buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem1.LineNotConnected);
                    }


                    if (finalProductionRate > 0)
                    {
                        if (Hydraulics.checkGravityFlowForInlet(buildingID) == false)
                        {
                            if ((buildingData.m_flags & Building.Flags.Loading1) != Building.Flags.Loading1)
                            {
                                buildingData.m_flags |= Building.Flags.Loading1;
                                buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem1.NoPlaceforGoods);
                            }
                            finalProductionRate = 0;
                        }
                        else if ((buildingData.m_flags & Building.Flags.Loading1) == Building.Flags.Loading1)
                        {
                            buildingData.m_flags &= ~Building.Flags.Loading1;
                            buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem1.NoPlaceforGoods);
                        }
                    }

                    int outletCapacity = Hydraulics.getOutletCapacityForInlet(buildingID);
                    int stormWaterAccumulation = Hydraulics.getStormwaterAccumulation(buildingID);
                    int systemCapacity = outletCapacity + districtDetentionCapacity - stormWaterAccumulation - usedDetentionCapacity;
                    if (finalProductionRate > 0)
                    {
                        if (systemCapacity <= 0)
                        {
                            if ((buildingData.m_flags & Building.Flags.CapacityFull) != Building.Flags.CapacityFull)
                            {
                                buildingData.m_flags |= Building.Flags.CapacityFull;
                                buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem1.LandfillFull);
                            }
                            finalProductionRate = 0;
                        }
                        else if ((buildingData.m_flags & Building.Flags.CapacityFull) == Building.Flags.CapacityFull)
                        {
                            buildingData.m_flags &= ~Building.Flags.CapacityFull;
                            buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem1.LandfillFull);
                        }
                    }
                    float waterSurfaceElevation = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(buildingData.m_position));
                    int miny;
                    int avgy;
                    int maxy;
                    Singleton<TerrainManager>.instance.CalculateAreaHeight(buildingData.m_position.x, buildingData.m_position.z, buildingData.m_position.x, buildingData.m_position.z, out miny, out avgy, out maxy);
                    /*if (waterSurfaceElevation * 64 < miny &&  Hydraulics.getStormwaterAccumulation(buildingID) == 0 && OptionHandler.getDropdownSetting("GravityDrainageOption") == ModSettings._ImprovedGravityDrainageOption && this.m_electricityConsumption == 0  && Hydraulics.getHydraulicRate(buildingID) == 0 && ModSettings.ImprovedInletMechanics == true)
                    {
                        finalProductionRate = 0;

                        Debug.Log("[RF].SDai There is no water on top of the inlet. Production Rate = 0.");
                    }*/
                    int capturedWater;
                    bool flag1 = (buildingData.m_problems & Notification.Problem1.NoPlaceforGoods) == Notification.Problem1.None;
                    bool flag2 = (buildingData.m_problems & Notification.Problem1.LineNotConnected) == Notification.Problem1.None;
                    bool flag3 = (buildingData.m_problems & Notification.Problem1.WaterNotConnected) == Notification.Problem1.None;
                    bool flag4 = (buildingData.m_problems & Notification.Problem1.LandfillFull) == Notification.Problem1.None;
                    bool flag6 = (buildingData.m_problems & Notification.Problem1.TurnedOff) == Notification.Problem1.None;
                    bool flag5 = finalProductionRate > 0;
                    if (flag1 && flag2 && flag3 && flag4 && flag5 && flag6)
                    {
                        capturedWater = this.HandleWaterSource(buildingID, ref buildingData, false, stormWaterIntake, systemCapacity, 8f/*this.m_waterEffectDistance*/);
                        if (this.m_electricityConsumption == 0)
                        {
                            Vector3 pos = buildingData.CalculatePosition(this.m_waterLocationOffset);
                            Singleton<NaturalResourceManager>.instance.CheckPollution(pos, out buildingData.m_waterPollution);
                        }
                    }
                    else
                    {
                        capturedWater = 0;
                        if (buildingData.m_waterSource != 0)
                        {
                            waterSimulation.ReleaseWaterSource(buildingData.m_waterSource);
                            buildingData.m_waterSource = 0;
                        }
                    }
                    //Debug.Log("[RF].StormDrainAI  Num4 = " + num4.ToString());

                    if (capturedWater == 0)
                    {
                        finalProductionRate = 0;
                    }
                    else if (capturedWater > 0)
                    {

                        int num6 = Mathf.Min(capturedWater, stormWaterIntake, systemCapacity);
                        //Debug.Log("[RF].StormDrainAI  Num6 = " + num6.ToString());
                        //Debug.Log("[RF].StormDrainAI  StormwaterAccumulation = " + this.m_stormWaterAccumulation.ToString());

                        if (num6 > 0)
                        {
                            Hydraulics.addStormwaterAccumulation(buildingID, num6);
                            Hydraulics.setHydraulicRate(buildingID, num6);
                            if (num6 == systemCapacity)
                            {
                                //Debug.Log("[RF].StormDrainAI Flooded Inlet " + buildingID);

                                if ((buildingData.m_flags & Building.Flags.CapacityFull) != Building.Flags.CapacityFull)
                                {
                                    //Debug.Log("[RF].StormDrainAI Full Outlet problem true for " + buildingID);
                                    buildingData.m_flags |= Building.Flags.CapacityFull;
                                    //Debug.Log("[RF].StormDrainAI building " + buildingID + " now has flags " + buildingData.m_flags.ToString());
                                    //Debug.Log("[RF].StormDrainAI building " + buildingID + " has problems " + buildingData.m_problems.ToString());
                                    buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem1.LandfillFull);
                                    // Debug.Log("[RF].StormDrainAI building " + buildingID + " now has problems " + buildingData.m_problems.ToString());
                                }
                            }
                            else
                            {
                                if ((buildingData.m_flags & Building.Flags.CapacityFull) == Building.Flags.CapacityFull)
                                {
                                    // Debug.Log("[RF].StormDrainAI No longer Full");
                                    buildingData.m_flags &= ~Building.Flags.CapacityFull;
                                    buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem1.LandfillFull);
                                }
                            }

                        }
                    }
                    if (finalProductionRate == 0)
                    {
                        Hydraulics.setHydraulicRate(buildingID, 0);
                    }
                }

                //Debug.Log("[RF].StormDrainAI  production rate = " + finalProductionRate.ToString());


                //Debug.Log("[RF].StormDrainAI  StormwaterAccumulation = " + Hydraulics.getStormwaterAccumulation(buildingID).ToString() +" at building " + buildingID.ToString());
                // Debug.Log("[RF].StormDrainAI  TotalStormwaterAccumulation = " + calculateTotalStormDrainAccumulation().ToString());
                // Debug.Log("[RF].StormDrainAI  StormDrainCapacity = " + calculateStormDrainCapacity().ToString());



                int stormWaterOutlet = Mathf.RoundToInt(OptionHandler.getSliderSetting("OutletRateMultiplier")*(float)this.m_stormWaterOutlet) * finalProductionRate / 100;
                //Debug.Log("[RF].StormDrainAI  Num7 = " + stormWaterOutlet.ToString());
                if (stormWaterOutlet != 0)
                {
                    int currentStormWaterAccumulation = Hydraulics.getStormWaterAccumulationForOutlet(buildingID);
                    int districtDetentionCapacity = Hydraulics.getAreaDetentionCapacity(buildingID);
                    int districtDetainedStormwater = Hydraulics.getAreaDetainedStormwater(buildingID);
                    districtInletCapacity = Hydraulics.getAreaInletCapacity(buildingID);
                    if (districtInletCapacity == 0)
                    {
                        if ((buildingData.m_flags & Building.Flags.Incoming) != Building.Flags.Incoming)
                        {
                            buildingData.m_flags |= Building.Flags.Incoming;
                            buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem1.LineNotConnected);
                        }
                    }
                    else if ((buildingData.m_flags & Building.Flags.Incoming) == Building.Flags.Incoming)
                    {
                        buildingData.m_flags &= ~Building.Flags.Incoming;
                        buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem1.LineNotConnected);
                    }
                    //Debug.Log("[RF].StormDrainAI  Total StormwaterAccumulation = " + currentStormWaterAccumulation.ToString());
                    /*if (this.m_waterLocationOffset.z == 0 && buildingData.Width <= 1)
                    {
                        this.m_waterLocationOffset.z = 38;
                    }*/
                    float SDwaterSurfaceElevation = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(buildingData.m_position + this.m_waterLocationOffset));
                    float SDfloodingDifferential = this.m_invert;
                    float SDfloodedDifferential = this.m_soffit;
                    int[] SDfloodingRateModifiers = new int[7] { 95, 90, 75, 50, 33, 25, 20 };
                    if (SDwaterSurfaceElevation > buildingData.m_position.y + SDfloodedDifferential && OptionHandler.getDropdownSetting("GravityDrainageOption") == OptionHandler._ImprovedGravityDrainageOption && this.m_culvert == false && FloodingTimers.instance.getBuildingFloodedElapsedTime(buildingID) == -1f)
                    {
                        FloodingTimers.instance.setBuildingFloodedStartTime(buildingID);
                    } else if (SDwaterSurfaceElevation > buildingData.m_position.y + SDfloodingDifferential && OptionHandler.getDropdownSetting("GravityDrainageOption") == OptionHandler._ImprovedGravityDrainageOption && this.m_culvert == false && FloodingTimers.instance.getBuildingFloodingElapsedTime(buildingID) == -1f)
                    {
                        FloodingTimers.instance.setBuildingFloodingStartTime(buildingID);
                    }
                    else if (SDwaterSurfaceElevation > buildingData.m_position.y + SDfloodedDifferential && OptionHandler.getDropdownSetting("GravityDrainageOption") == OptionHandler._ImprovedGravityDrainageOption && this.m_culvert == false && FloodingTimers.instance.getBuildingFloodedElapsedTime(buildingID) >= OptionHandler.getSliderSetting("BuildingFloodedTimer") && OptionHandler.getCheckboxSetting("BuildingSufferFlooded"))
                    {
                        finalProductionRate = 20;
                        buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem1.Flood | Notification.Problem1.MajorProblem);
                    }
                    else if (SDwaterSurfaceElevation > buildingData.m_position.y + SDfloodingDifferential && OptionHandler.getDropdownSetting("GravityDrainageOption") == OptionHandler._ImprovedGravityDrainageOption && this.m_culvert == false && FloodingTimers.instance.getBuildingFloodingElapsedTime(buildingID) >= OptionHandler.getSliderSetting("BuildingFloodingTimer") && OptionHandler.getCheckboxSetting("BuildingSufferFlooding"))
                    {
                        FloodingTimers.instance.resetBuildingFloodedStartTime(buildingID);
                        if ((int)(SDwaterSurfaceElevation - buildingData.m_position.y - SDfloodingDifferential) >= 0 && (int)(SDwaterSurfaceElevation - buildingData.m_position.y - SDfloodingDifferential) < 7)
                        {
                            finalProductionRate = SDfloodingRateModifiers[(int)(SDwaterSurfaceElevation - buildingData.m_position.y - SDfloodingDifferential)];
                            //Debug.Log("[RF].StormDrainAI ProduceGoods-Outlet SDfloodingRateModifier is " + finalProductionRate.ToString());
                        }
                        else {
                            finalProductionRate = 50;
                            //Debug.Log("[RF].StormDrainAI ProduceGoods-Outlet SDfloodingRateModifier array out of index error");
                        }
                        buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem1.Flood);
                    }
                    else if (SDwaterSurfaceElevation <= buildingData.m_position.y + SDfloodingDifferential)
                    {
                        if (logging) Debug.Log("[RF]StormdrainAI.ProduceGoods  FloodingTimers.instance.getBuildingFloodingElapsedTime for building " + buildingID.ToString() + " = " + FloodingTimers.instance.getBuildingFloodingElapsedTime(buildingID).ToString());
                        FloodingTimers.instance.resetBuildingFloodedStartTime(buildingID);
                        FloodingTimers.instance.resetBuildingFloodingStartTime(buildingID);
                        if (logging) Debug.Log("[RF]StormdrainAI.ProduceGoods  FloodingTimers.instance.getBuildingFloodedElapsedTime for building " + buildingID.ToString() + " = " + FloodingTimers.instance.getBuildingFloodedElapsedTime(buildingID).ToString());
                        buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem1.Flood);
                        buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem1.Flood | Notification.Problem1.MajorProblem);
                    }
                    if ((buildingData.m_problems & Notification.Problem1.Electricity) != Notification.Problem1.None)
                    {
                        finalProductionRate = 0;
                    }
                    stormWaterOutlet = Mathf.RoundToInt(OptionHandler.getSliderSetting("OutletRateMultiplier")*(float)this.m_stormWaterOutlet) * finalProductionRate / 100;

                    if (currentStormWaterAccumulation > 0 && districtDetainedStormwater >= districtDetentionCapacity * 0.8 && finalProductionRate > 0 || currentStormWaterAccumulation > 0 && districtDetentionCapacity == 0 && finalProductionRate > 0)
                    {
                        int num10 = Mathf.Min(currentStormWaterAccumulation, Hydraulics.getAreaVariableCapacity(buildingID, Hydraulics.getOutletList()));


                        if (num10 > 0)
                        {
                            int num12;

                            num12 = this.HandleWaterSource(buildingID, ref buildingData, true, stormWaterOutlet, num10, 8f/*this.m_waterEffectDistance*/);
                            //Debug.Log("[RF].StormDrainAI  Num12 = " + num12.ToString());
                            if (num12 > 0)
                            {
                                int dumpedQuantity;
                                if (this.m_electricityConsumption == 0)
                                {
                                    dumpedQuantity = Hydraulics.removeAreaStormwaterAccumulation(num12, buildingID, true);
                                }
                                else
                                {
                                    if (num12 == stormWaterOutlet)
                                    {
                                        //Debug.Log("[RF]StormDrainAI.produce good Full outlet");

                                    }
                                    else {
                                        //Debug.Log("[RF]StormDrainAI.produce good Not Full outlet");

                                    }
                                    dumpedQuantity = Hydraulics.removeAreaStormwaterAccumulation(num12, buildingID, false);
                                }
                                if (dumpedQuantity >= stormWaterOutlet)
                                {

                                    if ((buildingData.m_flags & Building.Flags.CapacityFull) != Building.Flags.CapacityFull)
                                    {
                                        //Debug.Log("[RF].StormDrainAI Full Outlet problem true for " + buildingID);
                                        buildingData.m_flags |= Building.Flags.CapacityFull;
                                        //Debug.Log("[RF].StormDrainAI building " + buildingID + " now has flags " + buildingData.m_flags.ToString());
                                        //Debug.Log("[RF].StormDrainAI building " + buildingID + " has problems " + buildingData.m_problems.ToString());
                                        buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem1.LandfillFull);
                                        //Debug.Log("[RF].StormDrainAI building " + buildingID + " now has problems " + buildingData.m_problems.ToString());
                                    }

                                }
                                else
                                {

                                    if ((buildingData.m_flags & Building.Flags.CapacityFull) == Building.Flags.CapacityFull)
                                    {
                                        // Debug.Log("[RF].StormDrainAI No longer Full");
                                        buildingData.m_flags &= ~Building.Flags.CapacityFull;
                                        buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem1.LandfillFull);
                                    }
                                }
                                Hydraulics.setHydraulicRate(buildingID, dumpedQuantity);
                                //Debug.Log("[RF].StormDrainAI  dumpedQuantity = " + dumpedQuantity.ToString());
                            }


                            if (num12 == 0)
                            {
                                finalProductionRate = 0;
                            }
                        }
                        else
                        {
                            finalProductionRate = 0;
                        }
                        //Debug.Log("[RF].StormDrainAI  StormwaterAccumulation = " + Hydraulics.getAreaStormwaterAccumulation(district).ToString() + " at district " + district.ToString());
                    }
                    else
                    {
                        finalProductionRate = 0;
                        if ((buildingData.m_flags & Building.Flags.CapacityFull) == Building.Flags.CapacityFull)
                        {
                            // Debug.Log("[RF].StormDrainAI No longer Full");
                            buildingData.m_flags &= ~Building.Flags.CapacityFull;
                            buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem1.LandfillFull);
                        }
                    }

                    //Debug.Log("[RF].StormDrainAI  production rate = " + finalProductionRate.ToString());

                }
                if (stormWaterOutlet > 0)
                {
                    Hydraulics.setVariableCapacity(buildingID, stormWaterOutlet);
                }
                else if (this.m_stormWaterOutlet > 0)
                {
                    Hydraulics.setHydraulicRate(buildingID, 0);
                }

                int detentionCapacity = this.m_stormWaterDetention * finalProductionRate / 100;
                //Debug.Log("[RF].StormdrainAI detention capacity =" + detentionCapacity.ToString());
                if (detentionCapacity != 0)
                {
                    int remainingCapacity = detentionCapacity - Hydraulics.getDetainedStormwater(buildingID);
                    //Debug.Log("[RF].StormdrainAI remainingCapacity = " + remainingCapacity.ToString() + " detainedStormwater = " + Hydraulics.getDetainedStormwater(buildingID).ToString());
                    districtInletCapacity = Hydraulics.getAreaInletCapacity(buildingID);
                    if (districtInletCapacity == 0)
                    {
                        if ((buildingData.m_flags & Building.Flags.Incoming) != Building.Flags.Incoming)
                        {
                            buildingData.m_flags |= Building.Flags.Incoming;
                            buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem1.LineNotConnected);
                        }
                    }
                    else if ((buildingData.m_flags & Building.Flags.Incoming) == Building.Flags.Incoming)
                    {
                        buildingData.m_flags &= ~Building.Flags.Incoming;
                        buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem1.LineNotConnected);
                    }
                    int detainedQuantitity = Mathf.Min(Hydraulics.removeAreaStormwaterAccumulation(remainingCapacity, buildingID, true), remainingCapacity);
                    Hydraulics.setHydraulicRate(buildingID, detainedQuantitity);
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
                            buildingData.m_problems = Notification.AddProblems(buildingData.m_problems, Notification.Problem1.LandfillFull);
                            // Debug.Log("[RF].StormDrainAI building " + buildingID + " now has problems " + buildingData.m_problems.ToString());
                        }
                    }
                    else
                    {
                        if ((buildingData.m_flags & Building.Flags.CapacityFull) == Building.Flags.CapacityFull)
                        {
                            // Debug.Log("[RF].StormDrainAI No longer Full");
                            buildingData.m_flags &= ~Building.Flags.CapacityFull;
                            buildingData.m_problems = Notification.RemoveProblems(buildingData.m_problems, Notification.Problem1.LandfillFull);
                        }
                        if (remainingCapacity >= detentionCapacity)
                        {
                            finalProductionRate = 0;
                        }
                    }
                    if (finalProductionRate == 0)
                    {
                        Hydraulics.setHydraulicRate(buildingID, 0);
                    }

                }
                //Debug.Log("[RF]StormDrainAI About to do base functions");
                base.HandleDead(buildingID, ref buildingData, ref behaviour, totalWorkerCount);
                //Debug.Log("[RF]StormDrainAI hanlded dead");
                int num14 = finalProductionRate * this.m_noiseAccumulation / 100;
                if (num14 != 0)
                {
                    Singleton<ImmaterialResourceManager>.instance.AddResource(ImmaterialResourceManager.Resource.NoisePollution, num14, buildingData.m_position, this.m_noiseRadius);
                }
            }
            //Debug.Log("[RF]StormDrainAI about to base produce goods");
            base.ProduceGoods(buildingID, ref buildingData, ref frameData, productionRate, finalProductionRate, ref behaviour, aliveWorkerCount, totalWorkerCount, workPlaceCount, aliveVisitorCount, totalVisitorCount, visitPlaceCount);
            //Debug.Log("[RF]StormDrainAI finished produce goods");

        }
        public override string GetConstructionInfo(int productionRate)
        {

            if (this.m_placementMode == this.m_placementModeAlt)
            {
                return base.GetConstructionInfo(productionRate);
            }
            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                if (this.m_placementMode == BuildingInfo.PlacementMode.OnGround)
                {
                    return ("Current Elevation: " + this.currentElevation + System.Environment.NewLine + "Release ALT to Place On Ground.");
                }
                else if (this.m_placementMode == BuildingInfo.PlacementMode.OnSurface)
                {
                    return ("Current Elevation: " + this.currentElevation + System.Environment.NewLine + "Release ALT to Place on Surface.");
                }
                else if (this.m_placementMode == BuildingInfo.PlacementMode.OnTerrain)
                {
                    return ("Current Elevation: " + this.currentElevation + System.Environment.NewLine + "Release ALT to Place on Terrain.");
                }
                else if (this.m_placementMode == BuildingInfo.PlacementMode.OnWater)
                {
                    return ("Current Elevation: " + this.currentElevation + System.Environment.NewLine + "Release ALT to Place on Water.");
                }
                else if (this.m_placementMode == BuildingInfo.PlacementMode.Roadside)
                {
                    return ("Current Elevation: " + this.currentElevation + System.Environment.NewLine + "Release ALT to Place on Roadside.");
                }
                else if (this.m_placementMode == BuildingInfo.PlacementMode.Shoreline)
                {
                    return ("Current Elevation: " + this.currentElevation + System.Environment.NewLine + "Release ALT to Place on a Shoreline.");
                }
                else if (this.m_placementMode == BuildingInfo.PlacementMode.ShorelineOrGround)
                {
                    return ("Current Elevation: " + this.currentElevation + System.Environment.NewLine + "Release ALT to Place on Shoreline or Ground.");
                }
            }
            else
            {
                if (this.m_placementModeAlt == BuildingInfo.PlacementMode.OnGround)
                {
                    return ("Current Elevation: " + this.currentElevation + System.Environment.NewLine + "Press ALT to Place On Ground.");
                }
                else if (this.m_placementModeAlt == BuildingInfo.PlacementMode.OnSurface)
                {
                    return ("Current Elevation: " + this.currentElevation + System.Environment.NewLine + "Press ALT to Place on Surface.");
                }
                else if (this.m_placementModeAlt == BuildingInfo.PlacementMode.OnTerrain)
                {
                    return ("Current Elevation: " + this.currentElevation + System.Environment.NewLine + "Press ALT to Place on Terrain.");
                }
                else if (this.m_placementModeAlt == BuildingInfo.PlacementMode.OnWater)
                {
                    return ("Current Elevation: " + this.currentElevation + System.Environment.NewLine + "Press ALT to Place on Water.");
                }
                else if (this.m_placementModeAlt == BuildingInfo.PlacementMode.Roadside)
                {
                    return ("Current Elevation: " + this.currentElevation + System.Environment.NewLine + "Press ALT to Place on Roadside.");
                }
                else if (this.m_placementModeAlt == BuildingInfo.PlacementMode.Shoreline)
                {
                    return ("Current Elevation: " + this.currentElevation + System.Environment.NewLine + "Press ALT to Place on a Shoreline.");
                }
                else if (this.m_placementModeAlt == BuildingInfo.PlacementMode.ShorelineOrGround)
                {
                    return ("Current Elevation: " + this.currentElevation + System.Environment.NewLine + "Press ALT to Place on Shoreline or Ground.");
                }
            }
            return base.GetConstructionInfo(productionRate);
        }

        protected override bool CanEvacuate()
        {
            return false;
        }
        protected override bool CanSufferFromFlood(out bool onlyCollapse)
        {
            /*if (this.m_info.m_placementMode == BuildingInfo.PlacementMode.OnGround || this.m_info.m_placementMode == BuildingInfo.PlacementMode.Roadside)
            {
                onlyCollapse = true;
                return true;
            }*/
            onlyCollapse = false;
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
            bool logging = false;
            if (WaterSourceManager.AreYouAwake())
            {
                if (data.m_waterSource != 0)
                {
                    WaterSourceEntry currentWaterSourceEntry = WaterSourceManager.GetWaterSourceEntry(data.m_waterSource);
                    if (this.m_stormWaterIntake > 0)
                    {
                        if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.StormDrainInletFacility || currentWaterSourceEntry.GetBuildingID() != buildingID)
                        {
                            if (logging) Debug.Log("[RF]StormDrainAI.HandleWaterSource Set data.m_waterSource = 0 for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                            data.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0

                        }
                    } else if (this.m_stormWaterOutlet > 0)
                    {
                        if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.StormDrainOutletFacility || currentWaterSourceEntry.GetBuildingID() != buildingID)
                        {
                            if (logging) Debug.Log("[RF]StormDrainAI.HandleWaterSource Set data.m_waterSource = 0 for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                            data.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0

                        }
                    } else
                    {
                        Debug.Log("[RF]StormDrainAI.HandleWaterSource Handling water source with no intake or outlet???");
                    }
                    
                }
            }

            uint num = (uint)(Mathf.Min(rate, max) >> 1);
            if (num == 0u)
            {
                if (WaterSourceManager.AreYouAwake())
                {
                    if (data.m_waterSource != 0)
                    {
                        WaterSourceEntry currentWaterSourceEntry = WaterSourceManager.GetWaterSourceEntry(data.m_waterSource);
                        if (currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined || currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Empty)
                        {
                            if (this.m_stormWaterIntake > 0f)
                            {
                                WaterSourceManager.SetWaterSourceEntry(data.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.StormDrainInletFacility, buildingID));
                            }
                            else if (this.m_stormWaterOutlet > 0f)
                            {
                                WaterSourceManager.SetWaterSourceEntry(data.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.StormDrainOutletFacility, buildingID));
                            }
                            else
                            {
                                Debug.Log("[RF]StormDrainAI.HandleWaterSource Handling water source with no intake or outlet???");
                            }
                            if (logging) Debug.Log("[RF]StormDrainAI.HandleWaterSource SetWaterSourceEntry for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " and is " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);

                        }
                        else 
                        {

                            if (this.m_stormWaterIntake > 0)
                            {
                                if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.StormDrainInletFacility || currentWaterSourceEntry.GetBuildingID() != buildingID)
                                {
                                    if (logging) Debug.Log("[RF]StormDrainAI.HandleWaterSource Set data.m_waterSource = 0 for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                                    data.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0

                                }
                            }
                            else if (this.m_stormWaterOutlet > 0)
                            {
                                if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.StormDrainOutletFacility || currentWaterSourceEntry.GetBuildingID() != buildingID)
                                {
                                    if (logging) Debug.Log("[RF]StormDrainAI.HandleWaterSource Set data.m_waterSource = 0 for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                                    data.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0

                                }
                            }
                            else
                            {
                                Debug.Log("[RF]StormDrainAI.HandleWaterSource Handling water source with no intake or outlet???");
                            }
                        }
                    }
                }
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
                        Vector2 waterSourceOutputPositionXZ = new Vector2(sourceData.m_outputPosition.x, sourceData.m_outputPosition.z);
                        Vector3 waterLocationOffsetPosition = data.CalculatePosition(this.m_waterLocationOffset);
                        Vector2 stormDrainPositionXZ = new Vector2(waterLocationOffsetPosition.x, waterLocationOffsetPosition.z);
                        if (Vector2.Distance(stormDrainPositionXZ, waterSourceOutputPositionXZ) > 50f)
                        {
                            sourceData.m_outputPosition = waterLocationOffsetPosition;
                        }

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
                            /*
                            if (!Hydraulics.instance._SDoutletsToReleaseWaterSources.Contains(buildingID)) {
                                Hydraulics.instance._SDoutletsToReleaseWaterSources.Add(buildingID);
                            }*/
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
                        sourceData.m_outputRate = 0u;
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
                bool flag3;
                if (BuildingTool.SnapToCanal(vector2, out vector3, out vector4, out flag3, 0f, true))
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
                        sourceData2.m_outputRate = 0u;
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

            if (WaterSourceManager.AreYouAwake())
            {
                if (data.m_waterSource != 0)
                {
                    WaterSourceEntry currentWaterSourceEntry = WaterSourceManager.GetWaterSourceEntry(data.m_waterSource);
                    if (currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Undefined || currentWaterSourceEntry.GetWaterSourceType() == WaterSourceEntry.WaterSourceType.Empty)
                    {
                        if (this.m_stormWaterIntake > 0f)
                        {
                            WaterSourceManager.SetWaterSourceEntry(data.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.StormDrainInletFacility, buildingID));
                        } else if (this.m_stormWaterOutlet > 0f)
                        {
                            WaterSourceManager.SetWaterSourceEntry(data.m_waterSource, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.StormDrainOutletFacility, buildingID));
                        } else
                        {
                            Debug.Log("[RF]StormDrainAI.HandleWaterSource Handling water source with no intake or outlet???");
                        }
                        if (logging) Debug.Log("[RF]StormDrainAI.HandleWaterSource SetWaterSourceEntry for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " and is " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);

                    }
                    else
                    {
                        if (this.m_stormWaterIntake > 0)
                        {
                            if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.StormDrainInletFacility || currentWaterSourceEntry.GetBuildingID() != buildingID)
                            {
                                if (logging) Debug.Log("[RF]StormDrainAI.HandleWaterSource Set data.m_waterSource = 0 for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                                data.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0

                            }
                        }
                        else if (this.m_stormWaterOutlet > 0)
                        {
                            if (currentWaterSourceEntry.GetWaterSourceType() != WaterSourceEntry.WaterSourceType.StormDrainOutletFacility || currentWaterSourceEntry.GetBuildingID() != buildingID)
                            {
                                if (logging) Debug.Log("[RF]StormDrainAI.HandleWaterSource Set data.m_waterSource = 0 for buildingID " + buildingID.ToString() + " since WSM says WaterSource " + data.m_waterSource.ToString() + " is connected to buildingID " + currentWaterSourceEntry.GetBuildingID() + " and is a " + WaterSourceEntry.waterSourceTypeNames[currentWaterSourceEntry.GetWaterSourceType()]);
                                data.m_waterSource = 0; //If according to the WSM the watersource associated with building is already assocaited with another building then set watersource for this building to 0

                            }
                        }
                        else
                        {
                            Debug.Log("[RF]StormDrainAI.HandleWaterSource Handling water source with no intake or outlet???");
                        }
                    }
                }
            }
            //Debug.Log("[RF] num is " + ((int)num << 1).ToString());
            return (int)((int)num << 1);
        }
        public override void PlacementFailed(ToolBase.ToolErrors errors)
        {
            return;

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
            string organization;
            TerrainManager terrainManager = Singleton<TerrainManager>.instance;
            WaterSimulation waterSimulation = terrainManager.WaterSimulation;
            string area;
            string drainageGroup = Singleton<BuildingManager>.instance.GetBuildingName((ushort)buildingID, InstanceID.Empty);
            bool overrideDistrictControl = false;
            if (drainageGroup != Singleton<BuildingManager>.instance.GetDefaultBuildingName((ushort)buildingID, InstanceID.Empty))
            {
                overrideDistrictControl = true;
            }
            if (OptionHandler.getDropdownSetting("StormDrainAssetControlOption") == OptionHandler._DistrictControlOption || OptionHandler.getDropdownSetting("StormDrainAssetControlOption") == OptionHandler._IDOverrideOption && overrideDistrictControl == false)
            {
                organization = "District";
                area = "in district";
            }
            else if (OptionHandler.getDropdownSetting("StormDrainAssetControlOption") == OptionHandler._IDControlOption || OptionHandler.getDropdownSetting("StormDrainAssetControlOption") == OptionHandler._IDOverrideOption && overrideDistrictControl == true)
            {
                organization = "Group";
                area = "in group";
            }
            else
            {
                organization = "Total";
                area = "on map";
            }
            string elevationControl;
            if (OptionHandler.getDropdownSetting("GravityDrainageOption") == OptionHandler._ImprovedGravityDrainageOption)
            {
                elevationControl = "Invert";
            }
            else
            {
                elevationControl = "Outlet";
            }
            string floatFormat = "F1";
            if (this.m_stormWaterIntake > 0)
            {
                int InletStormwaterAccululation = (int)Hydraulics.getStormwaterAccumulation(buildingID);
                int DistrictInletCapacity = (int)Hydraulics.getAreaInletCapacity(buildingID);
                int DistrictStormwaterAccumulation = (int)Hydraulics.getAreaStormwaterAccumulation(buildingID);
                int miny;
                int avgy;
                int maxy;
                Singleton<TerrainManager>.instance.CalculateAreaHeight(data.m_position.x, data.m_position.z, data.m_position.x, data.m_position.z, out miny, out avgy, out maxy);
                int lines = 0;
                float inletHeight = (float)miny / 64f;
                int InletHydraulicRate = (int)Hydraulics.getHydraulicRate(buildingID);
                int DistrictHydraulicRate = (int)Hydraulics.getDistrictHydraulicRate(buildingID, Hydraulics.getInletList());
                float waterSurfaceElevation = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(data.m_position));
                ushort lowestOutletID = Hydraulics.getLowestOutletForInlet(buildingID);
                Building currentOutlet = BuildingManager.instance.m_buildings.m_buffer[lowestOutletID];
                StormDrainAI currentOutletAI = currentOutlet.Info.m_buildingAI as StormDrainAI;

                if ((data.m_problems & Notification.Problem1.LineNotConnected) != Notification.Problem1.None)
                {
                    sb.AppendLine("No outlet " + area + "!");
                    sb.AppendLine("No detention basin  " + area + "!");
                    lines += 2;
                }
                else if ((data.m_problems & Notification.Problem1.NoPlaceforGoods) != Notification.Problem1.None)
                {


                    if (currentOutletAI != null)
                    {
                        if (currentOutletAI.m_stormWaterOutlet > 0)
                        {
                            float currentOutletWSE = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(currentOutlet.m_position + currentOutletAI.m_waterLocationOffset));
                            float outletHeight = currentOutlet.m_position.y;
                            if (OptionHandler.getDropdownSetting("GravityDrainageOption") == OptionHandler._ImprovedGravityDrainageOption)
                                outletHeight += currentOutletAI.m_invert;
                            sb.AppendLine("Cannot gravity drain to outlet!");
                            //sb.AppendLine("Outlet | " + elevationControl + "/Water " + outletHeight.ToString(floatFormat) + "/" + currentOutletWSE.ToString(floatFormat));
                        }
                        else if (currentOutletAI.m_stormWaterDetention > 0)
                        {
                            float basinWSE = Hydraulics.getDetentionBasinWSE(lowestOutletID);
                            float basinInvert = currentOutlet.m_position.y - currentOutletAI.m_invert;
                            sb.AppendLine("Cannot gravity drain to basin!");
                            //sb.AppendLine("Basin | Invert/Water " + basinInvert.ToString(floatFormat)+"/"+basinWSE.ToString(floatFormat));
                        }
                        else
                        {
                            sb.AppendLine("Cannot gravity drain to anything!");
                        }
                        lines += 1;
                    }
                }
                else if ((data.m_problems & Notification.Problem1.LandfillFull) != Notification.Problem1.None)
                {
                    sb.AppendLine("Not enough outlet capacity!");
                    sb.AppendLine("Not enough detention capacity!");
                    lines += 2;
                }
                else
                {
                    sb.AppendLine("SWA | Inlet/" + organization + ": " + InletStormwaterAccululation + "/" + DistrictStormwaterAccumulation);
                    lines += 2;
                }

                sb.AppendLine("ELEV | Inlet/Water: " + inletHeight.ToString(floatFormat) + "/" + waterSurfaceElevation.ToString(floatFormat));
                lines += 1;
                if (currentOutletAI != null)
                {
                    if (lines < 4)
                    {
                        if (currentOutletAI.m_stormWaterOutlet > 0)
                        {
                            float currentOutletWSE = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(currentOutlet.m_position + currentOutletAI.m_waterLocationOffset));
                            float outletHeight = currentOutlet.m_position.y;
                            if (OptionHandler.getDropdownSetting("GravityDrainageOption") == OptionHandler._ImprovedGravityDrainageOption)
                                outletHeight += currentOutletAI.m_invert;
                            sb.AppendLine("Outlet | " + elevationControl + "/Water " + outletHeight.ToString(floatFormat) + "/" + currentOutletWSE.ToString(floatFormat));
                        }
                        else if (currentOutletAI.m_stormWaterDetention > 0)
                        {
                            float basinWSE = Hydraulics.getDetentionBasinWSE(lowestOutletID);
                            float basinInvert = currentOutlet.m_position.y - currentOutletAI.m_invert;
                            sb.AppendLine("Basin | Invert/Water " + basinInvert.ToString(floatFormat) + "/" + basinWSE.ToString(floatFormat));
                        }
                    }
                }
                sb.AppendLine("Inlet | Rate/Cap: " + InletHydraulicRate + "/" + Mathf.RoundToInt(OptionHandler.getSliderSetting("InletRateMultiplier")*(float)this.m_stormWaterIntake));
                sb.AppendLine(organization + " | Rate/Cap: " + DistrictHydraulicRate + "/" + DistrictInletCapacity);
                WaterSource sourceData = waterSimulation.m_waterSources.m_buffer[data.m_waterSource];
                //sb.AppendLine("WS | Rate/Water: " + sourceData.m_inputRate.ToString()+"/"+ sourceData.m_water.ToString());
                //sb.AppendLine("WS | ID: " + data.m_waterSource.ToString());
                //sb.AppendLine("DEBUG | X: " + data.m_position.x.ToString() + " Z:" + data.m_position.z.ToString());


            }
            else if (this.m_stormWaterOutlet > 0)
            {

                int DistrictOutletCapacity = (int)Hydraulics.getAreaOutletCapacity(buildingID);
                int OutletStormwaterAccumulation = (int)Hydraulics.getStormWaterAccumulationForOutlet(buildingID);
                float outletHeight = data.m_position.y;
                if (OptionHandler.getDropdownSetting("GravityDrainageOption") == OptionHandler._ImprovedGravityDrainageOption)
                    outletHeight += this.m_invert;
                float waterSurfaceElevation = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(data.m_position + this.m_waterLocationOffset));
                int outletHydraulicRate = (int)Hydraulics.getHydraulicRate(buildingID);
                int outletVariableCapacity = (int)Hydraulics.getVariableCapacity(buildingID);
                int districtVariableCapacity = (int)Hydraulics.getAreaVariableCapacity(buildingID, Hydraulics.getOutletList());
                int DistrictHydraulicRate = (int)Hydraulics.getDistrictHydraulicRate(buildingID, Hydraulics.getOutletList());

                if ((data.m_problems & Notification.Problem1.LineNotConnected) != Notification.Problem1.None)
                {
                    sb.AppendLine("No inlets " + area + "!");
                }
                else if ((data.m_problems & Notification.Problem1.LandfillFull) != Notification.Problem1.None)
                {
                    sb.AppendLine("Outlet rate at capacity!");
                }
                else if ((data.m_problems & Notification.Problem1.Flood) != Notification.Problem1.None && (data.m_problems & Notification.Problem1.MajorProblem) != Notification.Problem1.None)
                {
                    sb.AppendLine("Outlet is fully surcharged!");
                }
                else if ((data.m_problems & Notification.Problem1.Flood) != Notification.Problem1.None)
                {
                    sb.AppendLine("Operating at reduced capacity.");
                }

                sb.AppendLine("Available SWA " + area + ": " + OutletStormwaterAccumulation);

                sb.AppendLine("ELEV | " + elevationControl + "/Water " + outletHeight.ToString(floatFormat) + "/" + waterSurfaceElevation.ToString(floatFormat));
                //sb.Append("x=" + (data.m_position + this.m_waterLocationOffset).x + " y=" + (data.m_position + this.m_waterLocationOffset).y + " z=" + (data.m_position + this.m_waterLocationOffset).z);
                sb.AppendLine("Outlet | Rate/Cap: " + outletHydraulicRate + "/" + outletVariableCapacity);
                sb.AppendLine(organization + " | Rate/Cap: " + DistrictHydraulicRate + "/" + districtVariableCapacity);
            }
            else if (this.m_stormWaterDetention > 0)
            {
                int DetainedStormwater = (int)Hydraulics.getDetainedStormwater(buildingID);
                int DistrictDetainedStormwater = (int)Hydraulics.getAreaDetainedStormwater(buildingID);
                int InfiltrationRate = this.m_stormWaterInfiltration;
                int DistrictDetentionCapcity = (int)Hydraulics.getAreaDetentionCapacity(buildingID);
                int detentionRate = (int)Hydraulics.getHydraulicRate(buildingID);
                float basinElevation = data.m_position.y;
                float basinInvert = basinElevation + this.m_invert;
                float basinSoffit = basinElevation + this.m_soffit;
                float basinWSE = Hydraulics.getDetentionBasinWSE(buildingID);
                if ((data.m_problems & Notification.Problem1.LineNotConnected) != Notification.Problem1.None)
                {
                    sb.AppendLine("No inlets " + area + "!");
                }
                else {
                    sb.AppendLine("Rate | Influx/Infiltration: " + detentionRate.ToString() + "/" + InfiltrationRate.ToString());
                }
                sb.AppendLine("ELEV | Invert/Soffit: " + basinInvert.ToString(floatFormat) + "/" + basinSoffit.ToString(floatFormat));
                sb.AppendLine("ELEV | Water Surface:" + basinWSE.ToString(floatFormat));
                sb.AppendLine("Storage | " + DetainedStormwater + "/" + this.m_stormWaterDetention);
                sb.AppendLine(organization + " Storage | " + DistrictDetainedStormwater + "/" + DistrictDetentionCapcity);
            }

            return sb.ToString();
        }

        public override bool RequireRoadAccess()
        {
            if (this.m_filter)
            {
                return true;
            }
            return base.RequireRoadAccess() || this.m_workPlaceCount0 != 0 || this.m_workPlaceCount1 != 0 || this.m_workPlaceCount2 != 0 || this.m_workPlaceCount3 != 0;
        }

        public override ToolBase.ToolErrors CheckBulldozing(ushort buildingID, ref Building data)
        {
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
        public override bool CheckUnlocking()
        {
            MilestoneInfo unlockMilestoneInfo = null;
            try
            {
                if (!Singleton<UnlockManager>.instance.m_allMilestones.TryGetValue("Milestone" + this.m_milestone.ToString(), out unlockMilestoneInfo))
                {
                    unlockMilestoneInfo = null;
                }
                else
                {
                    //Debug.Log("Successcully read milestone");
                    this.m_info.m_UnlockMilestone = unlockMilestoneInfo;
                }
            }
            catch
            {
                //Debug.Log("Could not read milestone");
                unlockMilestoneInfo = null;
            }
            return base.CheckUnlocking();
        }

        public override bool GetWaterStructureCollisionRange(out float min, out float max)
        {
            /*
            if (this.m_stormWaterIntake > 0 && this.m_electricityConsumption == 0)
            {
                min = 0f;
                max = 0f;
                return true;
            }*/

            float num = Mathf.Max(0f, (float)m_info.m_cellLength * 4f + m_waterLocationOffset.z - 37f);
            min = 0f;
            max = num / Mathf.Max(num + 2f, (float)m_info.m_cellLength * 8f);
            return true;
        }
        public override bool IgnoreBuildingCollision()
        {
            if (this.m_stormWaterIntake > 0 && this.m_electricityConsumption == 0)
            {
                return true;
            }
            if (this.m_stormWaterDetention > 0)
            {
                return true;
            }
            return base.IgnoreBuildingCollision();
        }
        private static bool SplitSegment(ushort segment, out ushort node, Vector3 position)
        {
            NetSegment netSegment = Singleton<NetManager>.instance.m_segments.m_buffer[segment];
            NetInfo info = netSegment.Info;
            uint buildIndex = netSegment.m_buildIndex;
            NetNode netNode = Singleton<NetManager>.instance.m_nodes.m_buffer[netSegment.m_startNode];
            NetNode netNode2 = Singleton<NetManager>.instance.m_nodes.m_buffer[netSegment.m_endNode];
            Vector3 perpendicularPosition = new Vector3();
            Vector3 perpendicularDirection = new Vector3();
            netSegment.GetClosestPositionAndDirection(position, out perpendicularPosition, out perpendicularDirection);
            string text = null;
            if ((netSegment.m_flags & NetSegment.Flags.CustomName) != 0)
            {
                InstanceID empty = InstanceID.Empty;
                empty.NetSegment = segment;
                text = Singleton<InstanceManager>.instance.GetName(empty);
            }
            bool flag = false;
            if (((long)Singleton<NetManager>.instance.m_adjustedSegments[segment >> 6] & (1L << (int)segment)) != 0)
            {
                flag = true;
            }
            Singleton<NetManager>.instance.ReleaseSegment(segment, keepNodes: true);
            bool flag2 = false;
            if ((netNode.m_flags & (NetNode.Flags.Moveable | NetNode.Flags.Untouchable)) == NetNode.Flags.Moveable)
            {
                if ((netNode.m_flags & NetNode.Flags.Middle) != 0)
                {
                    MoveMiddleNode(ref netSegment.m_startNode, ref netSegment.m_startDirection, position);
                }
                else if ((netNode.m_flags & NetNode.Flags.End) != 0)
                {
                    MoveEndNode(ref netSegment.m_startNode, ref netSegment.m_startDirection, position);
                }
            }
            if ((netNode2.m_flags & (NetNode.Flags.Moveable | NetNode.Flags.Untouchable)) == NetNode.Flags.Moveable)
            {
                if ((netNode2.m_flags & NetNode.Flags.Middle) != 0)
                {
                    MoveMiddleNode(ref netSegment.m_endNode, ref netSegment.m_endDirection, position);
                }
                else if ((netNode2.m_flags & NetNode.Flags.End) != 0)
                {
                    MoveEndNode(ref netSegment.m_endNode, ref netSegment.m_endDirection, position);
                }
            }
            ushort segment2 = 0;
            ushort segment3 = 0;
            if (Singleton<NetManager>.instance.CreateNode(out node, ref Singleton<SimulationManager>.instance.m_randomizer, info, position, buildIndex))
            {
                Singleton<NetManager>.instance.m_nodes.m_buffer[node].m_flags |= (netNode.m_flags & netNode2.m_flags & (NetNode.Flags.Original | NetNode.Flags.Water | NetNode.Flags.Sewage | NetNode.Flags.Heating | NetNode.Flags.Electricity));
                if (info.m_netAI.IsUnderground())
                {
                    Singleton<NetManager>.instance.m_nodes.m_buffer[node].m_elevation = (byte)((netNode.m_elevation + netNode2.m_elevation) / 2);
                    Singleton<NetManager>.instance.m_nodes.m_buffer[node].m_flags |= NetNode.Flags.Underground;
                }
                else if (info.m_netAI.IsOverground())
                {
                    float num = Singleton<TerrainManager>.instance.SampleRawHeightSmooth(position);
                    Singleton<NetManager>.instance.m_nodes.m_buffer[node].m_elevation = (byte)Mathf.Clamp(Mathf.RoundToInt(position.y - num), 1, 255);
                }
                else
                {
                    Singleton<NetManager>.instance.m_nodes.m_buffer[node].m_elevation = 0;
                    Singleton<NetManager>.instance.m_nodes.m_buffer[node].m_flags |= NetNode.Flags.OnGround;
                }
                if (netSegment.m_startNode != 0)
                {
                    if (Singleton<NetManager>.instance.CreateSegment(out segment2, ref Singleton<SimulationManager>.instance.m_randomizer, info, Singleton<NetManager>.instance.m_segments.m_buffer[segment2].TreeInfo, netSegment.m_startNode, node, netSegment.m_startDirection, -perpendicularDirection, buildIndex, Singleton<SimulationManager>.instance.m_currentBuildIndex, (netSegment.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None))
                    {
                        Singleton<SimulationManager>.instance.m_currentBuildIndex += 2u;
                        Singleton<NetManager>.instance.m_segments.m_buffer[segment2].m_flags |= (netSegment.m_flags & (NetSegment.Flags.Original | NetSegment.Flags.Collapsed | NetSegment.Flags.WaitingPath | NetSegment.Flags.TrafficStart | NetSegment.Flags.CrossingStart | NetSegment.Flags.HeavyBan | NetSegment.Flags.Blocked | NetSegment.Flags.Flooded | NetSegment.Flags.BikeBan | NetSegment.Flags.CarBan | NetSegment.Flags.YieldStart));
                        Singleton<NetManager>.instance.m_segments.m_buffer[segment2].m_wetness = netSegment.m_wetness;
                        Singleton<NetManager>.instance.m_segments.m_buffer[segment2].m_condition = netSegment.m_condition;
                        Singleton<NetManager>.instance.m_segments.m_buffer[segment2].m_nameSeed = netSegment.m_nameSeed;
                        if (text != null)
                        {
                            Singleton<NetManager>.instance.m_segments.m_buffer[segment2].m_flags |= NetSegment.Flags.CustomName;
                            InstanceID empty2 = InstanceID.Empty;
                            empty2.NetSegment = segment2;
                            Singleton<InstanceManager>.instance.SetName(empty2, text);
                        }
                        if (flag)
                        {
                            Singleton<NetManager>.instance.m_adjustedSegments[segment2 >> 6] |= (ulong)(1L << (int)segment2);
                        }
                    }
                    else
                    {
                        flag2 = true;
                    }
                }
                if (netSegment.m_endNode != 0)
                {
                    if (info.m_requireContinuous)
                    {
                        if (Singleton<NetManager>.instance.CreateSegment(out segment3, ref Singleton<SimulationManager>.instance.m_randomizer, info, Singleton<NetManager>.instance.m_segments.m_buffer[segment3].TreeInfo, node, netSegment.m_endNode, perpendicularDirection, netSegment.m_endDirection, buildIndex, Singleton<SimulationManager>.instance.m_currentBuildIndex, (netSegment.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None))
                        {
                            Singleton<SimulationManager>.instance.m_currentBuildIndex += 2u;
                            Singleton<NetManager>.instance.m_segments.m_buffer[segment3].m_flags |= (netSegment.m_flags & (NetSegment.Flags.Original | NetSegment.Flags.Collapsed | NetSegment.Flags.WaitingPath | NetSegment.Flags.TrafficEnd | NetSegment.Flags.CrossingEnd | NetSegment.Flags.HeavyBan | NetSegment.Flags.Blocked | NetSegment.Flags.Flooded | NetSegment.Flags.BikeBan | NetSegment.Flags.CarBan | NetSegment.Flags.YieldEnd));
                            Singleton<NetManager>.instance.m_segments.m_buffer[segment3].m_wetness = netSegment.m_wetness;
                            Singleton<NetManager>.instance.m_segments.m_buffer[segment3].m_condition = netSegment.m_condition;
                            Singleton<NetManager>.instance.m_segments.m_buffer[segment3].m_nameSeed = netSegment.m_nameSeed;
                            if (text != null)
                            {
                                Singleton<NetManager>.instance.m_segments.m_buffer[segment3].m_flags |= NetSegment.Flags.CustomName;
                                InstanceID empty3 = InstanceID.Empty;
                                empty3.NetSegment = segment3;
                                Singleton<InstanceManager>.instance.SetName(empty3, text);
                            }
                            if (flag)
                            {
                                Singleton<NetManager>.instance.m_adjustedSegments[segment3 >> 6] |= (ulong)(1L << (int)segment3);
                            }
                        }
                        else
                        {
                            flag2 = true;
                        }
                    }
                    else if (Singleton<NetManager>.instance.CreateSegment(out segment3, ref Singleton<SimulationManager>.instance.m_randomizer, info, Singleton<NetManager>.instance.m_segments.m_buffer[segment3].TreeInfo,  netSegment.m_endNode, node, netSegment.m_endDirection, perpendicularDirection, buildIndex, Singleton<SimulationManager>.instance.m_currentBuildIndex, (netSegment.m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.None))
                    {
                        Singleton<SimulationManager>.instance.m_currentBuildIndex += 2u;
                        NetSegment.Flags flags = netSegment.m_flags & (NetSegment.Flags.Original | NetSegment.Flags.Collapsed | NetSegment.Flags.WaitingPath | NetSegment.Flags.HeavyBan | NetSegment.Flags.Blocked | NetSegment.Flags.Flooded | NetSegment.Flags.BikeBan | NetSegment.Flags.CarBan);
                        if ((netSegment.m_flags & NetSegment.Flags.CrossingEnd) != 0)
                        {
                            flags |= NetSegment.Flags.CrossingStart;
                        }
                        if ((netSegment.m_flags & NetSegment.Flags.TrafficEnd) != 0)
                        {
                            flags |= NetSegment.Flags.TrafficStart;
                        }
                        if ((netSegment.m_flags & NetSegment.Flags.YieldEnd) != 0)
                        {
                            flags |= NetSegment.Flags.YieldStart;
                        }
                        Singleton<NetManager>.instance.m_segments.m_buffer[segment3].m_flags |= flags;
                        Singleton<NetManager>.instance.m_segments.m_buffer[segment3].m_wetness = netSegment.m_wetness;
                        Singleton<NetManager>.instance.m_segments.m_buffer[segment3].m_condition = netSegment.m_condition;
                        Singleton<NetManager>.instance.m_segments.m_buffer[segment3].m_nameSeed = netSegment.m_nameSeed;
                        if (text != null)
                        {
                            Singleton<NetManager>.instance.m_segments.m_buffer[segment3].m_flags |= NetSegment.Flags.CustomName;
                            InstanceID empty4 = InstanceID.Empty;
                            empty4.NetSegment = segment3;
                            Singleton<InstanceManager>.instance.SetName(empty4, text);
                        }
                        if (flag)
                        {
                            Singleton<NetManager>.instance.m_adjustedSegments[segment3 >> 6] |= (ulong)(1L << (int)segment3);
                        }
                    }
                    else
                    {
                        flag2 = true;
                    }
                }
                info.m_netAI.AfterSplitOrMove(node, ref Singleton<NetManager>.instance.m_nodes.m_buffer[node], netSegment.m_startNode, netSegment.m_endNode);
            }
            else
            {
                flag2 = true;
            }
            if (flag2 && node != 0)
            {
                Singleton<NetManager>.instance.ReleaseNode(node);
                node = 0;
            }
            return !flag2;
        }

        private static void MoveMiddleNode(ref ushort node, ref Vector3 direction, Vector3 position)
        {
            NetNode netNode = Singleton<NetManager>.instance.m_nodes.m_buffer[node];
            uint buildIndex = netNode.m_buildIndex;
            ushort num = node;
            NetInfo info = netNode.Info;
            Vector3 vector = netNode.m_position - position;
            vector.y = 0f;
            if (vector.sqrMagnitude < 2500f)
            {
                int num2 = 0;
                ushort segment;
                while (true)
                {
                    if (num2 >= 8)
                    {
                        return;
                    }
                    segment = netNode.GetSegment(num2);
                    if (segment != 0)
                    {
                        break;
                    }
                    num2++;
                }
                NetSegment netSegment = Singleton<NetManager>.instance.m_segments.m_buffer[segment];
                NetInfo info2 = netSegment.Info;
                Vector3 position2 = Singleton<NetManager>.instance.m_nodes.m_buffer[netSegment.m_startNode].m_position;
                Vector3 position3 = Singleton<NetManager>.instance.m_nodes.m_buffer[netSegment.m_endNode].m_position;
                bool flag = !NetSegment.IsStraight(position2, netSegment.m_startDirection, position3, netSegment.m_endDirection);
                bool flag2 = netSegment.m_startNode == node;
                ushort num3 = (!flag2) ? netSegment.m_startNode : netSegment.m_endNode;
                uint buildIndex2 = netSegment.m_buildIndex;
                NetNode netNode2 = Singleton<NetManager>.instance.m_nodes.m_buffer[num3];
                vector = netNode2.m_position - position;
                vector.y = 0f;
                bool flag3 = vector.sqrMagnitude >= 10000f;
                Vector3 pos;
                Vector3 dir;
                if (flag && flag3)
                {
                    netSegment.GetClosestPositionAndDirection((position + netNode2.m_position) * 0.5f, out pos, out dir);
                    if (flag2)
                    {
                        dir = -dir;
                    }
                }
                else
                {
                    
                    pos = LerpPosition(position, netNode2.m_position, 0.5f, info.m_netAI.GetLengthSnap());
                    dir = ((!flag2) ? netSegment.m_startDirection : netSegment.m_endDirection);
                }
                direction = dir;
                string text = null;
                if ((netSegment.m_flags & NetSegment.Flags.CustomName) != 0)
                {
                    InstanceID empty = InstanceID.Empty;
                    empty.NetSegment = segment;
                    text = Singleton<InstanceManager>.instance.GetName(empty);
                }
                bool flag4 = false;
                if (((long)Singleton<NetManager>.instance.m_adjustedSegments[segment >> 6] & (1L << (int)segment)) != 0)
                {
                    flag4 = true;
                }
                Singleton<NetManager>.instance.ReleaseSegment(segment, keepNodes: true);
                Singleton<NetManager>.instance.ReleaseNode(node);
                if (flag3)
                {
                    if (Singleton<NetManager>.instance.CreateNode(out node, ref Singleton<SimulationManager>.instance.m_randomizer, info, pos, buildIndex))
                    {
                        Singleton<NetManager>.instance.m_nodes.m_buffer[node].m_flags |= (netNode.m_flags & (NetNode.Flags.Original | NetNode.Flags.Water | NetNode.Flags.Sewage | NetNode.Flags.Heating | NetNode.Flags.Electricity));
                        if (info.m_netAI.IsUnderground())
                        {
                            Singleton<NetManager>.instance.m_nodes.m_buffer[node].m_elevation = netNode.m_elevation;
                            Singleton<NetManager>.instance.m_nodes.m_buffer[node].m_flags |= NetNode.Flags.Underground;
                        }
                        else if (netNode.m_elevation > 0)
                        {
                            float num4 = Singleton<TerrainManager>.instance.SampleRawHeightSmooth(pos);
                            Singleton<NetManager>.instance.m_nodes.m_buffer[node].m_elevation = (byte)Mathf.Clamp(Mathf.RoundToInt(pos.y - num4), 1, 255);
                        }
                        else
                        {
                            Singleton<NetManager>.instance.m_nodes.m_buffer[node].m_elevation = 0;
                            Singleton<NetManager>.instance.m_nodes.m_buffer[node].m_flags |= NetNode.Flags.OnGround;
                        }
                        if (flag2)
                        {
                            if (Singleton<NetManager>.instance.CreateSegment(out segment, ref Singleton<SimulationManager>.instance.m_randomizer, info2, Singleton<NetManager>.instance.m_segments.m_buffer[segment].TreeInfo, node, num3, -dir, netSegment.m_endDirection, buildIndex2, Singleton<SimulationManager>.instance.m_currentBuildIndex, (netSegment.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None))
                            {
                                Singleton<SimulationManager>.instance.m_currentBuildIndex += 2u;
                                Singleton<NetManager>.instance.m_segments.m_buffer[segment].m_flags |= (netSegment.m_flags & (NetSegment.Flags.Original | NetSegment.Flags.Collapsed | NetSegment.Flags.WaitingPath | NetSegment.Flags.TrafficEnd | NetSegment.Flags.CrossingEnd | NetSegment.Flags.HeavyBan | NetSegment.Flags.Blocked | NetSegment.Flags.Flooded | NetSegment.Flags.BikeBan | NetSegment.Flags.CarBan | NetSegment.Flags.YieldEnd));
                                Singleton<NetManager>.instance.m_segments.m_buffer[segment].m_wetness = netSegment.m_wetness;
                                Singleton<NetManager>.instance.m_segments.m_buffer[segment].m_condition = netSegment.m_condition;
                                Singleton<NetManager>.instance.m_segments.m_buffer[segment].m_nameSeed = netSegment.m_nameSeed;
                                if (text != null)
                                {
                                    Singleton<NetManager>.instance.m_segments.m_buffer[segment].m_flags |= NetSegment.Flags.CustomName;
                                    InstanceID empty2 = InstanceID.Empty;
                                    empty2.NetSegment = segment;
                                    Singleton<InstanceManager>.instance.SetName(empty2, text);
                                }
                                if (flag4)
                                {
                                    Singleton<NetManager>.instance.m_adjustedSegments[segment >> 6] |= (ulong)(1L << (int)segment);
                                }
                            }
                        }
                        else if (Singleton<NetManager>.instance.CreateSegment(out segment, ref Singleton<SimulationManager>.instance.m_randomizer, info2, Singleton<NetManager>.instance.m_segments.m_buffer[segment].TreeInfo, num3, node, netSegment.m_startDirection, -dir, buildIndex2, Singleton<SimulationManager>.instance.m_currentBuildIndex, (netSegment.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None))
                        {
                            Singleton<SimulationManager>.instance.m_currentBuildIndex += 2u;
                            Singleton<NetManager>.instance.m_segments.m_buffer[segment].m_flags |= (netSegment.m_flags & (NetSegment.Flags.Original | NetSegment.Flags.Collapsed | NetSegment.Flags.WaitingPath | NetSegment.Flags.TrafficStart | NetSegment.Flags.CrossingStart | NetSegment.Flags.HeavyBan | NetSegment.Flags.Blocked | NetSegment.Flags.Flooded | NetSegment.Flags.BikeBan | NetSegment.Flags.CarBan | NetSegment.Flags.YieldStart));
                            Singleton<NetManager>.instance.m_segments.m_buffer[segment].m_wetness = netSegment.m_wetness;
                            Singleton<NetManager>.instance.m_segments.m_buffer[segment].m_condition = netSegment.m_condition;
                            if (text != null)
                            {
                                Singleton<NetManager>.instance.m_segments.m_buffer[segment].m_flags |= NetSegment.Flags.CustomName;
                                InstanceID empty3 = InstanceID.Empty;
                                empty3.NetSegment = segment;
                                Singleton<InstanceManager>.instance.SetName(empty3, text);
                            }
                            if (flag4)
                            {
                                Singleton<NetManager>.instance.m_adjustedSegments[segment >> 6] |= (ulong)(1L << (int)segment);
                            }
                        }
                        info2.m_netAI.AfterSplitOrMove(node, ref Singleton<NetManager>.instance.m_nodes.m_buffer[node], num, num);
                    }
                }
                else
                {
                    node = num3;
                }
            }
        }

        private static void MoveEndNode(ref ushort node, ref Vector3 direction, Vector3 position)
        {
            NetNode netNode = Singleton<NetManager>.instance.m_nodes.m_buffer[node];
            NetInfo info = netNode.Info;
            Vector3 vector = netNode.m_position - position;
            vector.y = 0f;
            float minNodeDistance = info.GetMinNodeDistance();
            if (vector.sqrMagnitude < minNodeDistance * minNodeDistance)
            {
                Singleton<NetManager>.instance.ReleaseNode(node);
                node = 0;
            }
        }
        private static Vector3 LerpPosition(Vector3 refPos1, Vector3 refPos2, float t, float snap)
        {
            if (snap != 0f)
            {
                float magnitude = new Vector2(refPos2.x - refPos1.x, refPos2.z - refPos1.z).magnitude;
                if (magnitude != 0f)
                {
                    t = Mathf.Round(t * magnitude / snap + 0.01f) * (snap / magnitude);
                }
            }
            return Vector3.Lerp(refPos1, refPos2, t);
        }


    }
}
