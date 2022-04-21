using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;


namespace Rainfall
{
	public class DrainageBasin
	{

		private static readonly BuildingManager _buildingManager = Singleton<BuildingManager>.instance;
		private static readonly int _capacity = _buildingManager.m_buildings.m_buffer.Length;
		private static readonly NetManager _netManager = Singleton<NetManager>.instance;
		private static readonly int _segmentCapacity = _netManager.m_segments.m_buffer.Length;

		private float compositeRunoffCoefficient;
		private ushort waterSourceID;
		private Vector3 outputPosition;
		private int drainageBasinID = -1;
		private Vector3 position;
		private float minX;
		private float maxX;
		private float minZ;
		private float maxZ;
		private readonly float gridSize = DrainageBasinGrid.gridQuotient;
		private readonly float basinArea = DrainageBasinGrid.gridQuotient * DrainageBasinGrid.gridQuotient;

		struct subbasin
		{
			private float runoffCoefficient;
			private float contributingArea;
			private Vector3 pointOfConcentration;
			public subbasin(float c, float a, Vector3 poc)
            {
				this.runoffCoefficient = c;
				this.contributingArea = a;
				this.pointOfConcentration = poc;
            }
			public float RunoffCoefficient
            {
				get { return runoffCoefficient; }
				set { runoffCoefficient = value; }
            }
			public float ContributingArea
			{
				get { return contributingArea; }
				set { contributingArea = value; }
            }
			public Vector3 PointOfConcentration
            {
				get { return pointOfConcentration; }
				set { pointOfConcentration = value; }
            }
		}
		public DrainageBasin(int newID, Vector3 newPosition)
		{
			bool logging = false;	
			outputPosition = new Vector3(newPosition.x, 0, newPosition.z);
			float outputY = Singleton<TerrainManager>.instance.SampleDetailHeight(outputPosition);
			outputPosition.y = outputY;
			drainageBasinID = newID;
			waterSourceID = generateEmptyWaterSource();
			//Debug.Log("[RF]DrainageBasin watersource id = " + waterSourceID.ToString());
			position = newPosition;
			minX = newPosition.x;
			maxX = newPosition.x + gridSize;
			minZ = newPosition.z;
			maxZ = newPosition.z + gridSize;
			compositeRunoffCoefficient = calculateCompositeRunoffCoefficient(logging);
		}

