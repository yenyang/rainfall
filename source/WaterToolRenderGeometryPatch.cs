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
		static void Postfix(ref RenderManager.CameraInfo cameraInfo, UnityEngine.MaterialPropertyBlock ___m_propertyBlock, WaterTool __instance, int ___ID_Color, int ___ID_Color2)
		{

			
			foreach (KeyValuePair<int, DrainageArea> currentDrainageArea in DrainageAreaGrid.DrainageAreaDictionary) 
			{
				if (currentDrainageArea.Value.m_hidden == true) continue;
				bool logging = false;
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
					___m_propertyBlock.SetColor(___ID_Color, new Color(1f, 0f, 0f, 1f));
				}
				Singleton<ToolManager>.instance.m_drawCallData.m_defaultCalls++;
				Graphics.DrawMesh(__instance.m_sourceMesh, matrix, __instance.m_sourceMaterial, 0, null, 0, ___m_propertyBlock, castShadows: false, receiveShadows: false);
			}

		}

	}
}
