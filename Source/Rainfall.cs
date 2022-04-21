
using ICities;
using System.Collections.Generic;
using ColossalFramework.UI;
using System.Reflection;
using UnityEngine;
using ColossalFramework;
using System;
using System.Runtime.Remoting;

namespace Rainfall
{
    public class RainfallMod : IUserMod
    {

        public string Name { get { return "Rainfall"; } }
        public string Description { get { return "Simulates runoff and Includes a Storm Drain AI to manage Storm Drain Assets. By [SSU]yenyang"; } }
        
      

        public void onEnabled()
        {
            CitiesHarmony.API.HarmonyHelper.EnsureHarmonyInstalled();
            
        }
        public void OnSettingsUI(UIHelperBase helper)
        {
            OptionHandler.SetUpOptions(helper);
        }




    }
    

}



    
 
