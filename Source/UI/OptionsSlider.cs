using ColossalFramework.UI;
using ICities;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Rainfall
{
    class OptionsSlider : OptionsItemBase
    {
        public float value
        {
            get { return (float)m_value;  }
            set { m_value = value; }
        }
        public float min = 0f;
        public float max = 4f;
        public float step = 0.1f;
        public UISlider slider;
        public string units = "";
        public float defaultValue = 0f;
        public float tooltipMultiplier = 1f;
        public string tooltipFormat = "F1";
        public float additionalSliderWidth = 100;
       

        public void Reset()
        {
            if (slider != null)
            {
                slider.value = defaultValue;
                SetValue(defaultValue);
            }
        }
        private void OnSliderChanged(float val)
        {
            SetValue(val);
            slider.tooltip = GetTooltip();
            slider.tooltipBox.Show();
            slider.RefreshTooltip();
        }
        private void SetValue(float val)
        {
            value = val;
            PlayerPrefs.SetFloat(OptionHandler.PlayerPrefPrefix + uniqueName, val);
            if (!OptionHandler.sliderValues.ContainsKey(uniqueName)) OptionHandler.sliderValues.Add(uniqueName, value);
            else OptionHandler.sliderValues[uniqueName] = value;
        }
        private void SetValueToPlayerPref()
        {
            value = PlayerPrefs.GetFloat(OptionHandler.PlayerPrefPrefix + uniqueName, defaultValue);
        }
        private string GetTooltip()
        {
            return (value * tooltipMultiplier).ToString(tooltipFormat) + units;
        }

        public override void Create(UIHelperBase helper)
        {
            SetValueToPlayerPref();
            slider = helper.AddSlider(readableName, min, max, step, value, OnSliderChanged) as UISlider;
            slider.enabled = true;
            slider.name = uniqueName;
            slider.width += additionalSliderWidth;
            slider.tooltip = GetTooltip();
            if (!OptionHandler.sliderValues.ContainsKey(uniqueName)) OptionHandler.sliderValues.Add(uniqueName, value);
            else OptionHandler.sliderValues[uniqueName] = value;
            
        }

    }
}
