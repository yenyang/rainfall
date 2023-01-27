using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;
using ColossalFramework.Math;

namespace Rainfall
{
    public static class DrainageAreaGrid
    {

        private static bool awake = false;


        private static readonly int waterSourceLimit = 65535;
        public static readonly int drainageAreaGridCoefficient = 135;
        private static readonly int magicNumber = 17280;
        public static readonly float drainageAreaGridQuotient = (float)magicNumber / (float)drainageAreaGridCoefficient;
        public static readonly float drainageAreaGridAddition = drainageAreaGridCoefficient / 2f;
        private static readonly float drainageAreaGridArea = drainageAreaGridQuotient * drainageAreaGridQuotient;
        private static readonly float areaGridCellSize = 1920f;
        private static readonly int areaGridResolution = 5;
        private static readonly int tiles = areaGridResolution * areaGridResolution;
        private static readonly float totalTileArea = areaGridCellSize * tiles;
        public static readonly int gridResolution = (int)Math.Ceiling(totalTileArea / drainageAreaGridArea);
        private static readonly int gridMinimum = 0; //set back to 0
        private static readonly int gridMaximum = 135; // set back to 135
        private static List<int> selectedTiles = new List<int>();
        private static gridOption selectedGridOption = gridOption.OwnedTiles;
        private static GameAreaManager gameAreaManager = Singleton<GameAreaManager>.instance;



        public enum gridOption {
            None,
            OwnedTiles,
            AdjacentTiles,
            AdjacentAndDiagonalTiles,
            TwentyFiveTiles,
            FortyNineTiles,
            EightyOneTiles,
        }
        public static readonly Dictionary<gridOption, string> gridOptionNames = new Dictionary<gridOption, string>()
        {
            {gridOption.None, "None"  },
            {gridOption.OwnedTiles, "Owned Tiles" },
            {gridOption.AdjacentTiles, "Adjacent Tiles" },
            {gridOption.AdjacentAndDiagonalTiles, "Adjacent and Diagonal Tiles" },
            {gridOption.TwentyFiveTiles, "25 Tiles" },
            {gridOption.FortyNineTiles, "49 Tiles (Requires 81 tiles)" },
            {gridOption.EightyOneTiles, "81 Tiles (Requires 81 tiles)" }
        };
        //private static gridOption[] gridOptions = new gridOption[] { gridOption.None, gridOption.OwnedTiles, gridOption.AdjacentTiles, gridOption.AdjacentAndDiagonalTiles, gridOption.TwentyFiveTiles };

        public static Dictionary<int, DrainageArea> DrainageAreaDictionary = new Dictionary<int, DrainageArea>();

        //private static readonly int gridDimension = gridCoefficient^2;
        public static void Awake()
        {
            generateDraiangeBasinGrid();
            awake = true;
            /*if (!WaterSourceManager.AreYouAwake())
            {
                WaterSourceManager.Awake();
            }*/
        }

        public static void Clear()
        {
            if (LoadingFunctions.loaded)
            {
                DrainageAreaDictionary.Clear();
                awake = false;
            }
        }

        public static bool areYouAwake()
        {
            return awake;
        }
        public static int CheckDrainageAreaGridX(Vector3 position)
        {
            
            return Mathf.Clamp(Mathf.FloorToInt(position.x / drainageAreaGridQuotient + drainageAreaGridAddition), 0, drainageAreaGridCoefficient - 1);
        }
        public static int CheckDrainageAreaGridZ(Vector3 position)
            