		public void recalculateCompositeRunoffCoefficent(bool logging)
        {
			compositeRunoffCoefficient = calculateCompositeRunoffCoefficient(logging);
        }
		private float calculateCompositeRunoffCoefficient(bool logging)
        {
			if (!validateDrainageBasinID())
            {
				return 0f;
            }
			subbasin unimprovedSubbasin = new subbasin(OptionHandler.getSliderSetting("UndevelopedRunoffCoefficient"), 0f, position);
			subbasin segmentSubbasin = calculateSegmentImperviousArea(logging);
			bool calculatePOCfromBuildings = true;
			if (segmentSubbasin.PointOfConcentration != position)
            {
				calculatePOCfromBuildings = false;
				outputPosition = segmentSubbasin.PointOfConcentration;
            }
			subbasin buildingSubbasin = calculateBuildingImperviousArea(logging, calculatePOCfromBuildings);
			if (buildingSubbasin.PointOfConcentration != position)
            {
				outputPosition = buildingSubbasin.PointOfConcentration;
            }
			float improvedSubbasinArea = buildingSubbasin.ContributingArea + segmentSubbasin.ContributingArea;
			if (improvedSubbasinArea < basinArea) {
				unimprovedSubbasin.ContributingArea = basinArea - improvedSubbasinArea;
			}
			if (improvedSubbasinArea == 0)
            {
				outputPosition = position;
            }
			float totalImperviousArea = unimprovedSubbasin.ContributingArea * unimprovedSubbasin.RunoffCoefficient + buildingSubbasin.ContributingArea * buildingSubbasin.RunoffCoefficient + segmentSubbasin.ContributingArea * segmentSubbasin.RunoffCoefficient;
			if (basinArea > 0)
            {
				if (logging)
                {
					Debug.Log("[RF]DrainageBasin.calculateCompositeRunoffCoefficient BasinID = " + drainageBasinID.ToString() + " Composite Runoff Coefficient = " + ((decimal)(totalImperviousArea / basinArea)).ToString());
                }
				return totalImperviousArea / basinArea;
            }
			return 0f;
        }
		private bool validateDrainageBasinID ()
        {
			if (drainageBasinID >= 0 && drainageBasinID < _buildingManager.m_buildingGrid.Length && drainageBasinID < _netManager.m_segmentGrid.Length)
            {
				return true;
            }
			return false;
        }
		private subbasin calculateBuildingImperviousArea(bool logging, bool calculatePOC)
        {
			int iterator = 0;
			subbasin buildingSubbasin = new subbasin(0f, 0f, position);
			if (!validateDrainageBasinID())
            {
				return buildingSubbasin;
            }
			ushort buildingGridZX = _buildingManager.m_buildingGrid[drainageBasinID];
			
			float totalArea = 0f;
			float cummulativeImperviousArea = 0f;
			
			while (buildingGridZX != 0)
            {
				if (reviewBuilding(buildingGridZX))
                {
					Building currentBuilding = _buildingManager.m_buildings.m_buffer[buildingGridZX];
					BuildingAI ai = currentBuilding.Info.m_buildingAI;
					float currentBuildingArea = (float)(currentBuilding.Length * currentBuilding.Width) * 64f;
					float currentBuildingRunoffCoefficient = 0.0f;
					if (OptionHandler.PublicBuildingAICatalog.ContainsKey(ai.GetType())) {
						string aiString = OptionHandler.PublicBuildingAICatalog[ai.GetType()];
						currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting(aiString);
                    } else if (OptionHandler.PublicBuildingAISpecialCatalog.Contains(ai.GetType())) {
						
						if (currentBuilding.Info.GetSubService() == ItemClass.SubService.PlayerIndustryFarming)
						{
							currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("IndustryAgriculture");
						}
						else if (currentBuilding.Info.GetSubService() == ItemClass.SubService.PlayerIndustryForestry)
						{
							currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("IndustryForest");
						}
						else if (currentBuilding.Info.GetSubService() == ItemClass.SubService.PlayerIndustryOre)
						{
							currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("IndustryOre");
						}
						else if (currentBuilding.Info.GetSubService() == ItemClass.SubService.PlayerIndustryOil)
						{
							currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("IndustryOil");
						}
						else
						{
							currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("IndustryGeneral");
						}
					}
					else if (ai is PrivateBuildingAI)
					{
						if (ai is ResidentialBuildingAI && (currentBuilding.m_flags & Building.Flags.HighDensity) == Building.Flags.None)
						{

							currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("LowDensityResidential");
							if (currentBuilding.Info.GetSubService() == ItemClass.SubService.ResidentialLowEco)
							{
								
								currentBuildingRunoffCoefficient += OptionHandler.getSliderSetting("SelfSufficientResidential");
							}
						}
						else if (ai is ResidentialBuildingAI && (currentBuilding.m_flags & Building.Flags.HighDensity) != Building.Flags.None)
						{
							
							currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("HighDensityResidential");
							if (currentBuilding.Info.GetSubService() == ItemClass.SubService.ResidentialHighEco)
							{
								
								currentBuildingRunoffCoefficient += OptionHandler.getSliderSetting("SelfSufficientResidential");
							}
						}
						else if (ai is CommercialBuildingAI && (currentBuilding.m_flags & Building.Flags.HighDensity) == Building.Flags.None)
						{
							
							currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("LowDensityCommercial");
							if (currentBuilding.Info.GetSubService() == ItemClass.SubService.CommercialTourist)
							{
								
								currentBuildingRunoffCoefficient += OptionHandler.getSliderSetting("TouristCommerical");
							}
							else if (currentBuilding.Info.GetSubService() == ItemClass.SubService.CommercialLeisure)
							{
								
								currentBuildingRunoffCoefficient += OptionHandler.getSliderSetting("LeisureCommerical");
							}
							else if (currentBuilding.Info.GetSubService() == ItemClass.SubService.CommercialEco)
							{
								currentBuildingRunoffCoefficient += OptionHandler.getSliderSetting("OrganicCommerical");
							}
						}
						else if (ai is CommercialBuildingAI && (currentBuilding.m_flags & Building.Flags.HighDensity) != Building.Flags.None)
						{
							currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("HighDensityCommercial");
							if (currentBuilding.Info.GetSubService() == ItemClass.SubService.CommercialTourist)
							{
								currentBuildingRunoffCoefficient += OptionHandler.getSliderSetting("TouristCommerical");
							}
							else if (currentBuilding.Info.GetSubService() == ItemClass.SubService.CommercialLeisure)
							{
								currentBuildingRunoffCoefficient += OptionHandler.getSliderSetting("LeisureCommerical");
							}
							else if (currentBuilding.Info.GetSubService() == ItemClass.SubService.CommercialEco)
							{
								currentBuildingRunoffCoefficient += OptionHandler.getSliderSetting("OrganicCommerical");
							}
						}
						else if (ai is IndustrialBuildingAI)
						{
							
							if (currentBuilding.Info.GetSubService() == ItemClass.SubService.IndustrialFarming)
							{
								currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("IndustryAgriculture");
							}
							else if (currentBuilding.Info.GetSubService() == ItemClass.SubService.IndustrialForestry)
							{
								currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("IndustryForest");
							}
							else if (currentBuilding.Info.GetSubService() == ItemClass.SubService.IndustrialOre)
							{
								currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("IndustryOre");
							}
							else if (currentBuilding.Info.GetSubService() == ItemClass.SubService.IndustrialOil)
							{
								currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("IndustryOil");
							}
							else
							{
								currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("IndustryGeneral");
							}
						}
						else if (ai is OfficeBuildingAI)
						{
							currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("Office");
							if (currentBuilding.Info.GetSubService() == ItemClass.SubService.OfficeHightech)
							{
								currentBuildingRunoffCoefficient += OptionHandler.getSliderSetting("HighTechOffice");
							}
						}
						else
						{
							currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("DefaultRunoffCoefficient");
						}
					}
					else if (ai is DummyBuildingAI)
					{
						currentBuildingRunoffCoefficient = 0.0f;
					}
					else
					{
						currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("DefaultRunoffCoefficient");
					}
					cummulativeImperviousArea += (float)currentBuildingArea * currentBuildingRunoffCoefficient;
					if (logging == true)
					{
						Debug.Log("[RF]DrainageBasin.calculatedBuildingImperviousArea " + "current building ai = " + ai.GetType().ToString() + " current building area = " + ((decimal)currentBuildingArea).ToString() + " currentBuildingRunoffCoefficent = " + ((decimal)currentBuildingRunoffCoefficient).ToString());
					}
					totalArea += currentBuildingArea;
					if (calculatePOC && currentBuilding.CalculateSidewalkPosition().y < buildingSubbasin.PointOfConcentration.y)
                    {
						buildingSubbasin.PointOfConcentration = currentBuilding.CalculateSidewalkPosition();
                    }
				}
				buildingGridZX = _buildingManager.m_buildings.m_buffer[(int)buildingGridZX].m_nextGridBuilding;
				if (++iterator >= 32768)
				{
					Debug.Log("[RF].DrainageBasin.calculateBuildingImperviousArea Invalid List Detected!!!");
					break;
				}
			}
			
			if (totalArea > 0)
			{
				buildingSubbasin.ContributingArea = totalArea;
				buildingSubbasin.RunoffCoefficient = cummulativeImperviousArea / totalArea;
			}

			return buildingSubbasin;
        }
		private subbasin calculateSegmentImperviousArea(bool logging)
        {
			int iterator = 0;
			subbasin segmentSubbasin = new subbasin(0f, 0f, position);
			if (!validateDrainageBasinID())
			{
				return segmentSubbasin;
			}
			ushort segmentGridZX = _netManager.m_segmentGrid[drainageBasinID];
			float totalArea = 0f;
			float cummulativeImperviousArea = 0f;
			
			while (segmentGridZX != 0)
            {
				if (reviewSegment(segmentGridZX)) {
					NetSegment currentSegment = _netManager.m_segments.m_buffer[segmentGridZX];
					NetAI ai = currentSegment.Info.m_netAI;
					float currentSegmentRunoffCoefficient = 0f;
					NetInfo currentSegmentInfo = currentSegment.Info;
					float currentSegmentArea = 0f;
					Vector3 startPosition = _netManager.m_nodes.m_buffer[currentSegment.m_startNode].m_position;
					Vector3 endPosition = _netManager.m_nodes.m_buffer[currentSegment.m_endNode].m_position;
					Vector3 midPosition = currentSegment.m_middlePosition;
					Vector3 quarterPosition = currentSegment.GetClosestPosition((startPosition + midPosition) / 2f);
					Vector3 threeQuarterPosition = currentSegment.GetClosestPosition((midPosition + endPosition) / 2f);
					if (currentSegment.m_averageLength > 0)
					{
						currentSegmentArea = currentSegment.m_averageLength * currentSegmentInfo.m_halfWidth * 2f;
					} else if (currentSegment.IsStraight())
                    {
						float length = Vector3.Distance(startPosition, endPosition);
						currentSegmentArea = length * currentSegmentInfo.m_halfWidth * 2f;
					} else
                    {
						float length = Vector3.Distance(startPosition, quarterPosition) + Vector3.Distance(quarterPosition, midPosition) + Vector3.Distance(midPosition, threeQuarterPosition) + Vector3.Distance(threeQuarterPosition, endPosition);
						currentSegmentArea = length * currentSegmentInfo.m_halfWidth * 2f;
					}
					if (OptionHandler.SegmentAIRunoffCatalog.ContainsKey(ai.GetType()))
                    {
						currentSegmentRunoffCoefficient = OptionHandler.getSliderSetting(OptionHandler.SegmentAIRunoffCatalog[ai.GetType()]);
                    }
					cummulativeImperviousArea += currentSegmentArea * currentSegmentRunoffCoefficient;
					if (logging == true)
					{
						Debug.Log("[RF]DrainageBasin.calculateSegmentImperviousArea " + "current net ai = " + ai.GetType().ToString() + "current segment average Length = " + currentSegment.m_averageLength.ToString() + " current segment Info.m_halfwidth*2f = " + (currentSegmentInfo.m_halfWidth*2f).ToString() + " current segment area = " + ((decimal)currentSegmentArea).ToString() + " currentSegmentRunoffCoefficient = " + ((decimal)currentSegmentRunoffCoefficient).ToString());
					}
					totalArea += currentSegmentArea;
					List<Vector3> potentialPointsOfConcentration = new List<Vector3> { startPosition, endPosition, midPosition, quarterPosition, threeQuarterPosition };
					foreach( Vector3 potentialPOC in potentialPointsOfConcentration)
                    {
						if (potentialPOC.y < segmentSubbasin.PointOfConcentration.y)
                        {
							segmentSubbasin.PointOfConcentration = potentialPOC;
                        }
                    }
                }
				segmentGridZX = _netManager.m_segments.m_buffer[(int)segmentGridZX].m_nextGridSegment;
				if (++iterator >= 32768)
				{
					Debug.Log("[RF].DrainageBasin.calculateSegmentImperviousArea Invalid List Detected!!!");
					break;
				}
			}
			if (totalArea > 0)
			{
				segmentSubbasin.ContributingArea = totalArea;
				segmentSubbasin.RunoffCoefficient = cummulativeImperviousArea / totalArea;
			}
			return segmentSubbasin;
        }
		public static bool reviewBuilding(int id)
		{
			if (id < 0 || id > _capacity)
			{
				return false;
			}
			if ((_buildingManager.m_buildings.m_buffer[id].m_flags & Building.Flags.Created) == Building.Flags.None || (_buildingManager.m_buildings.m_buffer[id].m_flags & Building.Flags.Untouchable) != Building.Flags.None || (_buildingManager.m_buildings.m_buffer[id].m_flags & Building.Flags.BurnedDown) != Building.Flags.None || (_buildingManager.m_buildings.m_buffer[id].m_flags & Building.Flags.Demolishing) != Building.Flags.None || (_buildingManager.m_buildings.m_buffer[id].m_flags & Building.Flags.Deleted) != Building.Flags.None)
			{
				// Debug.Log("[RF].Hydrology  Failed Flag Test: " + _buildingManager.m_buildings.m_buffer[id].m_flags.ToString());
				return false;
			}
			Building currentBuilding = _buildingManager.m_buildings.m_buffer[id];
			BuildingAI ai = currentBuilding.Info.m_buildingAI;
			List<Type> unacceptableBuildingTypes = new List<Type>() { typeof(WaterFacilityAI), typeof(WindTurbineAI), typeof(WildlifeSpawnPointAI), typeof(AnimalMonumentAI), typeof(PowerPoleAI), typeof(DecorationBuildingAI), typeof(StormDrainAI), /*typeof(SnowpackAI),*/ typeof(SnowDumpAI), typeof(WaterCleanerAI), typeof(EarthquakeSensorAI), typeof(RadioMastAI), typeof(SpaceRadarAI), typeof(TaxiStandAI), typeof(TollBoothAI), typeof(TsunamiBuoyAI) };
			if (unacceptableBuildingTypes.Contains(ai.GetType()))
			{
				//Debug.Log("[RF].DrainageBasinGrid reviewBuidling Found unacceptable building!");
				return false;
			}
			return true;
		}
		public static bool reviewSegment(ushort segmentID)
		{
			if (segmentID < 0 || segmentID > _segmentCapacity)
			{
				return false;
			}
			NetSegment currentSegment = _netManager.m_segments.m_buffer[segmentID];
			if ((currentSegment.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None || (currentSegment.m_flags & NetSegment.Flags.Deleted) != NetSegment.Flags.None || (currentSegment.m_flags & NetSegment.Flags.Untouchable) != NetSegment.Flags.None)
			{
				// Debug.Log("[RF].Hydrology  Failed Flag Test: " + _buildingManager.m_buildings.m_buffer[id].m_flags.ToString());
				return false;
			}
			NetAI ai = currentSegment.Info.m_netAI;
			//NetNode startNode = _netManager.m_nodes.m_buffer[currentSegment.m_startNode];
			//NetNode endNode = _netManager.m_nodes.m_buffer[currentSegment.m_endNode];
			if ( ai.IsUnderground()) //(startNode.m_flags & NetNode.Flags.Underground) != NetNode.Flags.None || (endNode.m_flags & NetNode.Flags.Underground) != NetNode.Flags.None ||
            {
				//Debug.Log("[RF].DrainageBasin.reviewSegment Rejected undeground Segment!");
				return false;
            }
			if (OptionHandler.SegmentAIRunoffCatalog.ContainsKey(ai.GetType()))
			{
				//Debug.Log("[RF].DrainageBasin.reviewSegment Found " + ai.GetType().ToString() + " Segment!");
				return true;
			}
			return false;
		}

		private ushort generateEmptyWaterSource()
        {
			WaterSource newWaterSource = default(WaterSource);
			newWaterSource.m_outputPosition = outputPosition;
			newWaterSource.m_inputPosition = outputPosition;
			newWaterSource.m_inputRate = 0;
			newWaterSource.m_outputRate = 0;
			newWaterSource.m_pollution = 0;
			newWaterSource.m_water = 0;
			newWaterSource.m_type = 2;
			newWaterSource.m_target = (ushort)Mathf.Clamp(outputPosition.y, 0, 65535);
			newWaterSource.m_flow = 0;
			ushort newWaterSourceID;
			if (Singleton<WaterSimulation>.instance.CreateWaterSource(out newWaterSourceID, newWaterSource))
            {
				return newWaterSourceID;
            }
			return 0;
        }

		public ushort getWaterSourceID()
        {
			return waterSourceID;
        }
		public float getCompositeRunoffCoefficient()
        {
			return compositeRunoffCoefficient;
        }

	}
}
