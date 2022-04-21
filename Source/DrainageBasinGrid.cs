using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace Rainfall
{
    public static class DrainageBasinGrid
    {

        private static bool awake = false;


        private static readonly int waterSourceLimit = 65535;
        public static readonly int gridCoefficient = 270;
        private static readonly int magicNumber = 17280;
        public static readonly float gridQuotient = (float)magicNumber / (float)gridCoefficient;
        public static readonly float gridAddition = gridCoefficient / 2f;
        private static readonly float gridArea = gridQuotient * gridQuotient;
        private static readonly float areaGridCellSize = 1920f;
        private static readonly int areaGridResolution = 5;
        private static readonly int tiles = areaGridResolution * areaGridResolution;
        private static readonly float totalTileArea = areaGridCellSize * tiles;
        public static readonly int gridResolution = (int)Math.Ceiling(totalTileArea / gridArea);
        private static readonly int gridMinimum = 14;
        private static readonly int gridMaximum = 256;
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
            FortyNineTilesPlus,
        }
        public static readonly Dictionary<gridOption, string> gridOptionNames = new Dictionary<gridOption, string>()
        {
            {gridOption.None, "None"  },
            {gridOption.OwnedTiles, "Owned Tiles" },
            {gridOption.AdjacentTiles, "Adjacent Tiles" },
            {gridOption.AdjacentAndDiagonalTiles, "Adjacent and Diagonal Tiles" },
            {gridOption.TwentyFiveTiles, "25 Tiles" },
            {gridOption.FortyNineTiles, "49 Tiles (Requires 81 tiles)" },
            {gridOption.FortyNineTilesPlus, "49 Tiles + Edges (Requires 81 tiles)" }
        };
        //private static gridOption[] gridOptions = new gridOption[] { gridOption.None, gridOption.OwnedTiles, gridOption.AdjacentTiles, gridOption.AdjacentAndDiagonalTiles, gridOption.TwentyFiveTiles };

        private static Dictionary<int, DrainageBasin> drainageBasinDictionary = new Dictionary<int, DrainageBasin>();

        private static Dictionary<uint, int> drainageBasinWaterSourceCatalog = new Dictionary<uint, int>();

        //private static readonly int gridDimension = gridCoefficient^2;
        public static void Awake()
        {
            generateDraiangeBasinGrid();
            awake = true;
        }

        public static void Clear()
        {
            if (LoadingFunctions.loaded)
            {
                List<ushort> previousStormWaterSourceIDs = new List<ushort>();
                WaterSimulation _waterSimulation = Singleton<TerrainManager>.instance.WaterSimulation;
                for (int i = 0; i < _waterSimulation.m_waterSources.m_size; i++)
                {
                    WaterSource ws = _waterSimulation.m_waterSources.m_buffer[i];
                    if (isWaterSourceAssociatedWithADrainageBasin((uint)i))
                    {

                        previousStormWaterSourceIDs.Add((ushort)(i + 1));
                    }
                }
                foreach (ushort id in previousStormWaterSourceIDs)
                {
                    _waterSimulation.ReleaseWaterSource(id);
                }
                drainageBasinDictionary.Clear();
                drainageBasinWaterSourceCatalog.Clear();
                awake = false;
            }
        }

        public static bool areYouAwake()
        {
            return awake;
        }
        public static int CheckGridX(Vector3 position)
        {
            
            return Mathf.Clamp(Mathf.FloorToInt(position.x / gridQuotient + gridAddition), 0, gridCoefficient - 1);
        }
        public static int CheckGridZ(Vector3 position)
            
        {
            return Mathf.Clamp(Mathf.FloorToInt(position.z / gridQuotient + gridAddition), 0, gridCoefficient - 1);
        }
        public static void generateDraiangeBasinGrid()
        {

            drainageBasinDictionary.Clear();
            
            selectedGridOption = getGridOptionFromInt(OptionHandler.getDropdownSetting("SelectedGridOption")); //need to find setting unique name
            selectedTiles = generateListOfGameAreas(selectedGridOption);
            bool logging = false;
            foreach (int tile in selectedTiles)
            {
                if (logging)
                {
                    Debug.Log("[RF]DrainageBasinGrid.generateDraiangeBasinGrid generatingDrainageBasinsForTile " + tile.ToString());
                }
                generateDrainageBasinsForTile(tile, logging);
            }
            Debug.Log("[RF]DrainageBasinGrid.generateDraiangeBasinGrid drainageBasinDiction.Count = " + drainageBasinDictionary.Count);
            return;

        }
        private static void generateDrainageBasinsForTile(int tile, bool logging)
        {
            float iterator = DrainageBasinGrid.gridQuotient; //64
            Vector3 position = new Vector3(0, 0, 0);
            int x = 0;
            int z = 0;
            GetTileXZ(tile, out x, out z);
            gameAreaManager.GetAreaBounds(x, z, out float startX, out float startZ, out float endX, out float endZ);
            for (position.z = startZ; position.z < endZ; position.z += iterator)   {
                for (position.x = startX; position.x < endX; position.x += iterator)   {
                    int gridX = Mathf.Clamp(Mathf.FloorToInt(position.x / gridQuotient + gridAddition), 0, gridCoefficient - 1);
                    int gridZ = Mathf.Clamp(Mathf.FloorToInt(position.z / gridQuotient + gridAddition), 0, gridCoefficient - 1);
                    int gridLocation = gridZ * gridCoefficient + gridX;
                    if (gridX >= gridMinimum && gridX <= gridMaximum && gridZ >= gridMinimum && gridZ <= gridMaximum)
                    {
                        if (!drainageBasinDictionary.ContainsKey(gridLocation) && drainageBasinDictionary.Count < 60000)
                        {
                            drainageBasinDictionary.Add(gridLocation, new DrainageBasin(gridLocation, position));
                            recordWaterSourceForDrainageBasin(drainageBasinDictionary[gridLocation].getWaterSourceID(), gridLocation);
                            if (logging)
                            {
                                Debug.Log("[RF]DrainageBasinGrid.generateDrainageBasinsForTile Added Drainage Basin to Grid at Grid Location " + gridLocation.ToString() + " and position " + position.ToString());
                            }
                        }
                        
                    }
                }
            }
            
        }

        public static void updateDrainageBasinGridForNewTile(bool logging)
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
                    Debug.Log("[RF]DrainageBasinGrid.updateDrainageBasinGridForNewTile generatingDrainageBasinsForTile " + tile.ToString());
                }
                generateDrainageBasinsForTile(tile, false);
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

            
            if (gameAreaManager.m_areaGrid.Length == 25 && selectedGridOption == gridOption.FortyNineTiles || gameAreaManager.m_areaGrid.Length == 25 && selectedGridOption == gridOption.FortyNineTilesPlus)
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
                        if (logging == true) Debug.Log("[RF]DrainageBasinGrid.generateListOfGameAreas 81 tiles enabled and Twenty Five Tiles Selected! Maybe this will work?");
                        continue;
                    } else if (gameAreaManager.m_areaGrid.Length == 81 && selectedGridOption == gridOption.FortyNineTiles)
                    {
                        if (x >= 1 && x <= 7 && z >= 1 && z <= 7)
                        {
                            selectedTiles.Add(z * areaGridResolution + x);
                        }
                        if (logging == true) Debug.Log("[RF]DrainageBasinGrid.generateListOfGameAreas 81 tiles enabled and Forty Nine Tiles Selected! Maybe this will work?");
                        continue;
                    } else if (gameAreaManager.m_areaGrid.Length == 81 && selectedGridOption == gridOption.FortyNineTilesPlus)
                    {
                        selectedTiles.Add(z * areaGridResolution + x);
                        if (logging == true) Debug.Log("[RF]DrainageBasinGrid.generateListOfGameAreas 81 tiles enabled and Forty Nine Tiles Plus Selected! Maybe this will work?");
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
                Debug.Log("[RF]DrainageBasinGrid.GenerateListOfGameAreas selected tiles as followed: ");
                foreach (int tile in selectedTiles)
                {
                    Debug.Log("[RF]DrainageBasinGrid.GenerateListOfGameAreas " + tile.ToString());
                }
            }
            
            return selectedTiles;
        }

        public static bool recalculateCompositeRunoffCoefficentForBasinAtGridLocation(int gridLocation)
        {
            if (drainageBasinDictionary != null)
            {
                if (drainageBasinDictionary.ContainsKey(gridLocation))
                {
                    bool logging = false;
                    drainageBasinDictionary[gridLocation].recalculateCompositeRunoffCoefficent(logging);
                    return true;
                }
            }
            return false;
        }

        public static bool recordWaterSourceForDrainageBasin(uint waterSourceID, int drainageBasinID)
        {
            bool logging = false;
            if (!drainageBasinWaterSourceCatalog.ContainsKey(waterSourceID) && drainageBasinDictionary.ContainsKey(drainageBasinID))
            {
                if (drainageBasinDictionary[drainageBasinID].getWaterSourceID() == waterSourceID)
                {
                    if (logging)
                    {
                        Debug.Log("[RF]DrainageBasinGrid.recordWaterSourceForDrainageBasin Added WaterSourceID " + waterSourceID.ToString() + " to drainageBasin " + drainageBasinID.ToString());
                    }
                    drainageBasinWaterSourceCatalog.Add(waterSourceID, drainageBasinID);
                    return true;
                }
                if (logging)
                {
                    Debug.Log("[RF]DrainageBasinGrid.recordWaterSourceForDrainageBasin drainageBasinDictionary[drainageBasinID].getWaterSourceID() != waterSourceID");
                    Debug.Log("[RF]DrainageBasinGrid.recordWaterSourceForDrainageBasin Couldn't Add WaterSourceID " + waterSourceID.ToString() + " to drainageBasin " + drainageBasinID.ToString());

                }
                return false;
            }
            if (logging)
            {
                Debug.Log("[RF]DrainageBasinGrid.recordWaterSourceForDrainageBasin drainageBasinWaterSourceCatalog.ContainsKey(waterSourceID) || !drainageBasinDictionary.ContainsKey(drainageBasinID)");
                Debug.Log("[RF]DrainageBasinGrid.recordWaterSourceForDrainageBasin Couldn't Add WaterSourceID " + waterSourceID.ToString() + " to drainageBasin " + drainageBasinID.ToString());
            }
            return false;
        }
        public static bool isWaterSourceAssociatedWithADrainageBasin(uint waterSourceID)
        {
            if (drainageBasinWaterSourceCatalog.ContainsKey(waterSourceID))
            {
                if (drainageBasinDictionary[drainageBasinWaterSourceCatalog[waterSourceID]].getWaterSourceID() == waterSourceID)
                {
                    return true;
                }
                return false;
            }
            return false;
        }
        public static int getDrainageBasinIDfromWaterSource(uint waterSourceID)
        {
            if (isWaterSourceAssociatedWithADrainageBasin(waterSourceID))
            {
                return drainageBasinWaterSourceCatalog[waterSourceID];
            }
            return -1;
        }
        public static float getCompositeRunoffCoefficientForDrainageBasin(int drainageBasinID)
        {
            if (drainageBasinDictionary.ContainsKey(drainageBasinID)) {
               return drainageBasinDictionary[drainageBasinID].getCompositeRunoffCoefficient();
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
            if (selection == 6) return gridOption.FortyNineTilesPlus;
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
    }
}
