using ColossalFramework;
using ColossalFramework.DataBinding;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using System.Text;
using System;

using UnityEngine;

namespace Rainfall
{
    public class NaturalDrainageAI : PlayerBuildingAI
    {
        
        [CustomizableProperty("Water Location Offset", "Water")]
        public Vector3 m_waterLocationOffset = Vector3.zero;

        [CustomizableProperty("Max Water Placement Distance", "Water")]
        public float m_maxWaterDistance = 1000000f;

        [CustomizableProperty("Water Effect Distance", "Water")]
        public float m_waterEffectDistance = 100f;

        
        [CustomizableProperty("Milestone", "Water")]
        public int m_milestone = 0;

        [CustomizableProperty("Natural Drainage Multiplier", "Water")]
        public int m_naturalDrainageMultiplier = 1;

        [CustomizableProperty("Allow Overlap", "Water")]
        public bool m_allowOverlap = false;

        [CustomizableProperty("Standing Water", "Water")]
        public int m_standingWaterDepth = 0;

        [CustomizableProperty("Placement Mode", "Water")]
        public BuildingInfo.PlacementMode m_placementMode = BuildingInfo.PlacementMode.OnTerrain;

        [CustomizableProperty("Placement Mode Alt", "Water")]
        public BuildingInfo.PlacementMode m_placementModeAlt = BuildingInfo.PlacementMode.OnTerrain;

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
                m_resource = ImmaterialResourceManager.Resource.None,
                m_radius = 0f,
            }
            };
        }

        public override Color GetColor(ushort buildingID, ref Building data, InfoManager.InfoMode infoMode, InfoManager.SubInfoMode subInfoMode)
        {
            if (infoMode == InfoManager.InfoMode.Water)
            {
                if (this.m_naturalDrainageMultiplier != 0 && Singleton<WeatherManager>.instance.m_currentRain > 0)
                {
                    return Color.cyan;
                } 
                if ((data.m_flags & Building.Flags.Active) == Building.Flags.None)
                {
                    return Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_inactiveColor;
                } else if (this.m_naturalDrainageMultiplier != 0)
                {
                    return Color.gray;
                }
            
                return base.GetColor(buildingID, ref data, infoMode, subInfoMode);
            }
            else
            {
               
                if (infoMode == InfoManager.InfoMode.TerrainHeight)
                {
                    if ((data.m_flags & Building.Flags.Active) == Building.Flags.None)
                    {
                        return Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_inactiveColor;
                    }
                   
                }
                if (infoMode != InfoManager.InfoMode.Pollution)
                {
                    return base.GetColor(buildingID, ref data, infoMode, subInfoMode);
                }
            
                return base.GetColor(buildingID, ref data, infoMode, subInfoMode);
            }
        }

        public override void CreateBuilding(ushort buildingID, ref Building data)
        {
            base.CreateBuilding(buildingID, ref data);
            Hydraulics.addNaturalDrainageAsset(buildingID);
        }
        public override void ReleaseBuilding(ushort buildingID, ref Building data)
        {
            Hydraulics.removeNaturalDrainageAsset(buildingID);
            base.ReleaseBuilding(buildingID, ref data);
        }

        public override int GetWaterRate(ushort buildingID, ref Building data)
        {
            
          
            return 0;
        }
      
        public override void GetPlacementInfoMode(out InfoManager.InfoMode mode, out InfoManager.SubInfoMode subMode, float elevation)
        {
            
                mode = InfoManager.InfoMode.Water;
                subMode = InfoManager.SubInfoMode.WaterPower;


        }
        

        protected override void ManualActivation(ushort buildingID, ref Building buildingData)
        {
            Vector3 position = buildingData.m_position;
            position.y += this.m_info.m_size.y;
            
           
        }

        protected override void ManualDeactivation(ushort buildingID, ref Building buildingData)
        {
            Vector3 position = buildingData.m_position;
            position.y += this.m_info.m_size.y;
            
           
        }

        public override ToolBase.ToolErrors CheckBuildPosition(ushort relocateID, ref Vector3 position, ref float angle, float waterHeight, float elevation, ref Segment3 connectionSegment, out int productionRate, out int constructionCost)
        {
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                if (this.m_info.m_placementMode != this.m_placementModeAlt)
                {
                    this.m_info.m_placementMode = this.m_placementModeAlt;
                }
            } else if (this.m_info.m_placementMode != this.m_placementMode) {
                this.m_info.m_placementMode = this.m_placementMode;
            }
        
            ToolBase.ToolErrors toolErrors = base.CheckBuildPosition(relocateID, ref position, ref angle, waterHeight, elevation, ref connectionSegment, out productionRate, out constructionCost);
           
         
            return toolErrors;
        }



        protected override void ProduceGoods(ushort buildingID, ref Building buildingData, ref Building.Frame frameData, int productionRate, int finalProductionRate, ref Citizen.BehaviourData behaviour, int aliveWorkerCount, int totalWorkerCount, int workPlaceCount, int aliveVisitorCount, int totalVisitorCount, int visitPlaceCount)
        {
            productionRate = 0;
            TerrainManager instance = Singleton<TerrainManager>.instance;
            WaterSimulation waterSimulation = instance.WaterSimulation;
            if (this.m_standingWaterDepth > 0 && buildingData.m_waterSource == 0)
            {
                HandleWaterSource(buildingID, ref buildingData, true, (int)this.m_standingWaterDepth * 10, 1000, 1);
                       
            } else if (this.m_standingWaterDepth > 0)
            {
                
                WaterSource watersourceData = waterSimulation.LockWaterSource(buildingData.m_waterSource);
             
                float waterSurfaceElevation = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(buildingData.m_position));
                if (waterSurfaceElevation > watersourceData.m_target)
                {
                    watersourceData.m_outputRate = 0u;
                } else if (watersourceData.m_outputRate == 0u)
                {
                    watersourceData.m_outputRate = ((50u*watersourceData.m_water)/1000u)- ((50u * watersourceData.m_water) / 1000u)%50+50;
                }
                if (watersourceData.m_water < 50u)
                {
                    watersourceData.m_outputRate += 50u;
                    watersourceData.m_water = 1000u * (watersourceData.m_outputRate / 50);
                }
                waterSimulation.UnlockWaterSource(buildingData.m_waterSource, watersourceData);
                
            }

            if (this.m_naturalDrainageMultiplier > 1f && this.m_standingWaterDepth <= 0) {
                if (buildingData.m_waterSource == 0)
                {
                    HandleFloodSource(buildingID, ref buildingData, true, (int)(50f * m_naturalDrainageMultiplier * Singleton<WeatherManager>.instance.m_currentRain), (int)(1000f * m_naturalDrainageMultiplier), 1f);
                } else
                {
                    WaterSource watersourceData = waterSimulation.LockWaterSource(buildingData.m_waterSource);

                    
                    if (Singleton<WeatherManager>.instance.m_currentRain == 0f || Hydrology.instance.terminated == true)
                    {
                        watersourceData.m_outputRate = 0u;
                    }
                    else if (Singleton<WeatherManager>.instance.m_currentRain > 0f)
                    {
                        watersourceData.m_outputRate = (uint)(m_naturalDrainageMultiplier * Singleton<WeatherManager>.instance.m_currentRain * OptionHandler.getSliderSetting("GlobalRunoffScalar") * OptionHandler.getSliderSetting("FloodSpawnerScalar"));
                    }
                    if (watersourceData.m_water < (uint)(50f * m_naturalDrainageMultiplier))
                    {
                        watersourceData.m_water = (uint)(1000f * m_naturalDrainageMultiplier);
                    }
                    waterSimulation.UnlockWaterSource(buildingData.m_waterSource, watersourceData);
                }

            }

                

            base.ProduceGoods(buildingID, ref buildingData, ref frameData, productionRate, finalProductionRate, ref behaviour, aliveWorkerCount, totalWorkerCount, workPlaceCount, aliveVisitorCount, totalVisitorCount, visitPlaceCount);

        }

        private bool HandleWaterSource(ushort buildingID, ref Building data, bool output, int rate, int max, float radius)
        {
            TerrainManager instance = Singleton<TerrainManager>.instance;
            WaterSimulation waterSimulation = instance.WaterSimulation;
            if (data.m_waterSource != 0)
            {
                return false;
            } else
            {
                Vector3 vector2 = data.CalculatePosition(this.m_waterLocationOffset);
                WaterSource sourceData2 = default(WaterSource);
                sourceData2.m_type = 2;
                sourceData2.m_inputPosition = vector2;
                sourceData2.m_outputPosition = vector2;
                sourceData2.m_outputRate = 50u;
                sourceData2.m_inputRate = 1u;
                sourceData2.m_water = 1000u;
                //Debug.Log("[RF]NDai.HWS vector2 = " + vector2.ToString());

                sourceData2.m_target = (ushort)(vector2.y + this.m_standingWaterDepth);
                //Debug.Log("[RF]NDai.HWS target = " + sourceData2.m_target.ToString());
                if (!waterSimulation.CreateWaterSource(out data.m_waterSource, sourceData2))
                {
                    return false;
                }
            }
            return true;
        }

        private bool HandleFloodSource(ushort buildingID, ref Building data, bool output, int rate, int max, float radius)
        {
            TerrainManager instance = Singleton<TerrainManager>.instance;
            WaterSimulation waterSimulation = instance.WaterSimulation;
            if (data.m_waterSource != 0)
            {
                return false;
            }
            else
            {
                Vector3 vector2 = data.CalculatePosition(this.m_waterLocationOffset);
                WaterSource sourceData2 = default(WaterSource);
                sourceData2.m_type = 2;
                sourceData2.m_inputPosition = vector2;
                sourceData2.m_outputPosition = vector2;
                sourceData2.m_outputRate = (uint)(m_naturalDrainageMultiplier * Singleton<WeatherManager>.instance.m_currentRain * OptionHandler.getSliderSetting("GlobalRunoffScalar") * OptionHandler.getSliderSetting("FloodSpawnerScalar"));
                sourceData2.m_inputRate = 0u;
                sourceData2.m_water = (uint)(1000f * m_naturalDrainageMultiplier);
                //Debug.Log("[RF]NDai.HWS vector2 = " + vector2.ToString());

                sourceData2.m_target = (ushort)(vector2.y + 25f);
                //Debug.Log("[RF]NDai.HWS target = " + sourceData2.m_target.ToString());
                if (!waterSimulation.CreateWaterSource(out data.m_waterSource, sourceData2))
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool CanSufferFromFlood( out bool onlyCollapse)
        {
            
            onlyCollapse = false;
            return false;
        }
        
        public override float ElectricityGridRadius()
        {
            
                return 0f;
        }
        

        public override void PlacementSucceeded()
        {
            
        }



        public override string GetLocalizedTooltip()
        {
           
         
            return TooltipHelper.Append(base.GetLocalizedTooltip(), TooltipHelper.Format(new string[]
            {
            
            LocaleFormatter.Info2,
            LocaleFormatter.FormatGeneric("AIINFO_WATER_OUTLET", new object[]
            {
                0
            })
            }));
        }

        public override string GetLocalizedStats(ushort buildingID, ref Building data)
        {
            StringBuilder sb = new StringBuilder();
           
          
            string floatFormat = "F1";
            if (this.m_standingWaterDepth > 0)
            {
                float bottomElevation = data.m_position.y;
                float waterSurfaceElevation = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(data.m_position));
                float targetElevation = data.m_position.y + this.m_standingWaterDepth;
                TerrainManager instance = Singleton<TerrainManager>.instance;
                WaterSimulation waterSimulation = instance.WaterSimulation;
                if (data.m_waterSource != 0)
                {
                     WaterSource sourceData = waterSimulation.LockWaterSource(data.m_waterSource);
                    targetElevation = sourceData.m_target;
                   
                    //sb.AppendLine("Flow Rate: " + sourceData.m_flow.ToString(floatFormat));
                    /*sb.AppendLine("Water: " + sourceData.m_water.ToString(floatFormat));
                    sb.AppendLine("Target Elevation: " + targetElevation2.ToString(floatFormat));*/
                    
                    waterSimulation.UnlockWaterSource(data.m_waterSource, sourceData);
                }
                sb.AppendLine("Bottom Elevation:" + bottomElevation.ToString(floatFormat));
                sb.AppendLine("Ground Water Elevation: " + targetElevation.ToString(floatFormat));
                sb.AppendLine("Water Surface Elevation: " + waterSurfaceElevation.ToString(floatFormat));
                
            }
           
            return sb.ToString();
        }

        public override bool RequireRoadAccess()
        {
            return false;
        }

        
        public override bool CheckUnlocking()
        {
            MilestoneInfo unlockMilestoneInfo = null;
            try
            {
                if (!Singleton<UnlockManager>.instance.m_allMilestones.TryGetValue("Milestone"+this.m_milestone.ToString(), out unlockMilestoneInfo))
                {
                    unlockMilestoneInfo = null;
                } else
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

        public override bool AllowOverlap(BuildingInfo other)
        {
            return this.m_allowOverlap;
        }

    }
}
