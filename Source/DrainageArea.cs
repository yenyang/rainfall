using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;
using ColossalFramework.Math;


namespace Rainfall
{
	public class DrainageArea
	{

		private static readonly BuildingManager _buildingManager = Singleton<BuildingManager>.instance;
		private static readonly int _capacity = _buildingManager.m_buildings.m_buffer.Length;
		private static readonly NetManager _netManager = Singleton<NetManager>.instance;
		private static readonly int _segmentCapacity = _netManager.m_segments.m_buffer.Length;

		private float compositeRunoffCoefficient;
		public bool m_disabled = false;
		public Vector3 m_outputPosition;
		public uint m_outputRate = 0;
		public float m_pollution = 0;
		private int DrainageAreaID = -1;
		private Vector3 m_position;
		private Quad2 m_quad;
		private readonly float gridSize = DrainageAreaGrid.gridQuotient;
		private readonly float basinArea = DrainageAreaGrid.gridQuotient * DrainageAreaGrid.gridQuotient;

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
		public DrainageArea(int newID, Vector3 newPosition)
		{
			bool logging = false;
			DrainageAreaID = newID;
			m_position = new Vector3(newPosition.x + gridSize / 2f, 0f, newPosition.z + gridSize / 2f);
			float positionRawY = Singleton<TerrainManager>.instance.SampleRawHeightSmooth(m_position);
			float positionY = positionRawY;
			m_position.y = positionY;
			m_outputPosition = m_position;
			Vector2 a = new Vector2(m_position.x - gridSize / 2f, m_position.z - gridSize / 2f);
			Vector2 b = new Vector2(m_position.x + gridSize / 2f, m_position.z - gridSize / 2f);
			Vector2 c = new Vector2(m_position.x + gridSize / 2f, m_position.z + gridSize / 2f);
			Vector2 d = new Vector2(m_position.x - gridSize / 2f, m_position.z + gridSize / 2f);
			m_quad = new Quad2(a, b, c, d);
			compositeRunoffCoefficient = calculateCompositeRunoffCoefficient(logging);
		}

