
using ColossalFramework;
using ICities;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Threading;
using ColossalFramework.UI;

namespace Rainfall
{
    public class Hydrology : ThreadingExtensionBase
    {
        private float _realTimeCount;
        private BuildingManager _buildingManager;
        private WeatherManager _weatherManager;
        private WaterSimulation _waterSimulation;
        private GameAreaManager _gameAreaManager;
        private NetManager _netManager;
        private TerrainManager _terrainManager;
        public bool isRaining;
        System.Random random = new System.Random();
       

        private int _capacity;
        public bool terminated;
        public bool purged;
        public bool initialized;
        public static Hydrology instance = null;
        public bool loaded;
        private const decimal secondsToMinutes = (decimal)(1f / 60f);
        private const int ChirpRainTweetChance = 1;
        private string forecasterName;
        private string forecasterChannel;
        public int[] _preRainfallLandvalues;
        private string rainUnlockMilestone = "Milestone3";
        private float eightyOneTilesDelay = 5f;
        private float eightyOneTileCheckPeriod = 30f;
        public bool cleanUpCycle = false;
        public bool endStorm = false;
        public bool intensityCurveFinished = false;
        public bool holdLandValue = true;
        public List<ushort> _waterSourceIDs;

        private float _realTimeCountSinceLastStorm;

        private List<string> mildQuotes;
        private List<string> normalQuotes;
        private List<string> heavyQuotes;
        private List<string> extremeQuotes;
        private List<string> chirperFirstNames;
        private List<string> chirperLastNames;
        private List<string> introductionStatements;
        private List<string> beforeTimeStatements;
        private List<string> beforeIntensityAdjectiveStatements;
        private SortedList<float, string> intensityAdjectives;
        private List<string> beforeReturnRateStatements;
        private List<string> closingStatements;
        private bool ReleaseNextClickedWaterSource = false;

        public List<ushort> buildingToReviewAndAdd;

        private readonly string versionNumber = "V2.14.0.0";
        private readonly string buildTimestamp = "2023.08.02 07:17 pm";

        private int initialTileCount = 0;

        public int[] gameAreas;
        public Hydrology()
        {

        }

        public override void OnCreated(IThreading threading)
        {
            InitializeManagers();
            
            _capacity = _buildingManager.m_buildings.m_buffer.Length;
            
            instance = this;

            initialized = false;
            terminated = false;
            purged = false;
            isRaining = false;
            loaded = false;
            _realTimeCount = 0;
            _waterSourceIDs = new List<ushort>();
            initializeQuotes();
            initializeRainFallForecastStrings();
            buildingToReviewAndAdd = new List<ushort>();

            base.OnCreated(threading);
        }

        public static void deinitialize()
        {

            Hydrology.instance.initialized = false;
            Hydrology.instance.loaded = false;
            Hydrology.instance.terminated = true;
        }
        private void InitializeManagers()
        {
            _buildingManager = Singleton<BuildingManager>.instance;
            _weatherManager = Singleton<WeatherManager>.instance;
            _terrainManager = Singleton<TerrainManager>.instance;
            _waterSimulation = _terrainManager.WaterSimulation;
            _netManager = Singleton<NetManager>.instance;
            _gameAreaManager = Singleton<GameAreaManager>.instance;
        }

        public override void OnBeforeSimulationTick()
        {
            if (terminated) return;

            if (!initialized) return;

            if (!loaded) return;


            if (endStorm == true)
            {
                _weatherManager.m_currentRain = 0;
                _weatherManager.m_targetRain = 0;
                isRaining = false;
                endStorm = false;
            }
           
            
            
            base.OnBeforeSimulationTick();

        }

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            bool logging = false;
            
            if (!terminated && OptionHandler.getSliderSetting("GlobalRunoffScalar") == 0 || _weatherManager.m_enableWeather == false)
            {
                terminated = true;
            }

            if (terminated)
            {
                return;
            }

            if (!loaded)
                return;


            if (!initialized && Hydraulics.instance.initialized == true)
            {
                InitializeManagers();
                if (!WaterSourceManager.AreYouAwake())  WaterSourceManager.Awake();
                _preRainfallLandvalues = new int[_capacity];
                _capacity = _buildingManager.m_buildings.m_buffer.Length;
                terminated = false;
                isRaining = false;
                
                Debug.Log("[RF].Hydrology  Starting Storm Drain Mod! Version: " + versionNumber + " Build Timestamp: " + buildTimestamp);
                initialized = true;
            }
            else if (!initialized)
            {
                return;
            }