        {
            return Mathf.Clamp(Mathf.FloorToInt(position.z / drainageAreaGridQuotient + drainageAreaGridAddition), 0, drainageAreaGridCoefficient - 1);
        }
        public static void generateDraiangeBasinGrid()
        {

            DrainageAreaDictionary.Clear();
            List<int> allTiles = generateListOfGameAreas(gridOption.EightyOneTiles); //if only 25 tiles possible it will only do 25 tiles.
            bool logging = false;
            foreach (int tile in allTiles)
            {
                if (logging)
                {
                    Debug.Log("[RF]DrainageAreaGrid.generateDraiangeBasinGrid generatingDrainageAreasForTile " + tile.ToString());
                }
                generateDrainageAreasForTile(tile, logging);
            }
            Debug.Log("[RF]DrainageAreaGrid.generateDraiangeBasinGrid DrainageAreaDiction.Count = " + DrainageAreaDictionary.Count);
            AddBuildingsToDrainageAreaGrid();
            AddSegmentsToDrainageAreaGrid();
            RecalculateCoefficientsForAllDrainageAreas();
            selectedGridOption = getGridOptionFromInt(OptionHandler.getDropdownSetting("SelectedGridOption")); //need to find setting unique name
            selectedTiles = generateListOfGameAreas(selectedGridOption);
            foreach (int tile in selectedTiles)
            {
                if (logging)
                {
                    Debug.Log("[RF]DrainageAreaGrid.generateDraiangeBasinGrid EnablingDrainageAreasInTile " + tile.ToString());
                }
                EnableDrainageAreasInTile(tile);
            }
            return;

        }
        private static void generateDrainageAreasForTile(int tile, bool logging)
        {
            float iterator = DrainageAreaGrid.drainageAreaGridQuotient; //128
            Vector3 position = new Vector3(0, 0, 0);
            int x = 0;
            int z = 0;
            GetTileXZ(tile, out x, out z);
            gameAreaManager.GetAreaBounds(x, z, out float startX, out float startZ, out float endX, out float endZ);
            for (position.z = startZ; position.z < endZ; position.z += iterator)
            {
                for (position.x = startX; position.x < endX; position.x += iterator)
                {
                    int gridX = Mathf.Clamp(Mathf.FloorToInt(position.x / drainageAreaGridQuotient + drainageAreaGridAddition), 0, drainageAreaGridCoefficient - 1);
                    int gridZ = Mathf.Clamp(Mathf.FloorToInt(position.z / drainageAreaGridQuotient + drainageAreaGridAddition), 0, drainageAreaGridCoefficient - 1);
                    int gridLocation = gridZ * drainageAreaGridCoefficient + gridX;
                    if (gridX >= gridMinimum && gridX <= gridMaximum && gridZ >= gridMinimum && gridZ <= gridMaximum)
                    {
                        if (!DrainageAreaDictionary.ContainsKey(gridLocation) /*&& DrainageAreaDictionary.Count < 60000*/)
                        {
                            DrainageAreaDictionary.Add(gridLocation, new DrainageArea(gridLocation, position));

                            if (logging)
                            {
                                Debug.Log("[RF]DrainageAreaGrid.generateDrainageAreasForTile Added Drainage Basin to Grid at Grid Location " + gridLocation.ToString() + " and position " + position.ToString());
                            }
                        }

                    }
                }
            }
        }

