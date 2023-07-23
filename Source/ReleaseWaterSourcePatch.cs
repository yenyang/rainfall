using HarmonyLib;
using UnityEngine;

namespace Rainfall
{
    [HarmonyPatch(typeof(WaterSimulation), "ReleaseWaterSource")]
    class WaterSimulationReleaseWaterSourcePatch
    {
       
        static bool Prefix(ushort source, ref WaterSimulation __instance)
        {
            bool logging = false;
            if (logging) Debug.Log("[RF]WaterSimulationReleaseWaterSourcePatch.Prefix Hello!");
            if (WaterSourceManager.AreYouAwake())
            {
                WaterSourceManager.SetWaterSourceEntry((int)source, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.Empty));
                if (logging) Debug.Log("[RF]WaterSimulationReleaseWaterSourcePatch.Prefix SetWaterSourceEntry " + source.ToString() + " to Empty.");
            }
            if (source > __instance.m_waterSources.m_size)
            {
                Debug.Log("[RF]WaterSimulationReleaseWaterSourcePatch.Prefix source = " + source.ToString() + " __instance.m_waterSources.m_size = " + __instance.m_waterSources.m_size.ToString());
                Debug.Log("[RF]WaterSimulationReleaseWaterSourcePatch.Prefix source > __instance.m_waterSources.m_size therefore skip releaseWaterSource ");
                return false;
            }
            return true;
        }
        
    }
}
