using ICities;
using UnityEngine;
using ColossalFramework.UI;
using System.Reflection;
using System;
using ColossalFramework;
using System.Text;

namespace Rainfall
{
    public class LoadingFunctions : LoadingExtensionBase
    {
        private LoadMode _mode;

        public override void OnLevelLoaded(LoadMode mode)
        {

            _mode = mode;
            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame)
                return;
            Hydrology.instance.loaded = true;
            Hydraulics.instance.loaded = true;
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

       
    }
}