            if (eightyOneTilesDelay > 0f)
            {
                eightyOneTilesDelay -= realTimeDelta;
                if (_gameAreaManager.m_areaGrid.Length == 81)
                {
                    Debug.Log("[RF]Hydrology.OnUpdate eightyOneTilesDelayNeeded = " + (5f - eightyOneTilesDelay).ToString());
                    eightyOneTilesDelay = 0f;
                }
                return;
            } else if (!DrainageAreaGrid.areYouAwake() && WaterSourceManager.AreYouAwake())
            {
                purgePreviousWaterSources();
                DrainageAreaGrid.Awake();
                initialTileCount = _gameAreaManager.m_areaGrid.Length;
                return;
                         
            } else if (_gameAreaManager.m_areaGrid.Length != initialTileCount && DrainageAreaGrid.areYouAwake())
            {
                DrainageAreaGrid.Clear();
                return;
            } else if (eightyOneTileCheckPeriod > 0f)
            {
                eightyOneTileCheckPeriod -= realTimeDelta;
            } else if (_gameAreaManager.m_areaGrid.Length == 81 && DrainageAreaGrid.areYouAwake())
            {
                DrainageAreaGrid.updateDrainageAreaGridForNewTile(logging);
                eightyOneTileCheckPeriod = 15f;
            } 
            