        public static void updateDrainageAreaGridForNewTile(bool logging)
        {
            List<int> newSelectedTiles = generateListOfGameAreas(selectedGridOption);
            
            if (newSelectedTiles.Equals(selectedTiles))
            {
                return;
            }
            foreach(int tile in newSelectedTiles)
            {
                if (selectedTiles.Contains(tile))
                {
                    continue;
                }
                if (logging)
                {
                    Debug.Log("[RF]DrainageAreaGrid.updateDrainageAreaGridForNewTile generatingDrainageAreasForTile " + tile.ToString());
                }
                EnableDrainageAreasInTile(tile);
            }
            selectedTiles = newSelectedTiles;

        }
        private static List<int> generateListOfGameAreas(gridOption selectedGridOption)
        {
            List<int> selectedTiles = new List<int>();
            bool logging = false;
            if (selectedGridOption == gridOption.None)
            {
                return selectedTiles;
            }
            if (logging == true) Debug.Log("[RF]DBG.generateListOfGameAreas gameAreaManager.m_areaGrid.Length " + gameAreaManager.m_areaGrid.Length);

            
            if (gameAreaManager.m_areaGrid.Length == 25 && selectedGridOption == gridOption.FortyNineTiles || gameAreaManager.m_areaGrid.Length == 25 && selectedGridOption == gridOption.EightyOneTiles)
            {
                selectedGridOption = gridOption.TwentyFiveTiles;
            }

            int areaGridResolution = 5;
            if (gameAreaManager.m_areaGrid.Length == 81)
            {
                areaGridResolution = 9;
            }
            for (int z = 0; z < areaGridResolution; z++)   {
                for (int x = 0; x < areaGridResolution; x++)   { 
                    if (gameAreaManager.m_areaGrid.Length == 25 && selectedGridOption == gridOption.TwentyFiveTiles)
                    {
                        selectedTiles.Add(z * areaGridResolution + x);
                        continue;
                    } else if (gameAreaManager.m_areaGrid.Length == 81 && selectedGridOption == gridOption.TwentyFiveTiles)
                    {
                        if (x >= 2 && x <= 6 && z >= 2 && z <= 6)
                        {
                            selectedTiles.Add(z * areaGridResolution + x);
                        }
                        if (logging == true) Debug.Log("[RF]DrainageAreaGrid.generateListOfGameAreas 81 tiles enabled and Twenty Five Tiles Selected! Maybe this will work?");
                        continue;
                    } else if (gameAreaManager.m_areaGrid.Length == 81 && selectedGridOption == gridOption.FortyNineTiles)
                    {
                        if (x >= 1 && x <= 7 && z >= 1 && z <= 7)
                        {
                            selectedTiles.Add(z * areaGridResolution + x);
                        }
                        if (logging == true) Debug.Log("[RF]DrainageAreaGrid.generateListOfGameAreas 81 tiles enabled and Forty Nine Tiles Selected! Maybe this will work?");
                        continue;
                    } else if (gameAreaManager.m_areaGrid.Length == 81 && selectedGridOption == gridOption.EightyOneTiles)
                    {
                        selectedTiles.Add(z * areaGridResolution + x);
                        if (logging == true) Debug.Log("[RF]DrainageAreaGrid.generateListOfGameAreas 81 tiles enabled and Forty Nine Tiles Plus Selected! Maybe this will work?");
                        continue;
                    }
                    //to make it to this point it must be Owned, Adjacent or Adjacent + diagonal
                    if (gameAreaManager.IsUnlocked(x, z))
                    {
                        selectedTiles.Add(z * areaGridResolution + x); //For Owned, Adjacent, and Adjacent+Diagonal add Owned Tiles
                        continue;
                    } else if (selectedGridOption == gridOption.OwnedTiles) {
                        continue;
                    }
                    
                    //to make it to this point it must be Adjacent or Adjacent+Diagonal
                    if (gameAreaManager.IsUnlocked(x+1, z) || gameAreaManager.IsUnlocked(x-1, z) || gameAreaManager.IsUnlocked(x, z-1) || gameAreaManager.IsUnlocked(x, z+1))
                    {
                        selectedTiles.Add(z * areaGridResolution + x); //For Adjacent, and Adjacent+Diagonal add Adjacent to Owned Tiles
                        continue;
                    } else if (selectedGridOption == gridOption.AdjacentTiles)
                    {
                        continue;
                    }
                    //to make it to this point it must be Adjacent+Diagonal
                    if (gameAreaManager.IsUnlocked(x + 1, z+1) || gameAreaManager.IsUnlocked(x + 1, z - 1) || gameAreaManager.IsUnlocked(x - 1, z - 1) || gameAreaManager.IsUnlocked(x - 1, z + 1))
                    {
                        selectedTiles.Add(z * areaGridResolution + x); //For Adjacent+Diagonal add Diagonal to Owned Tiles
                    }
                }
            }
            if (logging == true) {
                Debug.Log("[RF]DrainageAreaGrid.GenerateListOfGameAreas selected tiles as followed: ");
                foreach (int tile in selectedTiles)
                {
                    Debug.Log("[RF]DrainageAreaGrid.GenerateListOfGameAreas " + tile.ToString());
                }
            }
            
            return selectedTiles;
        }

