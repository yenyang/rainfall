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
        static bool Prefix(int pollutionDisposeRate, ref WaterSimulation __instance, ref ushort[] ___m_heightBuffer, ref uint ___m_waterFrameIndex, ref ulong[] ___m_waterExists1, ref ulong[] ___m_waterExists2, ref ulong[] ___m_waterExists3, ref int ___m_bitCells, ref WaterSimulation.Cell[][] ___m_waterBuffers, ref int ___m_stepIndex, ref FastList<WaterWave>___m_tsunamiWaves, ref FastList<WaterWave> ___m_impactWaves, ref FastList<WaterWave> ___m_waterWaves, ref byte[][] ___m_heightMaps, ref Vector4[] ___m_heightBounds, ref object ___m_simulationBufferLock, ref int ___m_readLockCount, ref uint ___m_waterFrameIndex2, ref WaterSimulation.Status ___m_simulationStatus, ref uint ___m_renderingFrameIndex)
        {
			//Debug.Log("[RF]Watersimulation.SimulateWater Overriding Water Simulation!");
			float currentSeaLevel = __instance.m_currentSeaLevel; //verified
			float nextSeaLevel = __instance.m_nextSeaLevel; //verified
			bool resetWater = __instance.m_resetWater; //verified
			__instance.m_currentSeaLevel = nextSeaLevel; //verified
			__instance.m_resetWater = false; //verified
			int num = (int)(currentSeaLevel * 64f); //verified
			int num2 = (int)(nextSeaLevel * 64f); //verified
			bool flag = num2 != num || resetWater; //verified
			float const16f = 16f; //verified
			int const1080f = 1080; //verified
			int num5 = 120; //verified
			float magicNumber17280f = (float)const1080f * const16f; //verified

			//begin edit

			ushort[] heightBuffer = ___m_heightBuffer; //verified

			uint num7 = (uint)((int)___m_waterFrameIndex & -64);//edit variable with ___ //verified
			uint waterFrameIndex = ___m_waterFrameIndex;//edit variable with ___ //verified
			//end edit
			uint num8 = (uint)(600 + (const1080f + 1) * 4); //verified
			uint maxProgress = 81u; //verified
			uint num9 = 200u; //verified
			uint num10 = 0u; //verified
			uint num11 = ((num7 >> 6) + 2) % 3u; //verified
			uint num12 = (num7 >> 6) % 3u; //verified
			uint num13 = ((num7 >> 6) + 1) % 3u; //verified
			ulong[] array; //verified
			switch (num11) //verified
			{ 
				case 0u: //verified
					array = ___m_waterExists1; //edit variable with ___  //verified
					break; //verified
				case 1u: //verified
					array = ___m_waterExists2;//edit variable with ___  //verified
					break; //verified
				default://verified
					array = ___m_waterExists3;//edit variable with ___ //verified
					break;
			}
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
			ulong[] array3; //verified
			switch (num13) //verified
			{
				case 0u: //verified
					array3 = ___m_waterExists1;//edit variable with ___ //verified
					break; //verified
				case 1u:
					array3 = ___m_waterExists2;//edit variable with ___ //verified
					break; //verified
				default:
					array3 = ___m_waterExists3;//edit variable with ___ //verified
					break; //verified
			}
		
			for (int i = 0; i < 64; i++) //verified
			{
				if (array[i] != 0) //verified
				{
					int num14 = Mathf.Min(___m_bitCells, const1080f + 1 - i * ___m_bitCells);//edit variable with ___  //verified
					num8 = (uint)((int)num8 + ((int)FixedMath.NumberOfSetBits(array[i]) * num14 << 1)); //verified
				}
				array2[i] = 0uL; //verified
			}
			
			for (int j = 0; j < 9; j++) //verified
			{
				for (int k = 0; k < 9; k++)  //verified
				{
					int minX = num5 * k; //verified
					int maxX = num5 * (k + 1); //verified
					int minZ = num5 * j; //verified
					int maxZ = num5 * (j + 1); //verified
					//begin edit
					MethodInfo countWaterExistsMethod = AccessTools.Method(typeof(WaterSimulation), "CountWaterExists", null, null); //verified

					object countWaterExistsObj = countWaterExistsMethod.Invoke(__instance, new object[] { minX, minZ, maxX, maxZ, array });

					uint countWaterExistsUInt = (uint)countWaterExistsObj;

					num8 = (uint)((int)num8 + ((int)countWaterExistsUInt * ___m_bitCells + 2)); //verified
				}
			}
			
			WaterSimulation.Cell[] array4 = ___m_waterBuffers[~(num7 >> 6) & 1]; //verified
			WaterSimulation.Cell[] array5 = ___m_waterBuffers[(num7 >> 6) & 1]; //verified
			int num15 = ((___m_stepIndex & 3) == 0) ? int.MaxValue : 0; //verified
			int num16 = ((___m_stepIndex & 3) == 1) ? int.MaxValue : 0;//verified
			int num17 = ((___m_stepIndex & 3) == 2) ? int.MaxValue : 0; //verified
			int num18 = ((___m_stepIndex & 3) == 3) ? int.MaxValue : 0; //verified
			int num19 = ((___m_stepIndex & 3) <= 1) ? int.MaxValue : 0; //verified
			ushort num20 = (ushort)(((___m_stepIndex & 3) == 0) ? 1 : 0); //verified
			ushort num21 = (ushort)(((___m_stepIndex & 0xF) < pollutionDisposeRate) ? 1 : 0); //verified
			Randomizer randomizer = new Randomizer(___m_stepIndex); //verified
			___m_stepIndex++; //verified
			int num22 = 11; //verified
			uint num23 = (uint)(1 << num22); //verified
			int num24 = (int)(num23 - 1); //verified
			___m_tsunamiWaves.Clear(); //verified
			___m_impactWaves.Clear(); //verified

			while (!Monitor.TryEnter(___m_waterWaves, 0)) //verified
			{
			}
			try
			{
				for (int l = 0; l < ___m_waterWaves.m_size; l++) //verified
				{
					switch (___m_waterWaves.m_buffer[l].m_type) //verified
					{
						case 1:
							___m_tsunamiWaves.Add(___m_waterWaves.m_buffer[l]); 
							___m_waterWaves.m_buffer[l].m_currentTime = (ushort)Mathf.Min(___m_waterWaves.m_buffer[l].m_currentTime + 64, 65535);
							break;
						case 2:
							___m_impactWaves.Add(___m_waterWaves.m_buffer[l]);
							___m_waterWaves.m_buffer[l].m_currentTime = (ushort)Mathf.Min(___m_waterWaves.m_buffer[l].m_currentTime + 64, 65535);
							if (___m_waterWaves.m_buffer[l].m_currentTime > ___m_waterWaves.m_buffer[l].m_duration)
							{
								__instance.ReleaseWaterWave((ushort)(l + 1)); //added __instance to public method.
							}
							break;
					}
				}
			}
			finally
			{
				Monitor.Exit(___m_waterWaves);
			}
			
			for (int m = -5; m <= const1080f; m++)
			{
				int num25 = (m + 5 + num5) / num5 - 1;
				if (num25 == (m + 4 + num5) / num5 && num25 < 9)
				{
					for (int n = 0; n < 9; n++)
					{
						//begin edit
						//begin edit
						MethodInfo SetCurrentWaterFrameMethod3 = AccessTools.Method(typeof(WaterSimulation), "SetCurrentWaterFrame", null, null);
						object SetCurrentWaterFrameBool3 = SetCurrentWaterFrameMethod3.Invoke(__instance, new object[] { num7, num9, num10, num8, maxProgress, true });
						if ((bool)SetCurrentWaterFrameBool3) {// end edit

							return false; //not sure what to do here?
						}
						int num26 = num25 * 9 + n;
						int num27 = num5 * n;
						int num28 = num5 * (n + 1);
						int num29 = num5 * num25;
						int num30 = num5 * (num25 + 1);
						//begin edit
						MethodInfo CountWaterExistsMethod2 = AccessTools.Method(typeof(WaterSimulation), "CountWaterExists", null, null);
						object CountWaterExistsObj2 = CountWaterExistsMethod2.Invoke(__instance, new object[] { num27, num29, num28, num30, array });
						uint CountWaterExistsUint2 = (uint)CountWaterExistsObj2;
						num9 = (uint)((int)num9 + ((int)CountWaterExistsUint2 * ___m_bitCells + 2)); // end edit
						num10++;
					
						
						if (__instance.WaterExists(num27, num29, num28, num30))
						{
							int num31 = 128 - num5 >> 1;
							int xOffset = 64 - num5 * n - (num5 >> 1);
							int zOffset = 64 - num5 * num25 - (num5 >> 1);
							int minX2 = num27 - num31;
							int maxX2 = num28 + num31 - 1;
							int minZ2 = num29 - num31;
							int maxZ2 = num30 + num31 - 1;
							//begin Edit
							MethodInfo FillHeightMapMethod = AccessTools.Method(typeof(WaterSimulation), "FillHeightMap", null, null);
							FillHeightMapMethod.Invoke(__instance, new object[] { minX2, minZ2, maxX2, maxZ2, ___m_heightMaps[num26],  ___m_heightBounds[num26], xOffset, zOffset, array4, array });
							// end edit
							num31 = 64 - (num5 >> 1) >> 1;
							xOffset = 32 - (num5 >> 1) * n - (num5 >> 2);
							zOffset = 32 - (num5 >> 1) * num25 - (num5 >> 2);
							minX2 = Mathf.Max((num27 >> 1) - num31, 0);
							maxX2 = Mathf.Min((num28 >> 1) + num31 - 1, 540);
							minZ2 = Mathf.Max((num29 >> 1) - num31, 0);
							maxZ2 = Mathf.Min((num30 >> 1) + num31 - 1, 540);
							//begin edit
							MethodInfo FillSurfaceMapMethod = AccessTools.Method(typeof(WaterSimulation), "FillSurfaceMap", null, null);
							FillSurfaceMapMethod.Invoke(__instance, new object[] { minX2, minZ2, maxX2, maxZ2, __instance.m_surfaceMapsA[num26], __instance.m_surfaceMapsB[num26], xOffset, zOffset, array4, array });
							//end edit
						}
					}
				}
				num25 = m + 3;
				if (num25 >= 0 && num25 <= const1080f)
				{
					//begin edit
					MethodInfo SetCurrentWaterFrameMethod4 = AccessTools.Method(typeof(WaterSimulation), "SetCurrentWaterFrame", null, null);
					object SetCurrentWaterFrameBool4 = SetCurrentWaterFrameMethod4.Invoke(__instance, new object[] { num7, num9, num10, num8, maxProgress, false });
					if ((bool)SetCurrentWaterFrameBool4) // end edit
					{
						return false; //not sure what to do here
					}
					num9 += FixedMath.NumberOfSetBits(array[num25 / ___m_bitCells]) + 2;
					ulong num32 = array[Mathf.Max((num25 - 2) / ___m_bitCells, 0)] | array[Mathf.Min((num25 + 2) / ___m_bitCells, 63)] | array3[Mathf.Max((num25 - 2) / ___m_bitCells, 0)] | array3[Mathf.Min((num25 + 2) / ___m_bitCells, 63)];
					int num33 = 0;
					int num34 = 0;
					while (num34 < const1080f)
					{
						num33 = num34;
						while (num33 <= const1080f && (num32 & 1) == 0)
						{
							num33 += ___m_bitCells;
							num32 >>= 1;
						}
						num34 = num33;
						while (num34 <= const1080f && (num32 & 1) != 0)
						{
							num34 += ___m_bitCells;
							num32 >>= 1;
						}
						int num35 = Mathf.Max(num33 - 2, 0);
						int num36 = Mathf.Min(num34 + 1, const1080f);
						if (num35 > num36)
						{
							break;
						}
						int num37 = num25 * (const1080f + 1) + num35;
						WaterSimulation.Cell cell = default(WaterSimulation.Cell);
						WaterSimulation.Cell cell2 = array4[num37];
						int num38 = heightBuffer[num37];
						if (cell2.m_height != 0)
						{
							cell2.m_height -= num20;
						}
						if (cell2.m_pollution != 0)
						{
							cell2.m_pollution -= num21;
						}
						if (cell2.m_pollution > cell2.m_height)
						{
							cell2.m_pollution = cell2.m_height;
						}
						for (int num39 = num35; num39 <= num36; num39++)
						{
							WaterSimulation.Cell cell3 = default(WaterSimulation.Cell);
							int num40 = 0;
							int num41 = 0;
							int num42 = 0;
							for (int num43 = 0; num43 < ___m_impactWaves.m_size; num43++)
							{
								int num44 = num39;
								WaterWave waterWave = ___m_impactWaves[num43];
								if (num44 < waterWave.m_minX)
								{
									continue;
								}
								int num45 = num39;
								WaterWave waterWave2 = ___m_impactWaves[num43];
								if (num45 > waterWave2.m_maxX + 1)
								{
									continue;
								}
								int num46 = num25;
								WaterWave waterWave3 = ___m_impactWaves[num43];
								if (num46 < waterWave3.m_minZ)
								{
									continue;
								}
								int num47 = num25;
								WaterWave waterWave4 = ___m_impactWaves[num43];
								if (num47 <= waterWave4.m_maxZ + 1)
								{
									WaterWave waterWave5 = ___m_impactWaves[num43];
									ushort maxX3 = waterWave5.m_maxX;
									WaterWave waterWave6 = ___m_impactWaves[num43];
									int a = maxX3 - waterWave6.m_origX;
									WaterWave waterWave7 = ___m_impactWaves[num43];
									ushort origX = waterWave7.m_origX;
									WaterWave waterWave8 = ___m_impactWaves[num43];
									int num48 = 1 + Mathf.Max(a, origX - waterWave8.m_minX);
									num48 *= num48;
									int num49 = num39;
									WaterWave waterWave9 = ___m_impactWaves[num43];
									int num50 = num49 - waterWave9.m_origX;
									int num51 = num25;
									WaterWave waterWave10 = ___m_impactWaves[num43];
									int num52 = num51 - waterWave10.m_origZ;
									int num53 = num50 + 1;
									int num54 = num52 + 1;
									int num55 = num50 * num50 + num52 * num52;
									int num56 = num53 * num53 + num52 * num52;
									int num57 = num50 * num50 + num54 * num54;
									if (num55 < num48)
									{
										WaterWave waterWave11 = ___m_impactWaves[num43];
										int delta = waterWave11.m_delta;
										delta -= delta * num55 / num48;
										num41 += delta;
										num42 += delta;
									}
									if (num56 < num48)
									{
										WaterWave waterWave12 = ___m_impactWaves[num43];
										int delta2 = waterWave12.m_delta;
										delta2 -= delta2 * num56 / num48;
										num41 -= delta2;
									}
									if (num57 < num48)
									{
										WaterWave waterWave13 = ___m_impactWaves[num43];
										int delta3 = waterWave13.m_delta;
										delta3 -= delta3 * num57 / num48;
										num42 -= delta3;
									}
								}
							}
							if (num39 != const1080f)
							{
								cell3 = array4[num37 + 1];
								num40 = heightBuffer[num37 + 1];
								if (cell3.m_height != 0)
								{
									cell3.m_height -= num20;
								}
								if (cell3.m_pollution != 0)
								{
									cell3.m_pollution -= num21;
								}
								if (cell3.m_pollution > cell3.m_height)
								{
									cell3.m_pollution = cell3.m_height;
								}
								int num58 = num38 + num41 + cell2.m_height - num40 - cell3.m_height;
								int num59 = cell2.m_velocityX;
								if (num59 > 0)
								{
									num59 = randomizer.Int32(num23) + num59 * num24 >> num22;
								}
								if (num59 < 0)
								{
									num59 = -(randomizer.Int32(num23) - num59 * num24 >> num22);
								}
								if (num58 > 0)
								{
									num59 += num58 + randomizer.Int32(4u) >> 2;
								}
								else if (num58 < 0)
								{
									num59 -= randomizer.Int32(4u) - num58 >> 2;
								}
								if (num59 > cell2.m_height)
								{
									num59 = cell2.m_height;
								}
								if (num59 < -cell3.m_height)
								{
									num59 = -cell3.m_height;
								}
								cell2.m_velocityX = (short)Mathf.Clamp(num59, -32766, 32766);
							}
							if (num25 != const1080f)
							{
								WaterSimulation.Cell cell4 = array4[num37 + const1080f + 1];
								int num60 = heightBuffer[num37 + const1080f + 1];
								if (cell4.m_height != 0)
								{
									cell4.m_height -= num20;
								}
								int num61 = num38 + num42 + cell2.m_height - num60 - cell4.m_height;
								int num62 = cell2.m_velocityZ;
								if (num62 > 0)
								{
									num62 = randomizer.Int32(num23) + num62 * num24 >> num22;
								}
								if (num62 < 0)
								{
									num62 = -(randomizer.Int32(num23) - num62 * num24 >> num22);
								}
								if (num61 > 0)
								{
									num62 += num61 + randomizer.Int32(4u) >> 2;
								}
								else if (num61 < 0)
								{
									num62 -= randomizer.Int32(4u) - num61 >> 2;
								}
								if (num62 > cell2.m_height)
								{
									num62 = cell2.m_height;
								}
								if (num62 < -cell4.m_height)
								{
									num62 = -cell4.m_height;
								}
								cell2.m_velocityZ = (short)Mathf.Clamp(num62, -32766, 32766);
							}
							WaterSimulation.Cell cell5 = default(WaterSimulation.Cell);
							if (num25 != 0)
							{
								cell5 = array5[num37 - const1080f - 1];
							}
							int num63 = 0;
							int num64 = 0;
							if (cell.m_velocityX < 0)
							{
								num63 -= cell.m_velocityX;
							}
							else
							{
								num64 += cell.m_velocityX;
							}
							if (cell5.m_velocityZ < 0)
							{
								num63 -= cell5.m_velocityZ;
							}
							else
							{
								num64 += cell5.m_velocityZ;
							}
							if (cell2.m_velocityX > 0)
							{
								num63 += cell2.m_velocityX;
							}
							else
							{
								num64 -= cell2.m_velocityX;
							}
							if (cell2.m_velocityZ > 0)
							{
								num63 += cell2.m_velocityZ;
							}
							else
							{
								num64 -= cell2.m_velocityZ;
							}
							if (num63 > cell2.m_height)
							{
								if (cell.m_velocityX < 0)
								{
									cell.m_velocityX = (short)(-((((num63 - 1) & num15) - cell.m_velocityX * cell2.m_height) / num63));
									array5[num37 - 1] = cell;
								}
								if (cell5.m_velocityZ < 0)
								{
									cell5.m_velocityZ = (short)(-((((num63 - 1) & num17) - cell5.m_velocityZ * cell2.m_height) / num63));
									array5[num37 - const1080f - 1] = cell5;
								}
								if (cell2.m_velocityX > 0)
								{
									cell2.m_velocityX = (short)((((num63 - 1) & num16) + cell2.m_velocityX * cell2.m_height) / num63);
								}
								if (cell2.m_velocityZ > 0)
								{
									cell2.m_velocityZ = (short)((((num63 - 1) & num18) + cell2.m_velocityZ * cell2.m_height) / num63);
								}
							}
							if (num64 > 65535 - cell2.m_height)
							{
								if (cell.m_velocityX > 0)
								{
									cell.m_velocityX = (short)((((num64 - 1) & num15) + cell.m_velocityX * (65535 - cell2.m_height)) / num64);
									array5[num37 - 1] = cell;
								}
								if (cell5.m_velocityZ > 0)
								{
									cell5.m_velocityZ = (short)((((num64 - 1) & num17) + cell5.m_velocityZ * (65535 - cell2.m_height)) / num64);
									array5[num37 - const1080f - 1] = cell5;
								}
								if (cell2.m_velocityX < 0)
								{
									cell2.m_velocityX = (short)(-((((num64 - 1) & num16) - cell2.m_velocityX * (65535 - cell2.m_height)) / num64));
								}
								if (cell2.m_velocityZ < 0)
								{
									cell2.m_velocityZ = (short)(-((((num64 - 1) & num18) - cell2.m_velocityZ * (65535 - cell2.m_height)) / num64));
								}
							}
							array5[num37] = cell2;
							cell = cell2;
							cell2 = cell3;
							num38 = num40;
							num37++;
						}
					}
				}
				num25 = m;
				if (num25 < 0 || num25 > const1080f)
				{
					continue;
				}
				//begin edit
				MethodInfo SetCurrentWaterFrameMethod5 = AccessTools.Method(typeof(WaterSimulation), "SetCurrentWaterFrame", null, null);
				object SetCurrentWaterFrameBool5 = SetCurrentWaterFrameMethod5.Invoke(__instance, new object[] { num7, num9, num10, num8, maxProgress, false });
				if ((bool)SetCurrentWaterFrameBool5) // end edit
				{
					return false; //not sure what to do here
				}
				num9 += FixedMath.NumberOfSetBits(array[num25 / ___m_bitCells]) + 2;
				ulong num65 = array[Mathf.Max((num25 - 1) / ___m_bitCells, 0)] | array[Mathf.Min((num25 + 1) / ___m_bitCells, 63)];
				int num66 = 0;
				int num67 = 0;
				while (num67 < const1080f)
				{
					num66 = num67;
					while (num66 <= const1080f && (num65 & 1) == 0)
					{
						num66 += ___m_bitCells;
						num65 >>= 1;
					}
					num67 = num66;
					while (num67 <= const1080f && (num65 & 1) != 0)
					{
						num67 += ___m_bitCells;
						num65 >>= 1;
					}
					int num68 = Mathf.Max(num66 - 1, 0);
					int num69 = Mathf.Min(num67, const1080f);
					if (num68 > num69)
					{
						break;
					}
					int num70 = num25 * (const1080f + 1) + num68;
					for (int num71 = num68; num71 <= num69; num71++)
					{
						WaterSimulation.Cell cell6 = array5[num70];
						if (num71 != const1080f)
						{
							if (cell6.m_velocityX > 0)
							{
								WaterSimulation.Cell cell7 = array5[num70 + 1];
								int velocityX = cell6.m_velocityX;
								if (cell6.m_pollution != 0 && cell6.m_height != 0)
								{
									int num72 = (velocityX * cell6.m_pollution + ((cell6.m_height - 1) & num19)) / (int)cell6.m_height;
									cell6.m_pollution = (ushort)(cell6.m_pollution - num72);
									cell7.m_pollution = (ushort)(cell7.m_pollution + num72);
								}
								cell6.m_height = (ushort)Mathf.Max(cell6.m_height - velocityX, 0);
								cell7.m_height = (ushort)Mathf.Min(cell7.m_height + velocityX, 65535);
								array5[num70] = cell6;
								array5[num70 + 1] = cell7;
								array2[num25 / ___m_bitCells] |= (ulong)(1L << (num71 + 1) / ___m_bitCells);
							}
							else if (cell6.m_velocityX < 0)
							{
								WaterSimulation.Cell cell8 = array5[num70 + 1];
								int num73 = -cell6.m_velocityX;
								if (cell8.m_pollution != 0 && cell8.m_height != 0)
								{
									int num74 = (num73 * cell8.m_pollution + ((cell8.m_height - 1) & num19)) / (int)cell8.m_height;
									cell6.m_pollution = (ushort)(cell6.m_pollution + num74);
									cell8.m_pollution = (ushort)(cell8.m_pollution - num74);
								}
								cell6.m_height = (ushort)Mathf.Min(cell6.m_height + num73, 65535);
								cell8.m_height = (ushort)Mathf.Max(cell8.m_height - num73, 0);
								array5[num70] = cell6;
								array5[num70 + 1] = cell8;
							}
						}
						if (num25 != const1080f)
						{
							if (cell6.m_velocityZ > 0)
							{
								WaterSimulation.Cell cell9 = array5[num70 + const1080f + 1];
								int velocityZ = cell6.m_velocityZ;
								if (cell6.m_pollution != 0 && cell6.m_height != 0)
								{
									int num75 = (velocityZ * cell6.m_pollution + ((cell6.m_height - 1) & num19)) / (int)cell6.m_height;
									cell6.m_pollution = (ushort)(cell6.m_pollution - num75);
									cell9.m_pollution = (ushort)(cell9.m_pollution + num75);
								}
								cell6.m_height = (ushort)Mathf.Max(cell6.m_height - velocityZ, 0);
								cell9.m_height = (ushort)Mathf.Min(cell9.m_height + velocityZ, 65535);
								array5[num70] = cell6;
								array5[num70 + const1080f + 1] = cell9;
								array2[(num25 + 1) / ___m_bitCells] |= (ulong)(1L << num71 / ___m_bitCells);
							}
							else if (cell6.m_velocityZ < 0)
							{
								WaterSimulation.Cell cell10 = array5[num70 + const1080f + 1];
								int num76 = -cell6.m_velocityZ;
								if (cell10.m_pollution != 0 && cell10.m_height != 0)
								{
									int num77 = (num76 * cell10.m_pollution + ((cell10.m_height - 1) & num19)) / (int)cell10.m_height;
									cell6.m_pollution = (ushort)(cell6.m_pollution + num77);
									cell10.m_pollution = (ushort)(cell10.m_pollution - num77);
								}
								cell6.m_height = (ushort)Mathf.Min(cell6.m_height + num76, 65535);
								cell10.m_height = (ushort)Mathf.Max(cell10.m_height - num76, 0);
								array5[num70] = cell6;
								array5[num70 + const1080f + 1] = cell10;
							}
						}
						if (cell6.m_height != 0)
						{
							array2[num25 / ___m_bitCells] |= (ulong)(1L << num71 / ___m_bitCells);
						}
						num70++;
					}
				}
				if (flag)
				{
					int num78 = num25 * (const1080f + 1);
					for (int num79 = 0; num79 <= const1080f; num79++)
					{
						WaterSimulation.Cell cell11 = array5[num78];
						int num80 = heightBuffer[num78];
						int num81 = num80 + cell11.m_height;
						if (cell11.m_height == 0 || resetWater)
						{
							int num82 = num2 - num80;
							if (num82 > 0)
							{
								cell11.m_height = (ushort)Mathf.Min(num82, 65535);
								array2[num25 / ___m_bitCells] |= (ulong)(1L << num79 / ___m_bitCells);
							}
							else
							{
								cell11 = default(WaterSimulation.Cell);
							}
						}
						else
						{
							int num83 = num2 - num;
							if (num81 > num + 128)
							{
								if (num83 < 0)
								{
									num83 += num81 - num - 128;
									if (num83 > 0)
									{
										num83 = 0;
									}
								}
								else
								{
									num83 -= num81 - num - 128;
									if (num83 < 0)
									{
										num83 = 0;
									}
								}
							}
							int num84 = cell11.m_height + num83;
							if (num84 > 0)
							{
								cell11.m_height = (ushort)Mathf.Min(num84, 65535);
							}
							else
							{
								cell11 = default(WaterSimulation.Cell);
							}
						}
						array5[num78] = cell11;
						num78++;
					}
				}
				int num85 = num25 * (const1080f + 1);
				int num86 = (num25 == 0 || num25 == const1080f) ? 1 : const1080f;
				for (int num87 = 0; num87 <= const1080f; num87 += num86)
				{
					int num88 = num2;
					for (int num89 = 0; num89 < ___m_tsunamiWaves.m_size; num89++)
					{
						num88 = ___m_tsunamiWaves.m_buffer[num89].GetSeaLevel(num88, num87, num25);
					}
					WaterSimulation.Cell cell12 = array5[num85];
					int num90 = heightBuffer[num85] + cell12.m_height - num88;
					if (num90 > 0 && cell12.m_height != 0)
					{
						if (num90 > cell12.m_height)
						{
							num90 = cell12.m_height;
						}
						if (cell12.m_pollution != 0)
						{
							int num91 = (num90 * cell12.m_pollution + ((cell12.m_height - 1) & num19)) / (int)cell12.m_height;
							cell12.m_pollution = (ushort)(cell12.m_pollution - num91);
						}
						cell12.m_height = (ushort)(cell12.m_height - num90);
						array5[num85] = cell12;
					}
					else if (num90 < 0)
					{
						cell12.m_height = (ushort)(cell12.m_height - num90);
						array5[num85] = cell12;
						array2[num25 / ___m_bitCells] |= (ulong)(1L << num87 / ___m_bitCells);
					}
					num85 += num86;
				}
			}
			//begin edit
			MethodInfo SetCurrentWaterFrameMethod = AccessTools.Method(typeof(WaterSimulation), "SetCurrentWaterFrame", null, null);
			object SetCurrentWaterFrameBool = SetCurrentWaterFrameMethod.Invoke(__instance, new object[] { num7, num9, num10, num8, maxProgress, false });
			if ((bool)SetCurrentWaterFrameBool) // end edit
			{
				//Debug.Log("[RF]WSSWP Early Return!");
				return false; //not sure what to do here
			}
			num9 += 400;
			
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
					if (isNaturalWaterSource) //if m_type = Natural (placed by water tool in map editor or landscape tools mod)
					{
						value.m_water = 0u;
						value.m_pollution = 0u;
					}
					int inputRate = (int)value.m_inputRate;
					if (inputRate > 0)
					{
						float inputRadius = Mathf.Sqrt(inputRate) * 0.4f + 10f;
						if (value.m_type == 2 || value.m_type == 3) //if m_type = Drinage Structure or Cleaning
						{
							inputRadius = Mathf.Min(50f, inputRadius);
						}
						int inputMinX = Mathf.Max(Mathf.CeilToInt((value.m_inputPosition.x - inputRadius + magicNumber17280f * 0.5f) / const16f), 0); //const16f = 16f, magicNumber17280f = 17280f, 
						int inputMinZ = Mathf.Max(Mathf.CeilToInt((value.m_inputPosition.z - inputRadius + magicNumber17280f * 0.5f) / const16f), 0);
						int inputMaxX = Mathf.Min(Mathf.FloorToInt((value.m_inputPosition.x + inputRadius + magicNumber17280f * 0.5f) / const16f), const1080f); //const1080f = 1080
						int inputMaxZ = Mathf.Min(Mathf.FloorToInt((value.m_inputPosition.z + inputRadius + magicNumber17280f * 0.5f) / const16f), const1080f);
						int num98 = 0;
						for (int iteratorZ = inputMinZ; iteratorZ <= inputMaxZ; iteratorZ++)
						{
							float currentInputZ = (float)iteratorZ * const16f - magicNumber17280f * 0.5f - value.m_inputPosition.z;
							for (int iteratorX = inputMinX; iteratorX <= inputMaxX; iteratorX++)
							{
								float currentInputX = (float)iteratorX * const16f - magicNumber17280f * 0.5f - value.m_inputPosition.x;
								if (currentInputZ * currentInputZ + currentInputX * currentInputX < inputRadius * inputRadius)
								{
									int targetCellID = iteratorZ * (const1080f + 1) + iteratorX;
									WaterSimulation.Cell targetWaterCell = array5[targetCellID];
									int terrainBlockHeight = heightBuffer[targetCellID];
									int maxTargetOrTerrain = Mathf.Max(value.m_target, terrainBlockHeight);
									num98 += Mathf.Min(terrainBlockHeight + targetWaterCell.m_height - maxTargetOrTerrain, targetWaterCell.m_height);
								}
							}
						}
						inputRate = ((!isNaturalWaterSource) ? Mathf.Min(inputRate, num98) : Mathf.Min(inputRate, num98 >> 1));
						if (inputRate > 0)
						{
							for (int iteratorZ2 = inputMinZ; iteratorZ2 <= inputMaxZ; iteratorZ2++)
							{
								float currentInputZ2 = (float)iteratorZ2 * const16f - magicNumber17280f * 0.5f - value.m_inputPosition.z;
								for (int iteratorX2 = inputMinX; iteratorX2 <= inputMaxX; iteratorX2++)
								{
									float currentInputX2 = (float)iteratorX2 * const16f - magicNumber17280f * 0.5f - value.m_inputPosition.x;
									if (!(currentInputZ2 * currentInputZ2 + currentInputX2 * currentInputX2 < inputRadius * inputRadius))
									{
										continue;
									}
									int targetCellID2 = iteratorZ2 * (const1080f + 1) + iteratorX2;
									WaterSimulation.Cell targetCell2 = array5[targetCellID2];
									int terrainBlockHeight2 = heightBuffer[targetCellID2];
									int maxTargetOrTerrain2 = Mathf.Max(value.m_target, terrainBlockHeight2);
									int num113 = Mathf.Min(terrainBlockHeight2 + targetCell2.m_height - maxTargetOrTerrain2, targetCell2.m_height);
									num113 = (num113 * inputRate + num98 - 1) / num98;
									if (num113 > 0)
									{
										if (targetCell2.m_pollution != 0 && targetCell2.m_height != 0)
										{
											int num114 = (num113 * targetCell2.m_pollution + ((targetCell2.m_height - 1) & num19)) / (int)targetCell2.m_height;
											targetCell2.m_pollution = (ushort)(targetCell2.m_pollution - num114);
											value.m_pollution += (uint)num114;
										}
										value.m_water += (uint)num113;
										targetCell2.m_height = (ushort)(targetCell2.m_height - num113);
										array5[targetCellID2] = targetCell2;
									}
								}
							}
						}
					}
					if (value.m_type == 3)
					{
						uint pollutionReduction = value.m_water >> 3;
						if (pollutionReduction > value.m_pollution)
						{
							pollutionReduction = value.m_pollution;
						}
						value.m_pollution -= pollutionReduction;
					}
					if (value.m_pollution > value.m_water)
					{
						value.m_pollution = value.m_water;
					}
					int currentOutputRate;
					int currentPollutantRate;
					if (isNaturalWaterSource)
					{
						currentOutputRate = (int)value.m_outputRate;
						currentPollutantRate = 0;
					}
					//begin edits for Rainfall!!!
					else if (!DrainageBasinGrid.isWaterSourceAssociatedWithADrainageBasin((uint)currentWaterSourceID))
					{
						currentOutputRate = Mathf.Min((int)value.m_outputRate, (int)value.m_water);
						currentPollutantRate = (int)((value.m_water != 0) ? (value.m_pollution * currentOutputRate / (long)value.m_water) : 0);
					} else
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
					if (currentOutputRate > 0)
					{
						float outputRadius = (value.m_type != 2 && value.m_type != 3) ? (Mathf.Sqrt(currentOutputRate) * 0.4f + 10f) : Mathf.Clamp(Mathf.Sqrt(currentOutputRate) * 0.4f, 10f, 50f);
						int outputMinX = Mathf.Max(Mathf.CeilToInt((value.m_outputPosition.x - outputRadius + magicNumber17280f * 0.5f) / const16f), 0);
						int outputMinZ = Mathf.Max(Mathf.CeilToInt((value.m_outputPosition.z - outputRadius + magicNumber17280f * 0.5f) / const16f), 0);
						int outputMaxX = Mathf.Min(Mathf.FloorToInt((value.m_outputPosition.x + outputRadius + magicNumber17280f * 0.5f) / const16f), const1080f);
						int outputMaxZ = Mathf.Min(Mathf.FloorToInt((value.m_outputPosition.z + outputRadius + magicNumber17280f * 0.5f) / const16f), const1080f);
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
									int targetCellID3 = outputIteratorZ * (const1080f + 1) + outputIteratorX;
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
									int targetCellID4 = outputIteratorZ2 * (const1080f + 1) + outputIteratorX2;
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

			
			
			MethodInfo SetCurrentWaterFrameMethod2 = AccessTools.Method(typeof(WaterSimulation), "SetCurrentWaterFrame", null, null);
			SetCurrentWaterFrameMethod2.Invoke(__instance, new object[] { num7, num9, num10, num8, maxProgress,  false});
			return false;
		}
		
	}

}
