
using ColossalFramework.UI;
using ICities;
using UnityEngine;
using System.Collections.Generic;

namespace Rainfall
{
    class OptionsDropdown : OptionsItemBase
    {
        public int value
        {
            get { return (int)m_value; }
            set { m_value = value; }
        }
        public UIDropDown dropDown;
        public List<string> options = new List<string>();
        public int defaultValue = 0;
        public void OnDropDownChanged(int sel)
        {
            setValue(sel);
        }

        public void reset()
        {
            if (dropDown != null)
            {
                dropDown.selectedIndex = defaultValue;
                setValue(defaultValue);
            }
        }

        private void setValue(int val)
        {
            PlayerPrefs.SetInt(OptionHandler.PlayerPrefPrefix + uniqueName, val);
            value = val;
            if (!OptionHandler.dropdownValues.ContainsKey(uniqueName)) OptionHandler.dropdownValues.Add(uniqueName, value);
            else OptionHandler.dropdownValues[uniqueName] = value;
        }
        private void setValueToPlayerPref()
        {
            value = PlayerPrefs.GetInt(OptionHandler.PlayerPrefPrefix + uniqueName, defaultValue);
        }
        public override void Create(UIHelperBase helper)
        {
            if (options.Count > 0)
            {
                setValueToPlayerPref();
                dropDown = helper.AddDropdown(readableName, options.ToArray(), value, OnDropDownChanged) as UIDropDown;
                if (!OptionHandler.dropdownValues.ContainsKey(uniqueName)) OptionHandler.dropdownValues.Add(uniqueName, value);
                else OptionHandler.dropdownValues[uniqueName] = value;
            }

        }
    }
}