        public static bool recalculateCompositeRunoffCoefficentForBasinAtGridLocation(int gridLocation)
        {
            if (DrainageAreaDictionary != null)
            {
                if (DrainageAreaDictionary.ContainsKey(gridLocation))
                {
                    bool logging = false;
                    DrainageAreaDictionary[gridLocation].recalculateCompositeRunoffCoefficent(logging);
                    return true;
                }
            }
            return false;
        }

        public static float getCompositeRunoffCoefficientForDrainageArea(int DrainageAreaID)
        {
            if (DrainageAreaDictionary.ContainsKey(DrainageAreaID)) {
               return DrainageAreaDictionary[DrainageAreaID].GetCompositeRunoffCoefficient();
            }
            return 0f;
        }

        public static gridOption getGridOptionFromInt(int selection)
        {
            if (selection == 0) return gridOption.None;
            if (selection == 1) return gridOption.OwnedTiles;
            if (selection == 2) return gridOption.AdjacentTiles;
            if (selection == 3) return gridOption.AdjacentAndDiagonalTiles;
            if (selection == 4) return gridOption.TwentyFiveTiles;
            if (selection == 5) return gridOption.FortyNineTiles;
            if (selection == 6) return gridOption.EightyOneTiles;
            return gridOption.OwnedTiles;
        }

