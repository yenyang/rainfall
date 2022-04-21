using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICities;
using UnityEngine;
using ColossalFramework;

namespace Rainfall
{
    public class RFAreasExtension : IAreasExtension
    {
        public void OnCreated(IAreas areas)
        {

        }
        public void OnReleased()
        {

        }
        public bool OnCanUnlockArea(int x, int z, bool originalResult)
        {
            return originalResult;
        }
        public int OnGetAreaPrice(uint ore, uint oil, uint forest, uint fertility, uint water, bool road, bool train, bool ship, bool plane, float landFlatness, int originalPrice)
        {
            return originalPrice;
        }

        public void OnUnlockArea(int x, int z)
        {
            bool logging = false;
            int areaGridResolution = 5;
            if (Singleton<GameAreaManager>.instance.m_areaGrid.Length == 81)
            {
                areaGridResolution = 9;
            }
            int index = z * areaGridResolution + x;
            if (logging)
            {
                Debug.Log("[RF]RFAreasExtension.OnUnlockArea New Area #" + index.ToString() + " to Add to AreaGrid!");
            }
            DrainageBasinGrid.updateDrainageBasinGridForNewTile(logging);
        }
    }
}
