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
	[HarmonyPatch(typeof(WaterTool), "RenderGeometry")]
	class WaterToolRenderGeometryPatch
	{
		static void Postfix(ref RenderManager.CameraInfo cameraInfo, UnityEngine.MaterialPropertyBlock ___m_propertyBlock, WaterTool __instance, int ___ID_Color, int ___ID_Color2, Vector3 ___m_mousePosition)
		{

			
			foreach (KeyValuePair<int, DrainageArea> currentDrainageArea in DrainageAreaGrid.DrainageAreaDictionary) 
			{
				if (currentDrainageArea.Value.m_hidden == true) continue;
				bool logging = true;
				if (logging) Debug.Log("[RF]WaterToolRenderGeometry currentDrainageArea.Value.m_outputPosition.y = " + currentDrainageArea.Value.m_outputPosition.y);

                float num = Mathf.Sqrt(Mathf.Abs(currentDrainageArea.Value.m_outputRate)) * 0.4f + 10f;
				float num2 = (Mathf.Abs(currentDrainageArea.Value.m_outputRate) + 1f) * 0.015625f;
				Vector3 pos = currentDrainageArea.Value.m_outputPosition + new Vector3(0f, num2 * 0.5f, 0f);
				Matrix4x4 matrix = Matrix4x4.TRS(s: new Vector3(num * 2f, num2 * 0.5f, num * 2f), pos: pos, q: Quaternion.identity);
				___m_propertyBlock.Clear();
				if (currentDrainageArea.Value.m_enabled == true)
				{
					___m_propertyBlock.SetColor(___ID_Color2, new Color(0f, 0f, 1f, 1f));
				} else
                {
					___m_propertyBlock.SetColor(___ID_Color, new Color(0.7f, 0f, 0f, 1f));
				}
				Singleton<ToolManager>.instance.m_drawCallData.m_defaultCalls++;
				Graphics.DrawMesh(__instance.m_sourceMesh, matrix, __instance.m_sourceMaterial, 0, null, 0, ___m_propertyBlock, castShadows: false, receiveShadows: false);
			}
            FastList<WaterSource> waterSources = Singleton<TerrainManager>.instance.WaterSimulation.m_waterSources;
			for (int i = 0; i < waterSources.m_size; i++)
			{
				if (waterSources.m_buffer[i].m_type >= 2)
				{
                    
                    WaterSourceEntry.WaterSourceType currentWaterSourceType = WaterSourceManager.GetWaterSourceEntry(i + 1).GetWaterSourceType();

					if (WaterSourceManager.WaterSourceColors.ContainsKey(currentWaterSourceType))
					{
						WaterSource currentWaterSource = waterSources.m_buffer[i];
						float num = Mathf.Sqrt(Mathf.Abs(currentWaterSource.m_outputRate)) * 0.4f + 10f;
						float num2 = (Mathf.Abs(currentWaterSource.m_outputRate) + 1f) * 0.015625f;
						Vector3 pos = currentWaterSource.m_outputPosition + new Vector3(0f, num2 * 0.5f, 0f);
						Vector2 waterSourcePositionXZ = new Vector2(currentWaterSource.m_outputPosition.x, currentWaterSource.m_outputPosition.z);
						Vector2 mousePositionXZ = new Vector2(___m_mousePosition.x, ___m_mousePosition.z);
						Matrix4x4 matrix = Matrix4x4.TRS(s: new Vector3(num * 2f, num2 * 0.5f, num * 2f), pos: pos, q: Quaternion.identity);
						___m_propertyBlock.Clear();
						if (Vector2.Distance(waterSourcePositionXZ, mousePositionXZ) < 8f)
						{
							___m_propertyBlock.SetColor(___ID_Color2, new Color(1f, 1f, 1f, 1f));
						}
						else
						{
							___m_propertyBlock.SetColor(___ID_Color, WaterSourceManager.WaterSourceColors[currentWaterSourceType]);
                            ___m_propertyBlock.SetColor(___ID_Color2, WaterSourceManager.WaterSourceColors[currentWaterSourceType]);
                        }
						Singleton<ToolManager>.instance.m_drawCallData.m_defaultCalls++;
						Graphics.DrawMesh(__instance.m_sourceMesh, matrix, __instance.m_sourceMaterial, 0, null, 0, ___m_propertyBlock, castShadows: false, receiveShadows: false);
					}
                   
                }
			}
            
        }
		

	}

	[HarmonyPatch(typeof(WaterTool), "SecondaryDown")]
	class WaterToolSecondaryDown
	{
		static void Postfix(WaterTool.Mode ___m_mode, Vector3 ___m_mousePosition)
		{
			FastList<WaterSource> waterSources = Singleton<TerrainManager>.instance.WaterSimulation.m_waterSources;
			List<int> waterSourcesToRelease = new List<int>();
			for (int i = 0; i < waterSources.m_size; i++)
			{
				if (waterSources.m_buffer[i].m_type >= 2)
				{
					WaterSource currentWaterSource = waterSources.m_buffer[i];
					Vector2 waterSourcePositionXZ = new Vector2(currentWaterSource.m_outputPosition.x, currentWaterSource.m_outputPosition.z);
					Vector2 mousePositionXZ = new Vector2(___m_mousePosition.x, ___m_mousePosition.z);
					if (Vector2.Distance(waterSourcePositionXZ, mousePositionXZ) < 8f)
					{
						waterSourcesToRelease.Add(i + 1);
					}
				}
			}
			foreach (int i in waterSourcesToRelease)
			{
				Singleton<TerrainManager>.instance.WaterSimulation.ReleaseWaterSource((ushort)i);
			}
		}
	}
	/*
    [HarmonyPatch(typeof(WaterTool), "OnToolGUI")]
    class WaterToolOnToolGUI
    {
        static void Postfix(Event e)
        {
            Debug.Log("[RF]WaterTool.OnToolGUI Tool GUI!");
        }
    }*/
}