        public static void GetTileXZ(int tile, out int x, out int z)
        {
            int areaGridResolution = 5;
            if (gameAreaManager.m_areaGrid.Length == 81)
            {
                areaGridResolution = 9;
            }
            //begin mod
            x = tile % areaGridResolution;
            z = tile / areaGridResolution;
            //end mod
        }
        public static void DisableBuildingCoveredDrainageAreas(ushort buildingID)
        {
            bool logging = false;
            Quad2 buildingQuad = GetBuildingQuad(buildingID, 10f);
            float minX = Mathf.Min(buildingQuad.a.x, buildingQuad.b.x, buildingQuad.c.x, buildingQuad.d.x) - DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            float maxX = Mathf.Max(buildingQuad.a.x, buildingQuad.b.x, buildingQuad.c.x, buildingQuad.d.x) + DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            float minZ = Mathf.Min(buildingQuad.a.y, buildingQuad.b.y, buildingQuad.c.y, buildingQuad.d.y) - DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            float maxZ = Mathf.Max(buildingQuad.a.y, buildingQuad.b.y, buildingQuad.c.y, buildingQuad.d.y) + DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            int minXint = Mathf.Max((int)(minX / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0);
            int minZint = Mathf.Max((int)(minZ / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0);
            int maxXint = Mathf.Min((int)(maxX / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), DrainageAreaGrid.drainageAreaGridCoefficient-1);
            int maxZint = Mathf.Min((int)(maxZ / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            for (int i = minZint; i <= maxZint; i++)
            {
                for (int j = minXint; j <= maxXint; j++)
                {
                    int currentDrainageAreaID = i * DrainageAreaGrid.drainageAreaGridCoefficient + j;
                    if (DrainageAreaGrid.DrainageAreaDictionary.ContainsKey(currentDrainageAreaID))
                    {
                        DrainageArea currentDrainageArea = DrainageAreaGrid.DrainageAreaDictionary[currentDrainageAreaID];
                        Vector2 outputPositionXZ = new Vector2(currentDrainageArea.m_outputPosition.x, currentDrainageArea.m_outputPosition.z);
                        if (buildingQuad.Intersect(outputPositionXZ))
                        {
                            currentDrainageArea.m_enabled = false;
                            if (logging)
                            {
                                Debug.Log("[RF]DrainageAreaGrid.DisableBuildingCoveredDrainageAreas disabled drainage area " + currentDrainageAreaID);
                            }
                        }
                    }
                }
            }
            if (logging)
            {
                Debug.Log("[RF]DrainageAreaGrid.DisableBuildingCoveredDrainageAreas finished disabling covered drainage areas ");
            }
        }
        public static void EnableBuildingUncoveredDrainageAreas(ushort buildingID)
        {
            bool logging = false;
            Quad2 buildingQuad = GetBuildingQuad(buildingID, 10f);
            float minX = Mathf.Min(buildingQuad.a.x, buildingQuad.b.x, buildingQuad.c.x, buildingQuad.d.x) - DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            float maxX = Mathf.Max(buildingQuad.a.x, buildingQuad.b.x, buildingQuad.c.x, buildingQuad.d.x) + DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            float minZ = Mathf.Min(buildingQuad.a.y, buildingQuad.b.y, buildingQuad.c.y, buildingQuad.d.y) - DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            float maxZ = Mathf.Max(buildingQuad.a.y, buildingQuad.b.y, buildingQuad.c.y, buildingQuad.d.y) + DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            int minXint = Mathf.Max((int)(minX / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0);
            int minZint = Mathf.Max((int)(minZ / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0);
            int maxXint = Mathf.Min((int)(maxX / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            int maxZint = Mathf.Min((int)(maxZ / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            for (int i = minZint; i <= maxZint; i++)
            {
                for (int j = minXint; j <= maxXint; j++)
                {
                    int currentDrainageAreaID = i * DrainageAreaGrid.drainageAreaGridCoefficient + j;
                    if (DrainageAreaGrid.DrainageAreaDictionary.ContainsKey(currentDrainageAreaID))
                    {
                        DrainageArea currentDrainageArea = DrainageAreaGrid.DrainageAreaDictionary[currentDrainageAreaID];
                        Vector2 outputPositionXZ = new Vector2(currentDrainageArea.m_outputPosition.x, currentDrainageArea.m_outputPosition.z);
                        if (buildingQuad.Intersect(outputPositionXZ))
                        {
                            List<int> selectedTiles = generateListOfGameAreas(selectedGridOption);
                            int outputPositionTile = Singleton<GameAreaManager>.instance.GetAreaIndex(outputPositionXZ);
                            if (selectedTiles.Contains(outputPositionTile)) {
                                currentDrainageArea.m_enabled = true;
                                if (logging)
                                {
                                    Debug.Log("[RF]DrainageAreaGrid.EnableBuildingUncoveredDrainageAreas enabled drainage area " + currentDrainageAreaID);
                                }
                            }
                        }
                    }
                }
            }
            if (logging)
            {
                Debug.Log("[RF]DrainageAreaGrid.EnableBuildingUncoveredDrainageAreas finished enabling uncoverd drainage areas ");
            }
        }
        public static Quad2 GetBuildingQuad(ushort buildingID, float additionalWidth)
        {
            Building currentBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
            int width = currentBuilding.Width;
            int length = currentBuilding.Length;
            Vector2 b = new Vector2(Mathf.Cos(currentBuilding.m_angle), Mathf.Sin(currentBuilding.m_angle));
            Vector2 b2 = new Vector2(b.y, 0f - b.x);
            Vector2 b4 = b2;
            b *= (float)width * 4f+additionalWidth;
            b2 *= (float)length * 4f+additionalWidth;
            b4 *= (float)length * 4f;
            Vector2 a = VectorUtils.XZ(currentBuilding.m_position);
            Quad2 quad = default(Quad2);
            quad.a = a - b - b4;
            quad.b = a + b - b4;
            quad.c = a + b + b2;
            quad.d = a - b + b2;
            /*quad.a = a - b - b2;
            quad.b = a + b - b2;
            quad.c = a + b + b2;
            quad.d = a - b + b2;*/
            return quad;
        }

        public static void DisableRoadwayCoveredDrainageAreas(ushort segmentID)
        {
            bool logging = false;
            NetSegment currentSegment = Singleton<NetManager>.instance.m_segments.m_buffer[segmentID];
            float width = currentSegment.Info.m_halfWidth;
            Vector3 startPosition = Singleton<NetManager>.instance.m_nodes.m_buffer[currentSegment.m_startNode].m_position;
            Vector3 endPosition = Singleton<NetManager>.instance.m_nodes.m_buffer[currentSegment.m_endNode].m_position;
            Vector3 midPosition = currentSegment.GetClosestPosition((startPosition + endPosition) / 2f);
            Vector3 quarterPosition = currentSegment.GetClosestPosition((startPosition + midPosition) / 2f);
            Vector3 threeQuarterPosition = currentSegment.GetClosestPosition((midPosition + endPosition) / 2f);
            float minX = Mathf.Min(startPosition.x, endPosition.x, midPosition.x, quarterPosition.x, threeQuarterPosition.x) - DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            float maxX = Mathf.Max(startPosition.x, endPosition.x, midPosition.x, quarterPosition.x, threeQuarterPosition.x) + DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            float minZ = Mathf.Min(startPosition.z, endPosition.z, midPosition.z, quarterPosition.z, threeQuarterPosition.z) - DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            float maxZ = Mathf.Max(startPosition.z, endPosition.z, midPosition.z, quarterPosition.z, threeQuarterPosition.z) + DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            int minXint = Mathf.Max((int)(minX / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0);
            int minZint = Mathf.Max((int)(minZ / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0);
            int maxXint = Mathf.Min((int)(maxX / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            int maxZint = Mathf.Min((int)(maxZ / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            for (int i = minZint; i <= maxZint; i++)
            {
                for (int j = minXint; j <= maxXint; j++)
                {
                    int currentDrainageAreaID = i * DrainageAreaGrid.drainageAreaGridCoefficient + j;
                    if (DrainageAreaGrid.DrainageAreaDictionary.ContainsKey(currentDrainageAreaID))
                    {
                        DrainageArea currentDrainageArea = DrainageAreaGrid.DrainageAreaDictionary[currentDrainageAreaID];
                        Vector3 centerlinePosition = currentSegment.GetClosestPosition(currentDrainageArea.m_outputPosition);
                        float distance = Vector3.Distance(centerlinePosition, currentDrainageArea.m_outputPosition);
                        if (distance > 0f && distance < width + 10f)
                        {
                            currentDrainageArea.m_outputPosition = centerlinePosition;
                            if (logging)
                            {
                                Debug.Log("[RF]DrainageAreaGrid.DisableRoadwayCoveredDrainageAreas distance = " + distance.ToString());
                                Debug.Log("[RF]DrainageAreaGrid.DisableRoadwayCoveredDrainageAreas width = " + width.ToString());
                            }
                        }
                        
                    }
                }
            }
            if (logging)
            {
                Debug.Log("[RF]DrainageAreaGrid.DisableRoadwayCoveredDrainageAreas finished disabling coverd drainage areas ");
            }
        }

        public static void EnableRoadwayUncoveredDrainageAreas(ushort segmentID)
        {
            bool logging = false;
            NetSegment currentSegment = Singleton<NetManager>.instance.m_segments.m_buffer[segmentID];
            float width = currentSegment.Info.m_halfWidth;
            Vector3 startPosition = Singleton<NetManager>.instance.m_nodes.m_buffer[currentSegment.m_startNode].m_position;
            Vector3 endPosition = Singleton<NetManager>.instance.m_nodes.m_buffer[currentSegment.m_endNode].m_position;
            Vector3 midPosition = currentSegment.GetClosestPosition((startPosition + endPosition) / 2f);
            Vector3 quarterPosition = currentSegment.GetClosestPosition((startPosition + midPosition) / 2f);
            Vector3 threeQuarterPosition = currentSegment.GetClosestPosition((midPosition + endPosition) / 2f);
            float minX = Mathf.Min(startPosition.x, endPosition.x, midPosition.x, quarterPosition.x, threeQuarterPosition.x) - DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            float maxX = Mathf.Max(startPosition.x, endPosition.x, midPosition.x, quarterPosition.x, threeQuarterPosition.x) + DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            float minZ = Mathf.Min(startPosition.z, endPosition.z, midPosition.z, quarterPosition.z, threeQuarterPosition.z) - DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            float maxZ = Mathf.Max(startPosition.z, endPosition.z, midPosition.z, quarterPosition.z, threeQuarterPosition.z) + DrainageAreaGrid.drainageAreaGridQuotient / 2f;
            int minXint = Mathf.Max((int)(minX / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0);
            int minZint = Mathf.Max((int)(minZ / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0);
            int maxXint = Mathf.Min((int)(maxX / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            int maxZint = Mathf.Min((int)(maxZ / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), DrainageAreaGrid.drainageAreaGridCoefficient - 1);
            for (int i = minZint; i <= maxZint; i++)
            {
                for (int j = minXint; j <= maxXint; j++)
                {
                    int currentDrainageAreaID = i * DrainageAreaGrid.drainageAreaGridCoefficient + j;
                    if (DrainageAreaGrid.DrainageAreaDictionary.ContainsKey(currentDrainageAreaID))
                    {
                        DrainageArea currentDrainageArea = DrainageAreaGrid.DrainageAreaDictionary[currentDrainageAreaID];
                        Vector3 centerlinePosition = currentSegment.GetClosestPosition(currentDrainageArea.m_outputPosition);
                        float distance = Vector3.Distance(centerlinePosition, currentDrainageArea.m_outputPosition);
                        if (distance < width + 10f)
                        {
                            DrainageAreaGrid.recalculateCompositeRunoffCoefficentForBasinAtGridLocation(currentDrainageAreaID);
                            if (logging)
                            {
                                Debug.Log("[RF]DrainageAreaGrid.EnableRoadwayUncoveredDrainageAreas distance = " + distance.ToString());
                                Debug.Log("[RF]DrainageAreaGrid.EnableRoadwayUncoveredDrainageAreas width = " + width.ToString());
                            }
                        }
                    }
                }
            }
            if (logging)
            {
                Debug.Log("[RF]DrainageAreaGrid.EnableRoadwayUncoveredDrainageAreas finished enabling uncoverd drainage areas ");
            }
        }

       public static bool AddBuildingToDrainageArea(ushort buildingID, int gridLocation)
       {
            if (DrainageAreaDictionary != null)
            {
                if (DrainageAreaDictionary.ContainsKey(gridLocation))
                {
                    return DrainageAreaDictionary[gridLocation].AddBuilding(buildingID);
                }
            }
            return false;
        }
        public static bool AddSegmentToDrainageArea(ushort segmentID, int gridLocation)
        {
            if (DrainageAreaDictionary != null)
            {
                if (DrainageAreaDictionary.ContainsKey(gridLocation))
                {
                    return DrainageAreaDictionary[gridLocation].AddSegment(segmentID);
                }
            }
            return false;
        }

        public static bool RemoveBuildingFromDrainageArea(ushort buildingID, int gridLocation)
        {
            if (DrainageAreaDictionary != null)
            {
                if (DrainageAreaDictionary.ContainsKey(gridLocation))
                {
                    return DrainageAreaDictionary[gridLocation].RemoveBuilding(buildingID);
                }
            }
            return false;
        }
        public static bool RemoveSegmentFromDrainageArea(ushort segmentID, int gridLocation)
        {
            if (DrainageAreaDictionary != null)
            {
                if (DrainageAreaDictionary.ContainsKey(gridLocation))
                {
                    return DrainageAreaDictionary[gridLocation].removeSegment(segmentID);
                }
            }
            return false;
        }
        public static void AddBuildingsToDrainageAreaGrid()
        {
            if (DrainageAreaDictionary == null)
            {
                Debug.Log("[RF]DrainageAreaGrid.AddbuildingToDrainageAreaGrid Failed! Drainage Area Dictionary == null");
                return;
            }
            for (int buildingID = 0; buildingID < Singleton<BuildingManager>.instance.m_buildings.m_size; buildingID++)
            {
                Building currentBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
                if (DrainageArea.ReviewBuilding(buildingID))
                {
                    int gridX = Mathf.Clamp(Mathf.FloorToInt(currentBuilding.m_position.x / drainageAreaGridQuotient + drainageAreaGridAddition), 0, drainageAreaGridCoefficient - 1);
                    int gridZ = Mathf.Clamp(Mathf.FloorToInt(currentBuilding.m_position.z / drainageAreaGridQuotient + drainageAreaGridAddition), 0, drainageAreaGridCoefficient - 1);
                    int gridLocation = gridZ * drainageAreaGridCoefficient + gridX;

                    if (DrainageAreaDictionary.ContainsKey(gridLocation))
                    {
                        DrainageAreaDictionary[gridLocation].AddBuilding((ushort)buildingID);
                    }
                }
            }
        }
        public static void AddSegmentsToDrainageAreaGrid()
        {
            if (DrainageAreaDictionary == null)
            {
                Debug.Log("[RF]DrainageAreaGrid.AddSegmentsToDrainageAreaGrid Failed! Drainage Area Dictionary == null");
                return;
            }
            for (int segmentID = 0; segmentID < Singleton<NetManager>.instance.m_segments.m_size; segmentID++)
            {
                NetSegment currentSegment = Singleton<NetManager>.instance.m_segments.m_buffer[segmentID];
                if (DrainageArea.ReviewSegment((ushort)segmentID))
                {
                    Vector3 centerPos = (Singleton<NetManager>.instance.m_nodes.m_buffer[currentSegment.m_startNode].m_position + Singleton<NetManager>.instance.m_nodes.m_buffer[currentSegment.m_endNode].m_position) * 0.5f;
                    int gridX = Mathf.Clamp(Mathf.FloorToInt(centerPos.x / drainageAreaGridQuotient + drainageAreaGridAddition), 0, drainageAreaGridCoefficient - 1);
                    int gridZ = Mathf.Clamp(Mathf.FloorToInt(centerPos.z / drainageAreaGridQuotient + drainageAreaGridAddition), 0, drainageAreaGridCoefficient - 1);
                    int gridLocation = gridZ * drainageAreaGridCoefficient + gridX;
                    if (DrainageAreaDictionary.ContainsKey(gridLocation))
                    {
                        DrainageAreaDictionary[gridLocation].AddSegment((ushort)segmentID);
                    }
                }
            }
        }
        public static void RecalculateCoefficientsForAllDrainageAreas()
        {
            if (DrainageAreaDictionary == null)
            {
                Debug.Log("[RF]DrainageAreaGrid.RecalculateCoefficientsForAllDrainageAreas Failed! Drainage Area Dictionary == null");
                return;
            }
            foreach (DrainageArea currentDrainageArea in DrainageAreaDictionary.Values)
            {
                bool logging = false;
                currentDrainageArea.recalculateCompositeRunoffCoefficent(logging);
            }
            Debug.Log("[RF]DrainageAreaGrid.RecalculateCoefficientsForAllDrainageAreas Finished!");
        }

        private static void EnableDrainageAreasInTile(int tile)
        {
            float iterator = DrainageAreaGrid.drainageAreaGridQuotient; //128
            Vector3 position = new Vector3(0, 0, 0);
            int x = 0;
            int z = 0;
            GetTileXZ(tile, out x, out z);
            gameAreaManager.GetAreaBounds(x, z, out float startX, out float startZ, out float endX, out float endZ);
            for (position.z = startZ; position.z < endZ; position.z += iterator)
            {
                for (position.x = startX; position.x < endX; position.x += iterator)
                {
                    int gridX = Mathf.Clamp(Mathf.FloorToInt(position.x / drainageAreaGridQuotient + drainageAreaGridAddition), 0, drainageAreaGridCoefficient - 1);
                    int gridZ = Mathf.Clamp(Mathf.FloorToInt(position.z / drainageAreaGridQuotient + drainageAreaGridAddition), 0, drainageAreaGridCoefficient - 1);
                    int gridLocation = gridZ * drainageAreaGridCoefficient + gridX;
                    if (DrainageAreaDictionary.ContainsKey(gridLocation))
                    {
                        DrainageAreaDictionary[gridLocation].m_enabled = true;
                    }
                }
            }
        }
    }
}
