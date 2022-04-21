using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using ColossalFramework.Globalization;
using HarmonyLib;
using System;
using System.Reflection;
using ICities;
using System.Threading;

namespace Rainfall
{
    [HarmonyPatch(typeof(WaterSimulation), "SimulateWater")]
    class WaterSimulationSimulateWaterPatch
    {
        static void Postfix(int pollutionDisposeRate, ref WaterSimulation __instance, ref ushort[] ___m_heightBuffer, ref uint ___m_waterFrameIndex, ref ulong[] ___m_waterExists1, ref ulong[] ___m_waterExists2, ref ulong[] ___m_waterExists3, ref int ___m_bitCells, ref WaterSimulation.Cell[][] ___m_waterBuffers)
        {
			//Debug.Log("[RF]Watersimulation.SimulateWater Overriding Water Simulation!");
			ushort[] heightBuffer = ___m_heightBuffer; //verified
			uint waterFrameIndex = ___m_waterFrameIndex;//edit variable with ___ //verified
			const float magicNumber17280f = 17280f;
			const float const16f = 16f;
			const int const1080i = 1080;
			//finally the fun part!!!
			//Real edits might follow that aren't actually about just getting the code to run.
			while (!Monitor.TryEnter(__instance.m_waterSources, 0))
			{
			}
			try
			{
				for (int currentWaterSourceID = 0; currentWaterSourceID < __instance.m_waterSources.m_size; currentWaterSourceID++)
				{
					WaterSource value = __instance.m_waterSources[currentWaterSourceID];
					if (value.m_type == 0)
					{
						continue;
					}
					bool isNaturalWaterSource = value.m_type == 1;
					if (isNaturalWaterSource || value.m_type == 3) continue;
					if (!DrainageBasinGrid.isWaterSourceAssociatedWithADrainageBasin((uint)currentWaterSourceID))
					{
						continue;
					}
					//Real Edits for Rainfall
					if (DrainageBasinGrid.isWaterSourceAssociatedWithADrainageBasin((uint)currentWaterSourceID))
                    {
						//Debug.Log("[RF]WaterSimulation.SimulateWater Found water source " + currentWaterSourceID.ToString() + " associated with drainage basin " + DrainageBasinGrid.getDrainageBasinIDfromWaterSource((uint)currentWaterSourceID).ToString());
						if (Singleton<WeatherManager>.instance.m_currentRain == 0)
                        {
							if (value.m_outputRate > 0)
                            {
								value.m_outputRate = 0;
								value.m_water = 0;
                            }
							continue;
                        }
					}
					
					//end Real Edits for Rainfall
					value.m_flow = 0u;
					if (value.m_pollution > value.m_water)
					{
						value.m_pollution = value.m_water;
					}
					int currentOutputRate = 0;
					int currentPollutantRate = 0;
					 if (DrainageBasinGrid.isWaterSourceAssociatedWithADrainageBasin((uint)currentWaterSourceID))
					{
						int currentDrainageBasinID = DrainageBasinGrid.getDrainageBasinIDfromWaterSource((uint)currentWaterSourceID);
						
						float drainageBasinArea = DrainageBasinGrid.gridQuotient * DrainageBasinGrid.gridQuotient;
						float currentCompositeRunoffCoefficent = DrainageBasinGrid.getCompositeRunoffCoefficientForDrainageBasin(currentDrainageBasinID);
						currentOutputRate = Mathf.CeilToInt(Singleton<WeatherManager>.instance.m_currentRain * currentCompositeRunoffCoefficent * OptionHandler.getSliderSetting("GlobalRunoffScalar")) ;
						float MinimumBasinRunoff = OptionHandler.getSliderSetting("MinimumBasinRunoff");
						float MaximumBasinRunoff = OptionHandler.getSliderSetting("MaximumBasinRunoff");
						if (currentOutputRate < MinimumBasinRunoff)
                        {
							currentOutputRate = (int)MinimumBasinRunoff;
                        }
						if (currentOutputRate > MaximumBasinRunoff)
                        {
							currentOutputRate = (int)MaximumBasinRunoff;
                        }
						
						//Debug.Log("[RF]WSSWP Rainfall Edits!!! Singleton<WeatherManager>.instance.m_currentRain = " + Singleton<WeatherManager>.instance.m_currentRain.ToString());
						//Debug.Log("[RF]WSSWP Rainfall Edits!!! currentCompositeRunoffCoefficent = " + currentCompositeRunoffCoefficent.ToString());
						//Debug.Log("[RF]WSSWP Rainfall Edits!!! ModSettings.Difficulty " + (OptionHandler.getSliderSetting("GlobalRunoffScalar")).ToString());
						//Debug.Log("[RF]WSSWP Rainfall Edits!!! currentOutput Rate = " + currentOutputRate.ToString());
						currentPollutantRate = 0; //Need to calculate pollution runoff here for entrie drainage basin.
						if (value.m_water < currentOutputRate)
                        {
								value.m_water = (uint)currentOutputRate;
						}
					}//end edits for Rainfall so far!!!
					uint num7 = (uint)((int)___m_waterFrameIndex & -64);//edit variable with ___ //verified
					uint num12 = (num7 >> 6) % 3u; //verified
					ulong[] array2; //verified
					switch (num12) //verified
					{
						case 0u: //verified
							array2 = ___m_waterExists1;//edit variable with ___ //verified
							break; //verified
						case 1u: //verified
							array2 = ___m_waterExists2;//edit variable with ___ //verified
							break; //verified
						default: //verified
							array2 = ___m_waterExists3;//edit variable with ___ //verified
							break; //verified
					}
					WaterSimulation.Cell[] array5 = ___m_waterBuffers[(num7 >> 6) & 1]; //verified

					if (currentOutputRate > 0)
					{
						float outputRadius = (value.m_type != 2 && value.m_type != 3) ? (Mathf.Sqrt(currentOutputRate) * 0.4f + 10f) : Mathf.Clamp(Mathf.Sqrt(currentOutputRate) * 0.4f, 10f, 50f);
						int outputMinX = Mathf.Max(Mathf.CeilToInt((value.m_outputPosition.x - outputRadius + magicNumber17280f * 0.5f) / const16f), 0);
						int outputMinZ = Mathf.Max(Mathf.CeilToInt((value.m_outputPosition.z - outputRadius + magicNumber17280f * 0.5f) / const16f), 0);
						int outputMaxX = Mathf.Min(Mathf.FloorToInt((value.m_outputPosition.x + outputRadius + magicNumber17280f * 0.5f) / const16f), const1080i);
						int outputMaxZ = Mathf.Min(Mathf.FloorToInt((value.m_outputPosition.z + outputRadius + magicNumber17280f * 0.5f) / const16f), const1080i);
						int num123 = 0;
						int outputCellCount = 0;
						for (int outputIteratorZ = outputMinZ; outputIteratorZ <= outputMaxZ; outputIteratorZ++)
						{
							float currentOutputZ = (float)outputIteratorZ * const16f - magicNumber17280f * 0.5f - value.m_outputPosition.z;
							for (int outputIteratorX = outputMinX; outputIteratorX <= outputMaxX; outputIteratorX++)
							{
								float currentOutputX = (float)outputIteratorX * const16f - magicNumber17280f * 0.5f - value.m_outputPosition.x;
								if (currentOutputZ * currentOutputZ + currentOutputX * currentOutputX < outputRadius * outputRadius)
								{
									int targetCellID3 = outputIteratorZ * (const1080i + 1) + outputIteratorX;
									WaterSimulation.Cell targetCell3 = array5[targetCellID3];
									int terrainBlockHeight3 = heightBuffer[targetCellID3];
									if (!isNaturalWaterSource || terrainBlockHeight3 < value.m_target)
									{
										int maxTargetOrTerrain3 = Mathf.Max(value.m_target, terrainBlockHeight3);
										num123 += Mathf.Min(terrainBlockHeight3 + targetCell3.m_height - maxTargetOrTerrain3, targetCell3.m_height);
										outputCellCount++;
									}
								}
							}
						}
						if (isNaturalWaterSource)
						{
							currentOutputRate = Mathf.Min(currentOutputRate, -(num123 >> 1));
						}
						if (currentOutputRate > 0 && outputCellCount > 0)
						{
							for (int outputIteratorZ2 = outputMinZ; outputIteratorZ2 <= outputMaxZ; outputIteratorZ2++)
							{
								float currentOutputZ2 = (float)outputIteratorZ2 * const16f - magicNumber17280f * 0.5f - value.m_outputPosition.z;
								for (int outputIteratorX2 = outputMinX; outputIteratorX2 <= outputMaxX; outputIteratorX2++)
								{
									float currentOutputX2 = (float)outputIteratorX2 * const16f - magicNumber17280f * 0.5f - value.m_outputPosition.x;
									if (!(currentOutputZ2 * currentOutputZ2 + currentOutputX2 * currentOutputX2 < outputRadius * outputRadius))
									{
										continue;
									}
									int targetCellID4 = outputIteratorZ2 * (const1080i + 1) + outputIteratorX2;
									WaterSimulation.Cell targetCell4 = array5[targetCellID4];
									if (isNaturalWaterSource && heightBuffer[targetCellID4] >= value.m_target)
									{
										continue;
									}
									int flowRate = (currentOutputRate + (outputCellCount >> 1)) / outputCellCount;
									if (flowRate > 65535 - targetCell4.m_height)
									{
										flowRate = 65535 - targetCell4.m_height;
									}
									if (flowRate > 0)
									{
										if (currentPollutantRate != 0)
										{
											int num138 = (currentPollutantRate + (outputCellCount >> 1)) / outputCellCount;
											targetCell4.m_pollution = (ushort)Mathf.Min(65535, targetCell4.m_pollution + num138);
											value.m_pollution = (uint)Mathf.Max(0, (int)value.m_pollution - num138);
										}
										targetCell4.m_height = (ushort)Mathf.Min(65535, targetCell4.m_height + flowRate);
										value.m_water = (uint)Mathf.Max(0, (int)value.m_water - flowRate);
										value.m_flow += (uint)flowRate;
										array5[targetCellID4] = targetCell4;
										if (targetCell4.m_height != 0)
										{
											array2[outputIteratorZ2 / ___m_bitCells] |= (ulong)(1L << outputIteratorX2 / ___m_bitCells);
										}
									}
								}
							}
						}
					}
					if (waterFrameIndex < ___m_waterFrameIndex)
					{
						__instance.m_waterSources[currentWaterSourceID] = value;
					}
				}
			}
			finally
			{
				Monitor.Exit(__instance.m_waterSources);
			}

		}
		
	}

}
