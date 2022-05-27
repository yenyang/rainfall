using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace Rainfall
{
    class OptionResetButton : OptionsItemBase
    {
        public UIButton button;
        public List<OptionsItemBase> optionsToReset = new List<OptionsItemBase>();
        public override void Create(UIHelperBase helper)
        {
            button = helper.AddButton(uniqueName, OnButtonPressed) as UIButton;
        }
        private void OnButtonPressed()
        {
            if (optionsToReset.Count > 0)
            {
                foreach(OptionsItemBase currentOption in optionsToReset)
                {
                    if (currentOption is OptionsCheckbox)
                    {
                        OptionsCheckbox currentCheckBox = currentOption as OptionsCheckbox;
                        currentCheckBox.reset();
                    } else if (currentOption is OptionsDropdown)
                    {
                        OptionsDropdown currentDropDown = currentOption as OptionsDropdown;
                        currentDropDown.reset();
                    } else if (currentOption is OptionsSlider)
                    {
                        OptionsSlider currentSlider = currentOption as OptionsSlider;
                        currentSlider.Reset();
                    }
                }
            }
        }
    }
}
