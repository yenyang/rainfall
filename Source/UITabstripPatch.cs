using HarmonyLib;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using System.Reflection;

namespace Rainfall
{
    [HarmonyPatch(typeof(UITabstrip), "OnDisable")]
    class UITabstripPatch
    {
       
        static bool Prefix(ref UITabstrip __instance, ref ColossalFramework.PoolList<UIRenderData> ___m_RenderData)
        {
			__instance.Invalidate();
			if (___m_RenderData != null)
			{
				___m_RenderData.Release();
				___m_RenderData = null;
			}
			if (UIView.HasFocus(__instance))
			{
				UIView.SetFocus(null);
			}
			

			MethodInfo OnIsEnabledChangedMethod = AccessTools.Method(typeof(UITabstrip), "OnIsEnabledChanged", null, null);
			OnIsEnabledChangedMethod.Invoke(__instance, new object[] {});
			return false;
		}
    }
}
