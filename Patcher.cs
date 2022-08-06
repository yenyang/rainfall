using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace Rainfall
{
    class Patcher
    {
        private const string HarmonyId = "yenyang.Rainfall";
        private static bool patched = false;

        public static void PatchAll()
        {
            if (patched) return;

            patched = true;
            var harmony = new Harmony(HarmonyId);

            //harmony.PatchAll(typeof(Patcher).GetType().Assembly);
            harmony.PatchAll();
            //Debug.Log("[RF].Patcher.PatchAll getPatchedMethods = " + harmony.GetPatchedMethods().ToString());
               
        }

        public static void UnpatchAll()
        {
            if (!patched) return;

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            patched = false;
        }
    }
}
