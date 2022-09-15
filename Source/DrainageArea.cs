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

		private ushort waterSourceID;
		private float compositeRunoffCoefficient;
		public bool m_enabled = false;
		public bool m_hidden = false;
		public Vector3 m_outputPosition;
		public uint m_outputRate = 0;
		public float m_pollution = 0;
		private int DrainageAreaID = -1;
		private Vector3 m_position;
		private Quad2 m_quad;
		private readonly float gridSize = DrainageAreaGrid.drainageAreaGridQuotient;
		private readonly float basinArea = DrainageAreaGrid.drainageAreaGridQuotient * DrainageAreaGrid.drainageAreaGridQuotient;

		public static readonly int buildingGridCoefficient = 270;
		private static readonly int magicNumber = 17280;
		public static readonly float buildingGridQuotient = (float)magicNumber / (float)buildingGridCoefficient;
		public static readonly float buildingGridAddition = buildingGridCoefficient / 2f;
		private static readonly float buildingGridArea = buildingGridQuotient * buildingGridQuotient;
		private List<ushort> m_buildings = new List<ushort>();
		private List<ushort> m_segments = new List<ushort>();



		struct subbasin
		{
			private float runoffCoefficient;
			private float contributingArea;
			private Vector3 pointOfConcentration;
			private float pollutionRatio;
			public subbasin(float c, float a, Vector3 poc, float d)
            {
				this.runoffCoefficient = c;
				this.contributingArea = a;
				this.pointOfConcentration = poc;
				this.pollutionRatio = d;
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
            public float PollutionRatio
            {
                get { return pollutionRatio; }
                set { pollutionRatio = value; }
            }
        }
		public DrainageArea(int newID, Vector3 newPosition)
		{
			
			DrainageAreaID = newID;
			waterSourceID = GenerateEmptyWaterSource();
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
			
			
			compositeRunoffCoefficient = 0.2f;
			m_pollution = 0f;
		}

		public void recalculateCompositeRunoffCoefficent(bool logging)
        {
			float positionRawY = Singleton<TerrainManager>.instance.SampleRawHeightSmooth(m_position);
			float positionY = positionRawY;
			m_position.y = positionY;
			compositeRunoffCoefficient = calculateCompositeRunoffCoefficient(logging);
        }
		private float calculateCompositeRunoffCoefficient(bool logging)
        {
			
			if (!validateDrainageAreaID())
            {
				return 0f;
            }
			subbasin unimprovedSubbasin = new subbasin(OptionHandler.getSliderSetting("UndevelopedRunoffCoefficient"), 0f, m_position, 0f);
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

			if (buildingSubbasin.PollutionRatio > 0f) m_pollution = buildingSubbasin.ContributingArea * buildingSubbasin.PollutionRatio / basinArea;
			else m_pollution = 0f;

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
			if (DrainageAreaID >= 0 && DrainageAreaID < _buildingManager.m_buildingGrid.Length/4 && DrainageAreaID < _netManager.m_segmentGrid.Length/4)
            {
				return true;
            }
			return false;
        }
		private subbasin calculateBuildingImperviousArea(bool logging, bool calculatePOC)
        {
			
			subbasin buildingSubbasin = new subbasin(0f, 0f, m_position, 0f);
			if (!validateDrainageAreaID())
            {
				return buildingSubbasin;
            }
			float totalArea = 0f;
			float cummulativeImperviousArea = 0f;
			float cummulativePollutionArea = 0f;
			List<ushort> removeBuildingIDS = new List<ushort>();
			foreach (ushort buildingID in this.m_buildings)
			{
				if (!ReviewBuilding(buildingID))
				{
					removeBuildingIDS.Add(buildingID);
					continue;
				}
				else
				{
					Building currentBuilding = _buildingManager.m_buildings.m_buffer[buildingID];
					BuildingAI ai = currentBuilding.Info.m_buildingAI;
					float currentBuildingArea = (float)(currentBuilding.Length * currentBuilding.Width) * 64f;
					float currentBuildingRunoffCoefficient = 0.0f;
					bool currentBuildingPollution = false;
					if (OptionHandler.PublicBuildingAICatalog.ContainsKey(ai.GetType()))
					{
						string aiString = OptionHandler.PublicBuildingAICatalog[ai.GetType()];
						currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting(aiString);
					}
					else if (OptionHandler.PublicBuildingAISpecialCatalog.Contains(ai.GetType()))
					{

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
							currentBuildingPollution = true;
						}
						else if (currentBuilding.Info.GetSubService() == ItemClass.SubService.PlayerIndustryOil)
						{
							currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("IndustryOil");
							currentBuildingPollution = true;
						}
						else
						{
							currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("IndustryGeneral");
							currentBuildingPollution = true;
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
                                currentBuildingPollution = true;
                            }
							else if (currentBuilding.Info.GetSubService() == ItemClass.SubService.IndustrialOil)
							{
								currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("IndustryOil");
                                currentBuildingPollution = true;
                            }
							else
							{
								currentBuildingRunoffCoefficient = OptionHandler.getSliderSetting("IndustryGeneral");
                                currentBuildingPollution = true;
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
					if (currentBuildingPollution) cummulativePollutionArea += (float)currentBuildingArea;

                    if (logging == true)
					{
						Debug.Log("[RF]DrainageArea.calculatedBuildingImperviousArea " + "current building ai = " + ai.GetType().ToString() + " current building area = " + ((decimal)currentBuildingArea).ToString() + " currentBuildingRunoffCoefficent = " + ((decimal)currentBuildingRunoffCoefficient).ToString());
					}
					totalArea += currentBuildingArea;

					if (calculatePOC)
					{
						Vector3 potentialPOC = new Vector3();
						potentialPOC = currentBuilding.CalculateSidewalkPosition();
						FindRoadCenterline(currentBuilding.CalculateSidewalkPosition(), out potentialPOC, DrainageAreaGrid.drainageAreaGridQuotient * 2);
						if (buildingSubbasin.PointOfConcentration == m_position) buildingSubbasin.PointOfConcentration = potentialPOC;
						else if (potentialPOC.y < buildingSubbasin.PointOfConcentration.y && potentialPOC.y > 0)
						{
							buildingSubbasin.PointOfConcentration = potentialPOC;
						}
					}
				}
			}
			foreach(ushort buildingID in removeBuildingIDS)
            {
				if (this.m_buildings.Contains(buildingID))
                {
					this.m_buildings.Remove(buildingID);
                }
            }		
			removeBuildingIDS.Clear();
			

			if (totalArea > 0)
			{
				buildingSubbasin.ContributingArea = totalArea;
				buildingSubbasin.RunoffCoefficient = cummulativeImperviousArea / totalArea;
				buildingSubbasin.PollutionRatio = cummulativePollutionArea / totalArea;
			}
			if (logging == true) Debug.Log("[RF]DrainageArea.calculateBuildingImperviousArea buildingSubbasin.POC.y = " + buildingSubbasin.PointOfConcentration.y);
			return buildingSubbasin;
        }
		private subbasin calculateSegmentImperviousArea(bool logging)
        {
			logging = false;
			subbasin segmentSubbasin = new subbasin(0f, 0f, m_position, 0f);
			if (!validateDrainageAreaID())
			{
				return segmentSubbasin;
			}
			float totalArea = 0f;
			float cummulativeImperviousArea = 0f;
			List<ushort> removeSegmentIDS = new List<ushort>();
			foreach (ushort segmentID in this.m_segments)
			{
				if (!ReviewSegment(segmentID))
				{
					removeSegmentIDS.Add(segmentID);
					continue;
				}
				NetSegment currentSegment = _netManager.m_segments.m_buffer[segmentID];
				Vector3 startPosition = _netManager.m_nodes.m_buffer[currentSegment.m_startNode].m_position;
				NetAI ai = currentSegment.Info.m_netAI;
				float currentSegmentRunoffCoefficient = 0f;
				NetInfo currentSegmentInfo = currentSegment.Info;
				float currentSegmentArea = 0f;
						
				Vector3 endPosition = _netManager.m_nodes.m_buffer[currentSegment.m_endNode].m_position;
				Vector3 midPosition = currentSegment.GetClosestPosition((startPosition + endPosition) / 2f);
				Vector3 quarterPosition = currentSegment.GetClosestPosition((startPosition + midPosition) / 2f);
				Vector3 threeQuarterPosition = currentSegment.GetClosestPosition((midPosition + endPosition) / 2f);
				if (currentSegment.m_averageLength > 0)
				{
					currentSegmentArea = currentSegment.m_averageLength * currentSegmentInfo.m_halfWidth * 2f;
				}
				else if (currentSegment.IsStraight())
				{
					float length = Vector3.Distance(startPosition, endPosition);
					currentSegmentArea = length * currentSegmentInfo.m_halfWidth * 2f;
				}
				else
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
					Debug.Log("[RF]DrainageArea.calculateSegmentImperviousArea " + "current net ai = " + ai.GetType().ToString() + "current segment average Length = " + currentSegment.m_averageLength.ToString() + " current segment Info.m_halfwidth*2f = " + (currentSegmentInfo.m_halfWidth * 2f).ToString() + " current segment area = " + ((decimal)currentSegmentArea).ToString() + " currentSegmentRunoffCoefficient = " + ((decimal)currentSegmentRunoffCoefficient).ToString());
				}
				totalArea += currentSegmentArea;
				if (segmentSubbasin.PointOfConcentration == m_position) segmentSubbasin.PointOfConcentration = startPosition;
				List<Vector3> potentialPointsOfConcentration = new List<Vector3> { startPosition, endPosition, midPosition, quarterPosition, threeQuarterPosition };
				foreach (Vector3 potentialPOC in potentialPointsOfConcentration)
				{
					if (logging == true) Debug.Log("[RF]DrainageArea.calculateSegmentImperviousArea potentionPOC.x = " + potentialPOC.x + " potentionPOC.y = " + potentialPOC.y + " potentionPOC.z = " + potentialPOC.z);
					if (potentialPOC.y < segmentSubbasin.PointOfConcentration.y && potentialPOC.y > 0)
					{
						segmentSubbasin.PointOfConcentration = potentialPOC;
					}
				}
					
				
			}
			foreach (ushort segmentID in removeSegmentIDS)
			{
				if (this.m_segments.Contains(segmentID))
				{
					this.m_segments.Remove(segmentID);
				}
			}
			removeSegmentIDS.Clear();
			if (totalArea > 0)
			{
				segmentSubbasin.ContributingArea = totalArea;
				segmentSubbasin.RunoffCoefficient = cummulativeImperviousArea / totalArea;
			}
			if (logging == true) Debug.Log("[RF]DrainageArea.calculateSegmentImperviousArea segmentSubbain.POC.y = " + segmentSubbasin.PointOfConcentration.y);
			return segmentSubbasin;
        }
		public static bool ReviewBuilding(int id)
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
			Vector3 buildingPosition = currentBuilding.m_position;
			List<Type> unacceptableBuildingTypes = new List<Type>() { typeof(WaterFacilityAI), typeof(WindTurbineAI), typeof(WildlifeSpawnPointAI), typeof(AnimalMonumentAI), typeof(PowerPoleAI), typeof(DecorationBuildingAI), typeof(StormDrainAI), /*typeof(SnowpackAI),*/ typeof(SnowDumpAI), typeof(WaterCleanerAI), typeof(EarthquakeSensorAI), typeof(RadioMastAI), typeof(SpaceRadarAI), typeof(TaxiStandAI), typeof(TollBoothAI), typeof(TsunamiBuoyAI), typeof(WaterJunctionAI), typeof(NaturalDrainageAI) };
			if (unacceptableBuildingTypes.Contains(ai.GetType()))
			{
				//Debug.Log("[RF].DrainageAreaGrid reviewBuidling Found unacceptable building!");
				return false;
			}
			else
			{

				return true;
			}
		}
		

		public static bool ReviewSegment(ushort segmentID)
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
				//Debug.Log("[RF].DrainageBasin.reviewSegment Found " + ai.GetType().ToString() + " Segment!");
				return true;
			}

			return false;
		}

		public float GetCompositeRunoffCoefficient()
        {
			return compositeRunoffCoefficient;
        }

		
		public Quad2 GetDrainageAreaQuad()
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
		public static int CheckBuildingGridX(Vector3 position)
		{

			return Mathf.Clamp(Mathf.FloorToInt(position.x / buildingGridQuotient + buildingGridAddition), 0, buildingGridCoefficient - 1);
		}
		public static int CheckBuildingGridZ(Vector3 position)

		{
			return Mathf.Clamp(Mathf.FloorToInt(position.z / buildingGridQuotient + buildingGridAddition), 0, buildingGridCoefficient - 1);
		}

		public bool AddBuilding(ushort buildingID)
        {
			bool logging = false;
			if (this.m_buildings == null)
            {
				this.m_buildings = new List<ushort>();
            }
			if (ReviewBuilding(buildingID) && !this.m_buildings.Contains(buildingID))
            {
				if (logging) Debug.Log("[RF]DrainageArea.Addbuilding addingBuilding " + buildingID + " to grid location " + DrainageAreaID);
				this.m_buildings.Add(buildingID);
				return true;
            }

			return false;
        }
		public bool AddSegment(ushort segmentID)
		{
			if (this.m_segments == null)
            {
				this.m_segments = new List<ushort>();
            }
			bool logging = false;
			if (ReviewSegment(segmentID) && !this.m_segments.Contains(segmentID))
			{
				if (logging) Debug.Log("[RF]DrainageArea.addSegment addingSegment " + segmentID + " to grid location " + DrainageAreaID);
				this.m_segments.Add(segmentID);
				return true;
			}
			return false;
		}

		public bool RemoveBuilding(ushort buildingID)
		{

			bool logging = false;
			if (this.m_buildings.Contains(buildingID))
			{
				if (logging) Debug.Log("[RF]DrainageArea.removeBuilding removingBuilding " + buildingID + " from grid location " + DrainageAreaID);
				this.m_buildings.Remove(buildingID);
				return true;
			}
			return false;
		}
		public bool removeSegment(ushort segmentID)
		{
			bool logging = false;
			if (this.m_segments.Contains(segmentID))
			{
				if (logging) Debug.Log("[RF]DrainageArea.removeSegment removingSegment " + segmentID + " from grid location " + DrainageAreaID);
				this.m_segments.Remove(segmentID);
				return true;
			}
			return false;
		}

		private ushort GenerateEmptyWaterSource()
		{
			WaterSource newWaterSource = default(WaterSource);
			newWaterSource.m_outputPosition = this.m_outputPosition;
			newWaterSource.m_inputPosition = this.m_outputPosition;
			newWaterSource.m_inputRate = 0;
			newWaterSource.m_outputRate = 0;
			newWaterSource.m_pollution = 0;
			newWaterSource.m_water = 0;
			newWaterSource.m_type = 2;
			newWaterSource.m_target = (ushort)Mathf.Clamp(this.m_outputPosition.y, 0, 65535);
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

		public bool DoesOutputPositionEqualPosition()
        {
			return this.m_outputPosition == this.m_position;
        }
	}
}
