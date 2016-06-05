using ColossalFramework.UI;
using ICities;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Rainfall.UI
{
    class OptionsRunoffCoefficientSlider : OptionsItemBase
    {
        public float value
        {
            get { return (float)m_value;  }
            set { m_value = value; }
        }
        public float min = 0f;
        public float max = 1f;
        public float step = 1f;
        public UISlider slider;
        public override void Create(UIHelperBase helper)
        {
            slider = helper.AddSlider(readableName, min, max, step, value, IgnoredFunction) as UISlider;
            slider.enabled = enabled;
            slider.name = uniqueName;
            slider.tooltip = value.ToString();
            slider.eventValueChanged += new PropertyChangedEventHandler<float>(delegate (UIComponent component, float newValue)
            {
                value = newValue;
                slider.tooltip = value.ToString();
                slider.RefreshTooltip();
                ModSettings.setRunoffCoefficient(uniqueName, value);
                
            });
        }
    }
}
