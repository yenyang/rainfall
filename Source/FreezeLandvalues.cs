using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using System;
using UnityEngine;

namespace Rainfall
{
    public class FreezeLandvalues : LevelUpExtensionBase
    {
        public override void OnCreated(ILevelUp levelUp)
        {
            Debug.Log("[RF].LUEB LevelUpExtension Base Created.");
            base.OnCreated(levelUp);
        }

        public override void OnReleased()
        {
            Debug.Log("[RF].LUEB LevelUpExtension Base Released.");
            base.OnReleased();
        }

        public override ResidentialLevelUp OnCalculateResidentialLevelUp(ResidentialLevelUp levelUp, int averageEducation, int landValue, ushort buildingID, Service service, SubService subService, Level currentLevel)
        {
            if (Hydrology.instance.isRaining == true && Hydrology.instance._preRainfallLandvalues[buildingID] > 0 && landValue > Hydrology.instance._preRainfallLandvalues[buildingID] && ModSettings.FreezeLandvalues == true)
            {
                //Debug.Log("[RF].LUEB Residence " + buildingID.ToString() + " increased in Landvalue during a storm from " + Hydrology.instance._preRainfallLandvalues[buildingID].ToString() + " to " + landValue.ToString());
                landValue = Hydrology.instance._preRainfallLandvalues[buildingID];
                
            }
            return base.OnCalculateResidentialLevelUp(levelUp, averageEducation, landValue, buildingID, service, subService, currentLevel);
        }

        public override CommercialLevelUp OnCalculateCommercialLevelUp(CommercialLevelUp levelUp, int averageWealth, int landValue, ushort buildingID, Service service, SubService subService, Level currentLevel)
        {
            if (Hydrology.instance.isRaining == true && Hydrology.instance._preRainfallLandvalues[buildingID] > 0 && landValue > Hydrology.instance._preRainfallLandvalues[buildingID] && ModSettings.FreezeLandvalues == true)
            {
                //Debug.Log("[RF].LUEB Commercial Building " + buildingID.ToString() + " increased in Landvalue during a storm from " + Hydrology.instance._preRainfallLandvalues[buildingID].ToString() + " to " + landValue.ToString());
                landValue = Hydrology.instance._preRainfallLandvalues[buildingID];
            }
            return base.OnCalculateCommercialLevelUp(levelUp, averageWealth, landValue, buildingID, service, subService, currentLevel);
        }
    }
   
}
