using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using ColossalFramework.Globalization;
using HarmonyLib;
using System;
using System.Reflection;
using ICities;
using System.Threading;
using System.Collections.Generic;

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
			bool logging = true;
			//finally the fun part!!!
			//Real edits might follow that aren't actually about just getting the code to run.

			foreach (KeyValuePair<int,DrainageArea> currentDrainageArea in DrainageAreaGrid.DrainageAreaDictionary) 
			{
				if (currentDrainageArea.Value.m_disabled == true) continue;
				if (Singleton<WeatherManager>.instance.m_currentRain == 0)
				{
					if (currentDrainageArea.Value.m_outputRate > 0)
					{
						currentDrainageArea.Value.m_outputRate = 0;
					}
					continue;
				}

				int currentOutputRate = 0;
				int currentPollutantRate = 0;
				float DrainageAreaArea = DrainageAreaGrid.gridQuotient * DrainageAreaGrid.gridQuotient;
				float currentCompositeRunoffCoefficent = DrainageAreaGrid.getCompositeRunoffCoefficientForDrainageArea(currentDrainageArea.Key);
				currentOutputRate = Mathf.CeilToInt(Singleton<WeatherManager>.instance.m_currentRain * currentCompositeRunoffCoefficent * OptionHandler.getSliderSetting("GlobalRunoffScalar"));
				if (logging) Debug.Log("[RF]WaterSimulationSimulateWaterPatchPostfix currentOutputRate = " + currentOutputRate.ToString());

				float MinimumDrainageAreaRunoff = OptionHandler.getSliderSetting("MinimumDrainageAreaRunoff");
				float MaximumDrainageAreaRunoff = OptionHandler.getSliderSetting("MaximumDrainageAreaRunoff");
				if (currentOutputRate < MinimumDrainageAreaRunoff)
				{
					currentOutputRate = (int)MinimumDrainageAreaRunoff;
				}
				if (currentOutputRate > MaximumDrainageAreaRunoff)
				{
					currentOutputRate = (int)MaximumDrainageAreaRunoff;
				}
				if (logging) Debug.Log("[RF]WaterSimulationSimulateWaterPatchPostfix currentOutputRate = " + currentOutputRate.ToString());

				currentPollutantRate = 0; //Need to calculate pollution runoff here for entrie drainage basin.
			
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
					float outputRadius = 100f; //needs some revision
					int outputMinX = Mathf.Max(Mathf.CeilToInt((currentDrainageArea.Value.m_outputPosition.x - outputRadius + magicNumber17280f * 0.5f) / const16f), 0);
					int outputMinZ = Mathf.Max(Mathf.CeilToInt((currentDrainageArea.Value.m_outputPosition.z - outputRadius + magicNumber17280f * 0.5f) / const16f), 0);
					int outputMaxX = Mathf.Min(Mathf.FloorToInt((currentDrainageArea.Value.m_outputPosition.x + outputRadius + magicNumber17280f * 0.5f) / const16f), const1080i);
					int outputMaxZ = Mathf.Min(Mathf.FloorToInt((currentDrainageArea.Value.m_outputPosition.z + outputRadius + magicNumber17280f * 0.5f) / const16f), const1080i);
					
					int outputCellCount = 0;
					for (int outputIteratorZ = outputMinZ; outputIteratorZ <= outputMaxZ; outputIteratorZ++)
					{
						float currentOutputZ = (float)outputIteratorZ * const16f - magicNumber17280f * 0.5f - currentDrainageArea.Value.m_outputPosition.z;
						for (int outputIteratorX = outputMinX; outputIteratorX <= outputMaxX; outputIteratorX++)
						{
							float currentOutputX = (float)outputIteratorX * const16f - magicNumber17280f * 0.5f - currentDrainageArea.Value.m_outputPosition.x;
							if (currentOutputZ * currentOutputZ + currentOutputX * currentOutputX < outputRadius * outputRadius)
							{
								int targetCellID3 = outputIteratorZ * (const1080i + 1) + outputIteratorX;
								WaterSimulation.Cell targetCell3 = array5[targetCellID3]; //nothing is done with this
								int terrainBlockHeight3 = heightBuffer[targetCellID3]; //nothing is done with this
								outputCellCount++;
							}
						}
					}
					if (logging) Debug.Log("[RF]WaterSimulationSimulateWaterPatchPostfix outputCellCount = " + outputCellCount.ToString());
					if (currentOutputRate > 0 && outputCellCount > 0)
					{
						for (int outputIteratorZ2 = outputMinZ; outputIteratorZ2 <= outputMaxZ; outputIteratorZ2++)
						{
							float currentOutputZ2 = (float)outputIteratorZ2 * const16f - magicNumber17280f * 0.5f - currentDrainageArea.Value.m_outputPosition.z;
							for (int outputIteratorX2 = outputMinX; outputIteratorX2 <= outputMaxX; outputIteratorX2++)
							{
								float currentOutputX2 = (float)outputIteratorX2 * const16f - magicNumber17280f * 0.5f - currentDrainageArea.Value.m_outputPosition.x;
								if (!(currentOutputZ2 * currentOutputZ2 + currentOutputX2 * currentOutputX2 < outputRadius * outputRadius))
								{
									continue;
								}
								int targetCellID4 = outputIteratorZ2 * (const1080i + 1) + outputIteratorX2;
								WaterSimulation.Cell targetCell4 = array5[targetCellID4];

								int flowRate = (currentOutputRate + (outputCellCount >> 1)) / outputCellCount;
								if (logging) Debug.Log("[RF]WaterSimulationSimulateWaterPatchPostfix Flowrate = " + flowRate.ToString());
								if (flowRate > 65535 - targetCell4.m_height)
								{
									flowRate = 65535 - targetCell4.m_height;
								}
								if (flowRate > 0)
								{
									if (currentPollutantRate != 0) //currently always false because we haven't calculated pollutant rate
									{
										int num138 = (currentPollutantRate + (outputCellCount >> 1)) / outputCellCount;
										targetCell4.m_pollution = (ushort)Mathf.Min(65535, targetCell4.m_pollution + num138);
										currentDrainageArea.Value.m_pollution = (uint)Mathf.Max(0, (int)currentDrainageArea.Value.m_pollution - num138);
									}
									targetCell4.m_height = (ushort)Mathf.Min(65535, targetCell4.m_height + flowRate);
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
			}
			

		}
		
	}

}
