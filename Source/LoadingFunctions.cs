using ICities;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using Rainfall.Redirection;

namespace Rainfall
{
    public class LoadingFunctions : LoadingExtensionBase
    {
        private LoadMode _mode;

        private static Dictionary<MethodInfo, RedirectCallsState> _redirects;

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            
        }

        public override void OnLevelLoaded(LoadMode mode)
        {

            _mode = mode;
            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame)
                return;
            Hydrology.instance.loaded = true;
            Hydraulics.instance.loaded = true;
            Debug.Log("[RF] Level Loaded!");
            _redirects = RedirectionUtil.RedirectAssembly();
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
            base.OnReleased();
            RedirectionUtil.RevertRedirects(_redirects);
        }
    }
}