            if (buildingToReviewAndAdd.Count > 0)
            {
                foreach(ushort buildingID in buildingToReviewAndAdd)
                {
                    Building currentBuilding = _buildingManager.m_buildings.m_buffer[buildingID];
                    BuildingAI currentBuildingAI = currentBuilding.Info.m_buildingAI;
                    if (DrainageArea.ReviewBuilding(buildingID))
                    {
                        int gridX = Mathf.Clamp((int)(currentBuilding.m_position.x / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
                        int gridZ = Mathf.Clamp((int)(currentBuilding.m_position.z / DrainageAreaGrid.drainageAreaGridQuotient + DrainageAreaGrid.drainageAreaGridAddition), 0, DrainageAreaGrid.drainageAreaGridCoefficient - 1);
                        int gridLocation = gridZ * DrainageAreaGrid.drainageAreaGridCoefficient + gridX;

                        DrainageAreaGrid.AddBuildingToDrainageArea(buildingID, gridLocation);
                        DrainageAreaGrid.recalculateCompositeRunoffCoefficentForBasinAtGridLocation(gridLocation);
                        //Debug.Log("[RF].Hydrology Reviewed and Added Building ID " + buildingID);
                        DrainageAreaGrid.DisableBuildingCoveredDrainageAreas(buildingID);
                    } else
                    {
                        //Debug.Log("[RF].Hydrology Reviewed and didn't add Building ID " + buildingID);
                    }

                }
                buildingToReviewAndAdd.Clear();
            }

            if (_weatherManager.m_currentRain < 0f || _weatherManager.m_currentRain > 1f)
            {
                _weatherManager.m_currentRain = 0f;
            }
            if (_weatherManager.m_currentRain == 0 && isRaining == false)
            {

                if (cleanUpCycle == true)
                {
                    _weatherManager.m_targetRain = 1;
                }
                //Debug.Log("[RF].Hydrology  not raining ");
            }
            else if (_weatherManager.m_currentRain > 0 && isRaining == false && simulationTimeDelta > 0 && realTimeDelta > 0)
            {
                MilestoneInfo unlockMilestone = null;
                try
                {
                    if (!Singleton<UnlockManager>.instance.m_allMilestones.TryGetValue(rainUnlockMilestone, out unlockMilestone))
                    {
                        unlockMilestone = null;
                    }
                }
                catch
                {
                    //Debug.Log("Could not read milestone");
                    unlockMilestone = null;
                }
                if (unlockMilestone != null)
                {
                    Singleton<UnlockManager>.instance.CheckMilestone(unlockMilestone, false, false);
                }
                if (!Singleton<UnlockManager>.instance.Unlocked(unlockMilestone) && OptionHandler.getCheckboxSetting("PreventRainBeforeMilestone") == true || OptionHandler.getSliderSetting("BreakBetweenStorms") > _realTimeCountSinceLastStorm)
                {
                    _weatherManager.m_currentRain = 0;
                    _weatherManager.m_targetRain = 0;
                }
                else
                {

                    _preRainfallLandvalues = new int[_capacity];


                    isRaining = true;

                    
                    if (OptionHandler.getCheckboxSetting("ChirpForecasts") == true)
                    {
                        ChirpForecast.SendMessage(forecasterName, generateRainFallForecast());
                    }
                }
                
            }
            else if (_weatherManager.m_currentRain > 0 && isRaining == true && simulationTimeDelta > 0 && realTimeDelta > 0 /*&& stormTime < (decimal)stormDuration*/)
            {
                if (_weatherManager.m_currentRain < _weatherManager.m_targetRain && _weatherManager.m_currentRain > 0.001f)
                {
                    _weatherManager.m_currentRain = Mathf.Clamp(Mathf.Min(_weatherManager.m_targetRain, _weatherManager.m_currentRain - 0.0002f + OptionHandler.getSliderSetting("IntensityRateOfChange")),0f, 1f);
                }
                else if (_weatherManager.m_currentRain > _weatherManager.m_targetRain)
                {
                    _weatherManager.m_currentRain = Mathf.Clamp(Mathf.Max(_weatherManager.m_targetRain, _weatherManager.m_currentRain + 0.0002f - OptionHandler.getSliderSetting("IntensityRateOfChange")),0f,1f);
                }

                if (OptionHandler.getCheckboxSetting("ChirpRainTweets") == true && random.Next(0, 10000) < ChirpRainTweetChance)
                {
                    string tweeterName = randomString(chirperFirstNames) + " " + randomString(chirperLastNames);
                    if (_weatherManager.m_currentRain < 0.45)
                    {
                        ChirpForecast.SendMessage(tweeterName, randomString(mildQuotes) + " #Rainfall");
                    }
                    else if (_weatherManager.m_currentRain < 0.65)
                    {
                        ChirpForecast.SendMessage(tweeterName, randomString(normalQuotes) + " #Rainfall");
                    }
                    else if (_weatherManager.m_currentRain < 0.85)
                    {
                        ChirpForecast.SendMessage(tweeterName, randomString(heavyQuotes) + " #Rainfall");
                    }
                    else
                    {
                        ChirpForecast.SendMessage(tweeterName, randomString(extremeQuotes) + " #Rainfall");
                    }
                }

            }
            

            else if (_weatherManager.m_currentRain == 0 && isRaining == true)
            {
               
                isRaining = false;
                cleanUpCycle = false;
                
            }
            if (isRaining == false && simulationTimeDelta > 0)
            {
                _realTimeCountSinceLastStorm += realTimeDelta;
                if (_realTimeCountSinceLastStorm > OptionHandler.getSliderSetting("FreezeLandvaluesTimer")) 
                {
                    _preRainfallLandvalues = new int[_capacity];
                    holdLandValue = false;
                }
                if (_realTimeCountSinceLastStorm > OptionHandler.getSliderSetting("MaxTimeBetweenStorms")) 
                {
                    _weatherManager.m_targetRain = Mathf.Clamp((float)random.NextDouble(),0.2f,1.0f);
                }
            } else if (isRaining == true)
            {
                _realTimeCountSinceLastStorm = 0f;
                holdLandValue = true;
            }

                        
            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }
        
        public override void OnAfterSimulationTick()
        {
            base.OnAfterSimulationTick();
        }

        
        public string randomString(List<string> strings)
        {
            int stringNum = random.Next(0, strings.Count - 1);
            return strings[stringNum];
        }
        public void initializeQuotes()
        {
            mildQuotes = new List<string>();
            normalQuotes = new List<string>();
            heavyQuotes = new List<string>();
            extremeQuotes = new List<string>();
            chirperFirstNames = new List<string>();
            chirperLastNames = new List<string>();

            mildQuotes.Add("Is that Rain?");
            mildQuotes.Add("I think it might be sprinkling outside.");
            mildQuotes.Add("It's only a shower.");
            mildQuotes.Add("A light rain is beginning to fall.");
            mildQuotes.Add("Rain drops keep fallin' on my head.");
            mildQuotes.Add("Everybody wants happiness, nobody wants pain, but you can't have a rainbow without a little rain.");
            mildQuotes.Add("Upon us all, a little rain must fall.");
            normalQuotes.Add("I love it when it rains!");
            normalQuotes.Add("The way I see it, if you want the rainbow, you gotta put up with the rain.");
            normalQuotes.Add("A single gentle rain makes the grass many shades greener.");
            normalQuotes.Add("It looks like it's going to rain.");
            normalQuotes.Add("Let the rain wash away all the pain of yesterday.");
            normalQuotes.Add("Rain, rain, go away, come again another day.");
            normalQuotes.Add("It's supposed to clear up later.");
            normalQuotes.Add("When life gives you rainy days, wear cute boots and jump in the puddles.");
            normalQuotes.Add("Life isn't about waiting for the storm to pass. It's about learning to dance in the rain.");
            normalQuotes.Add("Rain showers my spirit and waters my soul.");
            normalQuotes.Add("Every storm runs out of rain.");
            heavyQuotes.Add("Bring an umbrealla and a jacket.");
            heavyQuotes.Add("I heard it is going to pour down buckets of rain.");
            heavyQuotes.Add("The nicest thing about the rain is that it always stops. Eventually.");
            heavyQuotes.Add("I always like walking in the rain, so no one can see me crying.");
            heavyQuotes.Add("Upcoming heavy rains may cause flooding.");
            heavyQuotes.Add("Thank God I remembered my umbrella.");
            extremeQuotes.Add("Better get inside. This is going to be a huge storm!");
            extremeQuotes.Add("It's going to be storm of the century!");
            extremeQuotes.Add("This is the storm of a lifetime!");
            extremeQuotes.Add("Expect major flooding from today's storm.");
            extremeQuotes.Add("Don't drown! Turn around!");
            extremeQuotes.Add("Flash flood warning!");

            chirperFirstNames.Add("Anna");
            chirperFirstNames.Add("Bernie");
            chirperFirstNames.Add("Catherine");
            chirperFirstNames.Add("David");
            chirperFirstNames.Add("Erik");
            chirperFirstNames.Add("Frank");
            chirperFirstNames.Add("George");
            chirperFirstNames.Add("Heather");
            chirperFirstNames.Add("Igor");
            chirperFirstNames.Add("Jodi");
            chirperFirstNames.Add("Kelly");
            chirperFirstNames.Add("Lynn");
            chirperFirstNames.Add("Matt");
            chirperFirstNames.Add("Megan");
            chirperFirstNames.Add("Natalie");
            chirperFirstNames.Add("Olivia");
            chirperFirstNames.Add("Philip");
            chirperFirstNames.Add("Quinton");
            chirperFirstNames.Add("Regis");
            chirperFirstNames.Add("Sam");
            chirperFirstNames.Add("Thomas");
            chirperFirstNames.Add("Trudy");
            chirperFirstNames.Add("Ulrick");
            chirperFirstNames.Add("Valerie");
            chirperFirstNames.Add("Whitney");
            chirperFirstNames.Add("Xander");
            chirperFirstNames.Add("Yadira");
            chirperFirstNames.Add("Zack");

            chirperLastNames.Add("Adams");
            chirperLastNames.Add("Betancourt");
            chirperLastNames.Add("Caltabiano");
            chirperLastNames.Add("Dizon");
            chirperLastNames.Add("Estrada");
            chirperLastNames.Add("Farrell");
            chirperLastNames.Add("Gutierrez");
            chirperLastNames.Add("Hayden");
            chirperLastNames.Add("Ising");
            chirperLastNames.Add("Jones");
            chirperLastNames.Add("Kelley");
            chirperLastNames.Add("Lerma");
            chirperLastNames.Add("Miller");
            chirperLastNames.Add("Nason");
            chirperLastNames.Add("Ortiz");
            chirperLastNames.Add("Pederson");
            chirperLastNames.Add("Roth");
            chirperLastNames.Add("Swartz");
            chirperLastNames.Add("Torres");
            chirperLastNames.Add("Ulmer");
            chirperLastNames.Add("Vanderbilt");
            chirperLastNames.Add("Walker");
            chirperLastNames.Add("Yoldi");
            chirperLastNames.Add("Zuniga");

        }
        public void initializeRainFallForecastStrings()
        {
            introductionStatements = new List<string>();
            beforeTimeStatements = new List<string>();
            beforeIntensityAdjectiveStatements = new List<string>();
            intensityAdjectives = new SortedList<float, string>();
            beforeReturnRateStatements = new List<string>();
            closingStatements = new List<string>();
            string forecasterFirestName = randomString(chirperFirstNames);
            string forecasterLastName = randomString(chirperLastNames);

            chirperFirstNames.Remove(forecasterFirestName);
            chirperLastNames.Remove(forecasterLastName);
            forecasterName = forecasterFirestName + " " + forecasterLastName;
            forecasterChannel = random.Next(1, 99).ToString();
            introductionStatements.Add("Hello Viewers! My name is ");
            introductionStatements.Add("This is ");
            introductionStatements.Add("Welcome! It's time for your weather forecast by ");
            introductionStatements.Add("Today's forecast brought to you by ");
            introductionStatements.Add("Back again folks. ");
            introductionStatements.Add("This just in! Hello, it's ");

            beforeTimeStatements.Add("In the next ");
            beforeTimeStatements.Add("Rain is expected to fall for the next ");
            beforeTimeStatements.Add("A storm will last for ");
            beforeTimeStatements.Add("Upcoming showers that may be around for the next ");
            beforeTimeStatements.Add("Bring a rainjacket, If you are going out during the next ");

            beforeIntensityAdjectiveStatements.Add("We are anticipating a max intensity of");
            beforeIntensityAdjectiveStatements.Add("We are expecting a peak intensity of");
            beforeIntensityAdjectiveStatements.Add("This storm will drop a");
            beforeIntensityAdjectiveStatements.Add("The climax of of the storm is forecasted to be a");

            

            intensityAdjectives.Add(0.2f, " measly");
            intensityAdjectives.Add(0.25f, " mild");
            intensityAdjectives.Add(0.3f, " small");
            intensityAdjectives.Add(0.4f, " normal");
            intensityAdjectives.Add(0.45f, "n average");
            intensityAdjectives.Add(0.5f, "n above average");
            intensityAdjectives.Add(0.55f, " significant");
            intensityAdjectives.Add(0.6f, " large");
            intensityAdjectives.Add(0.65f, " heavy");
            intensityAdjectives.Add(0.70f, " whopping");
            intensityAdjectives.Add(0.75f, " formidable");
            intensityAdjectives.Add(0.80f, " extreme");
            intensityAdjectives.Add(0.85f, " insane");
            intensityAdjectives.Add(0.90f, " godly");
            intensityAdjectives.Add(0.95f, " astronomical");
            intensityAdjectives.Add(1.0f, " ark-worthy");

            closingStatements.Add("Stay Dry!");
            closingStatements.Add("See you next time on Channel " + forecasterChannel + ".");
            closingStatements.Add("And that's your forecast. Good bye.");
            closingStatements.Add("That concludes our forecast for today.");
        }
        public string generateRainFallForecast()
        {
            StringBuilder fullForecast = new StringBuilder();
            fullForecast.Append(randomString(introductionStatements));
            fullForecast.Append(forecasterName + " from Channel " + forecasterChannel + ". ");
            //fullForecast.Append(randomString(beforeTimeStatements));
            //fullForecast.Append(convertSecToMinSec(stormDuration - Mathf.Round((float)stormTime)) + " ");
           
            fullForecast.Append(randomString(beforeIntensityAdjectiveStatements));
            float maxIntensity = _weatherManager.m_targetRain;
            foreach (KeyValuePair<float, string> pair in intensityAdjectives)
            {
                if (maxIntensity <= pair.Key)
                {
                    fullForecast.Append(pair.Value + " " + (Mathf.Floor(maxIntensity * 10f) / 10f).ToString() +  " units/time. ");
                    break;
                }
            }
            
            fullForecast.Append(". " + randomString(closingStatements));
            fullForecast.Append(" #RainForecast");
            return fullForecast.ToString();

        }
        private string convertSecToMinSec(float time)
        {
            string sec = ((int)time % 60).ToString();
            string min = ((int)time / 60).ToString();
            if (((int)time / 60) > 0)
                return (min + " min. and " + sec + " sec.");
            return (sec + " sec.");
        }
        
        private void RelocateBuilding(ushort building, ref Building data, Vector3 position, float angle)
        {
            BuildingInfo info = data.Info;
            RemoveFromGrid(building, ref data);
            if (info.m_hasParkingSpaces != VehicleInfo.VehicleType.None)
            {
                BuildingManager.instance.UpdateParkingSpaces(building, ref data);
            }
            data.m_position = position;
            data.m_angle = angle;

            AddToGrid(building, ref data);
            data.CalculateBuilding(building);
            BuildingManager.instance.UpdateBuildingRenderer(building, true);
        }

        private static void AddToGrid(ushort building, ref Building data)
        {
            int num = Mathf.Clamp((int)(data.m_position.x / 64f + 135f), 0, 269);
            int num2 = Mathf.Clamp((int)(data.m_position.z / 64f + 135f), 0, 269);
            int num3 = num2 * 270 + num;
            while (!Monitor.TryEnter(BuildingManager.instance.m_buildingGrid, SimulationManager.SYNCHRONIZE_TIMEOUT))
            {
            }
            try
            {
                BuildingManager.instance.m_buildings.m_buffer[(int)building].m_nextGridBuilding = BuildingManager.instance.m_buildingGrid[num3];
                BuildingManager.instance.m_buildingGrid[num3] = building;
            }
            finally
            {
                Monitor.Exit(BuildingManager.instance.m_buildingGrid);
            }
        }

        private static void RemoveFromGrid(ushort building, ref Building data)
        {
            BuildingManager buildingManager = BuildingManager.instance;

            BuildingInfo info = data.Info;
            int num = Mathf.Clamp((int)(data.m_position.x / 64f + 135f), 0, 269);
            int num2 = Mathf.Clamp((int)(data.m_position.z / 64f + 135f), 0, 269);
            int num3 = num2 * 270 + num;
            while (!Monitor.TryEnter(buildingManager.m_buildingGrid, SimulationManager.SYNCHRONIZE_TIMEOUT))
            {
            }
            try
            {
                ushort num4 = 0;
                ushort num5 = buildingManager.m_buildingGrid[num3];
                int num6 = 0;
                while (num5 != 0)
                {
                    if (num5 == building)
                    {
                        if (num4 == 0)
                        {
                            buildingManager.m_buildingGrid[num3] = data.m_nextGridBuilding;
                        }
                        else
                        {
                            BuildingManager.instance.m_buildings.m_buffer[(int)num4].m_nextGridBuilding = data.m_nextGridBuilding;
                        }
                        break;
                    }
                    num4 = num5;
                    num5 = BuildingManager.instance.m_buildings.m_buffer[(int)num5].m_nextGridBuilding;
                    if (++num6 > 49152)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
                data.m_nextGridBuilding = 0;
            }
            finally
            {
                Monitor.Exit(buildingManager.m_buildingGrid);
            }
            if (info != null)
            {
                Singleton<RenderManager>.instance.UpdateGroup(num * 45 / 270, num2 * 45 / 270, info.m_prefabDataLayer);
            }
        }
        private void elevateBuildingPad(ushort id)
        {
            Building building = _buildingManager.m_buildings.m_buffer[id];
            _buildingManager.m_buildings.m_buffer[id].m_flags = _buildingManager.m_buildings.m_buffer[id].m_flags | Building.Flags.FixedHeight;
            Vector3 newPosition = building.m_position;

            //Debug.Log("[RF]Hydrology.elevateBuildingPad miny = " + ((float)miny/64f).ToString() + " avgy = " + ((float)avgy / 64f).ToString() + " maxy = " + ((float)maxy / 64f).ToString());

            Vector3 sidewalkPosition1 = Building.CalculateSidewalkPosition(building.m_position, building.m_angle, building.m_length, building.Width * 4, 0);
            Vector3 sidewalkPosition2 = Building.CalculateSidewalkPosition(building.m_position, building.m_angle, building.m_length, -building.Width * 4, 0);
            Vector3 sidewalkPosition = building.CalculateSidewalkPosition();
            Vector3[] positions = new Vector3[] { sidewalkPosition, sidewalkPosition1, sidewalkPosition2 };
            float highestSidewalkPosition = 0;
            foreach (Vector3 position in positions)
            {
                int miny;
                int avgy;
                int maxy;
                _terrainManager.CalculateAreaHeight(position.x, position.y, position.x, position.y, out miny, out avgy, out maxy);

                if (maxy > highestSidewalkPosition)
                    highestSidewalkPosition = (float)maxy / 64f;
                //Debug.Log("[RF].Hydrology.ElevateBuildingPad currentSidewalkElevation = " + ((float)maxy/64f).ToString());
            }




            //Debug.Log("[RF]Hydrology.elevateBuildingPad differenceFromSidewalk = " + differenceFromTerrainToSidewalk.ToString());

            newPosition.y += (float)OptionHandler.getSliderSetting("IncreaseBuildingPadHeight");

            //Debug.Log("[RF]Hydrology.onUpdate " + _buildingManager.m_buildings.m_buffer[id].m_flags.ToString());
            //Debug.Log("[RF]Hydrology.onUpdate tried to elevate building " + id.ToString() + " from elevation " + _buildingManager.m_buildings.m_buffer[id].m_position.y.ToString() + " to elevation " + newPosition.y.ToString());
            RelocateBuilding(id, ref _buildingManager.m_buildings.m_buffer[id], newPosition, _buildingManager.m_buildings.m_buffer[id].m_angle);
            //Debug.Log("[RF]Hydrology.onUpdate building " + id.ToString() + " is at elevation " + _buildingManager.m_buildings.m_buffer[id].m_position.y.ToString());
        }

        public static void purgePreviousWaterSources()
        {
            List<ushort> previousStormWaterSourceIDs = new List<ushort>();

            for (int i = 0; i < Singleton<TerrainManager>.instance.WaterSimulation.m_waterSources.m_size; i++)
            {
                WaterSource ws = Singleton<TerrainManager>.instance.WaterSimulation.m_waterSources.m_buffer[i];
                if (ws.m_inputRate == 0u && ws.m_type == 2 && !Hydraulics.instance._previousFacilityWaterSources.Contains((ushort)(i + 1)))
                {
                    previousStormWaterSourceIDs.Add((ushort)(i + 1));
                } 
            }
            for (int i = 0; i < Singleton<BuildingManager>.instance.m_buildings.m_buffer.Length; i++)
            {
                if (previousStormWaterSourceIDs.Contains(Singleton<BuildingManager>.instance.m_buildings.m_buffer[i].m_waterSource))
                {
                    previousStormWaterSourceIDs.Remove(Singleton<BuildingManager>.instance.m_buildings.m_buffer[i].m_waterSource); //Do not remove Facility Water Sources
                }
            }
            if (previousStormWaterSourceIDs.Count > 0)
            {
                foreach (ushort id in previousStormWaterSourceIDs)
                {
                    Singleton<TerrainManager>.instance.WaterSimulation.ReleaseWaterSource(id);
                }
            }
        }


        public static void CleanUpCycle()
        {
            if (Hydrology.instance.initialized == true && Hydrology.instance.loaded == true)
            {
                Hydrology.instance.cleanUpCycle = true;

            }
        }
        public static void EndStorm()
        {
            if (Hydrology.instance.initialized == true && Hydrology.instance.loaded == true)
            {
                Hydrology.instance.endStorm = true;
            }
        }

        public static void MakeItRain()
        {
            if (Hydrology.instance.initialized == true && Hydrology.instance.loaded == true && Hydrology.instance.isRaining == false && Singleton<WeatherManager>.instance.m_targetRain == 0)
            {
                Singleton<WeatherManager>.instance.m_targetFog = 0;
                Singleton<WeatherManager>.instance.m_currentFog = 0;
                Singleton<WeatherManager>.instance.m_targetRain = OptionHandler.getSliderSetting("MakeItRainIntensity");
                Hydrology.instance._realTimeCountSinceLastStorm = 3601f;
            }
            if (OptionHandler.getCheckboxSetting("PreventRainBeforeMilestone")) {
                ChirpForecast.SendMessage("Failed to Make it Rain! because you have enabled the option to Prevent Rain Before Milestone 3.", "Make it Rain! Button");
            }
        }
        public static void Terminate()
        {
            Hydrology.instance.terminated = true;
            DrainageAreaGrid.Clear();
            purgePreviousWaterSources();
        }
        public static void Reinstate()
        {
            Hydrology.instance.terminated = false;
        }
    }
}