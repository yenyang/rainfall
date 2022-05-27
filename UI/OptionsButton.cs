using ColossalFramework.UI;
using ICities;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Rainfall
{
    class OptionsButton : OptionsItemBase
    {
        
        public UIButton button;
        public OnButtonClicked onButtonClicked;

        public override void Create(UIHelperBase helper)
        {
            button = helper.AddButton(readableName, onButtonClicked) as UIButton;
           
            
        }

    }
}