		public void recalculateCompositeRunoffCoefficent(bool logging)
        {
			compositeRunoffCoefficient = calculateCompositeRunoffCoefficient(logging);
        }
		private float calculateCompositeRunoffCoefficient(bool logging)
        {
			if (!validateDrainageAreaID())
            {
				return 0f;
            }
			subbasin unimprovedSubbasin = new subbasin(OptionHandler.getSliderSetting("UndevelopedRunoffCoefficient"), 0f, m_position);
			subbasin segmentSubbasin = calculateSegmentImperviousArea(logging);
			bool calculatePOCfromBuildings = true;
			if (segmentSubbasin.PointOfConcentration != m_position)
            {
				calculatePOCfromBuildings = false;
				m_outputPosition = segmentSubbasin.PointOfConcentration;
            }
			subbasin buildingSubbasin = calculateBuildingImperviousArea(logging, calculatePOCfromBuildings);
			if (buildingSubbasin.PointOfConcentration != m_position)
            {
				m_outputPosition = buildingSubbasin.PointOfConcentration;
            }
			float improvedSubbasinArea = buildingSubbasin.ContributingArea + segmentSubbasin.ContributingArea;
			if (improvedSubbasinArea < basinArea) {
				unimprovedSubbasin.ContributingArea = basinArea - improvedSubbasinArea;
			}
			if (improvedSubbasinArea == 0)
            {
				m_outputPosition = m_position;
            }
			float totalImperviousArea = unimprovedSubbasin.ContributingArea * unimprovedSubbasin.RunoffCoefficient + buildingSubbasin.ContributingArea * buildingSubbasin.RunoffCoefficient + segmentSubbasin.ContributingArea * segmentSubbasin.RunoffCoefficient;
			if (basinArea > 0)
            {
				if (logging)
                {
					Debug.Log("[RF]DrainageArea.calculateCompositeRunoffCoefficient BasinID = " + DrainageAreaID.ToString() + " Composite Runoff Coefficient = " + ((decimal)(totalImperviousArea / basinArea)).ToString());
                }
				return totalImperviousArea / basinArea;
            }
			return 0f;
        }
		private bool validateDrainageAreaID ()
        {
			if (DrainageAreaID >= 0 && DrainageAreaID < _buildingManager.m_buildingGrid.Length && DrainageAreaID < _netManager.m_segmentGrid.Length)
            {
				return true;
            }
			return false;
        }
		private subbasin calculateBuildingImperviousArea(bool logging, bool calculatePOC)
        {
			int iterator = 0;
			subbasin buildingSubbasin = new subbasin(0f, 0f, m_position);
			if (!validateDrainageAreaID())
            {
				return buildingSubbasin;
            }
			ushort buildingGridZX = _buildingManager.m_buildingGrid[DrainageAreaID];
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
						Debug.Log("[RF]DrainageArea.calculatedBuildingImperviousArea " + "current building ai = " + ai.GetType().ToString() + " current building area = " + ((decimal)currentBuildingArea).ToString() + " currentBuildingRunoffCoefficent = " + ((decimal)currentBuildingRunoffCoefficient).ToString());
					}
					totalArea += currentBuildingArea;
					if (calculatePOC)
					{
						Vector3 potentialPOC = new Vector3();
						potentialPOC = currentBuilding.CalculateSidewalkPosition();
						FindRoadCenterline(currentBuilding.CalculateSidewalkPosition(), out potentialPOC, 64f*2);
						if (buildingSubbasin.PointOfConcentration == m_position) buildingSubbasin.PointOfConcentration = potentialPOC;
						else if (potentialPOC.y < buildingSubbasin.PointOfConcentration.y && potentialPOC.y > 0)
						{
							buildingSubbasin.PointOfConcentration = potentialPOC;
						}
					}
				}
				buildingGridZX = _buildingManager.m_buildings.m_buffer[(int)buildingGridZX].m_nextGridBuilding;
				if (++iterator >= 32768)
				{
					Debug.Log("[RF].DrainageArea.calculateBuildingImperviousArea Invalid List Detected!!!");
					break;
				}
			}
			
			if (totalArea > 0)
			{
				buildingSubbasin.ContributingArea = totalArea;
				buildingSubbasin.RunoffCoefficient = cummulativeImperviousArea / totalArea;
			}
			if (logging == true) Debug.Log("[RF]DrainageArea.calculateBuildingImperviousArea buildingSubbasin.POC.y = " + buildingSubbasin.PointOfConcentration.y);
			return buildingSubbasin;
        }
		private subbasin calculateSegmentImperviousArea(bool logging)
        {
			int iterator = 0;
			subbasin segmentSubbasin = new subbasin(0f, 0f, m_position);
			if (!validateDrainageAreaID())
			{
				return segmentSubbasin;
			}
			ushort segmentGridZX = _netManager.m_segmentGrid[DrainageAreaID];
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
					Vector3 midPosition = currentSegment.GetClosestPosition((startPosition + endPosition) / 2f);
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
						Debug.Log("[RF]DrainageArea.calculateSegmentImperviousArea " + "current net ai = " + ai.GetType().ToString() + "current segment average Length = " + currentSegment.m_averageLength.ToString() + " current segment Info.m_halfwidth*2f = " + (currentSegmentInfo.m_halfWidth*2f).ToString() + " current segment area = " + ((decimal)currentSegmentArea).ToString() + " currentSegmentRunoffCoefficient = " + ((decimal)currentSegmentRunoffCoefficient).ToString());
					}
					totalArea += currentSegmentArea;
					if (segmentSubbasin.PointOfConcentration == m_position) segmentSubbasin.PointOfConcentration = startPosition;
					List<Vector3> potentialPointsOfConcentration = new List<Vector3> { startPosition, endPosition, midPosition, quarterPosition, threeQuarterPosition };
					foreach( Vector3 potentialPOC in potentialPointsOfConcentration)
                    {
						if (logging == true) Debug.Log("[RF]DrainageArea.calculateSegmentImperviousArea potentionPOC.x = " + potentialPOC.x + " potentionPOC.y = " + potentialPOC.y + " potentionPOC.z = " + potentialPOC.z);
						if (potentialPOC.y < segmentSubbasin.PointOfConcentration.y && potentialPOC.y > 0)
                        {
							segmentSubbasin.PointOfConcentration = potentialPOC;
                        }
                    }
                }
				segmentGridZX = _netManager.m_segments.m_buffer[(int)segmentGridZX].m_nextGridSegment;
				if (++iterator >= 32768)
				{
					Debug.Log("[RF].DrainageArea.calculateSegmentImperviousArea Invalid List Detected!!!");
					break;
				}
			}
			if (totalArea > 0)
			{
				segmentSubbasin.ContributingArea = totalArea;
				segmentSubbasin.RunoffCoefficient = cummulativeImperviousArea / totalArea;
			}
			if (logging == true) Debug.Log("[RF]DrainageArea.calculateSegmentImperviousArea segmentSubbain.POC.y = " + segmentSubbasin.PointOfConcentration.y);
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
				//Debug.Log("[RF].DrainageAreaGrid reviewBuidling Found unacceptable building!");
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
				//Debug.Log("[RF].DrainageArea.reviewSegment Rejected undeground Segment!");
				return false;
            }
			if (OptionHandler.SegmentAIRunoffCatalog.ContainsKey(ai.GetType()))
			{
				//Debug.Log("[RF].DrainageArea.reviewSegment Found " + ai.GetType().ToString() + " Segment!");
				return true;
			}
			return false;
		}

		
		public float getCompositeRunoffCoefficient()
        {
			return compositeRunoffCoefficient;
        }

		
		public Quad2 getDrainageAreaQuad()
        {
			return m_quad;
        }

		private bool FindRoadCenterline(Vector3 refPos, out Vector3 pos, float maxDistance)
		{
			bool result = false;
			pos = refPos;
			float minX = refPos.x - maxDistance;
			float minZ = refPos.z - maxDistance;
			float maxX = refPos.x + maxDistance;
			float maxZ = refPos.z + maxDistance;
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
									
									float distanceToRoad = Vector3.Distance(centerPos, refPos) - info.m_halfWidth;
									if (distanceToRoad < maxDistance)
									{
										
										Vector3 vector2 = new Vector3(centerDirection.z, 0f, -centerDirection.x);
										pos = centerPos;
										maxDistance = distanceToRoad;
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
		}

		
	}
}
