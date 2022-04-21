using HarmonyLib;
using UnityEngine;

namespace Rainfall
{
    [HarmonyPatch(typeof(GameAreaManager), "UnlockArea")]
    class GameAreaManagerUnlockAreaPatch
    {
       
        static void Postfix(int index, ref bool __result)
        {
           if (__result)
            {
                bool logging = false;
                if (logging)
                {
                    Debug.Log("[RF]GameAreaManagerUnlockAreaPatch.UnlockArea New Area #" + index.ToString() + " to Add to AreaGrid!");
                }
                DrainageBasinGrid.updateDrainageBasinGridForNewTile(logging);
            }
        }
    }
}
