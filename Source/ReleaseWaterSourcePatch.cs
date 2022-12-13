using HarmonyLib;
using UnityEngine;

namespace Rainfall
{
    [HarmonyPatch(typeof(WaterSimulation), "ReleaseWaterSource")]
    class WaterSimulationReleaseWaterSourcePatch
    {
       
        static bool Prefix(ushort source)
        {
            bool logging = false;
            if (logging) Debug.Log("[RF]WaterSimulationReleaseWaterSourcePatch.Prefix Hello!");
            if (WaterSourceManager.AreYouAwake())
            {
                WaterSourceManager.SetWaterSourceEntry((int)source, new WaterSourceEntry(WaterSourceEntry.WaterSourceType.Empty));
                if (logging) Debug.Log("[RF]WaterSimulationReleaseWaterSourcePatch.Prefix SetWaterSourceEntry " + source.ToString() + " to Empty.");
            }
            return true;
        }
        
    }
}
