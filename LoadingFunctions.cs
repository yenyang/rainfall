using HarmonyLib;
using ICities;
using UnityEngine;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using ColossalFramework;





namespace Rainfall
{

    public class LoadingFunctions : LoadingExtensionBase
    {
        private LoadMode _mode;
        public static bool fineRoadAnarchyLoaded = false;
        public static bool loaded = false;
        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            if (CitiesHarmony.API.HarmonyHelper.IsHarmonyInstalled) Patcher.PatchAll();

        }

        public override void OnLevelLoaded(LoadMode mode)
        {

            _mode = mode;
            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame)
                return;
            Hydrology.instance.loaded = true;
            Hydraulics.instance.loaded = true;
            loaded = true;
            Debug.Log("[RF] Level Loaded!");
            base.OnLevelLoaded(mode);
        }



        public override void OnLevelUnloading()
        {
            if (_mode != LoadMode.LoadGame && _mode != LoadMode.NewGame)
                return;
            Hydrology.deinitialize();
            Hydraulics.deinitialize();
            Debug.Log("[RF] Level Unloaded!");
            base.OnLevelUnloading();
        }


        public override void OnReleased()
        {
            if (CitiesHarmony.API.HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
          
            base.OnReleased();
            
        }
        public void OnEnabled()
        {
            CitiesHarmony.API.HarmonyHelper.EnsureHarmonyInstalled();
        }


    }
   
}

