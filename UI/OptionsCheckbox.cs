
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace Rainfall
{ 
    class OptionsCheckbox : OptionsItemBase
    {
        public bool value
        {
            get { return (bool)m_value; }
            set { m_value = value; }
        }
        public UICheckBox checkbox;
        private void onCheckBoxChanged(bool val)
        {
            setValue(val);
        }
        public bool defaultValue = false;
        public void reset()
        {
            if (checkbox != null)
            {
                checkbox.isChecked = defaultValue;
                setValue(defaultValue);
            }
        }
        
        private void setValue(bool val)
        {
            value = val;
            if (val) PlayerPrefs.SetInt(OptionHandler.PlayerPrefPrefix + uniqueName, 1);
            else PlayerPrefs.SetInt(OptionHandler.PlayerPrefPrefix + uniqueName, 0);
            if (!OptionHandler.checkboxValues.ContainsKey(uniqueName)) OptionHandler.checkboxValues.Add(uniqueName, value);
            else OptionHandler.checkboxValues[uniqueName] = value;
        }
        private void setValueToPlayerPref()
        {
            int defaultInt = 0;
            if (defaultValue) defaultInt = 1;
            value = false;
            if (PlayerPrefs.GetInt(OptionHandler.PlayerPrefPrefix + uniqueName, defaultInt) == 1) value = true;
        }
        public override void Create(UIHelperBase helper)
        {
            setValueToPlayerPref();
            checkbox = helper.AddCheckbox(readableName, value, onCheckBoxChanged) as UICheckBox;
            if (!OptionHandler.checkboxValues.ContainsKey(uniqueName)) OptionHandler.checkboxValues.Add(uniqueName, value);
            else OptionHandler.checkboxValues[uniqueName] = value;
        }
    }
}
