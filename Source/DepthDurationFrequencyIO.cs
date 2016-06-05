using ColossalFramework;
using ICities;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

namespace Rainfall
{
    class DepthDurationFrequencyIO
    {
        private StreamReader reader;
        private StreamWriter writer;

        private string fileDirectory;
        private string filePath;
        private string fileName;
        private const int numberOfStormFrequencies = 7;
        private const int numberOfStormDurations = 7;
        private Dictionary<string, float[,]> DDFAtlas;
        private float[] stormFrequencyIntensityRange = { 0.08f, 0.3f, 0.2f, 0.1f, 0.04f, 0.02f, 0.01f };
        private float[] stormReturnPeriods = { 1f, 2f, 5f, 10f, 25f, 50f, 100f };
        private float[] stormDurations = { 30f, 60f, 120f, 180f, 360f, 720f, 1440f };
        private List<string> cityNames;
        private Dictionary<string, float[,]> defaultDDFAtlas;
        private Dictionary<string, string> cityIntensityCurve;
        private Dictionary<string, string> defaultCityIntensityCurve;
        public static DepthDurationFrequencyIO instance;

        public DepthDurationFrequencyIO()
        {
            instance = this;
            fileDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Cities_Skylines\";
            filePath = fileDirectory + "RFDepthDurationFrequency.csv";
            fileName = "RFDepthDurationFrequency.csv";
            if (!File.Exists(fileName))
            {
                initializeDefaultDDFAtlas();
                try
                {
                    string deliminator = ",";
                    writer = new StreamWriter(File.OpenWrite(fileName));
                    foreach (KeyValuePair<string, float[,]> pair in defaultDDFAtlas)
                    {
                        string cityName = pair.Key;
                        string intensityCurveName;
                        if (defaultCityIntensityCurve.ContainsKey(cityName))
                            intensityCurveName = defaultCityIntensityCurve[cityName];
                        else
                            intensityCurveName = "";
                        StringBuilder firstLine = new StringBuilder();
                        firstLine.Append(cityName + deliminator + intensityCurveName + deliminator);
                        
                        for(int i=3; i<numberOfStormFrequencies; i++)
                        {
                            firstLine.Append(deliminator);
                        }
                        writer.WriteLine(firstLine);
                        
                        for (int i=0; i<numberOfStormDurations; i++)
                        {
                            StringBuilder nextLine = new StringBuilder();
                            for (int j=0; j<numberOfStormFrequencies; j++)
                            {
                                nextLine.Append(pair.Value[i,j].ToString());
                                if (j < numberOfStormDurations - 1)
                                    nextLine.Append(deliminator);
                            }
                            writer.WriteLine(nextLine);
                        }
                    }
                  
                    writer.Close();
                    Debug.Log("[RF].DepthDurationFrequency Successfullly wrote new DDF matrix file.");
                }
                catch (IOException ex)
                {
                    Debug.Log("[RF].DepthDurationFrequency Could not write DDF matrix file encountered excpetion " + ex.ToString());
                }

            }
            try
            {
                reader = new StreamReader(File.OpenRead(fileName));
                DDFAtlas = new Dictionary<string, float[,]>();
                cityIntensityCurve = new Dictionary<string, string>();
                cityNames = new List<string>();
                while (!reader.EndOfStream)
                {
                    string nextCityLine = reader.ReadLine();
                    string[] nextCityLineColumns = nextCityLine.Split(',');

                    string cityName = nextCityLineColumns[0];
                    cityNames.Add(cityName);
                    DDFAtlas.Add(cityName, new float[numberOfStormDurations, numberOfStormFrequencies]);
                    SortedList<float, float> emptyIntensityCurve = new SortedList<float, float>();
                    if (StormDistributionIO.GetDepthCurve(nextCityLineColumns[1], ref emptyIntensityCurve))
                    {
                        cityIntensityCurve.Add(cityName, nextCityLineColumns[1]);
                        Debug.Log("[RF].DepthDurationFrequency Added " + cityName.ToString() + " with default intensity curve" + nextCityLineColumns[1] + " to city list");
                    }
                     
                
                    for (int i = 0; i < numberOfStormDurations; i++)
                    {
                        if (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            string[] values = line.Split(',');
                            for (int j = 0; j < numberOfStormFrequencies; j++)
                            {
                                DDFAtlas[cityName][i, j] = (float)Convert.ToDecimal(values[j]);
                            }
                        }
                        else
                        {
                            DDFAtlas.Remove(cityName);
                        }
                    }


                }
                reader.Close();

                Debug.Log("[RF].DepthDurationFrequency Successfullly imported Depth-Duration-Frequency matrix file.");

               /*
                for (float i=30f; i<=1440f; i+=15f)
                {
                    for (float j=0.25f; j<=1.00f; j+=0.05f)
                    {
                        float depth = GetDepth("Boise. Idaho. USA", i, j);
                        Debug.Log("[RF] for Boise Idaho with duration i= " + i.ToString() + " & targetIntensity = " + j.ToString() + " depth = " + depth.ToString());
                    }
                   
                }
                */
                /*Used to create default data
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Dictionary<string, float[,]> defaultDDFAtlas = new Dictionary<string, float[,]> { ");
                foreach (string city in cityNames)
                {
                    sb.Append("{");
                    sb.Append("\"" + city + "\", new float[,] {");
                    for (int i = 0; i < numberOfStormDurations; i++)
                    {
                        sb.Append("{");
                        for (int j = 0; j < numberOfStormFrequencies; j++)
                        {
                            sb.Append(DDFAtlas[city][i, j].ToString() + "f");
                            if (j < numberOfStormFrequencies - 1)
                                sb.Append(", ");
                        }
                        sb.Append("}");
                        if (i < numberOfStormDurations - 1)
                            sb.Append(",");
                    }
                    sb.AppendLine("}},");
                }
                sb.AppendLine("};");
                Debug.Log(sb);
                */
            }
            catch (FileNotFoundException)
            {
                Debug.Log("[RF].DepthDurationFrequency file not found at " + fileDirectory + fileName);
            }
            catch (DirectoryNotFoundException)
            {
                Debug.Log("[RF].DepthDurationFrequency Directory not found at " + fileDirectory);
            }
            catch (IOException ex)
            {
                Debug.Log("[RF].DepthDurationFrequency Could not read Depth Duration Frequency file encountered excpetion " + ex.ToString());
            }
        }
        private void initializeDefaultDDFAtlas()
        {
            defaultDDFAtlas = new Dictionary<string, float[,]> {

                {"Boise. Idaho. USA", new float[,] {{0.18f, 0.22f, 0.35f, 0.4f, 0.55f, 0.6f, 0.7f},{0.21f, 0.3f, 0.42f, 0.55f, 0.62f, 0.78f, 0.95f},{0.3f, 0.4f, 0.55f, 0.7f, 0.8f, 0.95f, 1f},{0.4f, 0.5f, 0.7f, 0.9f, 1f, 1.1f, 1.25f},{0.65f, 0.75f, 1f, 1.25f, 1.4f, 1.5f, 1.7f},{0.75f, 1f, 1.25f, 1.5f, 1.75f, 2f, 2.2f},{1f, 1.25f, 1.5f, 1.75f, 2f, 2.25f, 2.5f}}},

                {"Las Vegas. Nevada. USA", new float[,] {{0.27f, 0.4f, 0.6f, 0.75f, 0.8f, 0.95f, 1.1f},{0.35f, 0.5f, 0.7f, 0.8f, 1f, 1.2f, 1.4f},{0.45f, 0.6f, 0.75f, 1f, 1.25f, 1.4f, 1.5f},{0.5f, 0.7f, 0.85f, 1.1f, 1.35f, 1.5f, 1.75f},{0.6f, 0.75f, 1.05f, 1.4f, 1.5f, 1.75f, 2f},{0.75f, 1f, 1.4f, 1.6f, 2f, 2.25f, 2.5f},{0.85f, 1.2f, 1.5f, 1.75f, 2.25f, 2.5f, 3f}}},

                {"Seattle. Washington. USA", new float[,] {{0.3f, 0.4f, 0.45f, 0.5f, 0.55f, 0.6f, 0.75f},{0.35f, 0.45f, 0.55f, 0.6f, 0.75f, 0.8f, 0.95f},{0.55f, 0.6f, 0.75f, 0.95f, 1.1f, 1.25f, 1.35f},{0.65f, 0.75f, 1f, 1.1f, 1.35f, 1.5f, 1.65f},{1f, 1.35f, 1.5f, 1.75f, 2f, 2.5f, 3f},{1.5f, 1.75f, 2f, 2.5f, 3f, 3.5f, 4f},{1.75f, 2f, 2.5f, 3f, 3.75f, 4.5f, 5f}}},

                {"San Francisco. California. USA", new float[,] {{0.5f, 0.6f, 0.75f, 0.85f, 0.95f, 1.15f, 1.25f},{0.6f, 0.7f, 0.9f, 1.1f, 1.2f, 1.4f, 1.5f},{0.85f, 1f, 1.25f, 1.5f, 1.75f, 1.95f, 2.2f},{1f, 1.25f, 1.6f, 1.75f, 2.1f, 2.25f, 2.75f},{1.6f, 1.75f, 2.25f, 2.5f, 3f, 3.5f, 3.75f},{1.75f, 2.5f, 3f, 3.5f, 4f, 4.5f, 5f},{2.5f, 3f, 4f, 5.25f, 5.5f, 5.75f, 6f}}},

                {"Los Angeles. California. USA", new float[,] {{0.4f, 0.6f, 0.8f, 1f, 1.2f, 1.4f, 1.5f},{0.6f, 0.8f, 1f, 1.2f, 1.4f, 1.8f, 2f},{0.8f, 1f, 1.5f, 2f, 2.3f, 2.5f, 3f},{1f, 1.5f, 2f, 2.5f, 3f, 3.3f, 3.8f},{2f, 2.3f, 3f, 4f, 5f, 5.5f, 6f},{2.5f, 3f, 4f, 5f, 6f, 7f, 8f},{3f, 4f, 6f, 7f, 8f, 9f, 10f}}},

                {"Orlando. Florida. USA", new float[,] {{1.5f, 1.75f, 2.1f, 2.4f, 2.8f, 2.9f, 3.2f},{1.95f, 2.2f, 2.7f, 3f, 3.4f, 3.8f, 4f},{2.3f, 2.7f, 3.25f, 3.75f, 4.25f, 4.75f, 5.25f},{2.5f, 3f, 3.75f, 4.25f, 4.75f, 5.25f, 5.75f},{3.1f, 3.5f, 4.4f, 5.25f, 6f, 6.8f, 7.25f},{3.4f, 4.25f, 5.6f, 6.25f, 7.25f, 8f, 9f},{4f, 4.75f, 6.25f, 7.25f, 8.25f, 9.25f, 10.25f}}},

                {"Houston. Texas. USA", new float[,] {{1.6f, 1.9f, 2.3f, 2.7f, 3.1f, 3.35f, 3.75f},{2f, 2.4f, 2.9f, 3.4f, 3.9f, 4.3f, 4.6f},{2.4f, 2.9f, 3.6f, 4.25f, 4.9f, 5.6f, 6.1f},{2.6f, 3.2f, 4.1f, 4.75f, 5.6f, 6.25f, 7f},{3f, 3.75f, 5f, 6f, 7f, 7.75f, 8.75f},{3.5f, 4.5f, 6f, 7.25f, 8.5f, 9.5f, 10.75f},{4f, 5.25f, 7.25f, 8.75f, 10f, 11.5f, 13f}}}

            };
            defaultCityIntensityCurve = new Dictionary<string, string>()
            {
                {"Boise. Idaho. USA", "Type II - Noncoastal US" },
                {"Las Vegas. Nevada. USA" , "Type II - Noncoastal US" },
                {"Seattle. Washington. USA",    "Type IA - Pacific Northwest"},
                {"San Francisco. California. USA","Type IA - Pacific Northwest" },
                {"Los Angeles. California. USA","Type I - Pacific Southwest/AK/HI"},
                {"Orlando. Florida. USA","Type III - East & Gulf Coasts" },
                {"Houston. Texas. USA","Type III - East & Gulf Coasts" }
            };
        }
        public static string[] GetCityNames()
        {
            
            if (DepthDurationFrequencyIO.instance.cityNames.Count > 0)
            {
                List<string> cityList = DepthDurationFrequencyIO.instance.cityNames;
                string[] cityListArray = cityList.ToArray();
                return cityListArray;
            }
            string[] emptyCityListArray = new string[1];
            return emptyCityListArray;
        }
        public static string GetStormDistributionForCity(string city)
        {
            if (DepthDurationFrequencyIO.instance.cityIntensityCurve.ContainsKey(city))
                return DepthDurationFrequencyIO.instance.cityIntensityCurve[city];
            return "";
        }
        public static float GetDepth(string city, float duration, float targetIntensity)
        {
            DepthDurationFrequencyIO instance = DepthDurationFrequencyIO.instance;
            if (instance.DDFAtlas.ContainsKey(city))
            {
                float[,] DDFmatrix = instance.DDFAtlas[city];
                float returnPeriodIndex = instance.GetReturnPeriodIndex(targetIntensity);
                float durationIndex = instance.GetDurationIndex(duration);
                if (returnPeriodIndex >= 0f && durationIndex >= 0f) 
                {
                   
                    int returnPeriodIndexFloor = (int)Math.Floor(returnPeriodIndex);
                    float returnPeriodIndexPercentage = returnPeriodIndex - (float)returnPeriodIndexFloor;
                    int returnPeriodIndexCeil = (int)Math.Ceiling(returnPeriodIndex);
                    int durationIndexFloor = (int)Math.Floor(durationIndex);
                    float durationIndexPercentage = durationIndex - (float)durationIndexFloor;
                    int durationIndexCeil =(int) Math.Ceiling(durationIndex);
                    float[,] depths = { { DDFmatrix[durationIndexFloor, returnPeriodIndexFloor], DDFmatrix[durationIndexFloor, returnPeriodIndexCeil] },
                                        { DDFmatrix[durationIndexCeil, returnPeriodIndexFloor], DDFmatrix[durationIndexCeil, returnPeriodIndexCeil]} };

                    float depthAtDurationFloor = Mathf.Lerp(depths[0, 0], depths[0, 1], returnPeriodIndexPercentage);
                    float depthAtDurationCeil = Mathf.Lerp(depths[1, 0], depths[1, 1], returnPeriodIndexPercentage);
                    float actualDepth = Mathf.Lerp(depthAtDurationFloor, depthAtDurationCeil, durationIndexPercentage);
                    return actualDepth;
                }
            }
            return 0f;
        }
        public float GetReturnPeriodIndex(float targetIntensity)
        {
            float totalIntensityRange = 0.75f;
            float maxIntensity = 1.0f;
            float rangeMaximum = maxIntensity;
            float rangeMinimum = maxIntensity;
            if (targetIntensity >= 0.99)
            {
                return (DepthDurationFrequencyIO.numberOfStormFrequencies - 1);
            }
            for (int i = numberOfStormFrequencies - 1; i >= 0; i--)
            {
                rangeMinimum -= stormFrequencyIntensityRange[i];
                if (rangeMinimum < maxIntensity - totalIntensityRange)
                    rangeMinimum = maxIntensity - totalIntensityRange;
                if (targetIntensity >= rangeMinimum && targetIntensity < rangeMaximum)
                {
                    float range = rangeMaximum - rangeMinimum;
                    float interpolationPercentage = (targetIntensity - rangeMinimum) / range;
                    return Mathf.Lerp(i, i + 1, interpolationPercentage);
                }
                rangeMaximum = rangeMinimum;
            }
            return -1f;
        }
        private float GetDurationIndex(float duration)
        {
          
            float maxDuration = stormDurations[numberOfStormDurations - 1];
            if (duration == maxDuration)
            {
                return (numberOfStormDurations - 1); 
            } else if (duration == stormDurations[0])
            {
                return 0;
            }
            for (int i=numberOfStormDurations-1; i>0; i--)
            {
                float rangeMaximum = stormDurations[i];
                float rangeMinimum = stormDurations[i-1];
                if (duration <= rangeMaximum && duration >= rangeMinimum)
                {
                    float range = rangeMaximum - rangeMinimum;
                    float interpolationPercentage = (duration - rangeMinimum) / range;
                    return Mathf.Lerp(i - 1, i, interpolationPercentage);
                }
            }
            return -1f;
        }
        public static bool HasCity(string city)
        {
            if (DepthDurationFrequencyIO.instance.DDFAtlas.ContainsKey(city))
                return true;
            return false;
        }
        public static float GetReturnPeriod(float targetIntensity)
        {
            float totalIntensityRange = 0.75f;
            float maxIntensity = 1.0f;
            float rangeMaximum = maxIntensity;
            float rangeMinimum = maxIntensity;
            if (targetIntensity >= 0.99)
            {
                return DepthDurationFrequencyIO.instance.stormReturnPeriods[DepthDurationFrequencyIO.numberOfStormFrequencies - 1];
            }
            for (int i = DepthDurationFrequencyIO.numberOfStormFrequencies - 1; i >= 0; i--)
            {
                rangeMinimum -= DepthDurationFrequencyIO.instance.stormFrequencyIntensityRange[i];
                if (rangeMinimum < maxIntensity - totalIntensityRange)
                    rangeMinimum = maxIntensity - totalIntensityRange;
                if (targetIntensity >= rangeMinimum && targetIntensity < rangeMaximum)
                {
                    float returnMinimum = DepthDurationFrequencyIO.instance.stormReturnPeriods[i - 1];
                    float returnMaximum = DepthDurationFrequencyIO.instance.stormReturnPeriods[i];
                    float range = rangeMaximum - rangeMinimum;
                    float interpolationPercentage = (targetIntensity - rangeMinimum) / range;
                    return Mathf.Lerp(returnMinimum, returnMaximum, interpolationPercentage);
                }
                rangeMaximum = rangeMinimum;
            }
            return -1f;
        }
    }
}