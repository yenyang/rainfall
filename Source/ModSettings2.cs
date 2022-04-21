using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rainfall
{
    internal static class ModSettings
    {
        public const string PlayerPrefPrefix = "RF2_";
        public static List<String>  checkboxes =    new List<string>();
        public static List<String>  dropdowns =    new List<string>();
        public static List<String>  sliders =      new List<string>();

        public static bool getCheckboxSetting(string uniqueName) {
            bool checkboxSetting = false;
            if (checkboxes.Contains(uniqueName) && PlayerPrefs.HasKey(PlayerPrefPrefix + uniqueName))
            {
                if (PlayerPrefs.GetInt(PlayerPrefPrefix + uniqueName) == 1)
                {
                    checkboxSetting = true;
                }

            } else
            {
                Debug.Log("[RF].Modsettings2 Could not find Checkbox " + uniqueName);
            }
            return checkboxSetting;
        }
        public static int getDropdownSetting(string uniqueName)
        {
            int dropDownSetting = 0;
            if (dropdowns.Contains(uniqueName) && PlayerPrefs.HasKey(PlayerPrefPrefix + uniqueName))
            {
                dropDownSetting = PlayerPrefs.GetInt(PlayerPrefPrefix + uniqueName);
            }
            else
            {
                Debug.Log("[RF].Modsettings2 Could not find dropDown " + uniqueName);
            }
            return dropDownSetting;
        }
        public static float getSliderSetting(string uniqueName)
        {
            float sliderSetting = 0f;
            if (sliders.Contains(uniqueName) && PlayerPrefs.HasKey(PlayerPrefPrefix + uniqueName))
            {
                sliderSetting = PlayerPrefs.GetFloat(PlayerPrefPrefix + uniqueName);
            }
            else
            {
                Debug.Log("[RF].Modsettings2 Could not find slider " + uniqueName);
            }
            return sliderSetting;
        }
    }
}
